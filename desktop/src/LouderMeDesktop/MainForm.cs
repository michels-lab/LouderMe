using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace LouderMeDesktop;

internal sealed class MainForm : Form
{
    private readonly AudioDeviceController _audio = new();
    private readonly Label _deviceValue = new();
    private readonly Label _volumeValue = new();
    private readonly TrackBar _volume = new();
    private readonly CheckBox _mute = new();
    private readonly CheckBox _startWithWindows = new();
    private readonly Label _engineStatus = new();

    private static readonly Color Bg = Color.FromArgb(6, 9, 16);
    private static readonly Color Surface = Color.FromArgb(12, 20, 34);
    private static readonly Color Line = Color.FromArgb(35, 63, 92);
    private static readonly Color TextPrimary = Color.FromArgb(239, 246, 255);
    private static readonly Color Muted = Color.FromArgb(139, 161, 184);
    private static readonly Color Cyan = Color.FromArgb(48, 199, 255);
    private static readonly Color Gold = Color.FromArgb(242, 186, 73);

    public MainForm()
    {
        Text = "LouderMe";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 620);
        Size = new Size(900, 700);
        BackColor = Bg;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 10f);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildUi();
        LoadState();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _audio.Dispose();
        base.Dispose(disposing);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(28),
            BackColor = Bg,
            AutoScroll = true,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var brand = new Label
        {
            AutoSize = true,
            Text = "LOUDERME",
            Font = new Font("Segoe UI Semibold", 25f, FontStyle.Bold),
            ForeColor = TextPrimary,
            Margin = new Padding(0, 0, 0, 4),
        };
        var subtitle = new Label
        {
            AutoSize = true,
            Text = "Michel's Lab · Native Windows Audio Workspace",
            ForeColor = Gold,
            Font = new Font("Consolas", 9f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 20),
        };

        var header = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Top };
        header.Controls.Add(brand);
        header.Controls.Add(subtitle);
        root.Controls.Add(header);

        var outputCard = Card("SYSTEM OUTPUT");
        var outputLayout = (TableLayoutPanel)outputCard.Controls[0];

        _deviceValue.AutoSize = true;
        _deviceValue.ForeColor = TextPrimary;
        _deviceValue.Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
        _deviceValue.Text = "Detecting…";
        outputLayout.Controls.Add(_deviceValue, 0, 1);

        _volumeValue.AutoSize = true;
        _volumeValue.ForeColor = Cyan;
        _volumeValue.Font = new Font("Consolas", 11f, FontStyle.Bold);
        _volumeValue.Text = "0%";
        outputLayout.Controls.Add(_volumeValue, 1, 1);

        _volume.Minimum = 0;
        _volume.Maximum = 100;
        _volume.TickFrequency = 10;
        _volume.Dock = DockStyle.Fill;
        _volume.Margin = new Padding(0, 14, 0, 8);
        _volume.ValueChanged += (_, _) =>
        {
            try
            {
                _audio.VolumePercent = _volume.Value;
                _volumeValue.Text = $"{_volume.Value}%";
            }
            catch (Exception ex) { ShowEngineError(ex); }
        };
        outputLayout.SetColumnSpan(_volume, 2);
        outputLayout.Controls.Add(_volume, 0, 2);

        _mute.Text = "Mute system output";
        _mute.AutoSize = true;
        _mute.ForeColor = Muted;
        _mute.CheckedChanged += (_, _) =>
        {
            try { _audio.Muted = _mute.Checked; }
            catch (Exception ex) { ShowEngineError(ex); }
        };
        outputLayout.SetColumnSpan(_mute, 2);
        outputLayout.Controls.Add(_mute, 0, 3);
        root.Controls.Add(outputCard);

        var engineCard = Card("BOOST ENGINE");
        var engineLayout = (TableLayoutPanel)engineCard.Controls[0];
        _engineStatus.AutoSize = true;
        _engineStatus.ForeColor = Muted;
        _engineStatus.MaximumSize = new Size(760, 0);
        _engineStatus.Text =
            "Windows endpoint volume control is active. LouderMe Desktop does not yet claim system-wide gain above 100%; " +
            "that requires a validated Windows audio-processing path rather than relabeling the normal volume slider.";
        engineLayout.SetColumnSpan(_engineStatus, 2);
        engineLayout.Controls.Add(_engineStatus, 0, 1);
        root.Controls.Add(engineCard);

        var startupCard = Card("STARTUP");
        var startupLayout = (TableLayoutPanel)startupCard.Controls[0];
        _startWithWindows.Text = "Start LouderMe with Windows";
        _startWithWindows.AutoSize = true;
        _startWithWindows.ForeColor = TextPrimary;
        _startWithWindows.CheckedChanged += (_, _) =>
        {
            try
            {
                StartupManager.SetEnabled(_startWithWindows.Checked);
            }
            catch (Exception ex)
            {
                _startWithWindows.CheckedChanged -= StartupToggleNoOp;
                MessageBox.Show(ex.Message, "LouderMe startup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        startupLayout.SetColumnSpan(_startWithWindows, 2);
        startupLayout.Controls.Add(_startWithWindows, 0, 1);

        var startupInfo = new Label
        {
            AutoSize = true,
            ForeColor = Muted,
            Text = "Uses the current user's Windows sign-in startup. No administrator privileges are required.",
            Margin = new Padding(0, 8, 0, 0),
        };
        startupLayout.SetColumnSpan(startupInfo, 2);
        startupLayout.Controls.Add(startupInfo, 0, 2);
        root.Controls.Add(startupCard);

        var footer = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Top, Margin = new Padding(0, 10, 0, 0) };
        var refresh = Button("Refresh device", Cyan);
        refresh.Click += (_, _) => LoadState();
        var about = Button("About Michel's Lab", Gold);
        about.Click += (_, _) =>
        {
            MessageBox.Show(
                "LouderMe Desktop\n\nNative Windows edition by Michel's Lab.\n© 2026 Michel Armando Duarte Flores",
                "About LouderMe",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        };
        footer.Controls.Add(refresh);
        footer.Controls.Add(about);
        root.Controls.Add(footer);

        Controls.Add(root);
    }

    private static void StartupToggleNoOp(object? sender, EventArgs e) { }

    private void LoadState()
    {
        try
        {
            _audio.Refresh();
            _deviceValue.Text = _audio.DeviceName;
            _volume.Value = Math.Clamp(_audio.VolumePercent, _volume.Minimum, _volume.Maximum);
            _volumeValue.Text = $"{_volume.Value}%";
            _mute.Checked = _audio.Muted;
            _startWithWindows.Checked = StartupManager.IsEnabled();
        }
        catch (Exception ex)
        {
            ShowEngineError(ex);
        }
    }

    private void ShowEngineError(Exception ex)
    {
        _engineStatus.Text = "Audio endpoint unavailable: " + ex.Message;
        _engineStatus.ForeColor = Color.FromArgb(255, 112, 112);
    }

    private static Panel Card(string eyebrow)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 14),
            BackColor = Surface,
        };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(Line);
            var rect = panel.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(pen, rect);
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 4,
            BackColor = Surface,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var label = new Label
        {
            AutoSize = true,
            Text = eyebrow,
            ForeColor = Gold,
            Font = new Font("Consolas", 9f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8),
        };
        layout.SetColumnSpan(label, 2);
        layout.Controls.Add(label, 0, 0);
        panel.Controls.Add(layout);
        return panel;
    }

    private static Button Button(string text, Color accent)
    {
        return new Button
        {
            AutoSize = true,
            Text = text,
            FlatStyle = FlatStyle.Flat,
            ForeColor = accent,
            BackColor = Surface,
            Padding = new Padding(10, 6, 10, 6),
            Margin = new Padding(0, 0, 10, 0),
            FlatAppearance = { BorderColor = Line, BorderSize = 1 },
        };
    }
}
