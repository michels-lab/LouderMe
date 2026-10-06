namespace LouderMeDesktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\MichelsLab.LouderMeDesktop", out var firstInstance);
        if (!firstInstance)
        {
            MessageBox.Show(
                "LouderMe is already running.",
                "LouderMe",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
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
        Application.Run(main);
    }
}
