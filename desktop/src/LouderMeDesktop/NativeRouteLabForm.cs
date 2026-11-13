namespace LouderMeDesktop;

/// <summary>
/// Explicitly consented native Windows audio experiment. This is NOT the
/// Microsoft Store production Boost/EQ panel or a driver installer.
/// </summary>
internal sealed class NativeRouteLabForm : Form
{
    private readonly NativeRouteSession _session = new();
    private IReadOnlyList<NativeRouteEndpoint> _snapshot = [];
    private readonly ComboBox _virtual = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 450 };
    private readonly ComboBox _physical = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 450 };
    private readonly ComboBox _boost = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 125 };
    private readonly CheckBox _eqOn = new() { Text = "Enable own 7-band EQ", AutoSize = true };
    private readonly NumericUpDown[] _bands = new NumericUpDown[7];
    private readonly Button _refresh = new() { Text = "Refresh devices", AutoSize = true };
    private readonly Button _start = new() { Text = "Start native route (lab)", AutoSize = true };
    private readonly Button _stop = new() { Text = "Stop and release audio", AutoSize = true };
    private readonly Button _applyEq = new() { Text = "Apply 7-band EQ", AutoSize = true };
    private readonly Label _status = new() { AutoSize = true, MaximumSize = new Size(540, 0) };
    private bool _closing;
    private bool _changingBoost;

    internal NativeRouteLabForm()
    {
        Text = "LouderMe · Own Windows DSP route (experimental)";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(530, 530);
        Size = new Size(670, 740);
        AutoScroll = true;
        BackColor = BrandColors.Bg;
        ForeColor = BrandColors.Text;
        Font = new Font("Segoe UI", 10);
        Icon = BrandAssets.LoadIcon("LouderMeDesktop.Assets.LouderMe.ico");

        var root = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(18),
            BackColor = BrandColors.Bg
        };
        root.Controls.Add(new Label
        {
            Text = "Independent Windows audio route — controlled test",
            Font = new Font("Segoe UI Semibold", 15, FontStyle.Bold),
            AutoSize = true, ForeColor = BrandColors.Cyan
        });
        root.Controls.Add(Note(
            "The own virtual Windows driver must be installed separately. " +
            "LouderMe never installs a driver or changes the Windows default output here. " +
            "Route an external application to the LouderMe Virtual device yourself, " +
            "choose a different physical output and start at 100%. " +
            "WASAPI stream startup does NOT prove audible gain or Store approval."));
        root.Controls.Add(Title("1 · Virtual capture source"));
        root.Controls.Add(_virtual);
        root.Controls.Add(Title("2 · Physical output"));
        root.Controls.Add(_physical);
        root.Controls.Add(_refresh);
        root.Controls.Add(Title("3 · Own C++ DSP live controls"));
        foreach (var percent in new[] { 100, 125, 150, 175, 200, 225, 250, 275, 300 })
            _boost.Items.Add(percent);
        _boost.SelectedItem = 100;
        root.Controls.Add(_boost);
        root.Controls.Add(_eqOn);

        var bandsRow = new FlowLayoutPanel
        {
            Width = 570, Height = 94, WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        for (var i = 0; i < 7; i++)
        {
            var column = new FlowLayoutPanel
            {
                Width = 79, Height = 80, WrapContents = false,
                FlowDirection = FlowDirection.TopDown
            };
            column.Controls.Add(new Label
            {
                Text = new[] { "63", "160", "400", "1k", "2.5k", "6.3k", "16k" }[i],
                AutoSize = true, ForeColor = BrandColors.Muted
            });
            _bands[i] = new NumericUpDown
            {
                DecimalPlaces = 1, Minimum = -10, Maximum = 10, Increment = 0.5M,
                Width = 72, Value = 0
            };
            column.Controls.Add(_bands[i]);
            bandsRow.Controls.Add(column);
        }
        root.Controls.Add(bandsRow);
        root.Controls.Add(_applyEq);
        var operations = new FlowLayoutPanel
        {
            Width = 590, Height = 50, WrapContents = true
        };
        operations.Controls.Add(_start);
        operations.Controls.Add(_stop);
        root.Controls.Add(operations);

        _status.ForeColor = BrandColors.Gold;
        _status.Text = "No active native PCM route. Select an installed virtual source and physical output.";
        root.Controls.Add(_status);
        root.Controls.Add(Note(
            "No automatic reconnect. If Bluetooth, HDMI or USB changes endpoints, " +
            "the bridge exits instead of silently switching output. High gain can " +
            "clip or damage hearing: increase only after checking the actual volume."));
        Controls.Add(root);

        _session.StatusChanged += message =>
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke((Action)(() =>
                {
                    if (IsDisposed) return;
                    _status.Text = message;
                    UpdateControls();
                }));
            }
            catch (InvalidOperationException) { /* Window closing. */ }
        };

        _refresh.Click += (_, _) => RefreshDevices();
        _start.Click += async (_, _) => await StartRouteAsync();
        _stop.Click += async (_, _) => await StopRouteAsync();
        _applyEq.Click += async (_, _) => await ApplyEqAsync();
        _boost.SelectedIndexChanged += async (_, _) => await ApplyBoostAsync();

        FormClosing += async (_, e) =>
        {
            if (_closing) return;
            e.Cancel = true;
            _closing = true;
            try { await _session.DisposeAsync(); }
            finally
            {
                if (!IsDisposed) Close();
            }
        };
        RefreshDevices();
    }

    private static Label Title(string text) => new()
    {
        Text = text, AutoSize = true, ForeColor = BrandColors.Text,
        Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
        Margin = new Padding(2, 12, 0, 4)
    };

    private static Label Note(string text) => new()
    {
        Text = text, AutoSize = true,
        MaximumSize = new Size(550, 0),
        ForeColor = BrandColors.Muted,
        Margin = new Padding(2, 7, 0, 7)
    };

    private void RefreshDevices()
    {
        if (_session.IsRunning) return;
        _virtual.Items.Clear();
        _physical.Items.Clear();
        try
        {
            _snapshot = NativeRouteSession.Discover();
            foreach (var item in _snapshot)
            {
                if (item.IsVirtual) _virtual.Items.Add(item);
                else _physical.Items.Add(item);
            }
            if (_virtual.Items.Count == 1) _virtual.SelectedIndex = 0;
            if (_physical.Items.Count == 1) _physical.SelectedIndex = 0;
            _status.Text = _virtual.Items.Count == 0
                ? "Own LouderMe Virtual render device not installed. No native bridge session can start."
                : "Select the exact virtual source and a distinct physical output.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or
                                   System.Runtime.InteropServices.COMException)
        {
            _status.Text = "Windows audio-device enumeration failed. No native stream started.";
            _snapshot = [];
        }
        UpdateControls();
    }

    private void UpdateControls()
    {
        if (IsDisposed) return;
        var running = _session.IsRunning;
        _refresh.Enabled = !running;
        _virtual.Enabled = !running;
        _physical.Enabled = !running;
        _start.Enabled = !running && _virtual.SelectedItem is NativeRouteEndpoint &&
            _physical.SelectedItem is NativeRouteEndpoint;
        _stop.Enabled = running;
        _boost.Enabled = running;
        _eqOn.Enabled = running;
        _applyEq.Enabled = running;
        foreach (var b in _bands) b.Enabled = running;
    }

    private async Task StartRouteAsync()
    {
        if (_session.IsRunning || _virtual.SelectedItem is not NativeRouteEndpoint source ||
            _physical.SelectedItem is not NativeRouteEndpoint sink)
            return;
        var consent = MessageBox.Show(
            "Start an EXPERIMENTAL independent Windows route at 100% Boost?\n\n" +
            $"Source: {source.Name}\nPhysical output: {sink.Name}\n\n" +
            "LouderMe will not set the default playback endpoint. " +
            "A separate audio application must be routed to the selected virtual device. " +
            "No driver will be installed. The speaker/headphone output may be audible.",
            "LouderMe native DSP lab",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (consent != DialogResult.Yes) return;
        _start.Enabled = false;
        _status.Text = "Checking selected native source/output IDs…";
        try
        {
            await _session.StartAsync(_snapshot, source.Id, sink.Id);
            // Reset both logical control and live C++ engine to unity.
            _changingBoost = true;
            try { _boost.SelectedItem = 100; }
            finally { _changingBoost = false; }
            _eqOn.Checked = false;
            _status.Text =
                "Own virtual→physical WASAPI streams started at 100%. " +
                "Real audio from another app and acoustic gain are NOT verified.";
        }
        catch (Exception)
        {
            _status.Text = "Native session could not start safely. " +
                           "Check installed virtual driver, selected endpoints and signed-driver requirements.";
        }
        UpdateControls();
    }

    private async Task StopRouteAsync()
    {
        _stop.Enabled = false;
        try
        {
            await _session.StopAsync();
            _status.Text = "Stopped the native route. No default-device reassignment.";
        }
        catch (Exception)
        {
            _status.Text = "Native route shutdown encountered an error; verify the process is stopped.";
        }
        UpdateControls();
    }

    private async Task ApplyBoostAsync()
    {
        if (_changingBoost || !_session.IsRunning || _boost.SelectedItem is not int percent)
            return;
        if (percent > 150 &&
            MessageBox.Show(
                $"Experimental native Boost {percent}% can produce high signal levels and clipping. " +
                "Do you confirm this level for the selected physical output?",
                "Confirm high native gain", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            _changingBoost = true;
            try { _boost.SelectedItem = 100; }
            finally { _changingBoost = false; }
            percent = 100;
        }
        try
        {
            await _session.SetBoostAsync(percent);
            _status.Text = $"Sent {percent}% to the own C++ DSP callback. Audible gain not independently verified.";
        }
        catch (Exception)
        {
            _status.Text = "DSP control failed. Stop the native route and recheck devices.";
        }
        UpdateControls();
    }

    private async Task ApplyEqAsync()
    {
        if (!_session.IsRunning) return;
        try
        {
            var bands = _bands.Select(x => (float)x.Value).ToArray();
            await _session.SetEqualizerAsync(_eqOn.Checked, bands);
            _status.Text = _eqOn.Checked
                ? "Queued seven equalizer bands for own C++ audio callback."
                : "Own C++ equalizer bypass requested.";
        }
        catch (Exception)
        {
            _status.Text = "Native EQ command could not be sent. Check route state.";
        }
        UpdateControls();
    }
}
