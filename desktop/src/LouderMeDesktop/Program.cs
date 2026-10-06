namespace LouderMeDesktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        Application.ThreadException += (_, eventArgs) =>
            WriteCrashLog(eventArgs.Exception);

        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception exception)
            {
                WriteCrashLog(exception);
            }
        };

        var startMinimized = args.Any(arg =>
            string.Equals(arg, "--startup", StringComparison.OrdinalIgnoreCase));

        Application.Run(new MainForm(startMinimized));
    }

    private static void WriteCrashLog(Exception exception)
    {
        try
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MichelsLab",
                "LouderMe");
            Directory.CreateDirectory(root);
            File.AppendAllText(
                Path.Combine(root, "crash.log"),
                $"[{DateTimeOffset.Now:O}] {exception}\n\n");
        }
        catch
        {
            // Crash logging must never hide the original exception.
        }
    }
}
