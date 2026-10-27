using System.Text;

namespace LouderMeDesktop;

internal static class Program
{
    private static string DiagnosticsDirectory => DeveloperMode.DataDirectory;

    internal static string StartupCrashLogPath =>
        Path.Combine(DiagnosticsDirectory, "startup-crash.log");

    [STAThread]
    private static int Main(string[] args)
    {
        var developerEdition = args.Any(arg => arg.Equals("--developer", StringComparison.OrdinalIgnoreCase));
        DeveloperMode.Configure(developerEdition);
        var developerContractTest = args.Any(arg => arg.Equals("--check-developer-contract", StringComparison.OrdinalIgnoreCase));
        var developerSmokeTest = args.Any(arg => arg.Equals("--developer-smoke-test", StringComparison.OrdinalIgnoreCase));
        if (developerSmokeTest) DeveloperMode.Configure(true);
        var layoutSmokeTest = args.Any(arg =>
            arg.Equals("--layout-smoke-test", StringComparison.OrdinalIgnoreCase));
        var visualArgument = Array.FindIndex(args, arg =>
            arg.Equals("--visual-evidence", StringComparison.OrdinalIgnoreCase));
        var visualEvidence = visualArgument >= 0;
        var updaterContractTest = args.Any(arg =>
            arg.Equals("--check-update-contract", StringComparison.OrdinalIgnoreCase));
        var apoContractTest = args.Any(arg =>
            arg.Equals("--check-apo-contract", StringComparison.OrdinalIgnoreCase));
        // Explicit one-time UAC setup, intentionally performed before the
        // normal single-instance mutex and without starting the audio UI.
        if (args.Length > 0 && args[0].Equals("--setup-apo", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                if (args.Length != 2)
                    throw new ArgumentException("A Windows account SID is required.");
                new EqualizerApoEngine().SetupForUser(args[1]);
                return 0;
            }
            catch (Exception ex)
            {
                WriteStartupCrash(ex);
                MessageBox.Show(ex.Message, "LouderMe audio setup",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
        var smokeTest = layoutSmokeTest || visualEvidence || updaterContractTest || apoContractTest || developerContractTest || developerSmokeTest || args.Any(arg =>
            arg.Equals("--smoke-test", StringComparison.OrdinalIgnoreCase));

        try
        {
            if (developerContractTest)
            {
                DeveloperAudit.RunContractSelfTests();
                return 0;
            }
            if (updaterContractTest)
            {
                DesktopUpdateService.RunContractSelfTests();
                return 0;
            }
            if (apoContractTest)
            {
                EqualizerApoEngine.RunContractSelfTests();
                return 0;
            }

            using var mutex = new Mutex(true, DeveloperMode.Enabled
                ? @"Local\MichelsLab.LouderMeDesktop.Developer"
                : @"Local\MichelsLab.LouderMeDesktop", out var firstInstance);
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

            var developerAuditExitCode = 0;
            using var main = new MainForm();
            if (DeveloperMode.Enabled)
            {
                main.Text = "LouderMe — Developer Edition";
                main.Shown += (_, _) => main.BeginInvoke((Action)(() =>
                {
                    var inspector = new DeveloperDiagnosticsForm(main);
                    inspector.Show(main);
                    if (developerSmokeTest)
                    {
                        try { inspector.AssertRuntimeAudit(); }
                        catch (Exception ex)
                        {
                            developerAuditExitCode = 1;
                            WriteStartupCrash(ex);
                        }
                        finally
                        {
                            inspector.Close();
                            main.Close();
                        }
                    }
                }));
            }

            var start = Environment.TickCount64;
            while (Environment.TickCount64 - start < 520)
            {
                Application.DoEvents();
                Thread.Sleep(16);
            }

            splash.Close();
            if (developerSmokeTest)
            {
                Application.Run(main);
                return developerAuditExitCode;
            }
            if (visualEvidence)
            {
                if (visualArgument + 1 >= args.Length ||
                    string.IsNullOrWhiteSpace(args[visualArgument + 1]))
                    throw new ArgumentException("--visual-evidence requires a destination directory.");

                var visualResult = 0;
                var destination = Path.GetFullPath(args[visualArgument + 1]);
                main.Shown += (_, _) => main.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        main.CaptureVisualEvidence(destination);
                    }
                    catch (Exception ex)
                    {
                        WriteStartupCrash(ex);
                        visualResult = 1;
                    }
                    finally { main.Close(); }
                }));
                Application.Run(main);
                return visualResult;
            }
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
        if (DeveloperMode.Enabled)
            DeveloperRuntimeEvidence.TryRecord("APP_STARTUP_EXCEPTION");
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
