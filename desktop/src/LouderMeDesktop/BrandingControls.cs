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

// A real visual seven-band mixer: responsive curve + individually interactive
// faders. Numeric fine-tuning remains available in an optional advanced panel.
// Values are UI commands only; MainForm keeps the original APO state machine.
internal sealed class EqCurveControl : Control
{
    private static readonly string[] Frequencies = ["60", "150", "400", "1k", "2.5k", "6k", "12k"];
    private float[] _gains = new float[7];
    private int _selected = 3;
    private bool _dragging;

    public event Action<int, float>? BandGainChanged;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float[] Gains
    {
        get => (float[])_gains.Clone();
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
        SetStyle(ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = BrandColors.Surface;
        ForeColor = BrandColors.Text;
        Height = 300;
        MinimumSize = new Size(280, 290);
        TabStop = true;
        AccessibleName = "Interactive seven-band equalizer";
        AccessibleDescription = "Select a frequency band and drag vertically, or use arrow keys to adjust by half a decibel";
        Cursor = Cursors.Hand;
    }

    private RectangleF Plot => new(
        38f, 48f, Math.Max(100, Width - 64f), Math.Max(110, Height - 122f));

    private float BandX(int index)
    {
        var plot = Plot;
        return plot.Left + index * plot.Width / 6f;
    }

    private float BandY(float db)
    {
        var plot = Plot;
        return plot.Top + (10f - Math.Clamp(db, -10f, 10f)) * plot.Height / 20f;
    }

    private float DbAt(float y)
    {
        var plot = Plot;
        var db = 10f - (y - plot.Top) / plot.Height * 20f;
        return Math.Clamp(MathF.Round(db * 2f) / 2f, -10f, 10f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var plot = Plot;

        using var background = new SolidBrush(BrandColors.Surface);
        g.FillRectangle(background, ClientRectangle);
        using var headline = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);
        using var small = new Font("Consolas", 8.5f, FontStyle.Bold);
        using var titleBrush = new SolidBrush(BrandColors.Text);
        using var mutedBrush = new SolidBrush(BrandColors.Muted);
        using var cyanBrush = new SolidBrush(BrandColors.Cyan);
        g.DrawString("FREQUENCY RESPONSE", headline, titleBrush, 8f, 3f);
        g.DrawString("DRAG BANDS TO SHAPE THE SOUND", small, mutedBrush,
            Math.Max(8f, Width - 264f), 28f);

        foreach (var db in new[] { 10, 5, 0, -5, -10 })
        {
            var y = BandY(db);
            using var grid = new Pen(db == 0 ? BrandColors.Line : Color.FromArgb(30, 48, 70), db == 0 ? 1.4f : 1f)
            {
                DashStyle = db == 0 ? DashStyle.Solid : DashStyle.Dot
            };
            g.DrawLine(grid, plot.Left, y, plot.Right, y);
            g.DrawString(db > 0 ? $"+{db}" : db.ToString(), small, mutedBrush, 1f, y - 9f);
        }

        using var path = new GraphicsPath();
        for (var i = 0; i < 7; i++)
        {
            var current = new PointF(BandX(i), BandY(_gains[i]));
            if (i == 0) path.StartFigure();
            if (i == 0) continue;
            var previous = new PointF(BandX(i - 1), BandY(_gains[i - 1]));
            var mid = (previous.X + current.X) / 2f;
            path.AddBezier(previous,
                new PointF(mid, previous.Y),
                new PointF(mid, current.Y), current);
        }

        // Gradient wash below the actual response curve.
        using var fillPath = (GraphicsPath)path.Clone();
        fillPath.AddLine(BandX(6), BandY(_gains[6]), plot.Right, plot.Bottom);
        fillPath.AddLine(plot.Right, plot.Bottom, plot.Left, plot.Bottom);
        fillPath.CloseFigure();
        using var wash = new LinearGradientBrush(
            plot, Color.FromArgb(95, BrandColors.Cyan),
            Color.FromArgb(4, BrandColors.Blue), LinearGradientMode.Vertical);
        g.FillPath(wash, fillPath);

        using var lineGradient = new LinearGradientBrush(
            plot, BrandColors.Cyan, BrandColors.Violet, LinearGradientMode.Horizontal);
        using var response = new Pen(lineGradient, 3.5f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawPath(response, path);

        for (var i = 0; i < 7; i++)
        {
            var x = BandX(i);
            var y = BandY(_gains[i]);
            var selected = i == _selected;
            using var guide = new Pen(Color.FromArgb(selected ? 100 : 53,
                BrandColors.Cyan), selected ? 2.6f : 1.2f);
            g.DrawLine(guide, x, plot.Top, x, plot.Bottom);
            using var outer = new SolidBrush(selected ? BrandColors.Gold : BrandColors.Cyan);
            using var core = new SolidBrush(BrandColors.Surface);
            var radius = selected ? 8.5f : 6f;
            g.FillEllipse(outer, x - radius, y - radius, radius * 2, radius * 2);
            g.FillEllipse(core, x - radius + 3f, y - radius + 3f,
                radius * 2 - 6f, radius * 2 - 6f);
            var valueText = _gains[i].ToString("+0.0;-0.0;0.0") + "dB";
            var vsize = g.MeasureString(valueText, small);
            using var valueColor = new SolidBrush(selected ? BrandColors.Gold : BrandColors.Text);
            g.DrawString(valueText, small, valueColor,
                Math.Clamp(x - vsize.Width / 2, 0f, Width - vsize.Width),
                plot.Bottom + 11f);
            var label = Frequencies[i];
            var lsize = g.MeasureString(label, small);
            g.DrawString(label, small, mutedBrush,
                Math.Clamp(x - lsize.Width / 2, 0f, Width - lsize.Width),
                plot.Bottom + 36f);
        }
        g.DrawString("Hz", small, mutedBrush, Math.Max(0, Width - 18f), Height - 16f);
    }

    private void AdjustAt(Point point)
    {
        if (!Enabled) return;
        var index = Enumerable.Range(0, 7)
            .MinBy(i => Math.Abs(BandX(i) - point.X));
        _selected = index;
        var value = DbAt(point.Y);
        if (Math.Abs(_gains[index] - value) >= 0.01f)
        {
            _gains[index] = value;
            BandGainChanged?.Invoke(index, value);
        }
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !Enabled) return;
        Focus();
        _dragging = true;
        Capture = true;
        AdjustAt(e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging) AdjustAt(e.Location);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
        Capture = false;
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData.HasFlag(Keys.Up) || keyData.HasFlag(Keys.Down) ||
        keyData.HasFlag(Keys.Left) || keyData.HasFlag(Keys.Right) ||
        base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!Enabled) return;
        if (e.KeyCode == Keys.Left) _selected = Math.Max(0, _selected - 1);
        else if (e.KeyCode == Keys.Right) _selected = Math.Min(6, _selected + 1);
        else if (e.KeyCode is Keys.Up or Keys.Down)
        {
            var db = Math.Clamp(_gains[_selected] +
                (e.KeyCode == Keys.Up ? 0.5f : -0.5f), -10f, 10f);
            _gains[_selected] = db;
            BandGainChanged?.Invoke(_selected, db);
        }
        else return;
        e.Handled = true;
        Invalidate();
    }

    internal void AssertMixerContract()
    {
        if (!Visible || Height < 280 || Width < 275 || _gains.Length != 7)
            throw new InvalidOperationException("Visual seven-band equalizer collapsed");
        var points = Enumerable.Range(0, 7).Select(BandX).ToArray();
        if (points.Zip(points.Skip(1), (a, b) => b - a).Any(d => d < 28f) ||
            !(BandY(10f) < BandY(0f) && BandY(0f) < BandY(-10f)))
            throw new InvalidOperationException("EQ band geometry is not usable");
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
