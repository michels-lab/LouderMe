namespace LouderMeDesktop;

internal static class BoostMath
{
    public static double PercentToDb(int percent)
    {
        var safe = Math.Clamp(percent, 100, 250);
        return 20d * Math.Log10(safe / 100d);
    }

    public static int DbToPercent(double db)
    {
        var percent = (int)Math.Round(Math.Pow(10d, db / 20d) * 100d);
        return Math.Clamp(percent, 100, 250);
    }
}
