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

        // Render the official approved lockup, rather than two independent
        // labels and a waveform mark with differing logo proportions.
        stack.Controls.Add(new PictureBox
        {
            Name = "splash-official-louderme-lockup",
            Width = 490,
            Height = 206,
            Image = BrandAssets.LoadBitmap(
                "LouderMeDesktop.Assets.LouderMeOfficialLockup.png"),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(247, 250, 254),
            Margin = new Padding(0, 6, 0, 10),
            AccessibleName = "Official LouderMe logo",
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
