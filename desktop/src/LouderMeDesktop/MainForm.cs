using System.Diagnostics;
using System.Reflection;

namespace LouderMeDesktop;

internal sealed class MainForm : Form
{
    private static readonly Color Bg = Color.FromArgb(6, 9, 16);
    private static readonly Color Canvas = Color.FromArgb(9, 14, 23);
    private static readonly Color Surface = Color.FromArgb(13, 21, 33);
    private static readonly Color Surface2 = Color.FromArgb(17, 28, 43);
    private static readonly Color Cyan = Color.FromArgb(113, 215, 255);
    private static readonly Color Blue = Color.FromArgb(93, 156, 255);
    private static readonly Color Gold = Color.FromArgb(239, 189, 98);
    private static readonly Color TextPrimary = Color.FromArgb(239, 245, 255);
    private static readonly Color TextMuted = Color.FromArgb(148, 166, 190);
    private static readonly Color Line = Color.FromArgb(42, 58, 78);

    private readonly WindowsAudioController _audio = new();
    private readonly bool _startMinimized;
    private readonly System.Windows.Forms.Timer _pollTimer = new() { Interval = 1200 };
    private readonly NotifyIcon _trayIcon;

    private TrackBar _volumeSlider = null!;
    private Label _volumeValue = null!;
    private Label _deviceValue = null!;
    private Label _boostTarget = null!;
    private Label _boostStatus = null!;
    private CheckBox _startupToggle = null!;
    private FlowLayoutPanel _content = null!;
    private bool _syncingVolume;
    private bool _exitRequested;
    private int _requestedBoost;
    private readonly List<Button> _boostButtons = [];

    public MainForm(bool startMinimized)
    {
        _startMinimized = startMinimized;
        _requestedBoost = DesktopSettingsStore.ReadBoostTarget();

        Text = "LouderMe";
        BackColor = Bg;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 10f);
        MinimumSize = new Size(920, 680);
        Size = new Size(1120, 820);
        StartPosition = FormStartPosition.CenterScreen;

        try
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            Icon = SystemIcons.Application;
        }

        _trayIcon = new NotifyIcon
        {
            Text = "LouderMe",
            Icon = Icon ?? SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = BuildTrayMenu(),
        };
        _trayIcon.DoubleClick += (_, _) => OpenWindow();

        BuildUi();
        LoadAudioState();

        _pollTimer.Tick += (_, _) => LoadAudioState();
        _pollTimer.Start();

        Shown += (_, _) =>
        {
            if (_startMinimized)
            {
                HideToTray();
            }
        };

        FormClosing += (_, e) =>
        {
            if (!_exitRequested && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
            }
        };
    }

    private void BuildUi()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            ColumnCount = 1,
            RowCount = 2,
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        shell.Controls.Add(BuildTopBar(), 0, 0);

        _content = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(20, 18, 20, 26),
        };
        _content.SizeChanged += (_, _) => ResizeCards();
        shell.Controls.Add(_content, 0, 1);

        _content.Controls.Add(BuildHeroCard());
        _content.Controls.Add(BuildOutputCard());
        _content.Controls.Add(BuildBoostCard());
        _content.Controls.Add(BuildStartupCard());
        _content.Controls.Add(BuildAboutCard());
        ResizeCards();
    }

    private Control BuildTopBar()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Canvas,
            Padding = new Padding(18, 10, 18, 10),
        };

        var mark = new BrandMarkControl
        {
            Location = new Point(10, 7),
            Size = new Size(94, 82),
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
        };
        panel.Controls.Add(mark);

        var eyebrow = MakeLabel("MICHEL'S LAB · AUDIO WORKSPACE", 108, 17, 430, 18, 9, Gold, FontStyle.Bold);
        panel.Controls.Add(eyebrow);

        var title = MakeLabel("LouderMe", 108, 37, 430, 36, 23, TextPrimary, FontStyle.Bold);
        panel.Controls.Add(title);

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
        var subtitle = MakeLabel($"Desktop · v{version} · Native Windows app", 109, 72, 430, 20, 9, TextMuted);
        panel.Controls.Add(subtitle);

        var about = MakeButton("About", Blue, 110, 36);
        about.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        about.Location = new Point(Width - 150, 34);
        about.Click += (_, _) => ShowAbout();
        panel.Controls.Add(about);
        panel.Resize += (_, _) => about.Left = panel.ClientSize.Width - about.Width - 18;

        return panel;
    }

    private Panel BuildHeroCard()
    {
        var card = NewCard(145);
        card.Controls.Add(MakeLabel("LOUDERME DESKTOP", 18, 16, 400, 18, 9, Cyan, FontStyle.Bold));
        card.Controls.Add(MakeLabel("Windows Audio Center", 18, 39, 620, 38, 25, TextPrimary, FontStyle.Bold));
        card.Controls.Add(MakeLabel(
            "Native endpoint control, desktop startup and the foundation for LouderMe's Windows DSP engine.",
            20, 82, 800, 42, 10, TextMuted));
        return card;
    }

    private Panel BuildOutputCard()
    {
        var card = NewCard(236);
        card.Controls.Add(MakeLabel("SYSTEM OUTPUT", 18, 15, 260, 18, 9, Cyan, FontStyle.Bold));
        card.Controls.Add(MakeLabel("Windows master volume", 18, 38, 430, 28, 17, TextPrimary, FontStyle.Bold));

        _deviceValue = MakeLabel("Detecting output…", 18, 70, 800, 22, 9, TextMuted);
        card.Controls.Add(_deviceValue);

        _volumeValue = MakeLabel("0%", 18, 103, 90, 38, 24, TextPrimary, FontStyle.Bold);
        card.Controls.Add(_volumeValue);

        _volumeSlider = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 10,
            SmallChange = 1,
            LargeChange = 5,
            Value = 0,
            Location = new Point(105, 98),
            Size = new Size(620, 48),
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
        };
        _volumeSlider.ValueChanged += (_, _) =>
        {
            if (_syncingVolume) return;
            try
            {
                _audio.VolumePercent = _volumeSlider.Value;
                _volumeValue.Text = _volumeSlider.Value + "%";
            }
            catch (Exception ex)
            {
                _deviceValue.Text = "Audio endpoint error · " + ex.Message;
            }
        };
        card.Controls.Add(_volumeSlider);

        var quick = new[] { 25, 50, 75, 100 };
        for (var i = 0; i < quick.Length; i++)
        {
            var value = quick[i];
            var button = MakeButton(value + "%", Blue, 92, 36);
            button.Location = new Point(18 + i * 102, 166);
            button.Click += (_, _) => SetMasterVolume(value);
            card.Controls.Add(button);
        }

        var mute = MakeButton("Mute / unmute", Gold, 132, 36);
        mute.Location = new Point(432, 166);
        mute.Click += (_, _) =>
        {
            try { _audio.IsMuted = !_audio.IsMuted; } catch { }
        };
        card.Controls.Add(mute);

        var refresh = MakeButton("Refresh output", Cyan, 132, 36);
        refresh.Location = new Point(574, 166);
        refresh.Click += (_, _) =>
        {
            try { _audio.RefreshDefaultDevice(); LoadAudioState(); } catch { }
        };
        card.Controls.Add(refresh);

        return card;
    }

    private Panel BuildBoostCard()
    {
        var card = NewCard(262);
        card.Controls.Add(MakeLabel("BOOST ENGINE", 18, 15, 260, 18, 9, Gold, FontStyle.Bold));
        card.Controls.Add(MakeLabel("Target gain", 18, 38, 330, 28, 17, TextPrimary, FontStyle.Bold));

        _boostTarget = MakeLabel("", 18, 73, 220, 36, 23, TextPrimary, FontStyle.Bold);
        card.Controls.Add(_boostTarget);

        _boostStatus = MakeLabel("", 18, 111, 850, 54, 9, TextMuted);
        card.Controls.Add(_boostStatus);

        var levels = new[] { 100, 125, 150, 175, 200, 225, 250 };
        for (var i = 0; i < levels.Length; i++)
        {
            var level = levels[i];
            var button = MakeButton(level + "%", Blue, 92, 36);
            button.Location = new Point(18 + i * 101, 181);
            button.Tag = level;
            button.Click += (_, _) => SetBoostTarget(level);
            card.Controls.Add(button);
            _boostButtons.Add(button);
        }

        var note = MakeLabel(
            "Windows endpoint volume itself stops at its hardware maximum. LouderMe will not label 100% as 250%. " +
            "Targets above 100% are reserved for the native DSP/APO module.",
            18, 224, 900, 32, 8, Color.FromArgb(110, 130, 154));
        card.Controls.Add(note);

        UpdateBoostUi();
        return card;
    }

    private Panel BuildStartupCard()
    {
        var card = NewCard(150);
        card.Controls.Add(MakeLabel("STARTUP", 18, 15, 260, 18, 9, Gold, FontStyle.Bold));
        card.Controls.Add(MakeLabel("Start with Windows", 18, 39, 420, 30, 17, TextPrimary, FontStyle.Bold));
        card.Controls.Add(MakeLabel(
            "When enabled, LouderMe launches for your Windows account and starts minimized in the system tray.",
            18, 74, 700, 40, 9, TextMuted));

        _startupToggle = new CheckBox
        {
            Appearance = Appearance.Button,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            ForeColor = TextPrimary,
            BackColor = Surface2,
            Location = new Point(750, 45),
            Size = new Size(142, 44),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Text = StartupManager.IsEnabled() ? "ON" : "OFF",
            Checked = StartupManager.IsEnabled(),
        };
        _startupToggle.FlatAppearance.BorderColor = Line;
        _startupToggle.CheckedChanged += (_, _) =>
        {
            try
            {
                StartupManager.SetEnabled(_startupToggle.Checked);
                _startupToggle.Text = _startupToggle.Checked ? "ON" : "OFF";
                _startupToggle.BackColor = _startupToggle.Checked ? Color.FromArgb(23, 74, 121) : Surface2;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "LouderMe startup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        card.Controls.Add(_startupToggle);
        card.Resize += (_, _) => _startupToggle.Left = card.ClientSize.Width - _startupToggle.Width - 20;

        return card;
    }

    private Panel BuildAboutCard()
    {
        var card = NewCard(154);
        card.Controls.Add(MakeLabel("ABOUT", 18, 15, 260, 18, 9, Cyan, FontStyle.Bold));
        card.Controls.Add(MakeLabel("Michel's Lab", 18, 40, 360, 28, 17, TextPrimary, FontStyle.Bold));
        card.Controls.Add(MakeLabel(
            "LouderMe Desktop is a native Windows application. No browser shell or HTML runtime is used for this interface.",
            18, 75, 750, 42, 9, TextMuted));

        var releases = MakeButton("Release channel", Blue, 136, 36);
        releases.Location = new Point(18, 112);
        releases.Click += (_, _) => OpenUrl("https://github.com/realmichelduarte/michel-s-life-releases/releases");
        card.Controls.Add(releases);

        return card;
    }

    private void SetMasterVolume(int value)
    {
        try
        {
            _audio.VolumePercent = value;
            LoadAudioState();
        }
        catch (Exception ex)
        {
            _deviceValue.Text = "Audio endpoint error · " + ex.Message;
        }
    }

    private void SetBoostTarget(int percent)
    {
        _requestedBoost = Math.Clamp(percent, 100, 250);
        DesktopSettingsStore.WriteBoostTarget(_requestedBoost);

        if (_requestedBoost >= 100)
        {
            SetMasterVolume(100);
        }

        UpdateBoostUi();
    }

    private void UpdateBoostUi()
    {
        var gainDb = 20.0 * Math.Log10(_requestedBoost / 100.0);
        _boostTarget.Text = $"{_requestedBoost}%  ·  {gainDb:+0.00;-0.00;0.00} dB target";

        _boostStatus.Text = _requestedBoost == 100
            ? "100% baseline is applied through the real Windows default audio endpoint."
            : $"{_requestedBoost}% is saved as the requested LouderMe target. The Windows endpoint is held at its real 100% maximum; " +
              "extra gain is intentionally not faked while the native DSP/APO module is not installed.";

        foreach (var button in _boostButtons)
        {
            var selected = button.Tag is int value && value == _requestedBoost;
            button.BackColor = selected ? Color.FromArgb(28, 82, 132) : Surface2;
            button.ForeColor = selected ? Color.White : TextPrimary;
        }
    }

    private void LoadAudioState()
    {
        try
        {
            var volume = _audio.VolumePercent;
            var name = _audio.DeviceName;

            _syncingVolume = true;
            _volumeSlider.Value = Math.Clamp(volume, _volumeSlider.Minimum, _volumeSlider.Maximum);
            _volumeValue.Text = volume + (_audio.IsMuted ? "% · MUTED" : "%");
            _deviceValue.Text = name;
        }
        catch (Exception ex)
        {
            _deviceValue.Text = "No available Windows output · " + ex.Message;
        }
        finally
        {
            _syncingVolume = false;
        }
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open LouderMe", null, (_, _) => OpenWindow());
        menu.Items.Add("Exit", null, (_, _) =>
        {
            _exitRequested = true;
            _trayIcon.Visible = false;
            Close();
        });
        return menu;
    }

    private void HideToTray()
    {
        ShowInTaskbar = false;
        Hide();
    }

    private void OpenWindow()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            this,
            "LouderMe Desktop\n\nNative Windows audio workspace by Michel's Lab.\n" +
            "Official flowing-waveform product identity.\n\n" +
            "© 2026 Michel Armando Duarte Flores / Michel's Lab\nProprietary · All Rights Reserved",
            "About LouderMe",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void ResizeCards()
    {
        var width = Math.Max(760, _content.ClientSize.Width - 46);
        foreach (Control control in _content.Controls)
        {
            control.Width = width;
        }
    }

    private static Panel NewCard(int height) =>
        new()
        {
            Height = height,
            BackColor = Surface,
            Margin = new Padding(0, 0, 0, 14),
            Padding = new Padding(0),
        };

    private static Label MakeLabel(
        string text,
        int x,
        int y,
        int width,
        int height,
        float size,
        Color color,
        FontStyle style = FontStyle.Regular) =>
        new()
        {
            Text = text,
            AutoSize = false,
            Location = new Point(x, y),
            Size = new Size(width, height),
            ForeColor = color,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", size, style),
        };

    private static Button MakeButton(string text, Color accent, int width, int height)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = Surface2,
            ForeColor = TextPrimary,
            Cursor = Cursors.Hand,
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(
            Math.Min(255, accent.R + 15),
            Math.Min(255, accent.G + 15),
            Math.Min(255, accent.B + 15));
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 48, 72);
        return button;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pollTimer.Dispose();
            _trayIcon.Dispose();
            _audio.Dispose();
        }

        base.Dispose(disposing);
    }
}
