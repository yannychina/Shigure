namespace Shigure;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            MessageBox.Show(
                "Shigure 需要在 Windows 上运行。",
                "Shigure",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        ApplicationConfiguration.Initialize();

        var relaunchResult = RandomizedExecutableLauncher.TryRelaunch(args);
        if (relaunchResult == RandomizedRelaunchResult.Started)
        {
            return;
        }

        if (relaunchResult == RandomizedRelaunchResult.Failed)
        {
            return;
        }

        CleanupAuraRecognitionTempDirectory();

        Application.Run(new MainForm(AppOptions.FromArgs(args)));
    }

    private static void CleanupAuraRecognitionTempDirectory()
    {
        var directory = AuraIconRecognizer.DefaultTempAuraDirectory;
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch
        {
            CleanupAuraRecognitionTempDirectoryBestEffort(directory);
        }
    }

    private static void CleanupAuraRecognitionTempDirectoryBestEffort(string directory)
    {
        try
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                try { File.Delete(file); }
                catch { }
            }

            foreach (var childDirectory in Directory.EnumerateDirectories(directory, "*", SearchOption.AllDirectories)
                         .OrderByDescending(path => path.Length))
            {
                try { Directory.Delete(childDirectory, recursive: false); }
                catch { }
            }
        }
        catch
        {
            // 临时图标清理失败不应阻止程序启动。
        }
    }
}
