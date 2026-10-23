using System.ComponentModel;
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
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float Opacity { get; set; } = 1f;

    public WaveformMarkControl()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
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

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
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

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float[] Gains
    {
        get => _gains.ToArray();
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
        BackColor = BrandColors.Surface;
        MinimumSize = new Size(270, 164);
        Height = 182;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (Width < 120 || Height < 110) return;
        var left = 38f;
        var right = Width - 18f;
        var top = 20f;
        var bottom = Height - 26f;
        var middle = (top + bottom) / 2f;
        var amplitude = (bottom - top) / 20f;

        using var gridPen = new Pen(Color.FromArgb(70, BrandColors.Line), 1f);
        using var zeroPen = new Pen(Color.FromArgb(130, BrandColors.Muted), 1f)
            { DashStyle = DashStyle.Dash };
        using var axisFont = new Font("Consolas", 8f, FontStyle.Regular);
        using var labelBrush = new SolidBrush(BrandColors.Muted);
        foreach (var db in new[] { 10, 5, 0, -5, -10 })
        {
            var y = middle - db * amplitude;
            g.DrawLine(db == 0 ? zeroPen : gridPen, left, y, right, y);
            g.DrawString(db > 0 ? $"+{db}" : db.ToString(),
                axisFont, labelBrush, 2, y - 7);
        }

        var points = _gains.Select((gain, i) => new PointF(
            left + i * (right - left) / 6f,
            middle - Math.Clamp(gain, -10f, 10f) * amplitude)).ToArray();
        using var curve = new GraphicsPath();
        curve.StartFigure();
        for (var i = 1; i < points.Length; i++)
        {
            var previous = points[i - 1];
            var current = points[i];
            var midpoint = (previous.X + current.X) / 2f;
            curve.AddBezier(previous,
                new PointF(midpoint, previous.Y),
                new PointF(midpoint, current.Y), current);
        }
        using var fill = (GraphicsPath)curve.Clone();
        fill.AddLine(points[^1], new PointF(right, bottom));
        fill.AddLine(right, bottom, left, bottom);
        fill.CloseFigure();
        using var shade = new LinearGradientBrush(
            new RectangleF(left, top, Math.Max(1f, right - left),
                Math.Max(1f, bottom - top)),
            Color.FromArgb(66, BrandColors.Cyan),
            Color.FromArgb(8, BrandColors.Violet),
            LinearGradientMode.Vertical);
        g.FillPath(shade, fill);

        using var glow = new Pen(Color.FromArgb(60, BrandColors.Cyan), 9f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var strokeBrush = new LinearGradientBrush(
            new RectangleF(left, top, Math.Max(1f, right - left),
                Math.Max(1f, bottom - top)),
            BrandColors.Cyan, BrandColors.Violet,
            LinearGradientMode.Horizontal);
        using var stroke = new Pen(strokeBrush, 3.4f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawPath(glow, curve);
        g.DrawPath(stroke, curve);
        foreach (var point in points)
        {
            using var dot = new SolidBrush(BrandColors.Surface);
            g.FillEllipse(dot, point.X - 5f, point.Y - 5f, 10f, 10f);
            g.DrawEllipse(stroke, point.X - 5f, point.Y - 5f, 10f, 10f);
        }
    }
}

/// <summary>
/// Real interactive seven-band EQ fader, synchronized with the exact decimal
/// control and underlying APO settings. Pointer, keyboard and reset supported.
/// All changes remain disabled when the backend is not installed.
/// </summary>
internal sealed class EqBandFaderControl : Control
{
    private float _gainDb;
    private bool _dragging;
    public int FrequencyHz { get; set; }
    public event Action<float>? GainEdited;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float GainDb
    {
        get => _gainDb;
        set
        {
            _gainDb = Math.Clamp(value, -10f, 10f);
            Invalidate();
        }
    }

    public EqBandFaderControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        MinimumSize = new Size(41, 150);
        Height = 188;
        BackColor = BrandColors.Surface;
    }

    private float TopTrack => 34f;
    private float BottomTrack => Math.Max(TopTrack + 30f, Height - 39f);
    private float ValueY => TopTrack + (10f - GainDb) / 20f *
        (BottomTrack - TopTrack);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width < 35 || Height < 90) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var center = Width / 2f;
        using var mutedFont = new Font("Consolas", 8f, FontStyle.Bold);
        using var valueFont = new Font("Consolas", 9f, FontStyle.Bold);
        var freq = FrequencyHz >= 1000
            ? $"{FrequencyHz / 1000f:0.#}k" : FrequencyHz.ToString();
        using var text = new SolidBrush(Enabled ? BrandColors.Text : BrandColors.Muted);
        using var accent = new SolidBrush(Enabled ? BrandColors.Cyan : BrandColors.Muted);
        var valueLabel = $"{GainDb:+0.#;-0.#;0} dB";
        var size = g.MeasureString(valueLabel, valueFont);
        g.DrawString(valueLabel, valueFont, accent,
            Math.Max(0, center - size.Width / 2), 5);
        var h = BottomTrack - TopTrack;
        using var rail = new Pen(BrandColors.Line, 8f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(rail, center, TopTrack, center, BottomTrack);
        using var zero = new Pen(Color.FromArgb(120, BrandColors.Muted), 1f);
        var zeroY = (TopTrack + BottomTrack) / 2f;
        g.DrawLine(zero, center - 10, zeroY, center + 10, zeroY);
        using var active = new LinearGradientBrush(
            new RectangleF(center - 4, TopTrack, 8, Math.Max(1f, h)),
            Enabled ? BrandColors.Cyan : BrandColors.Muted,
            Enabled ? BrandColors.Violet : BrandColors.Line,
            LinearGradientMode.Vertical);
        using var live = new Pen(active, 6f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(live, center, zeroY, center, ValueY);
        using var thumb = new SolidBrush(Enabled ? BrandColors.Text : BrandColors.Muted);
        g.FillEllipse(thumb, center - 10, ValueY - 7, 20, 14);
        using var ring = new Pen(Enabled ? BrandColors.Cyan : BrandColors.Line, 2f);
        g.DrawEllipse(ring, center - 10, ValueY - 7, 20, 14);
        var fs = g.MeasureString(freq, mutedFont);
        g.DrawString(freq, mutedFont, text, center - fs.Width / 2f, Height - 24);
        if (Focused && Enabled)
        {
            using var focus = new Pen(BrandColors.Gold, 1f)
                { DashStyle = DashStyle.Dot };
            g.DrawRectangle(focus, 1, 1, Width - 3, Height - 3);
        }
    }

    private void EditAt(float y)
    {
        if (!Enabled) return;
        var ratio = Math.Clamp((y - TopTrack) /
            (BottomTrack - TopTrack), 0f, 1f);
        var next = Math.Clamp(MathF.Round((10f - ratio * 20f) * 2f) / 2f,
            -10f, 10f);
        if (MathF.Abs(next - GainDb) < .001f) return;
        GainDb = next;
        GainEdited?.Invoke(next);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Enabled || e.Button != MouseButtons.Left) return;
        Focus();
        _dragging = true;
        Capture = true;
        EditAt(e.Y);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging) EditAt(e.Y);
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;
        if (_dragging) EditAt(e.Y);
        _dragging = false;
        Capture = false;
    }
    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (Enabled && e.Button == MouseButtons.Left)
        {
            GainDb = 0f;
            GainEdited?.Invoke(0f);
        }
    }
    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Up or Keys.Down or Keys.Home || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!Enabled) return;
        float? next = e.KeyCode switch
        {
            Keys.Up => Math.Clamp(GainDb + 0.5f, -10f, 10f),
            Keys.Down => Math.Clamp(GainDb - 0.5f, -10f, 10f),
            Keys.Home => 0f,
            _ => null,
        };
        if (next is null) return;
        GainDb = next.Value;
        GainEdited?.Invoke(next.Value);
        e.Handled = true;
        e.SuppressKeyPress = true;
    }
}

internal enum SocialNetwork
{
    Instagram,
    Facebook,
    LinkedIn,
    GitHub,
    Email,
}

internal sealed class SocialGlyphControl : Control
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SocialNetwork Network { get; set; }

    public SocialGlyphControl()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Size = new Size(34, 34);
        MinimumSize = new Size(34, 34);
        MaximumSize = new Size(34, 34);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var border = new Pen(BrandColors.Line, 1f);
        using var background = new SolidBrush(BrandColors.Surface2);
        using var white = new SolidBrush(BrandColors.Text);
        using var whitePen = new Pen(BrandColors.Text, 2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };

        e.Graphics.FillRoundedRectangle(background, new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 8f);
        e.Graphics.DrawRoundedRectangle(border, new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 8f);

        switch (Network)
        {
            case SocialNetwork.Instagram:
            {
                var rect = new RectangleF(8f, 8f, 18f, 18f);
                e.Graphics.DrawRoundedRectangle(whitePen, rect, 5f);
                e.Graphics.DrawEllipse(whitePen, 12.5f, 12.5f, 9f, 9f);
                e.Graphics.FillEllipse(white, 22f, 10f, 2.7f, 2.7f);
                break;
            }

            case SocialNetwork.Facebook:
                DrawCenteredText(e.Graphics, "f", 20f);
                break;

            case SocialNetwork.LinkedIn:
                DrawCenteredText(e.Graphics, "in", 12f);
                break;

            case SocialNetwork.GitHub:
            {
                e.Graphics.FillEllipse(white, 9f, 9f, 16f, 16f);
                using var ears = new GraphicsPath();
                ears.AddPolygon(
                [
                    new PointF(10f, 12f),
                    new PointF(11f, 6f),
                    new PointF(15f, 10f),
                ]);
                ears.AddPolygon(
                [
                    new PointF(24f, 12f),
                    new PointF(23f, 6f),
                    new PointF(19f, 10f),
                ]);
                e.Graphics.FillPath(white, ears);
                e.Graphics.FillRoundedRectangle(white, new RectangleF(13f, 22f, 8f, 7f), 3f);
                break;
            }

            case SocialNetwork.Email:
            {
                var rect = new RectangleF(7f, 10f, 20f, 14f);
                e.Graphics.DrawRoundedRectangle(whitePen, rect, 3f);
                e.Graphics.DrawLine(whitePen, 8f, 11f, 17f, 18f);
                e.Graphics.DrawLine(whitePen, 26f, 11f, 17f, 18f);
                break;
            }
        }
    }

    private void DrawCenteredText(Graphics graphics, string text, float size)
    {
        using var font = new Font("Segoe UI Semibold", size, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(BrandColors.Text);
        var measured = graphics.MeasureString(text, font);
        graphics.DrawString(
            text,
            font,
            brush,
            (Width - measured.Width) / 2f,
            (Height - measured.Height) / 2f - 1f);
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(
        this Graphics graphics,
        Brush brush,
        RectangleF bounds,
        float radius)
    {
        using var path = Rounded(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(
        this Graphics graphics,
        Pen pen,
        RectangleF bounds,
        float radius)
    {
        using var path = Rounded(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath Rounded(RectangleF bounds, float radius)
    {
        var diameter = radius * 2f;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
