using Microsoft.Win32;

namespace LouderMeDesktop;

internal static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LouderMe";

    public static bool IsEnabled()
    {
        if (StorePackageContext.IsPackaged) return false; // MSIX cannot use unpackaged Run-key startup.
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(ValueName) is string value &&
               !string.IsNullOrWhiteSpace(value);
    }

    public static void SetEnabled(bool enabled)
    {
        if (StorePackageContext.IsPackaged)
            throw new NotSupportedException("Windows Store installs require Store-managed startup tasks.");
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true)
            ?? throw new InvalidOperationException("Windows startup registry key is unavailable.");

        if (enabled)
        {
            var exe = Environment.ProcessPath
                ?? throw new InvalidOperationException("Current LouderMe executable path is unavailable.");
            key.SetValue(ValueName, $"\"{exe}\" --startup", RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
