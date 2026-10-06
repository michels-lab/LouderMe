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
        Application.Run(new MainForm());
    }
}
