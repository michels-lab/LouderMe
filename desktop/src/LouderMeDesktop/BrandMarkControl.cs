using System.Drawing.Drawing2D;

namespace LouderMeDesktop;

internal sealed class BrandMarkControl : Control
{
    public BrandMarkControl()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(9, 14, 23);
        MinimumSize = new Size(120, 70);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        const float sourceWidth = 1024f;
        const float sourceHeight = 1024f;
        var scale = Math.Min(Width / sourceWidth, Height / sourceHeight) * 1.15f;
        var offsetX = (Width - sourceWidth * scale) / 2f;
        var offsetY = (Height - sourceHeight * scale) / 2f;

        using var wavePath = new GraphicsPath();
        wavePath.StartFigure();
        wavePath.AddBezier(120, 595, 205, 625, 242, 700, 310, 704);
        wavePath.AddBezier(310, 704, 392, 709, 394, 412, 486, 405);
        wavePath.AddBezier(486, 405, 574, 397, 586, 651, 660, 643);
        wavePath.AddBezier(660, 643, 735, 634, 725, 329, 807, 332);
        wavePath.AddBezier(807, 332, 868, 334, 889, 464, 906, 525);

        using var transform = new Matrix();
        transform.Translate(offsetX, offsetY);
        transform.Scale(scale, scale);
        wavePath.Transform(transform);

        using var gradient = new LinearGradientBrush(
            new PointF(offsetX + 110 * scale, 0),
            new PointF(offsetX + 920 * scale, 0),
            Color.FromArgb(18, 56, 184),
            Color.FromArgb(12, 49, 84));

        var blend = new ColorBlend
        {
            Colors =
            [
                Color.FromArgb(18, 56, 184),
                Color.FromArgb(8, 213, 239),
                Color.FromArgb(14, 95, 224),
                Color.FromArgb(244, 200, 79),
                Color.FromArgb(12, 49, 84),
            ],
            Positions = [0f, 0.36f, 0.66f, 0.88f, 1f],
        };
        gradient.InterpolationColors = blend;

        using var pen = new Pen(gradient, 94 * scale)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };
        e.Graphics.DrawPath(pen, wavePath);

        var circle = new RectangleF(
            offsetX + (881 - 47) * scale,
            offsetY + (605 - 47) * scale,
            94 * scale,
            94 * scale);
        e.Graphics.FillEllipse(gradient, circle);
    }
}
