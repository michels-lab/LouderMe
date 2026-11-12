using System.Runtime.InteropServices;
using System.Text;

namespace LouderMeDesktop;

/// <summary>
/// Detects Windows package identity. In a Store MSIX package, updates belong to
/// Microsoft Store and must never launch the external Inno Setup installer.
/// </summary>
internal static class StorePackageContext
{
    private const int AppmodelErrorNoPackage = 15700;
    private const int ErrorInsufficientBuffer = 122;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetCurrentPackageFullName")]
    private static extern int GetCurrentPackageFullName(
        ref int packageFullNameLength, StringBuilder? packageFullName);

    internal static bool IsPackaged
    {
        get
        {
            if (!OperatingSystem.IsWindows()) return false;
            var length = 0;
            var result = GetCurrentPackageFullName(ref length, null);
            if (result == AppmodelErrorNoPackage) return false;
            if (result == ErrorInsufficientBuffer && length > 0) return true;
            throw new InvalidOperationException(
                $"Windows package identity probe returned unexpected Win32 error {result}.");
        }
    }

    internal static void RejectExternalInstaller()
    {
        if (IsPackaged)
            throw new InvalidOperationException(
                "Microsoft Store manages updates for this LouderMe installation. " +
                "The external Windows Setup installer cannot update the Store package.");
    }

    internal static void RunContractSelfTests()
    {
        // Existing Desktop CI uses ordinary unpackaged WinForms. The installed
        // Store MSIX must run this again after deployment to prove true detection.
        if (IsPackaged)
        {
            try { RejectExternalInstaller(); }
            catch (InvalidOperationException) { return; }
            throw new InvalidOperationException("Store install allowed the external updater.");
        }
        RejectExternalInstaller();
    }
}
