using Microsoft.Win32;

namespace LouderMeDesktop;

internal static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LouderMe";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(ValueName) is string value &&
               !string.IsNullOrWhiteSpace(value);
    }

    public static void SetEnabled(bool enabled)
    {
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
