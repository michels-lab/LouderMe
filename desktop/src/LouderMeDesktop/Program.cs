using System.Text;

namespace LouderMeDesktop;

internal static class Program
{
    private static string DiagnosticsDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Michel's Lab",
            "LouderMe");

    internal static string StartupCrashLogPath =>
        Path.Combine(DiagnosticsDirectory, "startup-crash.log");

    [STAThread]
    private static int Main(string[] args)
    {
        var layoutSmokeTest = args.Any(arg =>
            arg.Equals("--layout-smoke-test", StringComparison.OrdinalIgnoreCase));
        var smokeTest = layoutSmokeTest || args.Any(arg =>
            arg.Equals("--smoke-test", StringComparison.OrdinalIgnoreCase));

        try
        {
            using var mutex = new Mutex(true, @"Local\MichelsLab.LouderMeDesktop", out var firstInstance);
            if (!firstInstance)
            {
                if (!smokeTest)
                {
                    MessageBox.Show(
                        "LouderMe is already running.",
                        "LouderMe",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                return 0;
            }

            ApplicationConfiguration.Initialize();

            using var splash = new SplashForm();
            splash.Show();
            Application.DoEvents();

            using var main = new MainForm();

            var start = Environment.TickCount64;
            while (Environment.TickCount64 - start < 520)
            {
                Application.DoEvents();
                Thread.Sleep(16);
            }

            splash.Close();
            if (layoutSmokeTest)
            {
                var layoutResult = 0;
                main.Shown += (_, _) => main.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        main.AssertCompactLayout();
                    }
                    catch (Exception ex)
                    {
                        WriteStartupCrash(ex);
                        layoutResult = 1;
                    }
                    finally { main.Close(); }
                }));
                Application.Run(main);
                return layoutResult;
            }
            Application.Run(main);
            return 0;
        }
        catch (Exception ex)
        {
            WriteStartupCrash(ex);

            if (!smokeTest)
            {
                MessageBox.Show(
                    "LouderMe could not start. A diagnostic log was saved to:\n\n" +
                    StartupCrashLogPath + "\n\n" +
                    ex.GetType().Name + ": " + ex.Message,
                    "LouderMe startup error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return 1;
        }
    }

    private static void WriteStartupCrash(Exception exception)
    {
        try
        {
            Directory.CreateDirectory(DiagnosticsDirectory);
            var report = new StringBuilder()
                .AppendLine($"UTC: {DateTimeOffset.UtcNow:O}")
                .AppendLine($"Version: {Application.ProductVersion}")
                .AppendLine($"OS: {Environment.OSVersion}")
                .AppendLine($"64-bit process: {Environment.Is64BitProcess}")
                .AppendLine($"Base directory: {AppContext.BaseDirectory}")
                .AppendLine()
                .AppendLine(exception.ToString())
                .ToString();
            File.WriteAllText(StartupCrashLogPath, report);
        }
        catch
        {
            // Startup diagnostics must never hide the original exception.
        }
    }
}
