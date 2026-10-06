using System.Globalization;
using System.Text;

namespace LouderMeDesktop;

internal static class EqualizerApoConfigBuilder
{
    public const string IncludeMarker = "# LouderMe managed include";
    public const string IncludeLine = "Include: LouderMe.txt";

    public static string Build(DesktopAudioSettings settings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# LouderMe managed configuration");
        sb.AppendLine("# Generated automatically. Manual changes may be overwritten.");
        sb.AppendLine("Device: all");
        sb.AppendLine("Stage: post-mix");
        sb.AppendLine("Channel: all");

        var gainDb = settings.BoostEnabled
            ? BoostMath.PercentToDb(settings.BoostPercent)
            : 0d;

        sb.AppendLine(
            $"Preamp: {gainDb.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture)} dB");

        var gains = settings.EqualizerEnabled && settings.EqualizerGainsDb.Length == 7
            ? settings.EqualizerGainsDb
            : new float[7];

        sb.Append("GraphicEQ: ");
        for (var i = 0; i < EqualizerPresets.FrequenciesHz.Length; i++)
        {
            if (i > 0) sb.Append("; ");
            sb.Append(EqualizerPresets.FrequenciesHz[i]);
            sb.Append(' ');
            sb.Append(Math.Clamp(gains[i], -10f, 10f)
                .ToString("0.0", CultureInfo.InvariantCulture));
        }
        sb.AppendLine();

        return sb.ToString();
    }

    public static string EnsureInclude(string existingConfig)
    {
        if (existingConfig.Contains(IncludeLine, StringComparison.OrdinalIgnoreCase))
            return existingConfig;

        var prefix = existingConfig.EndsWith(Environment.NewLine, StringComparison.Ordinal)
            ? existingConfig
            : existingConfig + Environment.NewLine;

        return prefix
            + Environment.NewLine
            + IncludeMarker
            + Environment.NewLine
            + IncludeLine
            + Environment.NewLine;
    }
}
