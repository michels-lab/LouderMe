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
    private int _draggedBand = -1;

    public event Action<int, float>? BandEdited;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

    private RectangleF Plot => new(30f, 26f, Math.Max(70f, ClientSize.Width - 60f),
        Math.Max(80f, ClientSize.Height - 72f));

    public EqCurveControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = BrandColors.Surface;
        Height = 208;
        MinimumSize = new Size(190, 178);
        Cursor = Cursors.Cross;
        AccessibleName = "Interactive seven-band equalizer, minus ten to plus ten decibels";
    }

    private int NearestBand(float x)
    {
        var plot = Plot;
        return Math.Clamp((int)Math.Round((x - plot.Left) / plot.Width * 6d), 0, 6);
    }

    private void ChangeBand(Point pos)
    {
        if (_draggedBand < 0) return;
        var plot = Plot;
        var y = Math.Clamp((pos.Y - plot.Top) / plot.Height, 0, 1);
        var db = (float)(Math.Round((10d - y * 20d) * 2d) / 2d);
        if (Math.Abs(db - _gains[_draggedBand]) < 0.001f) return;
        _gains[_draggedBand] = db;
        BandEdited?.Invoke(_draggedBand, db);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        _draggedBand = NearestBand(e.X);
        Capture = true;
        ChangeBand(e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_draggedBand >= 0) ChangeBand(e.Location);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _draggedBand = -1;
        Capture = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        var p = Plot;
        if (p.Width < 50 || p.Height < 50) return;

        using var gridPen = new Pen(Color.FromArgb(53, 94, 126), 1f);
        using var zeroPen = new Pen(Color.FromArgb(105, 135, 164), 1.2f)
        { DashStyle = DashStyle.Dash };
        using var quiet = new SolidBrush(BrandColors.Muted);
        using var textFont = new Font("Segoe UI", 8.3f, FontStyle.Regular);
        using var valueFont = new Font("Consolas", 8.6f, FontStyle.Bold);
        const string units = "dB";
        var half = p.Top + p.Height / 2f;
        for (var i = 0; i <= 4; i++)
        {
            var y = p.Top + p.Height * i / 4f;
            g.DrawLine(i == 2 ? zeroPen : gridPen, p.Left, y, p.Right, y);
        }
        foreach (var (v, y) in new[] { ("+10", p.Top), ("0", half), ("-10", p.Bottom) })
            g.DrawString(v, textFont, quiet, 0, y - 7);
        g.DrawString(units, textFont, quiet, 2, 3);

        var points = new PointF[7];
        var wave = new GraphicsPath();
        using (wave)
        {
            for (var i = 0; i < 7; i++)
            {
                var x = p.Left + p.Width * i / 6f;
                var y = half - Math.Clamp(_gains[i], -10f, 10f) / 10f * p.Height / 2f;
                points[i] = new PointF(x, y);
                using var track = new Pen(Color.FromArgb(38, 59, 83), 4f);
                g.DrawLine(track, x, p.Top, x, p.Bottom);
                using var stem = new Pen(Color.FromArgb(42, 193, 241), 4f);
                g.DrawLine(stem, x, half, x, y);
            }
            wave.StartFigure();
            for (var i = 1; i < points.Length; i++)
            {
                var prev = points[i - 1];
                var curr = points[i];
                var mid = (prev.X + curr.X) / 2f;
                wave.AddBezier(prev, new PointF(mid, prev.Y), new PointF(mid, curr.Y), curr);
            }

            using var gradient = new LinearGradientBrush(p, BrandColors.Cyan,
                BrandColors.Violet, LinearGradientMode.Horizontal);
            using var curve = new Pen(gradient, 3.6f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawPath(curve, wave);

            var labels = new[] { "60", "150", "400", "1k", "2.5k", "6k", "12k" };
            for (var i = 0; i < 7; i++)
            {
                var pt = points[i];
                g.FillEllipse(Brushes.White, pt.X - 6, pt.Y - 6, 12, 12);
                g.FillEllipse(gradient, pt.X - 4, pt.Y - 4, 8, 8);
                var freq = g.MeasureString(labels[i], textFont);
                g.DrawString(labels[i], textFont, quiet,
                    Math.Clamp(pt.X - freq.Width / 2, 0, Width - freq.Width),
                    p.Bottom + 13);
                var gain = $"{_gains[i]:+0.#;-0.#;0}"; 
                var measured = g.MeasureString(gain, valueFont);
                g.DrawString(gain, valueFont, Brushes.White,
                    Math.Clamp(pt.X - measured.Width / 2, 0, Width - measured.Width),
                    Math.Max(0, pt.Y - 25));
            }
        }
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
