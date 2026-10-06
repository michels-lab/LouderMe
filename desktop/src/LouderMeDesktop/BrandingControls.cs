using System.Drawing.Drawing2D;

namespace LouderMeDesktop;

internal static class BrandColors
{
    public static readonly Color Bg = Color.FromArgb(6, 9, 16);
    public static readonly Color Canvas = Color.FromArgb(9, 14, 23);
    public static readonly Color Surface = Color.FromArgb(12, 20, 34);
    public static readonly Color Surface2 = Color.FromArgb(17, 28, 43);
    public static readonly Color Line = Color.FromArgb(35, 63, 92);
    public static readonly Color Text = Color.FromArgb(239, 246, 255);
    public static readonly Color Muted = Color.FromArgb(139, 161, 184);
    public static readonly Color Cyan = Color.FromArgb(48, 199, 255);
    public static readonly Color Blue = Color.FromArgb(71, 142, 255);
    public static readonly Color Gold = Color.FromArgb(242, 186, 73);
    public static readonly Color Violet = Color.FromArgb(170, 140, 255);
    public static readonly Color Red = Color.FromArgb(255, 112, 112);
}

internal sealed class WaveformMarkControl : Control
{
    public float Opacity { get; set; } = 1f;

    public WaveformMarkControl()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        MinimumSize = new Size(150, 60);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = ClientRectangle;
        if (bounds.Width < 8 || bounds.Height < 8) return;

        const float minX = 80f;
        const float maxX = 950f;
        const float minY = 280f;
        const float maxY = 760f;

        float MapX(float x) => 8f + (x - minX) / (maxX - minX) * (bounds.Width - 16f);
        float MapY(float y) => 8f + (y - minY) / (maxY - minY) * (bounds.Height - 16f);

        using var path = new GraphicsPath();
        path.StartFigure();
        path.AddBezier(
            new PointF(MapX(120), MapY(595)),
            new PointF(MapX(205), MapY(625)),
            new PointF(MapX(242), MapY(700)),
            new PointF(MapX(310), MapY(704)));
        path.AddBezier(
            new PointF(MapX(310), MapY(704)),
            new PointF(MapX(392), MapY(709)),
            new PointF(MapX(394), MapY(412)),
            new PointF(MapX(486), MapY(405)));
        path.AddBezier(
            new PointF(MapX(486), MapY(405)),
            new PointF(MapX(574), MapY(397)),
            new PointF(MapX(586), MapY(651)),
            new PointF(MapX(660), MapY(643)));
        path.AddBezier(
            new PointF(MapX(660), MapY(643)),
            new PointF(MapX(735), MapY(634)),
            new PointF(MapX(725), MapY(329)),
            new PointF(MapX(807), MapY(332)));
        path.AddBezier(
            new PointF(MapX(807), MapY(332)),
            new PointF(MapX(868), MapY(334)),
            new PointF(MapX(889), MapY(464)),
            new PointF(MapX(906), MapY(525)));

        using var gradient = new LinearGradientBrush(
            bounds,
            Color.FromArgb((int)(255 * Opacity), 18, 56, 184),
            Color.FromArgb((int)(255 * Opacity), 244, 200, 79),
            LinearGradientMode.Horizontal);
        var blend = new ColorBlend
        {
            Positions = [0f, .36f, .66f, .88f, 1f],
            Colors =
            [
                Color.FromArgb((int)(255 * Opacity), 18, 56, 184),
                Color.FromArgb((int)(255 * Opacity), 8, 213, 239),
                Color.FromArgb((int)(255 * Opacity), 14, 95, 224),
                Color.FromArgb((int)(255 * Opacity), 244, 200, 79),
                Color.FromArgb((int)(255 * Opacity), 12, 49, 84),
            ],
        };
        gradient.InterpolationColors = blend;

        using var pen = new Pen(gradient, Math.Max(5f, bounds.Height * 0.105f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };
        e.Graphics.DrawPath(pen, path);

        var radius = Math.Max(5f, bounds.Height * 0.085f);
        var dot = new RectangleF(MapX(881) - radius, MapY(605) - radius, radius * 2, radius * 2);
        e.Graphics.FillEllipse(gradient, dot);
    }
}

internal sealed class AudioPulseControl : Control
{
    private int _boostPercent = 100;

    public int BoostPercent
    {
        get => _boostPercent;
        set
        {
            _boostPercent = Math.Clamp(value, 100, 250);
            Invalidate();
        }
    }

    public AudioPulseControl()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Height = 56;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var amp = 0.18f + (BoostPercent - 100) / 150f * 0.30f;
        var center = Height / 2f;
        using var path = new GraphicsPath();
        var points = 42;

        PointF P(int i)
        {
            var x = i * (Width - 1f) / (points - 1);
            var envelope = MathF.Sin(i / (float)(points - 1) * MathF.PI);
            var wave = MathF.Sin(i * 0.78f) + .42f * MathF.Sin(i * 1.43f + .8f);
            var y = center + wave * center * amp * envelope;
            return new PointF(x, y);
        }

        var first = P(0);
        path.StartFigure();
        path.AddLine(first, first);
        var previous = first;
        for (var i = 1; i < points; i++)
        {
            var current = P(i);
            path.AddLine(previous, current);
            previous = current;
        }

        using var brush = new LinearGradientBrush(
            ClientRectangle,
            BrandColors.Blue,
            BrandColors.Gold,
            LinearGradientMode.Horizontal);
        using var pen = new Pen(brush, 3f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        e.Graphics.DrawPath(pen, path);
    }
}

internal sealed class EqCurveControl : Control
{
    private float[] _gains = new float[7];

    public float[] Gains
    {
        get => _gains;
        set
        {
            _gains = value?.Take(7).Concat(Enumerable.Repeat(0f, 7)).Take(7).ToArray()
                ?? new float[7];
            Invalidate();
        }
    }

    public EqCurveControl()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Height = 68;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (Width < 10) return;

        var center = Height / 2f;
        using var axis = new Pen(Color.FromArgb(90, BrandColors.Line), 1f);
        e.Graphics.DrawLine(axis, 0, center, Width, center);

        var points = _gains.Select((gain, index) => new PointF(
            index * (Width - 1f) / 6f,
            center - Math.Clamp(gain, -10f, 10f) / 10f * Height * .34f)).ToArray();

        using var path = new GraphicsPath();
        path.StartFigure();
        for (var i = 1; i < points.Length; i++)
        {
            var a = points[i - 1];
            var b = points[i];
            var mid = (a.X + b.X) / 2f;
            path.AddBezier(a, new PointF(mid, a.Y), new PointF(mid, b.Y), b);
        }

        using var brush = new LinearGradientBrush(
            ClientRectangle,
            BrandColors.Cyan,
            BrandColors.Violet,
            LinearGradientMode.Horizontal);
        using var pen = new Pen(brush, 3f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        e.Graphics.DrawPath(pen, path);

        foreach (var point in points)
        {
            e.Graphics.FillEllipse(brush, point.X - 4f, point.Y - 4f, 8f, 8f);
            using var inner = new SolidBrush(BrandColors.Surface);
            e.Graphics.FillEllipse(inner, point.X - 1.7f, point.Y - 1.7f, 3.4f, 3.4f);
        }
    }
}
