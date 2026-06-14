using System.Diagnostics;
using System.Security.Cryptography;

namespace Shigure;

internal static class RandomizedExecutableLauncher
{
    private const string RelaunchedEnvironmentKey = "SHIGURE_RANDOMIZED_PROCESS";
    private const string RandomSuffixMarker = "SHIGURE-RANDOM-HASH-MARKER";
    private const string TemporaryDirectoryName = "tmp";
    private const int RandomNameLength = 16;
    private const int RandomHashBytes = 64;
    private static readonly string[] RuntimeFileExtensions = [".dll", ".pdb"];
    private static readonly byte[] RandomSuffixMarkerBytes = System.Text.Encoding.UTF8.GetBytes(RandomSuffixMarker);
    private static readonly char[] FileNameChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public static bool TryRelaunch(string[] args)
    {
        if (Environment.GetEnvironmentVariable(RelaunchedEnvironmentKey) == "1")
        {
            return false;
        }

        var currentExecutable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExecutable) || !File.Exists(currentExecutable))
        {
            return false;
        }

        var extension = Path.GetExtension(currentExecutable);
        if (!string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            CleanupOldTemporaryDirectories(currentExecutable);
            var randomizedExecutable = CreateRandomizedExecutable(currentExecutable);
            var startInfo = new ProcessStartInfo(randomizedExecutable)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(randomizedExecutable) ?? Environment.CurrentDirectory
            };
            startInfo.Environment[RelaunchedEnvironmentKey] = "1";
            startInfo.Environment[AppPaths.OriginalBaseDirectoryEnvironmentKey] = Path.GetDirectoryName(currentExecutable) ?? AppContext.BaseDirectory;

            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            Process.Start(startInfo);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"无法创建随机运行文件: {ex.Message}",
                "Shigure",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }

    private static void CleanupOldTemporaryDirectories(string sourcePath)
    {
        var tempRoot = GetTemporaryRoot(sourcePath);
        if (!Directory.Exists(tempRoot))
        {
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(tempRoot))
        {
            if (!IsRandomizedDirectoryName(directory) || !ContainsRandomizedExecutable(directory))
            {
                continue;
            }

            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
                // A previous randomized run may still be active.
            }
            catch (UnauthorizedAccessException)
            {
                // Leave locked or protected files alone.
            }
        }
    }

    private static string CreateRandomizedExecutable(string sourcePath)
    {
        var sourceDirectory = Path.GetDirectoryName(sourcePath) ?? AppContext.BaseDirectory;
        var tempRoot = GetTemporaryRoot(sourcePath);
        Directory.CreateDirectory(tempRoot);

        string runDirectory;
        do
        {
            runDirectory = Path.Combine(tempRoot, CreateRandomFileName());
        }
        while (Directory.Exists(runDirectory));

        Directory.CreateDirectory(runDirectory);
        CopyRuntimeFiles(sourceDirectory, runDirectory);

        string targetPath;
        do
        {
            targetPath = Path.Combine(runDirectory, $"{CreateRandomFileName()}.exe");
        }
        while (File.Exists(targetPath));

        File.Copy(sourcePath, targetPath, overwrite: false);
        AppendRandomHashMarker(targetPath);
        return targetPath;
    }

    private static string GetTemporaryRoot(string sourcePath)
    {
        var directory = Path.GetDirectoryName(sourcePath) ?? AppContext.BaseDirectory;
        return Path.Combine(directory, TemporaryDirectoryName);
    }

    private static void CopyRuntimeFiles(string sourceDirectory, string targetDirectory)
    {
        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            if (!ShouldCopyRuntimeFile(file))
            {
                continue;
            }

            File.Copy(file, Path.Combine(targetDirectory, Path.GetFileName(file)), overwrite: true);
        }

    }

    private static bool ShouldCopyRuntimeFile(string path)
    {
        var fileName = Path.GetFileName(path);
        var extension = Path.GetExtension(path);
        return RuntimeFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
            || fileName.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".runtimeconfig.json", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateRandomFileName()
    {
        Span<char> buffer = stackalloc char[RandomNameLength];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = FileNameChars[RandomNumberGenerator.GetInt32(FileNameChars.Length)];
        }

        return new string(buffer);
    }

    private static bool IsRandomizedExecutableName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return name.Length == RandomNameLength
            && name.All(char.IsAsciiLetterOrDigit);
    }

    private static bool IsRandomizedDirectoryName(string path)
    {
        var name = Path.GetFileName(path);
        return name.Length == RandomNameLength
            && name.All(char.IsAsciiLetterOrDigit);
    }

    private static bool ContainsRandomizedExecutable(string directory)
    {
        try
        {
            return Directory.EnumerateFiles(directory, "*.exe", SearchOption.TopDirectoryOnly)
                .Any(file => IsRandomizedExecutableName(file) && HasRandomHashMarker(file));
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool HasRandomHashMarker(string path)
    {
        try
        {
            var markerLength = RandomSuffixMarkerBytes.Length;
            var minimumLength = markerLength + RandomHashBytes;
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (stream.Length < minimumLength)
            {
                return false;
            }

            stream.Position = stream.Length - minimumLength;
            Span<byte> markerBuffer = stackalloc byte[markerLength];
            return stream.Read(markerBuffer) == markerLength
                && markerBuffer.SequenceEqual(RandomSuffixMarkerBytes);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void AppendRandomHashMarker(string path)
    {
        Span<byte> randomBytes = stackalloc byte[RandomHashBytes];
        RandomNumberGenerator.Fill(randomBytes);

        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        using var writer = new BinaryWriter(stream);
        writer.Write(RandomSuffixMarker);
        writer.Write(randomBytes);
    }
}
