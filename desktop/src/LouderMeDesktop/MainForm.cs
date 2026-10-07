using System.Diagnostics;
using System.Drawing.Drawing2D;

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
    private readonly System.Windows.Forms.Timer _deviceVolumeSyncTimer = new() { Interval = 500 };

    private readonly CheckBox _boostEnabled = new();
    private readonly TrackBar _boost = new();
    private readonly Label _boostValue = new();
    private readonly Label _boostDb = new();
    private readonly Label _engineStatus = new();
    private readonly CheckBox _eqEnabled = new();
    private readonly ComboBox _preset = new();
    private readonly NumericUpDown[] _eqBands = new NumericUpDown[7];
    private readonly Label[] _eqBandValues = new Label[7];
    private readonly AudioPulseControl _boostWave = new();
    private readonly EqCurveControl _eqWave = new();
    private readonly Label _updateStatus = new();
    private readonly Button _updateAction = new();
    private DesktopUpdateCheck? _lastUpdateCheck;

    private bool _loading;

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
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(820, 700);
        Size = new Size(980, 900);
        BackColor = Bg;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 10f);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildUi();
        LoadState();

        _deviceVolumeSyncTimer.Tick += (_, _) => RefreshDeviceVolumeFromSystem();
        _deviceVolumeSyncTimer.Start();

        Shown += async (_, _) => await CheckForUpdatesAsync(silent: true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _deviceVolumeSyncTimer.Stop();
            _deviceVolumeSyncTimer.Dispose();
            _audio.Dispose();
        }
        base.Dispose(disposing);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(28),
            BackColor = Bg,
            AutoScroll = true,
        };

        for (var i = 0; i < 6; i++)
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 18),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        header.Controls.Add(new WaveformMarkControl
        {
            Width = 138,
            Height = 84,
            Margin = new Padding(0, 0, 12, 0),
        }, 0, 0);

        var headerText = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
        };
        headerText.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "LouderMe",
            Font = new Font("Segoe UI Semibold", 27f, FontStyle.Bold),
            ForeColor = TextPrimary,
            Margin = new Padding(0, 4, 0, 1),
        });
        headerText.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "SOUND THAT LIFTS YOU",
            ForeColor = Gold,
            Font = new Font("Consolas", 9f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 3),
        });
        headerText.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Michel's Lab · Native Windows Audio Workspace",
            ForeColor = Muted,
            Font = new Font("Consolas", 8f, FontStyle.Bold),
        });
        header.Controls.Add(headerText, 1, 0);
        root.Controls.Add(header);

        BuildOutputCard(root);
        BuildBoostCard(root);
        BuildEqualizerCard(root);
        BuildStartupCard(root);
        BuildFooter(root);

        Controls.Add(root);
    }

    private void BuildOutputCard(TableLayoutPanel root)
    {
        var card = Card("DEVICE VOLUME · WINDOWS OUTPUT");
        var layout = (TableLayoutPanel)card.Controls[0];
        layout.RowCount = 5;

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

        _mute.Text = "Mute device volume";
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

        var note = new Label
        {
            AutoSize = true,
            ForeColor = Muted,
            MaximumSize = new Size(800, 0),
            Margin = new Padding(0, 8, 0, 0),
            Text =
                "Device Volume is the real Windows master output level (0–100%). " +
                "It is separate from LouderMe Global Boost (100–250% post-volume signal gain). " +
                "External Windows volume changes and active-device changes resync automatically.",
        };
        layout.SetColumnSpan(note, 2);
        layout.Controls.Add(note, 0, 4);
        root.Controls.Add(card);
    }

    private void BuildBoostCard(TableLayoutPanel root)
    {
        var card = Card("GLOBAL BOOST · SYSTEM-WIDE APO");
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
        foreach (var level in new[] { 100, 125, 150, 175, 200, 225, 250 })
        {
            var button = Button($"{level}%", Cyan);
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
        _engineStatus.MaximumSize = new Size(800, 0);
        layout.SetColumnSpan(_engineStatus, 2);
        layout.Controls.Add(_engineStatus, 0, 5);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 10, 0, 0),
        };
        var getEngine = Button("Get system-wide engine", Gold);
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
        refresh.Click += (_, _) => RefreshEngineStatus();
        actions.Controls.Add(getEngine);
        actions.Controls.Add(configure);
        actions.Controls.Add(refresh);
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
        root.Controls.Add(card);
    }

    private void BuildEqualizerCard(TableLayoutPanel root)
    {
        var card = Card("7-BAND EQUALIZER");
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

        _eqWave.Dock = DockStyle.Fill;
        _eqWave.Height = 68;
        _eqWave.Margin = new Padding(0, 8, 0, 8);
        layout.SetColumnSpan(_eqWave, 2);
        layout.Controls.Add(_eqWave, 0, 2);

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
                _loading = true;
                try { _preset.SelectedItem = "Custom"; }
                finally { _loading = false; }
                _settings.EqualizerGainsDb[index] = (float)value.Value;
                _eqWave.Gains = _settings.EqualizerGainsDb;
                ApplyProcessing();
            };
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
            layout.SetColumnSpan(bandRow, 2);
            layout.Controls.Add(bandRow, 0, 3 + i);
        }

        root.Controls.Add(card);
    }

    private void BuildStartupCard(TableLayoutPanel root)
    {
        var card = Card("STARTUP");
        var layout = (TableLayoutPanel)card.Controls[0];

        _startWithWindows.Text = "Start LouderMe with Windows";
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
        _updateAction.FlatStyle = FlatStyle.Flat;
        _updateAction.ForeColor = Cyan;
        _updateAction.BackColor = Surface;
        _updateAction.Padding = new Padding(10, 6, 10, 6);
        _updateAction.Margin = new Padding(0, 0, 10, 0);
        _updateAction.FlatAppearance.BorderColor = Line;
        _updateAction.FlatAppearance.BorderSize = 1;
        _updateAction.Click += async (_, _) => await HandleUpdateActionAsync();

        var about = Button("About LouderMe", Gold);
        about.Click += (_, _) =>
        {
            using var form = new AboutForm(_updates);
            form.ShowDialog(this);
        };

        _updateStatus.AutoSize = true;
        _updateStatus.ForeColor = Muted;
        _updateStatus.Text = $"v{_updates.CurrentVersionText} · stable";
        _updateStatus.Margin = new Padding(6, 9, 14, 0);

        footer.Controls.Add(refresh);
        footer.Controls.Add(_updateAction);
        footer.Controls.Add(_updateStatus);
        footer.Controls.Add(about);
        root.Controls.Add(footer);
    }

    private void LoadState()
    {
        _loading = true;
        try
        {
            try
            {
                _audio.Refresh();
                ApplyDeviceVolumeSnapshot(_audio.ReadSnapshot());
            }
            catch
            {
                _deviceValue.Text = "Device unavailable";
                _volumeValue.Text = "—";
                _volume.Enabled = false;
                _mute.Enabled = false;
            }

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

    private void RefreshDeviceVolumeFromSystem()
    {
        if (_loading || IsDisposed || !IsHandleCreated || _volume.Capture) return;

        try
        {
            var snapshot = _audio.ReadSnapshot();
            _loading = true;
            ApplyDeviceVolumeSnapshot(snapshot);
        }
        catch
        {
            _loading = true;
            _deviceValue.Text = "Device unavailable";
            _volumeValue.Text = "—";
            _volume.Enabled = false;
            _mute.Enabled = false;
        }
        finally
        {
            _loading = false;
        }
    }

    private void ApplyDeviceVolumeSnapshot(DeviceVolumeSnapshot snapshot)
    {
        _deviceValue.Text = snapshot.DeviceName;
        _volume.Enabled = true;
        _mute.Enabled = true;
        _volume.Value = Math.Clamp(
            snapshot.VolumePercent,
            _volume.Minimum,
            _volume.Maximum);
        _volumeValue.Text = $"{snapshot.VolumePercent}%";
        _mute.Checked = snapshot.Muted;
    }

    private void ApplyProcessing()
    {
        if (_loading) return;

        try
        {
            DesktopSettingsStore.Save(_settings);
            var status = _apo.GetStatus();

            if (!status.Installed)
            {
                RefreshEngineStatus();
                return;
            }

            _apo.Apply(_settings);
            RefreshEngineStatus();
        }
        catch (UnauthorizedAccessException)
        {
            _engineStatus.ForeColor = Gold;
            _engineStatus.Text =
                "Equalizer APO is installed, but Windows denied access to its config folder. " +
                "Run LouderMe as administrator once to link the managed configuration.";
        }
        catch (Exception ex)
        {
            ShowEngineError(ex);
        }
    }

    private void RefreshEngineStatus()
    {
        try
        {
            var status = _apo.GetStatus();

            if (!status.Installed)
            {
                _engineStatus.ForeColor = Gold;
                _engineStatus.Text =
                    "Boost engine required. Install Equalizer APO 1.4.2 x64 from the official SourceForge page, " +
                    "select the active playback device in Configurator, reboot if requested, then press Refresh engine. " +
                    "Official x64 SHA-256: 7403be7427bbe1936a40dded082829b6e217fc4f5990fee5cba501f0ae055afa";
                return;
            }

            if (!status.Configured)
            {
                _engineStatus.ForeColor = Gold;
                _engineStatus.Text =
                    status.Message + " Change any LouderMe boost/EQ control to link it.";
                return;
            }

            _engineStatus.ForeColor = Cyan;
            _engineStatus.Text =
                $"{status.Message} Target boost: {_settings.BoostPercent}% " +
                $"({BoostMath.PercentToDb(_settings.BoostPercent):+0.00;-0.00;0.00} dB).";
        }
        catch (UnauthorizedAccessException)
        {
            _engineStatus.ForeColor = Gold;
            _engineStatus.Text =
                "Equalizer APO is present, but Windows denied access to its configuration. " +
                "Device Volume remains available; run LouderMe as administrator once if you want to configure boost/EQ.";
        }
        catch (Exception ex)
        {
            ShowEngineError(ex);
        }
    }

    private void UpdateEqValueLabels()
    {
        for (var i = 0; i < _eqBands.Length; i++)
            _eqBandValues[i].Text = $"{_eqBands[i].Value:+0.0;-0.0;0.0} dB";
    }

    private async Task CheckForUpdatesAsync(bool silent)
    {
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
            BeginInvoke(new Action(Close));
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
