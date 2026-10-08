using System.Diagnostics;

namespace LouderMeDesktop;

internal sealed class AboutForm : Form
{
    private readonly DesktopUpdateService _updates;
    private DesktopUpdateCheck? _lastCheck;
    private readonly Label _updateStatus = new();
    private readonly Button _updateButton = new();

    private static readonly Color Bg = BrandColors.Bg;
    private static readonly Color Surface = BrandColors.Surface;
    private static readonly Color Surface2 = BrandColors.Surface2;
    private static readonly Color TextPrimary = BrandColors.Text;
    private static readonly Color Muted = BrandColors.Muted;
    private static readonly Color Cyan = BrandColors.Cyan;
    private static readonly Color Gold = BrandColors.Gold;

    public AboutForm(DesktopUpdateService updates)
    {
        _updates = updates;

        Text = "About LouderMe";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(480, 440);
        var workArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        Size = new Size(
            Math.Min(860, Math.Max(480, workArea.Width - 48)),
            Math.Min(840, Math.Max(440, workArea.Height - 48)));
        BackColor = Bg;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 10f);
        AutoScaleMode = AutoScaleMode.Dpi;
        ShowIcon = true;

        BuildUi();
    }

    private void BuildUi()
    {
        var root = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(22),
            BackColor = Bg,
        };

        root.Controls.Add(BuildProductHero());
        root.Controls.Add(BuildAuthorCard());
        root.Controls.Add(BuildUpdateCard());
        root.Controls.Add(BuildPrivacyCard());

        // Widths are based on the actual client viewport, never an 800px card
        // on a narrower monitor. The root remains vertically scrollable.
        void FitCardsToViewport()
        {
            if (root.IsDisposed) return;
            var available = Math.Max(340, root.ClientSize.Width - root.Padding.Horizontal - 22);
            root.SuspendLayout();
            try
            {
                foreach (Control card in root.Controls)
                {
                    if (card is not Panel) continue;
                    card.Width = available;
                    foreach (var row in card.Controls.OfType<TableLayoutPanel>())
                    {
                        foreach (var social in row.Controls.OfType<FlowLayoutPanel>())
                        {
                            if (social.Controls.OfType<Panel>().Any())
                            {
                                foreach (var panel in social.Controls.OfType<Panel>())
                                {
                                    panel.Width = Math.Max(260, available - card.Padding.Horizontal - 12);
                                    foreach (var label in panel.Controls.OfType<Label>())
                                    {
                                        if (label.Text is "Instagram" or "Facebook" or "LinkedIn" or "GitHub" or "Email")
                                            label.Width = Math.Max(120, panel.Width - 102);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally { root.ResumeLayout(true); }
        }
        root.ClientSizeChanged += (_, _) => FitCardsToViewport();
        Shown += (_, _) => FitCardsToViewport();

        var close = BrandButton("Close", Cyan);
        close.Click += (_, _) => Close();
        close.Margin = new Padding(0, 2, 0, 20);
        root.Controls.Add(close);

        Controls.Add(root);
    }

    private Control BuildProductHero()
    {
        var card = CardPanel(800);
        card.Padding = new Padding(24);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent,
        };

        var wave = new WaveformMarkControl
        {
            Width = 540,
            Height = 120,
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 2, 0, 6),
        };
        layout.Controls.Add(wave);

        layout.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "LouderMe Desktop",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 26f, FontStyle.Bold),
            Anchor = AnchorStyles.None,
        });

        layout.Controls.Add(new Label
        {
            AutoSize = true,
            Text = $"v{_updates.CurrentVersionText} · Windows x64 · Stable channel",
            ForeColor = Cyan,
            Font = new Font("Consolas", 9f, FontStyle.Bold),
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 6, 0, 4),
        });

        layout.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "SOUND THAT LIFTS YOU",
            ForeColor = Gold,
            Font = new Font("Consolas", 10f, FontStyle.Bold),
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 5, 0, 6),
        });

        layout.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(700, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "System-wide gain, equalization and transparent diagnostics in a focused Michel's Lab audio workspace.",
            ForeColor = Muted,
            Anchor = AnchorStyles.None,
        });

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildAuthorCard()
    {
        var card = CardPanel(800);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Color.Transparent,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var portrait = new PictureBox
        {
            Width = 96,
            Height = 128,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = BrandAssets.LoadBitmap("LouderMeDesktop.Assets.MichelDuarte.jpg"),
            Margin = new Padding(0, 0, 16, 8),
        };
        layout.SetRowSpan(portrait, 2);
        layout.Controls.Add(portrait, 0, 0);

        var author = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
        };
        author.Controls.Add(Eyebrow("ABOUT THE AUTHOR", Gold));
        author.Controls.Add(new Label
        {
            AutoSize = true,
            Text = DeveloperProfile.Developer,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold),
            Margin = new Padding(0, 7, 0, 2),
        });
        author.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Developer · Michel's Lab",
            ForeColor = Muted,
        });

        var studio = new PictureBox
        {
            Width = 260,
            Height = 82,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = BrandAssets.LoadBitmap("LouderMeDesktop.Assets.MichelsLabLockup.png"),
            Margin = new Padding(0, 10, 0, 0),
        };
        author.Controls.Add(studio);
        author.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Tools with identity",
            ForeColor = Gold,
            Font = new Font("Consolas", 9f, FontStyle.Bold),
            Margin = new Padding(0, 4, 0, 4),
        });
        layout.Controls.Add(author, 1, 0);

        var socials = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 12, 0, 0),
        };
        socials.Controls.Add(SocialRow(SocialNetwork.Instagram, "Instagram", DeveloperProfile.Instagram));
        socials.Controls.Add(SocialRow(SocialNetwork.Facebook, "Facebook", DeveloperProfile.Facebook));
        socials.Controls.Add(SocialRow(SocialNetwork.LinkedIn, "LinkedIn", DeveloperProfile.LinkedIn));
        socials.Controls.Add(SocialRow(SocialNetwork.GitHub, "GitHub", DeveloperProfile.GitHub));
        socials.Controls.Add(SocialRow(SocialNetwork.Email, "Email", $"mailto:{DeveloperProfile.Email}"));
        layout.SetColumnSpan(socials, 2);
        layout.Controls.Add(socials, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildUpdateCard()
    {
        var card = CardPanel(800);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Color.Transparent,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        layout.Controls.Add(Eyebrow("SOFTWARE", Cyan), 0, 0);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 2);

        _updateStatus.AutoSize = true;
        _updateStatus.ForeColor = Muted;
        _updateStatus.Text = $"Current version: v{_updates.CurrentVersionText}";
        _updateStatus.Margin = new Padding(0, 9, 10, 0);
        layout.Controls.Add(_updateStatus, 0, 1);

        _updateButton.Text = "Check for updates";
        _updateButton.AutoSize = true;
        _updateButton.FlatStyle = FlatStyle.Flat;
        _updateButton.ForeColor = Cyan;
        _updateButton.BackColor = Surface2;
        _updateButton.FlatAppearance.BorderColor = BrandColors.Line;
        _updateButton.Padding = new Padding(10, 5, 10, 5);
        _updateButton.Click += async (_, _) => await HandleUpdateActionAsync();
        layout.Controls.Add(_updateButton, 1, 1);

        var channel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(720, 0),
            ForeColor = Muted,
            Text = "LouderMe checks the public Michel's Lab stable Windows feed. Downloads are SHA-256 verified before Windows is asked to launch the installer.",
            Margin = new Padding(0, 9, 0, 0),
        };
        layout.SetColumnSpan(channel, 2);
        layout.Controls.Add(channel, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildPrivacyCard()
    {
        var card = CardPanel(800);
        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
        };

        stack.Controls.Add(Eyebrow("PRIVACY · LEGAL · THIRD-PARTY", Gold));
        stack.Controls.Add(Body(
            "LouderMe processes its settings locally. The Desktop app does not record or upload your audio. " +
            "Global Boost and EQ are written to the local Windows audio-effects configuration."));
        stack.Controls.Add(Body(
            "System-wide boost above the native Windows endpoint maximum uses user-installed Equalizer APO 1.4.2. " +
            "Equalizer APO is not bundled or redistributed by LouderMe. ASIO and WASAPI exclusive streams can bypass Windows system effects."));
        stack.Controls.Add(Body(
            "This build publishes SHA-256 integrity data. Authenticode publisher signing is not configured yet, so Windows SmartScreen may still show a reputation warning."));
        stack.Controls.Add(new Label
        {
            AutoSize = true,
            Text = DeveloperProfile.Copyright,
            ForeColor = TextPrimary,
            Margin = new Padding(0, 10, 0, 0),
        });
        stack.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Proprietary · All Rights Reserved",
            ForeColor = Gold,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        });

        var apo = BrandButton("Equalizer APO official project", Cyan);
        apo.Margin = new Padding(0, 10, 0, 0);
        apo.Click += (_, _) => OpenUrl("https://sourceforge.net/projects/equalizerapo/files/1.4.2/");
        stack.Controls.Add(apo);

        card.Controls.Add(stack);
        return card;
    }

    private async Task HandleUpdateActionAsync()
    {
        try
        {
            _updateButton.Enabled = false;

            if (_lastCheck?.UpdateAvailable == true && _lastCheck.Manifest is not null)
            {
                _updateStatus.Text = "Downloading verified installer…";
                var progress = new Progress<int>(p =>
                {
                    _updateStatus.Text = $"Downloading LouderMe v{_lastCheck.LatestVersion} · {p}%";
                });
                var installer = await _updates.DownloadInstallerAsync(_lastCheck.Manifest, progress);
                _updateStatus.Text = "Installer verified. Opening Windows installer…";
                DesktopUpdateService.LaunchInstaller(installer);
                return;
            }

            _updateStatus.Text = "Checking stable channel…";
            _lastCheck = await _updates.CheckAsync();
            _updateStatus.Text = _lastCheck.Message;
            _updateStatus.ForeColor = _lastCheck.UpdateAvailable ? Gold : Cyan;
            _updateButton.Text = _lastCheck.UpdateAvailable
                ? $"Install v{_lastCheck.LatestVersion}"
                : "Check again";
        }
        catch (Exception ex)
        {
            _updateStatus.ForeColor = BrandColors.Red;
            _updateStatus.Text = "Update check failed: " + ex.Message;
            _updateButton.Text = "Retry";
        }
        finally
        {
            _updateButton.Enabled = true;
        }
    }

    private static Panel SocialRow(SocialNetwork network, string name, string url)
    {
        var row = new Panel
        {
            Width = 720,
            Height = 46,
            BackColor = Surface2,
            Margin = new Padding(0, 4, 0, 0),
            Cursor = Cursors.Hand,
        };

        var badge = new SocialGlyphControl
        {
            Network = network,
            Location = new Point(6, 6),
            Cursor = Cursors.Hand,
        };
        var label = new Label
        {
            Text = name,
            Location = new Point(50, 0),
            Width = 600,
            Height = 46,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand,
        };
        var arrow = new Label
        {
            Text = "↗",
            Dock = DockStyle.Right,
            Width = 36,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Cyan,
            Cursor = Cursors.Hand,
        };

        void Open(object? _, EventArgs __) => OpenUrl(url);
        row.Click += Open;
        badge.Click += Open;
        label.Click += Open;
        arrow.Click += Open;

        row.Controls.Add(badge);
        row.Controls.Add(label);
        row.Controls.Add(arrow);
        return row;
    }

    private static Panel CardPanel(int width)
    {
        var panel = new Panel
        {
            Width = width,
            AutoSize = true,
            BackColor = Surface,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 14),
        };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(BrandColors.Line);
            var rect = panel.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            e.Graphics.DrawRectangle(pen, rect);
        };
        return panel;
    }

    private static Label Eyebrow(string text, Color color) => new()
    {
        AutoSize = true,
        Text = text,
        ForeColor = color,
        Font = new Font("Consolas", 9f, FontStyle.Bold),
    };

    private static Label Body(string text) => new()
    {
        AutoSize = true,
        MaximumSize = new Size(720, 0),
        Text = text,
        ForeColor = Muted,
        Margin = new Padding(0, 9, 0, 0),
    };

    private static Button BrandButton(string text, Color accent) => new()
    {
        AutoSize = true,
        Text = text,
        FlatStyle = FlatStyle.Flat,
        ForeColor = accent,
        BackColor = Surface2,
        Padding = new Padding(10, 5, 10, 5),
        FlatAppearance = { BorderColor = BrandColors.Line, BorderSize = 1 },
    };

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true,
        });
    }
}
