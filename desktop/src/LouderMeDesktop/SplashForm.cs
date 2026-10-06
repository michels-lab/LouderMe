namespace LouderMeDesktop;

internal sealed class SplashForm : Form
{
    public SplashForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(560, 330);
        BackColor = BrandColors.Bg;
        ShowInTaskbar = false;
        TopMost = true;

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(34),
            BackColor = BrandColors.Bg,
        };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(BrandColors.Line);
            var rect = panel.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            e.Graphics.DrawRectangle(pen, rect);
        };

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
        };

        stack.Controls.Add(new WaveformMarkControl
        {
            Width = 490,
            Height = 130,
            Margin = new Padding(0, 6, 0, 0),
        });
        stack.Controls.Add(new Label
        {
            AutoSize = true,
            Width = 490,
            Text = "LouderMe",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = BrandColors.Text,
            Font = new Font("Segoe UI Semibold", 29f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 2),
        });
        stack.Controls.Add(new Label
        {
            AutoSize = false,
            Width = 490,
            Height = 28,
            Text = "SOUND THAT LIFTS YOU",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = BrandColors.Gold,
            Font = new Font("Consolas", 10f, FontStyle.Bold),
        });
        stack.Controls.Add(new Label
        {
            AutoSize = false,
            Width = 490,
            Height = 24,
            Text = "MICHEL'S LAB · WINDOWS AUDIO WORKSPACE",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = BrandColors.Muted,
            Font = new Font("Consolas", 8f, FontStyle.Bold),
        });

        panel.Controls.Add(stack);
        Controls.Add(panel);
    }
}
