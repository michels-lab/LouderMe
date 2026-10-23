using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Security.Principal;

namespace LouderMeDesktop;

internal sealed class MainForm : Form
{
    private readonly AudioDeviceController _audio = new();
    private readonly EqualizerApoEngine _apo = new();
    private readonly DesktopAudioSettings _settings = DesktopSettingsStore.Load();
    private readonly DesktopUpdateService _updates = new();

    private readonly Label _deviceValue = new();
    private readonly Label _volumeValue = new();
    private readonly TrackBar _volume = new();
    private readonly CheckBox _mute = new();
    private readonly CheckBox _startWithWindows = new();

    private readonly CheckBox _boostEnabled = new();
    private readonly TrackBar _boost = new();
    private readonly Label _boostValue = new();
    private readonly Label _boostDb = new();
    private readonly Label _engineStatus = new();
    private readonly Button _connectEngineAction = new();
    private readonly Button _audioPathTestAction = new();
    private FlowLayoutPanel? _boostQuickActions;
    private bool _diagnosticRunning;
    private bool? _lastAudioTestHeard;
    private string? _startupProbeRecoveryNotice;
    private readonly CheckBox _eqEnabled = new();
    private readonly ComboBox _preset = new();
    private readonly NumericUpDown[] _eqBands = new NumericUpDown[7];
    private readonly Label[] _eqBandValues = new Label[7];
    private readonly AudioPulseControl _boostWave = new();
    private readonly EqCurveControl _eqWave = new();
    private Panel? _equalizerCard;
    private readonly Label _updateStatus = new();
    private readonly Button _updateAction = new();
    private readonly PictureBox _officialLogo = new();
    private readonly Button _aboutAction = new();
    private IMessageFilter? _wheelFilter;
    private Panel? _contentViewport;
    private DesktopUpdateCheck? _lastUpdateCheck;

    private bool _loading;
    private readonly System.Windows.Forms.Timer _deviceVolumeSyncTimer = new() { Interval = 1500 };

    private static readonly Color Bg = Color.FromArgb(6, 9, 16);
    private static readonly Color Surface = Color.FromArgb(12, 20, 34);
    private static readonly Color Line = Color.FromArgb(35, 63, 92);
    private static readonly Color TextPrimary = Color.FromArgb(239, 246, 255);
    private static readonly Color Muted = Color.FromArgb(139, 161, 184);
    private static readonly Color Cyan = Color.FromArgb(48, 199, 255);
    private static readonly Color Gold = Color.FromArgb(242, 186, 73);
    private static readonly Color Red = Color.FromArgb(255, 112, 112);

    public MainForm()
    {
        Text = "LouderMe";
        Icon = BrandAssets.LoadIcon("LouderMeDesktop.Assets.LouderMe.ico");
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(480, 440);
        var workArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        Size = new Size(
            Math.Min(980, Math.Max(480, workArea.Width - 48)),
            Math.Min(840, Math.Max(440, workArea.Height - 48)));
        BackColor = Bg;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 10f);
        AutoScaleMode = AutoScaleMode.Dpi;

        // The four-second listening probe can outlive the process after a
        // crash or forced shutdown. Only an EXACT generated LouderMe test
        // is eligible for automatic restoration; never discard external edits.
        try
        {
            if (_apo.RecoverInterruptedDiagnostic(_settings))
                _startupProbeRecoveryNotice =
                    "Recovered a previously interrupted -18 dB audio-path test. " +
                    "Original Boost/EQ configuration restored; physical audio remains unverified.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                   or InvalidOperationException)
        {
            _startupProbeRecoveryNotice =
                "A previous audio-path test could not be restored safely: " +
                ex.Message;
        }
        // Recover before building controls: UI setup must never accidentally
        // apply defaults over a still-pending legacy diagnostic suffix.
        BuildUi();
        LoadState();
        if (_startupProbeRecoveryNotice is not null)
        {
            _engineStatus.ForeColor = Gold;
            _engineStatus.Text = _startupProbeRecoveryNotice;
        }
        _deviceVolumeSyncTimer.Tick += (_, _) => SyncDeviceVolume();
        _deviceVolumeSyncTimer.Start();

        if (!DeveloperMode.Enabled)
            Shown += async (_, _) => await CheckForUpdatesAsync(silent: true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _deviceVolumeSyncTimer.Stop();
            _deviceVolumeSyncTimer.Dispose();
            if (_wheelFilter is not null)
                Application.RemoveMessageFilter(_wheelFilter);
            _audio.Dispose();
        }
        base.Dispose(disposing);
    }

    private void BuildUi()
    {
        Name = "main-window";
        _aboutAction.Name = "about";
        _volume.Name = "media-volume";
        _mute.Name = "media-mute";
        _boostEnabled.Name = "boost-toggle";
        _boost.Name = "boost-level";
        _eqEnabled.Name = "eq-toggle";
        _preset.Name = "eq-preset";
        _connectEngineAction.Name = "connect-engine";
        _audioPathTestAction.Name = "audio-path-test";
        _startWithWindows.Name = "start-with-windows";
        // Persistent header must remain visible independently of scroll position.
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(16, 10, 16, 10),
            BackColor = Bg,
        };
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Use the real official LouderMe lockup (Option 4), not separate
        // text and a WaveformMarkControl forced into an 88 px column.
        // That 150 px MinimumSize was painting over the L ("auderMe").
        // Keep the approved geometry with a transparent dark-theme variant.
        var header = new TableLayoutPanel
        {
            Height = 112,
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 8),
        };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _officialLogo.Name = "official-louderme-lockup";
        _officialLogo.Image = BrandAssets.LoadBitmap(
            "LouderMeDesktop.Assets.LouderMeDarkLockup.png");
        _officialLogo.SizeMode = PictureBoxSizeMode.Zoom;
        _officialLogo.Dock = DockStyle.Fill;
        _officialLogo.BackColor = Bg;
        _officialLogo.Margin = new Padding(0, 0, 12, 0);
        _officialLogo.AccessibleName = "Official LouderMe logo";
        header.Controls.Add(_officialLogo, 0, 0);

        _aboutAction.Text = "About";
        _aboutAction.AutoSize = true;
        _aboutAction.FlatStyle = FlatStyle.Flat;
        _aboutAction.ForeColor = Gold;
        _aboutAction.BackColor = Surface;
        _aboutAction.Padding = new Padding(8, 5, 8, 5);
        _aboutAction.FlatAppearance.BorderColor = Line;
        _aboutAction.Anchor = AnchorStyles.Right;
        _aboutAction.Click += (_, _) => OpenAbout();
        header.Controls.Add(_aboutAction, 2, 0);
        shell.Controls.Add(header, 0, 0);

        // A regular scrollable Panel is the scroll owner; the old percent-sized
        // TableLayoutPanel could grow off-screen without exposing working scroll.
        var viewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            TabStop = true,
            BackColor = Bg,
        };
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(0, 4, 8, 8),
            Margin = new Padding(0),
            BackColor = Bg,
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 5; i++)
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        BuildOutputCard(content);
        BuildBoostCard(content);
        BuildEqualizerCard(content);
        BuildStartupCard(content);
        BuildFooter(content);

        viewport.Controls.Add(content);
        _contentViewport = viewport;
        shell.Controls.Add(viewport, 0, 1);
        Controls.Add(shell);
        _wheelFilter = new ViewportWheelFilter(this, viewport);
        Application.AddMessageFilter(_wheelFilter);
        Shown += (_, _) => viewport.Focus();
    }

    internal void CaptureVisualEvidence(string destination)
    {
        // Called only by the running WinForms test process, after Shown.
        // Real controls are measured and rasterized, not replaced by mock screens.
        Directory.CreateDirectory(destination);
        foreach (var (viewport, width, height) in new[]
        {
            ("compact", 600, 520),
            ("wide", 900, 700),
        })
        {
            Size = new Size(width, height);
            PerformLayout();
            Application.DoEvents();
            var action = _aboutAction.RectangleToScreen(_aboutAction.ClientRectangle);
            var window = RectangleToScreen(ClientRectangle);
            if (!_aboutAction.Visible || !_aboutAction.Enabled || action.IsEmpty ||
                !window.Contains(action))
                throw new InvalidOperationException("About action is clipped in the rendered Home window.");

            SaveRenderedForm(this, Path.Combine(destination, $"home-{viewport}.png"));
            var eqCard = _equalizerCard ??
                throw new InvalidOperationException("Visual EQ card is missing from rendered Home");
            var scroller = _contentViewport ??
                throw new InvalidOperationException("Scrollable audio workspace missing");
            _eqWave.AssertMixerContract();
            // Additional real running-form evidence: visually inspect the
            // actual EQ, which Home-at-scroll-top cannot possibly show.
            var maxScroll = Math.Max(0, scroller.DisplayRectangle.Height - scroller.ClientSize.Height);
            scroller.AutoScrollPosition = new Point(0,
                Math.Clamp(eqCard.Top + 35, 0, maxScroll));
            Application.DoEvents();
            SaveRenderedForm(this, Path.Combine(destination, $"equalizer-{viewport}.png"));
            scroller.AutoScrollPosition = Point.Empty;
            Application.DoEvents();

            using var about = new AboutForm(_updates)
            {
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(width, height),
            };
            about.Show(this);
            about.PerformLayout();
            Application.DoEvents();
            if (!about.Visible || !about.IsHandleCreated)
                throw new InvalidOperationException("Real WinForms About could not be shown.");
            about.AssertVisibleLayout();
            _eqWave.AssertMixerContract();
        EqCurveControl.AssertInteractiveContract();
            SaveRenderedForm(about, Path.Combine(destination, $"about-{viewport}.png"));
            about.Close();
        }
    }

    private static void SaveRenderedForm(Form form, string path)
    {
        if (form.Width < 320 || form.Height < 320)
            throw new InvalidOperationException("Rendered UI viewport is implausibly small.");
        using var shot = new Bitmap(form.Width, form.Height);
        // WinForms DrawToBitmap rasterizes the actual running live form/control tree.
        // It is not a physical desktop screen capture; device evidence remains separate.
        form.DrawToBitmap(shot, new Rectangle(Point.Empty, form.Size));
        shot.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }

    private void OpenAbout()
    {
        using var form = new AboutForm(_updates);
        form.ShowDialog(this);
    }

    internal void AssertCompactLayout()
    {
        // Invoked by an installed/runtime smoke invocation after Shown.
        // This tests actual WinForms bounds and scrolling, not only source strings.
        // Test the compact minimum width as well as the standard 600px
        // layout. The official lockup and About must never overlap or clip.
        foreach (var width in new[] { 480, 600 })
        {
            Size = new Size(width, 520);
            PerformLayout();
            Application.DoEvents();
            var lockupBounds = _officialLogo.RectangleToScreen(_officialLogo.ClientRectangle);
            var about = _aboutAction.RectangleToScreen(_aboutAction.ClientRectangle);
            var available = RectangleToScreen(ClientRectangle);
            if (_officialLogo.Image is null || !_officialLogo.Visible ||
                lockupBounds.Width < 180 || lockupBounds.Height < 90 ||
                !available.Contains(lockupBounds) ||
                lockupBounds.IntersectsWith(about) ||
                !available.Contains(about))
                throw new InvalidOperationException(
                    "Official LouderMe lockup overlaps About or leaves the header.");
            if (Icon is null || Icon.Width < 16)
                throw new InvalidOperationException("Official LouderMe window icon is missing.");
        }

        var viewport = _contentViewport
            ?? throw new InvalidOperationException("Scrollable workspace not initialized.");
        if (!viewport.AutoScroll || viewport.ClientSize.Height < 100)
            throw new InvalidOperationException("Main workspace scroll viewport is unusable.");

        var aboutBounds = _aboutAction.RectangleToScreen(_aboutAction.ClientRectangle);
        var windowBounds = RectangleToScreen(ClientRectangle);
        if (!_aboutAction.Visible || !_aboutAction.Enabled || aboutBounds.IsEmpty ||
            !windowBounds.Contains(aboutBounds) || aboutBounds.Bottom > viewport.RectangleToScreen(viewport.ClientRectangle).Top)
            throw new InvalidOperationException("About is not visibly anchored in the fixed top header.");

        var maxScroll = Math.Max(0, viewport.DisplayRectangle.Height - viewport.ClientSize.Height);
        if (maxScroll <= 0)
            throw new InvalidOperationException("Compact workspace has no vertical scroll range.");

        viewport.AutoScrollPosition = new Point(0, maxScroll);
        Application.DoEvents();
        if (-viewport.AutoScrollPosition.Y <= 0)
            throw new InvalidOperationException("Compact workspace could not scroll to lower cards.");

        if (!_aboutAction.Visible || !windowBounds.Contains(
                _aboutAction.RectangleToScreen(_aboutAction.ClientRectangle)))
            throw new InvalidOperationException("About disappeared while workspace scrolled.");
        // Exercise real live About cards, not merely screenshot existence.
        foreach (var aboutWidth in new[] { 480, 600, 860 })
        {
            using var about = new AboutForm(_updates)
            {
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(aboutWidth, 520),
            };
            about.Show(this);
            Application.DoEvents();
            about.AssertVisibleLayout();
            about.Close();
        }
        _eqWave.AssertMixerContract();
        EqCurveControl.AssertInteractiveContract();
    }

    // Wheel scrolling must work even while a nested slider/button has focus.
    // Only intercept messages over the scroll viewport of the active main form.
    private sealed class ViewportWheelFilter(Form owner, Panel viewport) : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {
            const int WmMouseWheel = 0x020A;
            if (m.Msg != WmMouseWheel || Form.ActiveForm != owner ||
                !viewport.Visible || !viewport.RectangleToScreen(viewport.ClientRectangle).Contains(Cursor.Position))
                return false;

            var delta = unchecked((short)((m.WParam.ToInt64() >> 16) & 0xffff));
            if (delta == 0) return false;
            var maxScroll = Math.Max(0, viewport.DisplayRectangle.Height - viewport.ClientSize.Height);
            var next = Math.Clamp(
                -viewport.AutoScrollPosition.Y - Math.Sign(delta) *
                Math.Max(3, SystemInformation.MouseWheelScrollLines) * 24,
                0, maxScroll);
            viewport.AutoScrollPosition = new Point(0, next);
            return true;
        }
    }

    private void BuildOutputCard(TableLayoutPanel root)
    {
        var card = Card("DEVICE VOLUME · WINDOWS OUTPUT 0–100%");
        card.Name = "output-card";
        var layout = (TableLayoutPanel)card.Controls[0];

        _deviceValue.AutoSize = true;
        _deviceValue.ForeColor = TextPrimary;
        _deviceValue.Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
        _deviceValue.Text = "Detecting…";
        layout.Controls.Add(_deviceValue, 0, 1);

        _volumeValue.AutoSize = true;
        _volumeValue.ForeColor = Cyan;
        _volumeValue.Font = new Font("Consolas", 11f, FontStyle.Bold);
        _volumeValue.Text = "0%";
        layout.Controls.Add(_volumeValue, 1, 1);

        _volume.Minimum = 0;
        _volume.Maximum = 100;
        _volume.TickFrequency = 10;
        _volume.Dock = DockStyle.Fill;
        _volume.Margin = new Padding(0, 14, 0, 8);
        _volume.ValueChanged += (_, _) =>
        {
            if (_loading) return;
            try
            {
                _audio.VolumePercent = _volume.Value;
                _volumeValue.Text = $"{_volume.Value}%";
            }
            catch (Exception ex) { ShowEngineError(ex); }
        };
        layout.SetColumnSpan(_volume, 2);
        layout.Controls.Add(_volume, 0, 2);

        _mute.Text = "Mute system output";
        _mute.AutoSize = true;
        _mute.ForeColor = Muted;
        _mute.CheckedChanged += (_, _) =>
        {
            if (_loading) return;
            try { _audio.Muted = _mute.Checked; }
            catch (Exception ex) { ShowEngineError(ex); }
        };
        layout.SetColumnSpan(_mute, 2);
        layout.Controls.Add(_mute, 0, 3);
        root.Controls.Add(card);
    }

    private void BuildBoostCard(TableLayoutPanel root)
    {
        var card = Card("GLOBAL BOOST · SYSTEM-WIDE APO");
        card.Name = "boost-card";
        var layout = (TableLayoutPanel)card.Controls[0];
        layout.RowCount = 9;

        _boostEnabled.Text = "Enable global boost";
        _boostEnabled.AutoSize = true;
        _boostEnabled.ForeColor = TextPrimary;
        _boostEnabled.CheckedChanged += (_, _) =>
        {
            if (_loading) return;
            _settings.BoostEnabled = _boostEnabled.Checked;
            ApplyProcessing();
        };
        layout.Controls.Add(_boostEnabled, 0, 1);

        var values = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Anchor = AnchorStyles.Right,
        };
        _boostValue.AutoSize = true;
        _boostValue.ForeColor = TextPrimary;
        _boostValue.Font = new Font("Segoe UI Semibold", 22f, FontStyle.Bold);
        _boostDb.AutoSize = true;
        _boostDb.ForeColor = Cyan;
        _boostDb.Font = new Font("Consolas", 10f, FontStyle.Bold);
        _boostDb.Margin = new Padding(12, 10, 0, 0);
        values.Controls.Add(_boostValue);
        values.Controls.Add(_boostDb);
        layout.Controls.Add(values, 1, 1);

        _boost.Minimum = 100;
        _boost.Maximum = 250;
        _boost.TickFrequency = 25;
        _boost.SmallChange = 1;
        _boost.LargeChange = 25;
        _boost.Dock = DockStyle.Fill;
        _boost.Margin = new Padding(0, 12, 0, 4);
        _boost.ValueChanged += (_, _) =>
        {
            _boostValue.Text = $"{_boost.Value}%";
            _boostDb.Text = $"{BoostMath.PercentToDb(_boost.Value):+0.00;-0.00;0.00} dB";
            _boostWave.BoostPercent = _boost.Value;
            if (_loading) return;
            _settings.BoostPercent = _boost.Value;
            // A nonzero Boost level selected by the user is an activation request,
            // not merely a decorative target while the checkbox stays off.
            if (_boost.Value > 100 && !_boostEnabled.Checked)
            {
                _loading = true;
                try { _boostEnabled.Checked = true; }
                finally { _loading = false; }
                _settings.BoostEnabled = true;
            }
        };
        _boost.MouseUp += (_, _) => ApplyProcessing();
        _boost.KeyUp += (_, _) => ApplyProcessing();
        layout.SetColumnSpan(_boost, 2);
        layout.Controls.Add(_boost, 0, 2);

        _boostWave.Dock = DockStyle.Fill;
        _boostWave.Height = 58;
        _boostWave.Margin = new Padding(0, 2, 0, 6);
        layout.SetColumnSpan(_boostWave, 2);
        layout.Controls.Add(_boostWave, 0, 3);

        var quick = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 4, 0, 8),
        };
        _boostQuickActions = quick;
        foreach (var level in new[] { 100, 125, 150, 175, 200, 225, 250 })
        {
            var button = Button($"{level}%", Cyan);
            button.Name = $"boost-quick-{level}";
            button.Click += (_, _) =>
            {
                _boost.Value = level;
                _settings.BoostPercent = level;
                ApplyProcessing();
            };
            quick.Controls.Add(button);
        }
        layout.SetColumnSpan(quick, 2);
        layout.Controls.Add(quick, 0, 4);

        _engineStatus.AutoSize = true;
        _engineStatus.ForeColor = Muted;
        _engineStatus.MaximumSize = new Size(540, 0);
        layout.SetColumnSpan(_engineStatus, 2);
        layout.Controls.Add(_engineStatus, 0, 5);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 10, 0, 0),
        };
        var getEngine = Button("Install Equalizer APO", Gold);
        getEngine.Click += (_, _) =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://sourceforge.net/projects/equalizerapo/files/1.4.2/",
                UseShellExecute = true,
            });
        };
        var configure = Button("Open Configurator", Cyan);
        configure.Click += (_, _) =>
        {
            var path = Path.Combine(_apo.InstallDirectory, "Configurator.exe");
            if (!File.Exists(path))
            {
                MessageBox.Show(
                    "Equalizer APO is not installed in its standard location yet.",
                    "LouderMe audio engine",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        };
        var refresh = Button("Refresh engine", Cyan);
        refresh.Click += (_, _) =>
        {
            if (_apo.GetStatus().Installed) ApplyProcessing();
            else RefreshEngineStatus();
        };
        _connectEngineAction.Text = "Connect Boost/EQ (Windows setup)";
        _connectEngineAction.AutoSize = true;
        _connectEngineAction.FlatStyle = FlatStyle.Flat;
        _connectEngineAction.ForeColor = Gold;
        _connectEngineAction.BackColor = Surface;
        _connectEngineAction.Padding = new Padding(8, 5, 8, 5);
        _connectEngineAction.FlatAppearance.BorderColor = Line;
        _connectEngineAction.Click += async (_, _) => await ConnectEngineAsync();
        actions.Controls.Add(_connectEngineAction);
        actions.Controls.Add(getEngine);
        actions.Controls.Add(configure);
        actions.Controls.Add(refresh);

        _audioPathTestAction.AutoSize = true;
        _audioPathTestAction.Text = "Test audio path (-18 dB temporarily)";
        _audioPathTestAction.FlatStyle = FlatStyle.Flat;
        _audioPathTestAction.ForeColor = Cyan;
        _audioPathTestAction.BackColor = Surface;
        _audioPathTestAction.Padding = new Padding(8, 5, 8, 5);
        _audioPathTestAction.FlatAppearance.BorderColor = Line;
        _audioPathTestAction.Click += async (_, _) => await TestAudioPathAsync();
        actions.Controls.Add(_audioPathTestAction);

        var copyDiagnostics = Button("Copy audio diagnostics", Cyan);
        copyDiagnostics.Click += (_, _) =>
        {
            var status = _apo.GetStatus(_settings);
            var report =
                $"LouderMe Desktop v{_updates.CurrentVersionText}\\n" +
                $"Output device: {_deviceValue.Text}\\n" +
                $"Equalizer APO installed: {status.Installed}\\n" +
                $"LouderMe APO include active: {status.Configured}\\n" +
                $"Stored DSP config matches UI: {status.MatchesSettings}\\n" +
                $"Boost target: {_settings.BoostPercent}%, enabled: {_settings.BoostEnabled}\\n" +
                $"EQ enabled: {_settings.EqualizerEnabled}\\n" +
                $"Audio path test audible attenuation: {(_lastAudioTestHeard is null ? "Not tested" : _lastAudioTestHeard.Value ? "Yes (user-reported)" : "No (user-reported)")}\\n" +
                $"Status: {status.Message}\\n" +
                "Audio audible effect: NOT automatically verified";
            Clipboard.SetText(report.Replace("\\n", Environment.NewLine));
            _engineStatus.ForeColor = Cyan;
            _engineStatus.Text = "Audio diagnostic snapshot copied. This does not prove audible processing.";
        };
        actions.Controls.Add(copyDiagnostics);
        layout.SetColumnSpan(actions, 2);
        layout.Controls.Add(actions, 0, 6);

        var note = new Label
        {
            AutoSize = true,
            ForeColor = Muted,
            MaximumSize = new Size(800, 0),
            Margin = new Padding(0, 8, 0, 0),
            Text =
                "100–250% is mapped to real post-mix digital gain: 20·log10(percent/100). " +
                "250% = +7.96 dB. The engine must be attached to the active playback device in Equalizer APO's Configurator. " +
                "High positive gain can trigger Windows' final output limiting/compression on loud material. " +
                "ASIO and WASAPI exclusive streams can bypass Windows APO effects.",
        };
        layout.SetColumnSpan(note, 2);
        layout.Controls.Add(note, 0, 7);
        card.ClientSizeChanged += (_, _) =>
        {
            var textWidth = Math.Max(210, card.ClientSize.Width - card.Padding.Horizontal - 22);
            _engineStatus.MaximumSize = new Size(textWidth, 0);
            note.MaximumSize = new Size(textWidth, 0);
        };
        root.Controls.Add(card);
    }

    private void BuildEqualizerCard(TableLayoutPanel root)
    {
        var card = Card("7-BAND EQUALIZER");
        card.Name = "equalizer-card";
        var layout = (TableLayoutPanel)card.Controls[0];
        layout.RowCount = 12;

        _eqEnabled.Text = "Enable equalizer";
        _eqEnabled.AutoSize = true;
        _eqEnabled.ForeColor = TextPrimary;
        _eqEnabled.CheckedChanged += (_, _) =>
        {
            if (_loading) return;
            _settings.EqualizerEnabled = _eqEnabled.Checked;
            ApplyProcessing();
        };
        layout.Controls.Add(_eqEnabled, 0, 1);

        _preset.DropDownStyle = ComboBoxStyle.DropDownList;
        _preset.BackColor = Surface;
        _preset.ForeColor = TextPrimary;
        _preset.Width = 170;
        _preset.Items.AddRange(EqualizerPresets.Names.Cast<object>().ToArray());
        _preset.SelectedIndexChanged += (_, _) =>
        {
            if (_loading || _preset.SelectedItem is not string name) return;
            _settings.Preset = name;
            if (!_eqEnabled.Checked)
            {
                _loading = true;
                try { _eqEnabled.Checked = true; }
                finally { _loading = false; }
                _settings.EqualizerEnabled = true;
            }
            if (name.Equals("Custom", StringComparison.OrdinalIgnoreCase))
            {
                ApplyProcessing();
                return;
            }

            var gains = EqualizerPresets.For(name);
            _loading = true;
            try
            {
                for (var i = 0; i < _eqBands.Length; i++)
                    _eqBands[i].Value = (decimal)gains[i];
            }
            finally { _loading = false; }

            _settings.EqualizerGainsDb = gains;
            _eqWave.Gains = gains;
            UpdateEqValueLabels();
            ApplyProcessing();
        };
        layout.Controls.Add(_preset, 1, 1);

        _eqWave.Name = "interactive-seven-band-mixer";
        _eqWave.Height = 300;
        _eqWave.MinimumSize = new Size(280, 290);
        _eqWave.Dock = DockStyle.Top;
        _eqWave.Margin = new Padding(0, 12, 0, 8);
        _eqWave.BandGainChanged += (band, db) =>
        {
            // Go through the original NumericUpDown.ValueChanged event so
            // presets, APO configuration, persistence and clipping limits
            // remain identical across visual and precise input methods.
            if (band < 0 || band >= _eqBands.Length || _loading) return;
            _eqBands[band].Value = (decimal)Math.Clamp(db, -10f, 10f);
        };
        layout.SetColumnSpan(_eqWave, 2);
        layout.Controls.Add(_eqWave, 0, 2);

        var fineTune = Button("Fine-tune band values  + / - 0.5 dB", Cyan);
        fineTune.Name = "eq-fine-tune-toggle";
        fineTune.AutoSize = true;
        fineTune.Margin = new Padding(0, 8, 0, 4);
        var advancedRows = new TableLayoutPanel
        {
            Name = "eq-advanced-values",
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 7,
            Visible = false,
            Margin = Padding.Empty,
        };
        advancedRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fineTune.Click += (_, _) =>
        {
            advancedRows.Visible = !advancedRows.Visible;
            fineTune.Text = advancedRows.Visible
                ? "Hide numeric fine-tuning" : "Fine-tune band values  + / - 0.5 dB";
            card.PerformLayout();
        };
        layout.SetColumnSpan(fineTune, 2);
        layout.Controls.Add(fineTune, 0, 3);
        layout.SetColumnSpan(advancedRows, 2);
        layout.Controls.Add(advancedRows, 0, 4);

        for (var i = 0; i < EqualizerPresets.FrequenciesHz.Length; i++)
        {
            var index = i;
            var bandRow = new TableLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                Margin = new Padding(0, 4, 0, 4),
            };
            bandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75));
            bandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));

            var frequency = EqualizerPresets.FrequenciesHz[i];
            var label = new Label
            {
                AutoSize = true,
                ForeColor = TextPrimary,
                Font = new Font("Consolas", 9f, FontStyle.Bold),
                Text = frequency >= 1000
                    ? $"{frequency / 1000d:0.#}k"
                    : frequency.ToString(),
                Anchor = AnchorStyles.Left,
            };

            var value = new NumericUpDown
            {
                Minimum = -10,
                Maximum = 10,
                DecimalPlaces = 1,
                Increment = 0.5M,
                Width = 110,
                BackColor = Surface,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
            };
            value.ValueChanged += (_, _) =>
            {
                _eqBandValues[index].Text = $"{value.Value:+0.0;-0.0;0.0} dB";
                if (_loading) return;

                _settings.Preset = "Custom";
                _settings.EqualizerEnabled = true;
                _loading = true;
                try
                {
                    _preset.SelectedItem = "Custom";
                    _eqEnabled.Checked = true;
                }
                finally { _loading = false; }
                _settings.EqualizerGainsDb[index] = (float)value.Value;
                _eqWave.Gains = _settings.EqualizerGainsDb;
                ApplyProcessing();
            };
            value.Name = $"eq-band-{EqualizerPresets.FrequenciesHz[i]}";
            _eqBands[i] = value;

            var valueLabel = new Label
            {
                AutoSize = true,
                ForeColor = Cyan,
                Font = new Font("Consolas", 9f),
                Text = "0.0 dB",
                Anchor = AnchorStyles.Right,
            };
            _eqBandValues[i] = valueLabel;

            bandRow.Controls.Add(label, 0, 0);
            bandRow.Controls.Add(value, 1, 0);
            bandRow.Controls.Add(valueLabel, 2, 0);
            advancedRows.Controls.Add(bandRow, 0, i);
        }

        _equalizerCard = card;
        root.Controls.Add(card);
    }

    private void BuildStartupCard(TableLayoutPanel root)
    {
        var card = Card("STARTUP");
        card.Name = "startup-card";
        var layout = (TableLayoutPanel)card.Controls[0];

        _startWithWindows.Text = "Start LouderMe with Windows";
        if (DeveloperMode.Enabled) _startWithWindows.Enabled = false; // Never alter public auto-start via Developer UI.
        _startWithWindows.AutoSize = true;
        _startWithWindows.ForeColor = TextPrimary;
        _startWithWindows.CheckedChanged += (_, _) =>
        {
            if (_loading) return;
            try { StartupManager.SetEnabled(_startWithWindows.Checked); }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "LouderMe startup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        };
        layout.SetColumnSpan(_startWithWindows, 2);
        layout.Controls.Add(_startWithWindows, 0, 1);

        var startupInfo = new Label
        {
            AutoSize = true,
            ForeColor = Muted,
            Text = "Uses the current user's Windows sign-in startup. No administrator privileges are required.",
            Margin = new Padding(0, 8, 0, 0),
        };
        layout.SetColumnSpan(startupInfo, 2);
        layout.Controls.Add(startupInfo, 0, 2);
        card.ClientSizeChanged += (_, _) =>
            startupInfo.MaximumSize = new Size(
                Math.Max(210, card.ClientSize.Width - card.Padding.Horizontal - 22), 0);
        root.Controls.Add(card);
    }

    private void BuildFooter(TableLayoutPanel root)
    {
        var footer = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 10, 0, 0),
        };

        var refresh = Button("Refresh device", Cyan);
        refresh.Click += (_, _) => LoadState();

        _updateAction.AutoSize = true;
        _updateAction.Text = "Check updates";
        if (DeveloperMode.Enabled)
        {
            _updateAction.Enabled = false;
            _updateAction.Text = "Updates: public edition only";
        }
        _updateAction.FlatStyle = FlatStyle.Flat;
        _updateAction.ForeColor = Cyan;
        _updateAction.BackColor = Surface;
        _updateAction.Padding = new Padding(10, 6, 10, 6);
        _updateAction.Margin = new Padding(0, 0, 10, 0);
        _updateAction.FlatAppearance.BorderColor = Line;
        _updateAction.FlatAppearance.BorderSize = 1;
        _updateAction.Click += async (_, _) => await HandleUpdateActionAsync();

        _updateStatus.AutoSize = true;
        _updateStatus.ForeColor = Muted;
        _updateStatus.Text = $"v{_updates.CurrentVersionText} · stable";
        _updateStatus.Margin = new Padding(6, 9, 14, 0);

        footer.Controls.Add(refresh);
        footer.Controls.Add(_updateAction);
        footer.Controls.Add(_updateStatus);
        root.Controls.Add(footer);
    }

    private void SyncDeviceVolume()
    {
        if (_loading || _volume.Capture || _mute.Focused) return;
        try
        {
            _audio.Refresh();
            _loading = true;
            _deviceValue.Text = _audio.DeviceName;
            _volume.Value = Math.Clamp(_audio.VolumePercent, 0, 100);
            _volumeValue.Text = $"{_volume.Value}%";
            _mute.Checked = _audio.Muted;
        }
        catch (Exception ex) { _deviceValue.Text = $"Device unavailable: {ex.Message}"; }
        finally { _loading = false; }
    }

    private void LoadState()
    {
        _loading = true;
        try
        {
            _audio.Refresh();
            _deviceValue.Text = _audio.DeviceName;
            _volume.Value = Math.Clamp(_audio.VolumePercent, _volume.Minimum, _volume.Maximum);
            _volumeValue.Text = $"{_volume.Value}%";
            _mute.Checked = _audio.Muted;
            _startWithWindows.Checked = StartupManager.IsEnabled();

            _boostEnabled.Checked = _settings.BoostEnabled;
            _boost.Value = Math.Clamp(_settings.BoostPercent, 100, 250);
            _boostValue.Text = $"{_boost.Value}%";
            _boostDb.Text = $"{BoostMath.PercentToDb(_boost.Value):+0.00;-0.00;0.00} dB";
            _boostWave.BoostPercent = _boost.Value;
            _eqEnabled.Checked = _settings.EqualizerEnabled;

            var presetName = EqualizerPresets.Names.Contains(_settings.Preset)
                ? _settings.Preset
                : "Flat";
            _preset.SelectedItem = presetName;

            for (var i = 0; i < _eqBands.Length; i++)
            {
                var gain = _settings.EqualizerGainsDb.ElementAtOrDefault(i);
                _eqBands[i].Value = (decimal)Math.Clamp(gain, -10f, 10f);
            }
            _eqWave.Gains = _settings.EqualizerGainsDb;
            UpdateEqValueLabels();
        }
        catch (Exception ex)
        {
            ShowEngineError(ex);
        }
        finally
        {
            _loading = false;
        }

        RefreshEngineStatus();
    }

    private void ApplyProcessing()
    {
        if (_loading) return;

        DesktopSettingsStore.Save(_settings);
        if (_diagnosticRunning) return;
        var status = _apo.GetStatus(_settings);

        if (!status.Installed)
        {
            RefreshEngineStatus();
            return;
        }

        try
        {
            _apo.Apply(_settings);
            RefreshEngineStatus();
        }
        catch (UnauthorizedAccessException)
        {
            _engineStatus.ForeColor = Gold;
            _engineStatus.Text =
                "NOT APPLIED: Windows denied configuration access. Click Connect Boost/EQ " +
                "and approve the one-time Windows prompt. No administrator mode is required for normal use.";
        }
        catch (Exception ex)
        {
            ShowEngineError(ex);
        }
    }

    private async Task TestAudioPathAsync()
    {
        if (_diagnosticRunning) return;
        var status = _apo.GetStatus(_settings);
        if (!status.Configured || !status.MatchesSettings)
        {
            RefreshEngineStatus();
            MessageBox.Show(
                "Connect LouderMe to Equalizer APO first. The current " +
                "Boost/EQ settings are not present in an active configuration.",
                "Audio processing not active", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }
        if (MessageBox.Show(
            "Play music or speech at a comfortable level on the output " +
            "shown in LouderMe. This test will temporarily make the sound " +
            "QUIETER by 18 dB for four seconds, then restore your original settings. " +
            "No microphone or recording is used.\\n\\nStart test?",
            "Test Windows audio processing", MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        // Bind the temporary effect and its restoration to the SAME
        // immutable values even if a deferred UI event arrives mid-test.
        var testSettings = new DesktopAudioSettings
        {
            BoostEnabled = _settings.BoostEnabled,
            BoostPercent = _settings.BoostPercent,
            EqualizerEnabled = _settings.EqualizerEnabled,
            Preset = _settings.Preset,
            EqualizerGainsDb = (float[])_settings.EqualizerGainsDb.Clone(),
        };
        _diagnosticRunning = true;
        _audioPathTestAction.Enabled = false;
        RefreshEngineStatus(); // lock Boost/EQ controls for the 4-second probe
        var performed = false;
        try
        {
            _apo.ApplyDiagnosticAttenuation(testSettings);
            performed = true;
            _engineStatus.ForeColor = Gold;
            _engineStatus.Text = "TESTING -18 dB ATTENUATION · Your playing audio should " +
                "be much quieter for four seconds if Equalizer APO is really attached.";
            await Task.Delay(4000);
        }
        catch (Exception ex)
        {
            ShowEngineError(ex);
        }
        finally
        {
            // Restore ONLY the exact temporary test configuration. A changed
            // APO file is preserved instead of being silently overwritten.
            try
            {
                if (!_apo.RecoverInterruptedDiagnostic(testSettings) && performed)
                    throw new InvalidOperationException(
                        "The temporary audio test was changed externally. " +
                        "Original settings were not overwritten; inspect Equalizer APO.");
            }
            catch (Exception ex) { ShowEngineError(ex); performed = false; }
            _diagnosticRunning = false;
            _audioPathTestAction.Enabled = true;
            RefreshEngineStatus();
        }

        if (performed)
        {
            _lastAudioTestHeard = MessageBox.Show(
                "Did the sound become clearly QUIETER during the four-second test? " +
                "If not, Equalizer APO may not be attached to your actual Windows " +
                "playback output, or the app uses exclusive mode.",
                "Did LouderMe change your sound?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

            _engineStatus.ForeColor = _lastAudioTestHeard.Value ? Cyan : Red;
            _engineStatus.Text = _lastAudioTestHeard.Value
                ? "You confirmed audible -18 dB attenuation. " +
                  "The current APO output path appears active; boost is still " +
                  "a requested gain and may clip/compress loud recordings."
                : "NO AUDIBLE EFFECT REPORTED. LouderMe did not demonstrate audio " +
                  "processing on your selected output. Open APO Configurator, " +
                  "select that device and restart Windows if required.";
        }
    }

    private async Task ConnectEngineAsync()
    {
        if (!_apo.GetStatus().Installed)
        {
            MessageBox.Show(
                "Install Equalizer APO first, attach it to the active playback " +
                "device in Configurator, and restart Windows if requested.",
                "LouderMe audio engine", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var choice = MessageBox.Show(
            "LouderMe needs one-time administrator permission to add its own " +
            "include to Equalizer APO's configuration and grant your Windows " +
            "account write access to LouderMe.txt ONLY. Existing APO settings " +
            "are backed up; other audio settings are not replaced.\\n\\n" +
            "Windows will ask for approval. Continue?",
            "Connect LouderMe audio processing",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (choice != DialogResult.Yes) return;

        _connectEngineAction.Enabled = false;
        try
        {
            var sid = WindowsIdentity.GetCurrent().User?.Value
                ?? throw new InvalidOperationException("Current Windows SID unavailable.");
            var start = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? Application.ExecutablePath,
                Verb = "runas",
                UseShellExecute = true,
            };
            start.ArgumentList.Add("--setup-apo");
            start.ArgumentList.Add(sid);
            using var process = Process.Start(start)
                ?? throw new InvalidOperationException("Windows setup could not be started.");
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                _engineStatus.ForeColor = Red;
                _engineStatus.Text = "Audio connection setup failed or was cancelled. " +
                    "Check the Windows setup error; audio processing is not confirmed.";
                return;
            }

            ApplyProcessing();
            RefreshEngineStatus();
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            _engineStatus.ForeColor = Gold;
            _engineStatus.Text = "One-time audio activation was cancelled. Boost/EQ not verified.";
        }
        catch (Exception ex)
        {
            ShowEngineError(ex);
        }
        finally
        {
            _connectEngineAction.Enabled = true;
        }
    }

    private void RefreshEngineStatus()
    {
        var status = _apo.GetStatus(_settings);
        _connectEngineAction.Enabled = status.Installed;
        _audioPathTestAction.Enabled = status.Configured && status.MatchesSettings
            && !_diagnosticRunning;
        // Do not offer apparently functional effect controls when no processing
        // backend exists. Endpoint volume remains usable.
        // A listening test must not allow slider edits to change the saved
        // settings or regenerate the APO file before it has been restored.
        var controlsEnabled = status.Installed && !_diagnosticRunning;
        _boostEnabled.Enabled = controlsEnabled;
        _boost.Enabled = controlsEnabled;
        if (_boostQuickActions is not null) _boostQuickActions.Enabled = controlsEnabled;
        _eqEnabled.Enabled = controlsEnabled;
        _preset.Enabled = controlsEnabled;
        _eqWave.Enabled = controlsEnabled;
        foreach (var band in _eqBands)
            if (band is not null) band.Enabled = controlsEnabled;

        if (!status.Installed)
        {
            _engineStatus.ForeColor = Gold;
            _engineStatus.Text =
                "NOT ACTIVE · Boost/EQ unavailable until Equalizer APO is installed, " +
                "the output is selected in Configurator and Windows is restarted if required. " +
                "The Windows 0–100% volume control remains independent.";
            return;
        }

        if (!status.Configured || !status.MatchesSettings)
        {
            _engineStatus.ForeColor = Gold;
            _engineStatus.Text = status.Message +
                " Click Connect Boost/EQ for one-time access setup if Windows denies writes.";
            return;
        }

        _engineStatus.ForeColor = Gold;
        _engineStatus.Text =
            $"APO CONFIG WRITTEN, SOUND NOT VERIFIED · Target: " +
            $"{(_settings.BoostEnabled ? _settings.BoostPercent : 100)}% " +
            $"({(_settings.BoostEnabled ? BoostMath.PercentToDb(_settings.BoostPercent) : 0):+0.00;-0.00;0.00} dB). " +
            "If sound is unchanged, attach Equalizer APO to the selected playback device " +
            "in Configurator, check Windows audio enhancements and restart if requested.";
    }

    private void UpdateEqValueLabels()
    {
        for (var i = 0; i < _eqBands.Length; i++)
            _eqBandValues[i].Text = $"{_eqBands[i].Value:+0.0;-0.0;0.0} dB";
    }

    private async Task CheckForUpdatesAsync(bool silent)
    {
        if (DeveloperMode.Enabled) return; // A developer test cannot install over the public app.
        try
        {
            if (!silent)
            {
                _updateAction.Enabled = false;
                _updateStatus.ForeColor = Muted;
                _updateStatus.Text = "Checking stable channel…";
            }

            _lastUpdateCheck = await _updates.CheckAsync();

            if (_lastUpdateCheck.UpdateAvailable)
            {
                _updateStatus.ForeColor = Gold;
                _updateStatus.Text = $"v{_lastUpdateCheck.LatestVersion} available";
                _updateAction.Text = $"Install v{_lastUpdateCheck.LatestVersion}";
            }
            else
            {
                _updateStatus.ForeColor = Cyan;
                _updateStatus.Text = $"v{_updates.CurrentVersionText} · current";
                _updateAction.Text = "Check updates";
            }
        }
        catch (Exception ex)
        {
            if (!silent)
            {
                _updateStatus.ForeColor = Red;
                _updateStatus.Text = "Update check failed";
                MessageBox.Show(
                    ex.Message,
                    "LouderMe update",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        finally
        {
            _updateAction.Enabled = true;
        }
    }

    private async Task HandleUpdateActionAsync()
    {
        if (_lastUpdateCheck?.UpdateAvailable != true || _lastUpdateCheck.Manifest is null)
        {
            await CheckForUpdatesAsync(silent: false);
            return;
        }

        try
        {
            _updateAction.Enabled = false;
            _updateStatus.ForeColor = Gold;
            _updateStatus.Text = "Downloading update…";

            var progress = new Progress<int>(percent =>
            {
                _updateStatus.Text = $"Downloading · {percent}%";
            });

            var installer = await _updates.DownloadInstallerAsync(
                _lastUpdateCheck.Manifest,
                progress);

            _updateStatus.ForeColor = Cyan;
            _updateStatus.Text = "Verified · opening installer";
            DesktopUpdateService.LaunchInstaller(installer);
        }
        catch (Exception ex)
        {
            _updateStatus.ForeColor = Red;
            _updateStatus.Text = "Update failed";
            MessageBox.Show(
                ex.Message,
                "LouderMe update",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        finally
        {
            _updateAction.Enabled = true;
        }
    }

    private void ShowEngineError(Exception ex)
    {
        _engineStatus.Text = "Audio engine error: " + ex.Message;
        _engineStatus.ForeColor = Red;
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
