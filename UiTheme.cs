using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Shigure;

internal static class UiTheme
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaMicaEffect = 1029;
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;
    private const int DwmsbtMainWindow = 2;
    private const int DwmsbtTransientWindow = 3;
    private const int WcaAccentPolicy = 19;
    private const int AccentEnableBlurBehind = 3;
    private const int AccentEnableAcrylicBlurBehind = 4;

    public static readonly Color Background = Color.FromArgb(13, 15, 18);
    public static readonly Color Surface = Color.FromArgb(22, 25, 31);
    public static readonly Color SurfaceRaised = Color.FromArgb(27, 31, 38);
    public static readonly Color Field = Color.FromArgb(31, 35, 42);
    public static readonly Color Hover = Color.FromArgb(40, 45, 53);
    public static readonly Color Pressed = Color.FromArgb(49, 56, 66);
    public static readonly Color Border = Color.FromArgb(47, 54, 64);
    public static readonly Color RowAlt = Color.FromArgb(25, 29, 35);
    public static readonly Color Text = Color.FromArgb(225, 229, 235);
    public static readonly Color Muted = Color.FromArgb(128, 136, 148);
    public static readonly Color Accent = Color.FromArgb(86, 205, 192);
    public static readonly Color Danger = Color.FromArgb(235, 108, 108);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(nint hwnd, ref Margins margins);

    [DllImport("gdi32.dll")]
    private static extern nint CreateRoundRectRgn(int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(nint hwnd, ref WindowCompositionAttributeData data);

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int LeftWidth;
        public int RightWidth;
        public int TopHeight;
        public int BottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public nint Data;
        public int SizeOfData;
    }

    public static void ApplyDarkTitleBar(Form form)
    {
        var dark = 1;
        _ = DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int));
    }

    public static void ApplyRoundedCorners(Form form)
    {
        var preference = DwmwcpRound;
        if (DwmSetWindowAttribute(form.Handle, DwmwaWindowCornerPreference, ref preference, sizeof(int)) != 0)
        {
            // Windows 10 回退: 用 Region 裁剪圆角。
            form.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, form.Width + 1, form.Height + 1, 16, 16));
        }
    }

    public static void ApplyTranslucentBackground(Form form)
    {
        form.BackColor = Color.FromArgb(18, 21, 26);

        var margins = new Margins
        {
            LeftWidth = -1,
            RightWidth = -1,
            TopHeight = -1,
            BottomHeight = -1
        };
        _ = DwmExtendFrameIntoClientArea(form.Handle, ref margins);

        // Windows 11: transient backdrop is Acrylic-like and does not affect child control opacity.
        var backdrop = DwmsbtTransientWindow;
        var hr = DwmSetWindowAttribute(form.Handle, DwmwaSystemBackdropType, ref backdrop, sizeof(int));
        if (hr != 0)
        {
            backdrop = DwmsbtMainWindow;
            _ = DwmSetWindowAttribute(form.Handle, DwmwaSystemBackdropType, ref backdrop, sizeof(int));
        }

        // Windows 10/older fallback: apply Acrylic blur behind the form background only.
        if (!TryApplyAccentPolicy(form.Handle, AccentEnableAcrylicBlurBehind, Color.FromArgb(18, 21, 26), 10))
        {
            _ = TryApplyAccentPolicy(form.Handle, AccentEnableBlurBehind, Color.FromArgb(18, 21, 26), 150);
        }

        // Fallback for older Windows 11 builds where system backdrop exists but acrylic fails.
        var enable = 1;
        _ = DwmSetWindowAttribute(form.Handle, DwmwaMicaEffect, ref enable, sizeof(int));
    }

    private static bool TryApplyAccentPolicy(nint hwnd, int accentState, Color tint, byte alpha)
    {
        var policy = new AccentPolicy
        {
            AccentState = accentState,
            AccentFlags = 2,
            GradientColor = ToAbgr(tint, alpha),
            AnimationId = 0
        };

        var policySize = Marshal.SizeOf<AccentPolicy>();
        var policyPointer = Marshal.AllocHGlobal(policySize);
        try
        {
            Marshal.StructureToPtr(policy, policyPointer, false);
            var data = new WindowCompositionAttributeData
            {
                Attribute = WcaAccentPolicy,
                Data = policyPointer,
                SizeOfData = policySize
            };

            return SetWindowCompositionAttribute(hwnd, ref data) != 0;
        }
        finally
        {
            Marshal.FreeHGlobal(policyPointer);
        }
    }

    private static int ToAbgr(Color color, byte alpha)
    {
        return unchecked((int)(((uint)alpha << 24) | ((uint)color.B << 16) | ((uint)color.G << 8) | color.R));
    }

    public static Button CreateButton(string text, Color backColor, Color foreColor)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = foreColor,
            Padding = new Padding(10, 2, 10, 2),
            Margin = new Padding(6, 0, 0, 0),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            TabStop = false
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = backColor == Accent ? Accent : Border;
        button.FlatAppearance.MouseOverBackColor = backColor == Accent ? Color.FromArgb(103, 224, 211) : Hover;
        button.FlatAppearance.MouseDownBackColor = backColor == Accent ? Color.FromArgb(70, 181, 170) : Pressed;
        return button;
    }

    public static void StyleTextBox(TextBox textBox)
    {
        textBox.BackColor = Field;
        textBox.ForeColor = Text;
        textBox.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void StyleNumericUpDown(NumericUpDown numeric)
    {
        numeric.BackColor = Field;
        numeric.ForeColor = Text;
        numeric.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void StyleCheckedListBox(CheckedListBox listBox)
    {
        listBox.BackColor = Field;
        listBox.ForeColor = Text;
        listBox.BorderStyle = BorderStyle.FixedSingle;
        listBox.CheckOnClick = true;
        listBox.IntegralHeight = false;
        listBox.ItemHeight = 24;
    }

    public static void StyleComboBox(ComboBox comboBox)
    {
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.FlatStyle = FlatStyle.Flat;
        comboBox.BackColor = Field;
        comboBox.ForeColor = Text;
        comboBox.DrawMode = DrawMode.OwnerDrawFixed;
        comboBox.ItemHeight = 28;
        comboBox.DropDownHeight = 320;

        comboBox.DrawItem += (_, e) =>
        {
            if (e.Index < 0)
            {
                return;
            }

            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var isEditBox = (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;

            using (var background = new SolidBrush(isEditBox ? Field : isSelected ? Hover : Surface))
            {
                e.Graphics.FillRectangle(background, e.Bounds);
            }

            var textColor = !isEditBox && isSelected ? Accent : Text;
            var textBounds = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                comboBox.Items[e.Index]?.ToString(),
                comboBox.Font,
                textBounds,
                textColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        };
    }

    public static void StyleListBox(ListBox listBox, Font font)
    {
        listBox.BackColor = Surface;
        listBox.ForeColor = Text;
        listBox.BorderStyle = BorderStyle.None;
        listBox.DrawMode = DrawMode.OwnerDrawFixed;
        listBox.ItemHeight = 30;
        listBox.IntegralHeight = false;

        listBox.DrawItem += (_, e) =>
        {
            if (e.Index < 0)
            {
                return;
            }

            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var backgroundColor = selected
                ? Hover
                : e.Index % 2 == 0
                    ? listBox.BackColor
                    : RowAlt;
            using (var background = new SolidBrush(backgroundColor))
            {
                e.Graphics.FillRectangle(background, e.Bounds);
            }

            if (selected)
            {
                using var accent = new SolidBrush(Accent);
                e.Graphics.FillRectangle(accent, e.Bounds.Left, e.Bounds.Top + 5, 3, e.Bounds.Height - 10);
            }

            var textBounds = new Rectangle(e.Bounds.Left + 10, e.Bounds.Top, e.Bounds.Width - 14, e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                listBox.Items[e.Index]?.ToString() ?? string.Empty,
                font,
                textBounds,
                selected ? Text : Muted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        };
    }

    public static void StyleListView(ListView listView, Font font)
    {
        listView.Dock = DockStyle.Fill;
        listView.View = View.Details;
        listView.FullRowSelect = true;
        listView.GridLines = false;
        listView.HideSelection = false;
        listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        listView.BackColor = Surface;
        listView.ForeColor = Text;
        listView.BorderStyle = BorderStyle.None;
        listView.OwnerDraw = true;
        listView.ShowItemToolTips = true;
        listView.SmallImageList = new ImageList { ImageSize = new Size(1, 26) };
        typeof(Control)
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(listView, true);

        listView.DrawColumnHeader += (_, e) =>
        {
            using var brush = new SolidBrush(Field);
            e.Graphics.FillRectangle(brush, e.Bounds);
            using var border = new Pen(Border);
            e.Graphics.DrawLine(border, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            TextRenderer.DrawText(
                e.Graphics,
                e.Header?.Text ?? string.Empty,
                font,
                new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height),
                Muted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        };
        listView.DrawItem += (_, _) =>
        {
            // Sub-items draw the full row in Details view.
        };
        listView.DrawSubItem += (_, e) =>
        {
            if (e.Item is null)
            {
                return;
            }

            var selected = e.Item.Selected;
            var rowBack = selected
                ? Hover
                : e.Item.Index % 2 == 0
                    ? Surface
                    : RowAlt;
            using (var brush = new SolidBrush(rowBack))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            if (selected && e.ColumnIndex == 0)
            {
                using var accent = new SolidBrush(Accent);
                e.Graphics.FillRectangle(accent, e.Bounds.Left, e.Bounds.Top + 4, 3, e.Bounds.Height - 8);
            }

            var textBounds = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                e.SubItem?.Text ?? string.Empty,
                font,
                textBounds,
                selected ? Text : e.ColumnIndex == 0 ? Muted : Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        };
    }

    public static void StyleDataGridView(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.Margin = new Padding(0);
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Border;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 32;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Field;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Field;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Muted;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.DefaultCellStyle.BackColor = Surface;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = Hover;
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = RowAlt;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = Text;
        grid.RowTemplate.Height = 30;
        grid.RowHeadersVisible = false;
        grid.AllowUserToResizeRows = false;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
    }

    public static void StyleTabControl(TabControl tabs, int itemWidth = 132)
    {
        tabs.BackColor = Surface;
        tabs.ForeColor = Text;
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.ItemSize = new Size(itemWidth, 30);
        tabs.SizeMode = TabSizeMode.Fixed;
        tabs.Padding = new Point(12, 4);
        tabs.DrawItem += (_, e) =>
        {
            var isSelected = e.Index == tabs.SelectedIndex;
            var bounds = e.Bounds;
            bounds.Inflate(-1, -1);

            using var background = new SolidBrush(isSelected ? Field : Surface);
            e.Graphics.FillRectangle(background, bounds);

            if (isSelected)
            {
                using var accent = new SolidBrush(Accent);
                e.Graphics.FillRectangle(accent, bounds.Left + 8, bounds.Bottom - 3, bounds.Width - 16, 2);
            }

            TextRenderer.DrawText(
                e.Graphics,
                tabs.TabPages[e.Index].Text,
                tabs.Font,
                bounds,
                isSelected ? Text : Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
    }

    public static ListView CreateListView(Font font, params (string Text, int Width)[] columns)
    {
        var listView = new ListView();
        StyleListView(listView, font);

        foreach (var (text, width) in columns)
        {
            listView.Columns.Add(text, width);
        }

        // 最后一列拉伸填满, 避免表头右侧露出系统默认的白色区域。
        void StretchLastColumn()
        {
            if (listView.Columns.Count == 0)
            {
                return;
            }

            var othersWidth = 0;
            for (var i = 0; i < listView.Columns.Count - 1; i++)
            {
                othersWidth += listView.Columns[i].Width;
            }

            var lastWidth = listView.ClientSize.Width - othersWidth;
            if (lastWidth > 60)
            {
                listView.Columns[^1].Width = lastWidth;
            }
        }

        listView.Resize += (_, _) => StretchLastColumn();
        listView.HandleCreated += (_, _) => StretchLastColumn();

        return listView;
    }

    public static string FormatValue(object? value)
    {
        return value switch
        {
            null => "-",
            bool b => b ? "是" : "否",
            _ => value.ToString() ?? "-"
        };
    }
}
