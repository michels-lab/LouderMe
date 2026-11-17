using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LouderMeDesktop;

/// <summary>
/// A genuinely painted dark selector; WinForms' stock DropDownList draws its
/// Windows-white selection field despite BackColor on many themes.
/// Menu entries are physical playback endpoints ONLY: following the Windows
/// default is a separate, explicit mode checkbox in MainForm.
/// </summary>
internal sealed class DarkOutputPicker : Control
{
    private readonly ContextMenuStrip _menu;
    private readonly ToolTip _tooltip = new();
    private IReadOnlyList<PlaybackOutputChoice> _choices = [];
    private string? _selectedId;
    private string _displayName = "Detecting playback output…";
    private string _status = "Volume and mute only";
    private bool _available = true;

    internal event Action<PlaybackOutputChoice>? OutputChosen;

    internal bool MenuOpen => _menu.Visible;
    internal bool FollowingDefault => _selectedId is null;
    internal string DisplayName => _displayName;
    internal bool OutputAvailable => _available;

    internal DarkOutputPicker()
    {
        SetStyle(ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.Selectable, true);
        TabStop = true;
        Size = new Size(400, 62);
        MinimumSize = new Size(200, 62);
        BackColor = BrandColors.Surface2;
        ForeColor = BrandColors.Text;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleName = "Choose a Windows playback device";
        AccessibleDescription =
            "Choose a specific output for its volume and mute. " +
            "The separate checkbox follows the Windows default.";
        _menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            ShowCheckMargin = false,
            AutoSize = true,
            BackColor = BrandColors.Surface,
            ForeColor = BrandColors.Text,
            Font = new Font("Segoe UI", 10f),
            Renderer = new ToolStripProfessionalRenderer(new DarkOutputMenuColors()),
        };
        _menu.Closed += (_, _) => Invalidate();
    }

    internal void SetState(
        IReadOnlyList<PlaybackOutputChoice> manualChoices,
        string? selectedId, string displayName, bool available)
    {
        ArgumentNullException.ThrowIfNull(manualChoices);
        _choices = manualChoices.Where(x => x.Id is not null).ToArray();
        _selectedId = selectedId;
        _available = available;
        _displayName = !available
            ? selectedId is null ? "No default output available" : "Selected device unavailable"
            : string.IsNullOrWhiteSpace(displayName)
                ? selectedId is null ? "Windows default output" : "Selected playback device"
                : displayName;
        _status = !available
            ? "DISCONNECTED · volume and mute paused"
            : selectedId is null
                ? "Following Windows default"
                : "Pinned device · default unchanged";
        _tooltip.SetToolTip(this, _displayName + Environment.NewLine + _status);
        Invalidate();
    }

    private void OpenDeviceMenu()
    {
        if (!Enabled || _menu.Visible) return;
        _menu.Items.Clear();
        if (_choices.Count == 0)
        {
            _menu.Items.Add(new ToolStripMenuItem("No playback devices detected")
            {
                Enabled = false,
                ForeColor = BrandColors.Muted,
            });
        }
        foreach (var option in _choices)
        {
            // Manual list never contains "Follow Windows default"; the
            // independent checkbox owns that transition.
            string category = OutputDeviceVisualPolicy.Category(option.Name);
            var item = new ToolStripMenuItem(
                (string.Equals(option.Id, _selectedId, StringComparison.Ordinal)
                    ? "●  " : "   ") + option.Name)
            {
                AutoSize = false,
                Width = Math.Max(245, Width - 4),
                Height = 38,
                BackColor = BrandColors.Surface,
                ForeColor = option.Available ? BrandColors.Text : BrandColors.Muted,
                Enabled = option.Available,
                ToolTipText = category + " · " +
                    (option.Available ? "Volume and mute" : "Unavailable"),
            };
            var selected = option;
            item.Click += (_, _) => OutputChosen?.Invoke(selected);
            _menu.Items.Add(item);
        }
        _menu.Show(this, new Point(0, Height + 3));
        Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        OpenDeviceMenu();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space or Keys.Down)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            OpenDeviceMenu();
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (!Enabled) _menu.Close();
        Invalidate();
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BrandColors.Surface);
        if (Width < 90 || Height < 30) return;
        var frame = new Rectangle(1, 1, Width - 3, Height - 3);
        using (var path = Rounded(frame, 9))
        using (var fill = new SolidBrush(BrandColors.Surface2))
        using (var edge = new Pen(
            Focused || _menu.Visible ? BrandColors.Cyan : BrandColors.Line,
            Focused || _menu.Visible ? 1.5f : 1f))
        {
            g.FillPath(fill, path);
            g.DrawPath(edge, path);
        }

        using var foreground = new SolidBrush(Enabled
            ? BrandColors.Cyan : BrandColors.Muted);
        using var muted = new SolidBrush(BrandColors.Muted);
        var iconRect = new Rectangle(15, 16, 28, 28);
        DrawOutputIcon(g, iconRect,
            OutputDeviceVisualPolicy.Category(_displayName), Enabled);

        using var titleFont = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
        using var detailFont = new Font("Segoe UI", 9f);
        using var badgeFont = new Font("Consolas", 8f, FontStyle.Bold);
        string badge = !_available ? "OFFLINE" :
            FollowingDefault ? "DEFAULT" : "MANUAL";
        int rightReserved = 114;
        int labelWidth = Math.Max(58, Width - 56 - rightReserved);
        TextRenderer.DrawText(g, _displayName, titleFont,
            new Rectangle(56, 10, labelWidth, 24),
            Enabled ? BrandColors.Text : BrandColors.Muted,
            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine |
            TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, _status, detailFont,
            new Rectangle(56, 33, Math.Max(75, Width - 100), 20),
            _available ? BrandColors.Muted : BrandColors.Gold,
            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine |
            TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, badge, badgeFont,
            new Rectangle(Math.Max(Width - 100, 70), 12, 70, 20),
            !_available ? BrandColors.Gold : BrandColors.Cyan,
            TextFormatFlags.Right | TextFormatFlags.SingleLine |
            TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        using var arrowPen = new Pen(Enabled ? BrandColors.Cyan : BrandColors.Muted, 1.8f);
        float cx = Width - 18f, cy = Height / 2f;
        g.DrawLines(arrowPen, [new PointF(cx - 5, cy - 2),
            new PointF(cx, cy + 3), new PointF(cx + 5, cy - 2)]);
    }

    private static void DrawOutputIcon(Graphics g, Rectangle r, string kind, bool enabled)
    {
        var color = enabled ? BrandColors.Cyan : BrandColors.Muted;
        using var p = new Pen(color, 1.7f)
        {
            StartCap = LineCap.Round, EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };
        using var b = new SolidBrush(Color.FromArgb(32, color));
        g.FillEllipse(b, r);
        int x = r.Left + 5, y = r.Top + 6;
        if (kind == "Bluetooth")
        {
            g.DrawLine(p, x + 8, y - 1, x + 8, y + 16);
            g.DrawLines(p, [new Point(x + 8, y - 1),
                new Point(x + 14, y + 4), new Point(x + 5, y + 12)]);
            g.DrawLines(p, [new Point(x + 8, y + 16),
                new Point(x + 14, y + 10), new Point(x + 5, y + 3)]);
        }
        else if (kind == "Headphones")
        {
            g.DrawArc(p, x + 1, y - 1, 16, 16, 180, 180);
            g.DrawLine(p, x + 1, y + 8, x + 1, y + 15);
            g.DrawLine(p, x + 17, y + 8, x + 17, y + 15);
            g.DrawRectangle(p, x, y + 11, 4, 6);
            g.DrawRectangle(p, x + 14, y + 11, 4, 6);
        }
        else if (kind == "HDMI")
        {
            g.DrawRectangle(p, x + 1, y + 1, 16, 12);
            g.DrawLine(p, x + 9, y + 13, x + 9, y + 17);
            g.DrawLine(p, x + 5, y + 17, x + 13, y + 17);
        }
        else
        {
            g.DrawLines(p, [new Point(x, y + 7), new Point(x + 4, y + 7),
                new Point(x + 9, y + 3), new Point(x + 9, y + 15),
                new Point(x + 4, y + 11), new Point(x, y + 11),
                new Point(x, y + 7)]);
            g.DrawArc(p, x + 5, y + 2, 12, 14, -55, 110);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _menu.Dispose();
            _tooltip.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal static class OutputDeviceVisualPolicy
{
    internal static string Category(string deviceName)
    {
        string n = deviceName.ToLowerInvariant();
        if (n.Contains("bluetooth") || n.Contains("a2dp") || n.Contains("ble "))
            return "Bluetooth";
        if (n.Contains("headphone") || n.Contains("headset") ||
            n.Contains("audífono") || n.Contains("auricular"))
            return "Headphones";
        if (n.Contains("hdmi") || n.Contains("displayport") || n.Contains("monitor"))
            return "HDMI";
        if (n.Contains("usb")) return "USB";
        return "Speakers";
    }

    internal static void RunSelfTests()
    {
        if (Category("Altavoces (Realtek(R) Audio)") != "Speakers" ||
            Category("Bluetooth Headphones") != "Bluetooth" ||
            Category("USB Audio Headset") != "Headphones" ||
            Category("HDMI Display") != "HDMI" ||
            Category("USB DAC") != "USB")
            throw new InvalidOperationException(
                "Dark audio device selector categories regressed.");
        Console.WriteLine(
            "PASS: dark output selector speaker/headphone/Bluetooth/USB/HDMI icon categories.");
    }
}

internal sealed class DarkOutputMenuColors : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => BrandColors.Surface;
    public override Color MenuBorder => BrandColors.Line;
    public override Color MenuItemBorder => BrandColors.Cyan;
    public override Color MenuItemSelected => BrandColors.Surface2;
    public override Color MenuItemSelectedGradientBegin => BrandColors.Surface2;
    public override Color MenuItemSelectedGradientEnd => BrandColors.Surface2;
    public override Color MenuItemPressedGradientBegin => BrandColors.Surface2;
    public override Color MenuItemPressedGradientEnd => BrandColors.Surface2;
    public override Color ImageMarginGradientBegin => BrandColors.Surface;
    public override Color ImageMarginGradientMiddle => BrandColors.Surface;
    public override Color ImageMarginGradientEnd => BrandColors.Surface;
    public override Color CheckBackground => BrandColors.Surface2;
    public override Color CheckSelectedBackground => BrandColors.Surface2;
}
