using System.Drawing;
using System.Text;

namespace LouderMeDesktop;

// Deliberately modeless: the owner selects real controls in the running app,
// not a screenshot or static list. Never injected into the public edition.
internal sealed class DeveloperDiagnosticsForm : Form
{
    private readonly MainForm _application;
    private readonly List<DeveloperAnnotation> _annotations;
    private readonly ListBox _items = new();
    private readonly TextBox _summary = new();
    private readonly Button _inspect = new();
    private readonly DeveloperInspectFilter _filter;
    private bool _canSave = true;
    private bool _inspecting;

    internal DeveloperDiagnosticsForm(MainForm application)
    {
        if (!DeveloperMode.Enabled)
            throw new InvalidOperationException("The public application cannot open Developer Diagnostics.");
        _application = application;
        try { _annotations = DeveloperAudit.LoadNotes(); }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            _annotations = new();
            _canSave = false;
            MessageBox.Show("Saved owner annotations could not be loaded. The original file is preserved. " +
                "Editing is disabled until the file is repaired.", "Developer annotation recovery",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        _filter = new DeveloperInspectFilter(this, SelectLiveControl);

        Text = "LouderMe · Developer Edition · Inspector";
        Icon = BrandAssets.LoadIcon("LouderMeDesktop.Assets.LouderMe.ico");
        StartPosition = FormStartPosition.Manual;
        var area = Screen.FromControl(application).WorkingArea;
        Bounds = new Rectangle(Math.Max(area.Left, area.Right - 610), area.Top + 28,
            Math.Min(590, area.Width), Math.Min(660, area.Height - 28));
        MinimumSize = new Size(430, 410);
        BackColor = Color.FromArgb(8, 15, 28);
        ForeColor = Color.FromArgb(230, 245, 255);
        Font = new Font("Segoe UI", 9.5f);

        var title = new Label
        {
            Text = "DEVELOPER EDITION   ·   WINDOWS",
            Dock = DockStyle.Top,
            Height = 44,
            ForeColor = Color.FromArgb(71, 210, 254),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            Padding = new Padding(12, 0, 0, 0),
        };
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 86, AutoScroll = true,
            WrapContents = true, Padding = new Padding(8, 4, 8, 4),
        };
        actions.Controls.Add(Action("Run audit", (_, _) => RunAudit()));
        _inspect.Text = "Select section / Add note";
        _inspect.AutoSize = true;
        _inspect.Click += (_, _) => ToggleInspect();
        actions.Controls.Add(_inspect);
        actions.Controls.Add(Action("Edit selected", (_, _) => EditSelected()));
        actions.Controls.Add(Action("Delete selected", (_, _) => DeleteSelected()));
        actions.Controls.Add(Action("Export JSON", (_, _) => Export(false)));
        actions.Controls.Add(Action("Export AI prompt", (_, _) => Export(true)));

        var accountInfo = new Label
        {
            Text = "Account: LouderMe currently has no public sign-in/provider. No mock login is used.",
            Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 6, 12, 0),
            ForeColor = Color.FromArgb(180, 200, 216),
        };
        var notesLabel = new Label
        {
            Text = "OWNER ANNOTATIONS  ·  click a saved note to edit or delete",
            Dock = DockStyle.Top, Height = 29, Padding = new Padding(12, 7, 0, 0),
            ForeColor = Color.FromArgb(240, 186, 73),
        };
        _items.Dock = DockStyle.Top;
        _items.Height = 128;
        _items.BackColor = Color.FromArgb(18, 32, 49);
        _items.ForeColor = Color.White;
        _items.BorderStyle = BorderStyle.FixedSingle;

        _summary.Dock = DockStyle.Fill;
        _summary.Multiline = true;
        _summary.ReadOnly = true;
        _summary.ScrollBars = ScrollBars.Both;
        _summary.WordWrap = true;
        _summary.BackColor = Color.FromArgb(12, 22, 36);
        _summary.ForeColor = Color.FromArgb(220, 236, 247);
        _summary.BorderStyle = BorderStyle.FixedSingle;

        Controls.Add(_summary);
        Controls.Add(_items);
        Controls.Add(notesLabel);
        Controls.Add(accountInfo);
        Controls.Add(actions);
        Controls.Add(title);

        Application.AddMessageFilter(_filter);
        FormClosed += (_, _) => Application.RemoveMessageFilter(_filter);
        Shown += (_, _) => RunAudit();
        RefreshList();
    }

    private static Button Action(string text, EventHandler handler)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            BackColor = Color.FromArgb(18, 38, 58),
            ForeColor = Color.FromArgb(227, 243, 253),
            FlatStyle = FlatStyle.Flat,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(4),
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(35, 80, 115);
        button.Click += handler;
        return button;
    }


    // Runtime evidence: actually display this WinForms twin and inspect live
    // application controls. This is NOT an acoustic hardware verification.
    internal void AssertRuntimeAudit()
    {
        Application.DoEvents();
        if (!IsHandleCreated || !Visible || !_application.IsHandleCreated)
            throw new InvalidOperationException("Developer inspector or actual app UI was not displayed.");
        var report = DeveloperAudit.Run(_application, _annotations);
        if (report.Findings.Any(f => f.Code is "UI_WINDOW_NOT_READY" or
            "UI_VOLUME_SLIDER_MISSING" or "UI_EQ_BANDS_INCOMPLETE" or "UI_ABOUT_NOT_VISIBLE"))
            throw new InvalidOperationException("Developer runtime audit found a broken required control.");
        if (_items is null || !_inspect.Visible || !_summary.Visible)
            throw new InvalidOperationException("Developer controls are not visible in the launched application.");
        if (!DeveloperMode.DataDirectory.EndsWith("LouderMe.Developer", StringComparison.Ordinal))
            throw new InvalidOperationException("Developer UI is not isolated from public storage.");
        var exported = DeveloperAudit.JsonReport(report);
        if (!exported.Contains("Developer Edition", StringComparison.Ordinal) ||
            !DeveloperAudit.AiRepairPrompt(report).Contains("LouderMe", StringComparison.Ordinal))
            throw new InvalidOperationException("Live developer report exports are unavailable.");
    }

    private void RefreshList()
    {
        var selected = (_items.SelectedItem as NoteEntry)?.Note.Id;
        _items.Items.Clear();
        foreach (var note in _annotations.OrderByDescending(n => n.UpdatedAtUtc))
            _items.Items.Add(new NoteEntry(note));
        for (var i = 0; i < _items.Items.Count; i++)
            if ((_items.Items[i] as NoteEntry)?.Note.Id == selected)
                _items.SelectedIndex = i;
    }

    private sealed class NoteEntry(DeveloperAnnotation note)
    {
        internal DeveloperAnnotation Note { get; } = note;
        public override string ToString() => $"[{Note.Severity}] {Note.Status} · {Note.Screen} / {Note.Label}";
    }

    private void RunAudit()
    {
        var report = DeveloperAudit.Run(_application, _annotations);
        var output = new StringBuilder()
            .AppendLine($"LouderMe {report.Version} · Windows · developer-only audit")
            .AppendLine($"Captured: {report.AuditedAtUtc:O}")
            .AppendLine("Evidence: installed UI/config checks, not verified sound output.")
            .AppendLine($"Findings: {report.Findings.Count}; owner annotations: {_annotations.Count}")
            .AppendLine();
        foreach (var finding in report.Findings)
            output.AppendLine($"{finding.Severity} · {finding.Code}\r\n{finding.Observation}\r\n");
        if (report.Findings.Count == 0)
            output.AppendLine("No invariant failures detected by available probes. Not a guarantee of bug-free operation.");
        _summary.Text = output.ToString();
    }

    private void ToggleInspect()
    {
        if (!_canSave)
        {
            MessageBox.Show("Repair the preserved annotations file before adding notes.", "Developer Edition");
            return;
        }
        _inspecting = !_inspecting;
        _filter.Enabled = _inspecting;
        _inspect.Text = _inspecting ? "Cancel selection (Esc)" : "Select section / Add note";
        _summary.Text = _inspecting
            ? "Click a real control in LouderMe (Home or About). Inspect mode consumes the click WITHOUT running the selected action. Press Esc to cancel."
            : "Selection cancelled. Run audit to refresh findings.";
    }

    private void SelectLiveControl(Control target)
    {
        if (!_inspecting) return;
        _inspecting = false;
        _filter.Enabled = false;
        _inspect.Text = "Select section / Add note";
        var form = target.FindForm();
        if (form is not MainForm && form is not AboutForm) return;
        var screen = form is AboutForm ? "About" : "Home";
        var label = string.IsNullOrWhiteSpace(target.Name)
            ? target.GetType().Name
            : target.Name;
        var locator = DeveloperAudit.Locator(target);
        var item = EditDialog(null, screen, locator, label);
        if (item is null) return;
        _annotations.Add(item);
        Persist();
    }

    private void EditSelected()
    {
        if (!_canSave) return;
        if (_items.SelectedItem is not NoteEntry selected) return;
        var current = selected.Note;
        var revised = EditDialog(current, current.Screen, current.Locator, current.Label);
        if (revised is null) return;
        _annotations[_annotations.FindIndex(n => n.Id == current.Id)] = revised;
        Persist();
    }

    private void DeleteSelected()
    {
        if (!_canSave || _items.SelectedItem is not NoteEntry selected) return;
        if (MessageBox.Show("Delete this saved annotation? This cannot be undone.",
                "Developer Edition", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        _annotations.RemoveAll(n => n.Id == selected.Note.Id);
        Persist();
    }

    private void Persist()
    {
        try { DeveloperAudit.SaveNotes(_annotations); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _canSave = false;
            MessageBox.Show("Unable to save annotations; verify the developer data directory before continuing.",
                "Developer data error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        RefreshList();
        RunAudit();
    }

    private DeveloperAnnotation? EditDialog(DeveloperAnnotation? existing, string screen, string locator, string label)
    {
        using var dialog = new Form
        {
            Text = existing is null ? "Add anchored owner observation" : "Edit owner observation",
            StartPosition = FormStartPosition.CenterParent, Size = new Size(560, 420),
            MinimumSize = new Size(430, 310), BackColor = BackColor, ForeColor = ForeColor,
        };
        var metadata = new TextBox
        {
            Text = $"Screen: {screen}\r\nTarget: {locator}", Multiline = true,
            Dock = DockStyle.Top, Height = 70, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
        };
        var comment = new TextBox
        {
            Text = existing?.Comment ?? "", Multiline = true, Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical, MaxLength = 12000,
        };
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 54 };
        var severity = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 70 };
        severity.Items.AddRange(new object[] { "P0", "P1", "P2" });
        severity.SelectedItem = existing?.Severity ?? "P1";
        var status = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 105 };
        status.Items.AddRange(new object[] { "Open", "Investigating", "Resolved" });
        status.SelectedItem = existing?.Status ?? "Open";
        var save = Action("Save note", (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(comment.Text))
            {
                MessageBox.Show(dialog, "Enter an observation before saving.");
                return;
            }
            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        });
        var cancel = Action("Cancel", (_, _) => { dialog.DialogResult = DialogResult.Cancel; dialog.Close(); });
        footer.Controls.Add(severity);
        footer.Controls.Add(status);
        footer.Controls.Add(save);
        footer.Controls.Add(cancel);
        dialog.Controls.Add(comment);
        dialog.Controls.Add(footer);
        dialog.Controls.Add(metadata);
        if (dialog.ShowDialog(this) != DialogResult.OK) return null;
        var now = DateTimeOffset.UtcNow;
        return new DeveloperAnnotation(existing?.Id ?? Guid.NewGuid(), screen, locator, label,
            comment.Text, existing?.CreatedAtUtc ?? now, now,
            severity.SelectedItem?.ToString() ?? "P1", status.SelectedItem?.ToString() ?? "Open");
    }

    private void Export(bool aiPrompt)
    {
        var report = DeveloperAudit.Run(_application, _annotations);
        var full = aiPrompt ? DeveloperAudit.AiRepairPrompt(report) : DeveloperAudit.JsonReport(report);

        // Export always requires a deliberate review; only owner-authored notes
        // can contain raw personal text. The owner may redact before writing.
        using var review = new Form
        {
            Text = "Review / redact developer export (local only)",
            StartPosition = FormStartPosition.CenterParent, Size = new Size(660, 540),
            MinimumSize = new Size(460, 360), BackColor = BackColor,
        };
        var text = new TextBox
        {
            Text = full, Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both,
            WordWrap = false, MaxLength = 1000000,
        };
        var notice = new Label
        {
            Text = "Review or redact your verbatim comments before export. No credentials or stack traces are automatically collected. No network upload.",
            Dock = DockStyle.Top, Height = 53, ForeColor = Color.White,
        };
        var save = Action("Save reviewed file", (_, _) => { review.DialogResult = DialogResult.OK; review.Close(); });
        save.Dock = DockStyle.Bottom;
        review.Controls.Add(text);
        review.Controls.Add(save);
        review.Controls.Add(notice);
        if (review.ShowDialog(this) != DialogResult.OK) return;
        using var picker = new SaveFileDialog
        {
            Filter = aiPrompt ? "Text prompt (*.txt)|*.txt" : "JSON report (*.json)|*.json",
            FileName = $"LouderMe-Developer-{(aiPrompt ? "AI-Prompt" : "Report")}-{DateTime.UtcNow:yyyyMMdd-HHmmss}" +
                (aiPrompt ? ".txt" : ".json"),
            OverwritePrompt = true,
        };
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        try { File.WriteAllText(picker.FileName, text.Text, Encoding.UTF8); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show("The report could not be written. Nothing was uploaded.",
                "Developer export", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    internal void CancelInspect()
    {
        if (!_inspecting) return;
        _inspecting = false;
        _filter.Enabled = false;
        _inspect.Text = "Select section / Add note";
    }

    // Controls receive WM_* messages only after this filter. Returning true
    // consumes inspection clicks so buttons never apply audio/startup changes.
    private sealed class DeveloperInspectFilter(
        DeveloperDiagnosticsForm inspector, Action<Control> onSelected) : IMessageFilter
    {
        internal bool Enabled { get; set; }

        public bool PreFilterMessage(ref Message m)
        {
            if (!Enabled) return false;
            if (m.Msg == 0x0100 && (int)m.WParam == 0x1B)
            {
                inspector.CancelInspect();
                return true;
            }
            if (m.Msg is not (0x0201 or 0x0202 or 0x0203 or 0x0204 or 0x0205 or 0x0206))
                return false;
            var control = Control.FromHandle(m.HWnd);
            var form = control?.FindForm();
            if (form is not MainForm && form is not AboutForm) return false;
            if (m.Msg is 0x0201 or 0x0204)
            {
                var p = Cursor.Position;
                Control selected = control!;
                // Walk to the exact deepest live UI component under the pointer.
                while (true)
                {
                    var child = selected.GetChildAtPoint(selected.PointToClient(p),
                        GetChildAtPointSkip.Invisible | GetChildAtPointSkip.Transparent);
                    if (child is null) break;
                    selected = child;
                }
                inspector.BeginInvoke((Action)(() => onSelected(selected)));
            }
            return true;
        }
    }
}
