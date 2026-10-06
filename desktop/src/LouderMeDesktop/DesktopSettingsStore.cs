using Microsoft.Win32;

namespace LouderMeDesktop;

internal static class DesktopSettingsStore
{
    private const string KeyPath = @"Software\Michel's Lab\LouderMe";

    public static int ReadBoostTarget()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: false);
        var raw = key?.GetValue("BoostTargetPercent");
        return raw is int value ? Math.Clamp(value, 100, 250) : 100;
    }

    public static void WriteBoostTarget(int percent)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true)
            ?? throw new InvalidOperationException("Unable to open LouderMe settings.");

        key.SetValue("BoostTargetPercent", Math.Clamp(percent, 100, 250), RegistryValueKind.DWord);
    }
}
