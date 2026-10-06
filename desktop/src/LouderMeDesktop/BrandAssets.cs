using System.Reflection;

namespace LouderMeDesktop;

internal static class BrandAssets
{
    public static Bitmap LoadBitmap(string logicalName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException($"Embedded brand asset not found: {logicalName}");
        return new Bitmap(stream);
    }
}
