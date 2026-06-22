using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Shigure;

public sealed record ScreenScanResult(IReadOnlyDictionary<int, int>? RowData, IReadOnlyDictionary<int, int> BarData);

public sealed class PixelScanner
{
    private const int TopRowBlockCount = 510;
    private const int TopRowFirstSchemeMax = 255;
    private readonly string _windowTitle;

    public PixelScanner(string windowTitle)
    {
        _windowTitle = windowTitle;
        try
        {
            NativeMethods.SetProcessDPIAware();
        }
        catch
        {
            // DPI awareness is best effort.
        }
    }

    public ScreenScanResult ScanScreenData()
    {
        var hwnd = NativeMethods.FindWindow(null, _windowTitle);
        if (hwnd == 0 || NativeMethods.IsIconic(hwnd))
        {
            return new ScreenScanResult(null, new Dictionary<int, int>());
        }

        var point = new NativeMethods.Point(0, 0);
        if (!NativeMethods.ClientToScreen(hwnd, ref point) || !NativeMethods.GetClientRect(hwnd, out var rect))
        {
            return new ScreenScanResult(null, new Dictionary<int, int>());
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            return new ScreenScanResult(null, new Dictionary<int, int>());
        }

        try
        {
            var rowData = ScanTopRow(point.X, point.Y, width);
            var barData = ScanLeftMarkerRow(point.X, point.Y, width, height);
            return new ScreenScanResult(rowData.Count == 0 ? null : rowData, barData);
        }
        catch
        {
            return new ScreenScanResult(null, new Dictionary<int, int>());
        }
    }

    private static Dictionary<int, int> ScanTopRow(int baseX, int baseY, int width)
    {
        var rowData = new Dictionary<int, int>();
        using var top = Capture(baseX, baseY, width, 1);
        var pixels = ReadPixels(top);

        var startX = -1;
        for (var x = 0; x < Math.Min(TopRowBlockCount, width); x++)
        {
            var color = Color.FromArgb(pixels[x]);
            if (TryDecodeTopRowBlock(color, out var step, out _) && step == 1)
            {
                startX = x;
                break;
            }
        }

        if (startX < 0)
        {
            return rowData;
        }

        for (var x = startX; x < width; x++)
        {
            var color = Color.FromArgb(pixels[x]);
            if (TryDecodeTopRowBlock(color, out var step, out var value))
            {
                rowData[step] = value;
                if (step == TopRowBlockCount)
                {
                    break;
                }
            }
        }

        return rowData;
    }

    private static Dictionary<int, int> ScanLeftMarkerRow(int baseX, int baseY, int width, int height)
    {
        var barData = new Dictionary<int, int>();
        using var left = Capture(baseX, baseY, 1, height);
        var leftPixels = ReadPixels(left);
        int? markerY = null;
        for (var y = 0; y < height; y++)
        {
            if (IsRedMarker(Color.FromArgb(leftPixels[y])))
            {
                markerY = y;
                break;
            }
        }

        if (markerY is null)
        {
            return barData;
        }

        using var markerRow = Capture(baseX, baseY + markerY.Value, width, 1);
        var rowPixels = ReadPixels(markerRow);
        var segIndex = 0;
        var x = 0;
        var pendingRed = false;

        while (x < width)
        {
            var color = Color.FromArgb(rowPixels[x]);
            if (IsGrayEndMarker(color))
            {
                break;
            }

            if (pendingRed && IsRedGreenMarker(color))
            {
                pendingRed = false;
                segIndex++;
                var (value, nextX) = ConsumeValueFrom(rowPixels, x + 1, alreadySawWhite: false);
                barData[segIndex] = Math.Max(0, value - 1);
                x = nextX;
                continue;
            }

            if (IsRedMarker(color))
            {
                pendingRed = true;
                x++;
                continue;
            }

            if (IsWhite(color))
            {
                var prevWhite = x > 0 && IsWhite(Color.FromArgb(rowPixels[x - 1]));
                if (!prevWhite)
                {
                    pendingRed = false;
                    segIndex++;
                    var (value, nextX) = ConsumeValueFrom(rowPixels, x + 1, alreadySawWhite: true);
                    barData[segIndex] = Math.Max(0, value - 1);
                    x = nextX;
                    continue;
                }
            }

            x++;
        }

        return barData;
    }

    private static (int Value, int NextX) ConsumeValueFrom(int[] row, int fromX, bool alreadySawWhite)
    {
        var sx = fromX;
        var needWhite = !alreadySawWhite;
        while (sx < row.Length)
        {
            var color = Color.FromArgb(row[sx]);
            if (IsGrayEndMarker(color))
            {
                return (0, row.Length);
            }

            if (IsRedMarker(color))
            {
                return (0, sx);
            }

            if (needWhite)
            {
                if (IsWhite(color))
                {
                    needWhite = false;
                }

                sx++;
                continue;
            }

            if (IsWhite(color))
            {
                sx++;
                continue;
            }

            return (color.G, sx + 1);
        }

        return (0, row.Length);
    }

    private static Bitmap Capture(int x, int y, int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    // 32bpp 位图 stride 恒为 width*4(无填充), 一次 LockBits + Marshal.Copy 读完整张为 0xAARRGGBB,
    // 取代逐像素 GetPixel(每次都会 Lock/UnlockBits)。Color.FromArgb 还原后 R/G/B 与原先一致。
    private static int[] ReadPixels(Bitmap bitmap)
    {
        var data = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            var pixels = new int[bitmap.Width * bitmap.Height];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            return pixels;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static bool IsRedMarker(Color color) => color.R == 1 && color.G == 0 && color.B == 0;
    private static bool IsRedGreenMarker(Color color) => color.R == 1 && color.G == 1 && color.B == 0;
    private static bool IsWhite(Color color) => color.R == 255 && color.G == 255 && color.B == 255;
    private static bool IsGrayEndMarker(Color color) => color.R == 200 && color.G == 200 && color.B == 200;

    private static bool TryDecodeTopRowBlock(Color color, out int step, out int value)
    {
        step = 0;
        value = 0;

        if (color.G is < 1 or > TopRowFirstSchemeMax)
        {
            return false;
        }

        step = color.R switch
        {
            0 => color.G,
            1 => TopRowFirstSchemeMax + color.G,
            _ => 0
        };

        if (step is < 1 or > TopRowBlockCount)
        {
            step = 0;
            return false;
        }

        value = color.B;
        return true;
    }
}
