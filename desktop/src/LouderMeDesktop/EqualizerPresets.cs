namespace LouderMeDesktop;

internal static class EqualizerPresets
{
    public static readonly int[] FrequenciesHz = [60, 150, 400, 1000, 2500, 6000, 12000];

    private static readonly Dictionary<string, float[]> Presets =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Flat"] = [0f, 0f, 0f, 0f, 0f, 0f, 0f],
            ["Bass"] = [6f, 5f, 3f, 0f, -1f, -2f, -2f],
            ["Deep Bass"] = [8f, 6f, 3f, 0f, -2f, -3f, -3f],
            ["Dialogue"] = [-3f, -2f, 0f, 3f, 4f, 2f, 0f],
            ["Treble"] = [-2f, -1f, 0f, 1f, 3f, 5f, 6f],
            ["Speaker"] = [2f, 1f, 0f, 2f, 3f, 2f, 1f],
            ["Headphones"] = [1f, 1f, 0f, 0f, 1f, 2f, 2f],
        };

    public static IReadOnlyList<string> Names => Presets.Keys.Concat(["Custom"]).ToArray();

    public static float[] For(string name) =>
        Presets.TryGetValue(name, out var values)
            ? values.ToArray()
            : Presets["Flat"].ToArray();
}
