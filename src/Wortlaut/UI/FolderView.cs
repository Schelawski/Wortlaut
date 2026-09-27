using System.Windows.Forms.VisualStyles;
using Wortlaut.Core;

namespace Wortlaut.UI;

/// <summary>
/// Tab "Ganzer Ordner": transcribe all marked media files of a folder one after another.
/// </summary>
internal sealed class FolderView : UserControl, IRunView
{
    private const int CheckColumn = 0;
    private const int FileColumn = 1;
    private const int DurationColumn = 2;
    private const int StatusColumn = 3;

    private readonly IViewHost _host;
    private readonly BulkQueue _queue;
    private readonly List<FileRow> _rows = [];

    private readonly TextBox _folderBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly Button _chooseButton = UiStyle.CreateButton(UiText.ChooseFolder);
    private readonly Button _refreshButton = UiStyle.CreateButton(UiText.Refresh);
    private readonly CheckBox _skipExistingBox = new() { Text = UiText.SkipExisting, AutoSize = true, Margin = new Padding(3, 6, 18, 6) };
    private readonly CheckBox _includeSubfoldersBox = new() { Text = UiText.IncludeSubfolders, AutoSize = true, Margin = new Padding(3, 6, 3, 6) };
    private readonly DataGridView _grid = new();
    private readonly Button _startButton = UiStyle.CreateButton(UiText.TranscribeAll);
    private readonly Button _cancelButton = UiStyle.CreateButton(UiText.Cancel);
    private readonly ProgressBar _progressBar = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Height = 10, Maximum = 1000 };
    private readonly Label _countLabel = new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.Right,
        Font = UiStyle.CreateMonospaceFont(),
        ForeColor = UiStyle.MutedText,
    };
    private readonly LogBox _log = new();

    private RunState _runState = RunState.Idle;
    private OutputFormat _plannedFormat;
    private string? _loadedFolder;
    private CancellationTokenSource? _loadCancellation;

    // Progress of the running folder transcription.
    private int _runFinished;
    private int _runTotal;

    public FolderView(IViewHost host)
    {
        _host = host;
        _queue = new BulkQueue(host.Job);
        _plannedFormat = host.Settings.Format;
        Dock = DockStyle.Fill;
        Padding = new Padding(8);

        ConfigureGrid();
        BuildLayout();

        UiStyle.MakePrimary(_startButton);
        _cancelButton.Enabled = false;

        _folderBox.Text = host.Settings.LastFolder;
        _skipExistingBox.Checked = host.Settings.SkipExisting;
        _includeSubfoldersBox.Checked = host.Settings.IncludeSubfolders;

        _chooseButton.Click += (_, _) => ChooseFolder();
        _refreshButton.Click += async (_, _) => await LoadFolderAsync(reportMissing: true);
        _folderBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            await LoadFolderAsync(reportMissing: true);
        };
        _folderBox.Leave += async (_, _) =>
        {
            if (!string.Equals(_folderBox.Text.Trim(), _loadedFolder, StringComparison.OrdinalIgnoreCase))
                await LoadFolderAsync(reportMissing: false);
        };
        _skipExistingBox.CheckedChanged += (_, _) =>
        {
            _host.Settings.SkipExisting = _skipExistingBox.Checked;
            _host.SettingsChanged();
            RefreshPlannedStatuses();
        };
        _includeSubfoldersBox.CheckedChanged += async (_, _) =>
        {
            _host.Settings.IncludeSubfolders = _includeSubfoldersBox.Checked;
            _host.SettingsChanged();
            await LoadFolderAsync(reportMissing: false);
        };
        _startButton.Click += async (_, _) => await StartAsync();
        _cancelButton.Click += (_, _) => Cancel();

        _log.AppendMessage(UiText.LogReady);
        UpdateCountLabel();
    }

    /// <summary>Loads a folder, e.g. after drag and drop onto the window.</summary>
    public async Task SetFolderAsync(string folder)
    {
        _folderBox.Text = folder;
        await LoadFolderAsync(reportMissing: true);
    }

    public void SetRunState(RunState state)
    {
        _runState = state;
        var runningHere = state == RunState.RunningHere;
        _folderBox.ReadOnly = runningHere;
        _chooseButton.Enabled = !runningHere;
        _refreshButton.Enabled = !runningHere;
        _skipExistingBox.Enabled = !runningHere;
        _includeSubfoldersBox.Enabled = !runningHere;
        _grid.Columns[CheckColumn].ReadOnly = runningHere;
        _startButton.Enabled = state == RunState.Idle;
        _cancelButton.Enabled = runningHere;
    }

    public void OnWhisperSettingsChanged()
    {
        // Only the format changes the transcript names (and therefore which files are skipped).
        if (_runState != RunState.RunningHere && _host.Settings.Format != _plannedFormat)
            RefreshPlannedStatuses();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        UpdateGridMetrics();
        // The tab is loaded when it is shown for the first time (unless a folder was dropped before).
        if (_loadedFolder is null && !string.IsNullOrWhiteSpace(_folderBox.Text))
            await LoadFolderAsync(reportMissing: false);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        UpdateGridMetrics();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        UpdateGridMetrics();
    }

    // ----- Layout -----

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 6 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

        var caption = UiStyle.CreateCaption(UiText.FolderLabel);
        layout.Controls.Add(caption, 0, 0);
        layout.SetColumnSpan(caption, 3);

        layout.Controls.Add(_folderBox, 0, 1);
        layout.Controls.Add(_chooseButton, 1, 1);
        layout.Controls.Add(_refreshButton, 2, 1);

        var options = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0, 2, 0, 2) };
        options.Controls.Add(_skipExistingBox);
        options.Controls.Add(_includeSubfoldersBox);
        layout.Controls.Add(options, 0, 2);
        layout.SetColumnSpan(options, 3);

        _grid.Dock = DockStyle.Fill;
        layout.Controls.Add(_grid, 0, 3);
        layout.SetColumnSpan(_grid, 3);

        var actionRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4, RowCount = 1, Margin = new Padding(0, 6, 0, 6) };
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        actionRow.Controls.Add(_startButton, 0, 0);
        actionRow.Controls.Add(_cancelButton, 1, 0);
        _progressBar.Margin = new Padding(12, 3, 12, 3);
        actionRow.Controls.Add(_progressBar, 2, 0);
        actionRow.Controls.Add(_countLabel, 3, 0);
        layout.Controls.Add(actionRow, 0, 4);
        layout.SetColumnSpan(actionRow, 3);

        layout.Controls.Add(_log, 0, 5);
        layout.SetColumnSpan(_log, 3);

        Controls.Add(layout);
    }

    private void ConfigureGrid()
    {
        _grid.VirtualMode = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.AllowUserToOrderColumns = false;
        _grid.RowHeadersVisible = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.BackgroundColor = SystemColors.Window;
        _grid.BorderStyle = BorderStyle.None;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.GridColor = UiStyle.GridLine;
        _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Window;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = UiStyle.MutedText;
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = SystemColors.Window;
        _grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = UiStyle.MutedText;
        _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(2, 4, 2, 4);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
        _grid.DefaultCellStyle.SelectionForeColor = SystemColors.ControlText;
        _grid.DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
        _grid.ShowCellToolTips = true;

        _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Check", HeaderText = string.Empty, ToolTipText = UiText.ToggleAllTooltip, SortMode = DataGridViewColumnSortMode.NotSortable });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "File", HeaderText = UiText.ColumnFile, ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = UiText.ColumnDuration, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = UiText.ColumnStatus, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });

        _grid.CellValueNeeded += OnCellValueNeeded;
        _grid.CellValuePushed += OnCellValuePushed;
        _grid.CellPainting += OnCellPainting;
        _grid.CellToolTipTextNeeded += OnCellToolTipTextNeeded;
        _grid.ColumnHeaderMouseClick += OnColumnHeaderMouseClick;
        // Commit checkbox clicks immediately, not only when the cell is left.
        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty && _grid.CurrentCell?.ColumnIndex == CheckColumn)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
    }

    /// <summary>Sizes columns and rows from the current font, so they fit at every DPI.</summary>
    private void UpdateGridMetrics()
    {
        var font = _grid.Font;
        var padding = LogicalToDeviceUnits(24);
        _grid.Columns[CheckColumn].Width = font.Height + LogicalToDeviceUnits(16);
        _grid.Columns[DurationColumn].Width = TextRenderer.MeasureText("00:00:00", font).Width + padding;
        _grid.Columns[StatusColumn].Width = TextRenderer.MeasureText(UiText.RowExistsWillSkip, font).Width + padding;
        _grid.Columns[FileColumn].MinimumWidth = LogicalToDeviceUnits(120);

        var rowHeight = font.Height + LogicalToDeviceUnits(12);
        _grid.RowTemplate.Height = rowHeight;
        foreach (DataGridViewRow row in _grid.Rows)
            row.Height = rowHeight;
    }

    // ----- Loading -----

    private void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = UiText.FolderDialogTitle,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
        };

        if (Directory.Exists(_folderBox.Text.Trim()))
            dialog.InitialDirectory = _folderBox.Text.Trim();

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _ = SetFolderAsync(dialog.SelectedPath);
    }

    private async Task LoadFolderAsync(bool reportMissing)
    {
        if (_runState == RunState.RunningHere)
            return;

        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        _loadCancellation = cancellation;

        var folder = _folderBox.Text.Trim();
        _loadedFolder = folder;
        _rows.Clear();
        _grid.RowCount = 0;
        UpdateCountLabel();

        if (folder.Length == 0)
            return;

        if (!Directory.Exists(folder))
        {
            if (reportMissing)
                _host.ShowWarning(UiText.FolderMissing);
            return;
        }

        _host.Settings.LastFolder = folder;
        _host.SettingsChanged();

        var includeSubfolders = _includeSubfoldersBox.Checked;
        IReadOnlyList<string> files;
        IReadOnlyList<string> tempFolders;
        try
        {
            (files, tempFolders) = await Task.Run(
                () => (MediaFiles.FindInFolder(folder, includeSubfolders), MediaFiles.FindTempFolders(folder, includeSubfolders)),
                cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _log.AppendMessage(UiText.LogFolderError(folder, ex.Message));
            return;
        }

        if (cancellation.IsCancellationRequested)
            return;

        for (var index = 0; index < files.Count; index++)
            _rows.Add(new FileRow(index, files[index], Path.GetRelativePath(folder, files[index])));

        _grid.RowCount = _rows.Count;
        RefreshPlannedStatuses();

        _log.AppendMessage(UiText.LogFolderLoaded(_rows.Count, folder));
        // While a transcription runs, its own work folder exists legitimately.
        if (!_host.IsRunning)
        {
            foreach (var tempFolder in tempFolders)
                _log.AppendMessage(UiText.LogOrphanedTempFolder(tempFolder));
        }

        await ProbeDurationsAsync([.. _rows], cancellation.Token);
    }

    /// <summary>Fills the "Länge" column in the background.</summary>
    private async Task ProbeDurationsAsync(IReadOnlyList<FileRow> rows, CancellationToken cancellationToken)
    {
        // Rows are only changed on the UI thread.
        IProgress<(FileRow Row, TimeSpan? Duration)> progress = new Progress<(FileRow Row, TimeSpan? Duration)>(result =>
        {
            result.Row.Duration = result.Duration;
            result.Row.DurationProbed = true;
            if (IsCurrent(result.Row))
                _grid.InvalidateCell(DurationColumn, result.Row.Index);
        });

        try
        {
            await Parallel.ForEachAsync(
                rows,
                new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken },
                async (row, token) => progress.Report((row, await _host.DurationProbe.GetDurationAsync(row.MediaPath, token))));
        }
        catch (OperationCanceledException)
        {
            // A new folder was loaded.
        }
    }

    private bool IsCurrent(FileRow row) =>
        row.Index < _rows.Count && ReferenceEquals(_rows[row.Index], row);

    /// <summary>Recomputes "wartet" / "vorhanden, wird übersprungen" for all rows.</summary>
    private void RefreshPlannedStatuses()
    {
        _plannedFormat = _host.Settings.Format;
        var skipExisting = _skipExistingBox.Checked;
        foreach (var row in _rows)
        {
            row.Status = skipExisting && TargetExists(row) ? RowStatus.ExistsWillSkip : RowStatus.Waiting;
            row.Fraction = null;
            row.Detail = null;
        }

        _grid.Invalidate();
        UpdateCountLabel();
        UiStyle.SetProgressImmediately(_progressBar, 0);
    }

    private bool TargetExists(FileRow row) =>
        File.Exists(TranscriptionPaths.GetTargetPath(row.MediaPath, _host.Settings.Format));

    /// <summary>"0 von m" where m counts the marked files that will really be transcribed.</summary>
    private void UpdateCountLabel()
    {
        if (_runState == RunState.RunningHere)
        {
            _countLabel.Text = UiText.CountOf(_runFinished, _runTotal);
            return;
        }

        var skipExisting = _skipExistingBox.Checked;
        var total = _rows.Count(row => row.Selected && !(skipExisting && TargetExists(row)));
        _countLabel.Text = UiText.CountOf(0, total);
    }

    // ----- Running -----

    private async Task StartAsync()
    {
        var settings = _host.ValidateWhisperSettings();
        if (settings is null)
            return;

        if (!Directory.Exists(_folderBox.Text.Trim()))
        {
            _host.ShowWarning(UiText.FolderMissing);
            return;
        }

        RefreshPlannedStatuses();
        var selected = _rows.Where(row => row.Selected).ToList();
        if (selected.Count == 0)
        {
            _host.ShowWarning(UiText.NothingSelected);
            return;
        }

        if (selected.All(row => row.Status == RowStatus.ExistsWillSkip))
        {
            _host.ShowWarning(UiText.NothingToDo);
            return;
        }

        var skipExisting = _skipExistingBox.Checked;
        await _host.RunExclusiveAsync(this, cancellationToken => RunQueueAsync(selected, settings, skipExisting, cancellationToken));
    }

    private async Task RunQueueAsync(IReadOnlyList<FileRow> rows, WhisperSettings settings, bool skipExisting, CancellationToken cancellationToken)
    {
        _runFinished = 0;
        _runTotal = rows.Count(row => row.Status != RowStatus.ExistsWillSkip);
        UpdateCountLabel();
        UiStyle.SetProgressImmediately(_progressBar, 0);

        _log.AppendLine(string.Empty);
        _log.AppendMessage(UiText.LogBulkStart(rows.Count));

        var items = rows.Select(row => new BulkItem(row.MediaPath, row.Duration)).ToList();
        var progress = new Progress<BulkUpdate>(update => OnBulkUpdate(rows, update));
        var summary = await _queue.RunAsync(items, settings, skipExisting, progress, cancellationToken);

        _log.AppendMessage(UiText.LogBulkSummary(summary));
    }

    private void OnBulkUpdate(IReadOnlyList<FileRow> rows, BulkUpdate update)
    {
        var row = rows[update.ItemIndex];
        switch (update)
        {
            case BulkItemStarted started:
                row.Status = RowStatus.Running;
                row.Fraction = null;
                _runFinished = started.Position - 1;
                _runTotal = started.Total;
                _log.AppendMessage(UiText.LogBulkItem(started.Position, started.Total, row.DisplayName));
                ScrollIntoView(row);
                break;

            case BulkItemJobUpdate { Update: JobOutputUpdate output }:
                if (!string.IsNullOrWhiteSpace(output.Line))
                    _log.AppendLine(output.Line);
                break;

            case BulkItemJobUpdate { Update: JobMessageUpdate message }:
                _log.AppendMessage(UiText.LogMessage(message));
                break;

            case BulkItemJobUpdate { Update: JobProgressUpdate jobProgress }:
                row.Fraction = jobProgress.Fraction;
                break;

            case BulkItemFinished finished:
                row.Status = finished.Result.Outcome switch
                {
                    JobOutcome.Completed => RowStatus.Done,
                    JobOutcome.Skipped => RowStatus.Skipped,
                    JobOutcome.Cancelled => RowStatus.Cancelled,
                    _ => RowStatus.Failed,
                };
                row.Fraction = null;
                row.Detail = finished.Result.Outcome == JobOutcome.Failed ? UiText.ErrorText(finished.Result) : null;
                _runFinished = finished.Finished;
                _runTotal = finished.Total;
                _log.AppendMessage(UiText.LogResult(finished.Result));
                break;
        }

        if (IsCurrent(row))
            _grid.InvalidateRow(row.Index);

        UpdateOverallProgress(row);
        UpdateCountLabel();
    }

    private void ScrollIntoView(FileRow row)
    {
        if (!IsCurrent(row) || _grid.Rows[row.Index].Displayed)
            return;

        try
        {
            _grid.FirstDisplayedScrollingRowIndex = row.Index;
        }
        catch (InvalidOperationException)
        {
            // The grid is too small to show any row.
        }
    }

    private void UpdateOverallProgress(FileRow current)
    {
        if (_runTotal == 0)
            return;

        var running = current.Status == RowStatus.Running ? current.Fraction ?? 0 : 0;
        var overall = (_runFinished + running) / _runTotal;
        UiStyle.SetProgressImmediately(_progressBar, (int)Math.Round(overall * _progressBar.Maximum));
    }

    private void Cancel()
    {
        _cancelButton.Enabled = false;
        _log.AppendMessage(UiText.LogCancelling);
        _host.CancelRun();
    }

    // ----- Grid (virtual mode) -----

    private void OnCellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _rows.Count)
            return;

        var row = _rows[e.RowIndex];
        e.Value = e.ColumnIndex switch
        {
            CheckColumn => row.Selected,
            FileColumn => row.DisplayName,
            DurationColumn => row.Duration is { } duration ? UiText.FormatDuration(duration) : row.DurationProbed ? "–" : string.Empty,
            StatusColumn => StatusText(row),
            _ => null,
        };
    }

    private void OnCellValuePushed(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.ColumnIndex != CheckColumn || e.RowIndex < 0 || e.RowIndex >= _rows.Count || _runState == RunState.RunningHere)
            return;

        _rows[e.RowIndex].Selected = e.Value is true;
        _grid.InvalidateCell(StatusColumn, e.RowIndex);
        _grid.InvalidateCell(CheckColumn, -1);
        UpdateCountLabel();
    }

    private void OnColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex != CheckColumn || _runState == RunState.RunningHere || _rows.Count == 0)
            return;

        _grid.EndEdit();
        var select = !_rows.All(row => row.Selected);
        foreach (var row in _rows)
            row.Selected = select;
        _grid.Invalidate();
        UpdateCountLabel();
    }

    private void OnCellToolTipTextNeeded(object? sender, DataGridViewCellToolTipTextNeededEventArgs e)
    {
        if (e.RowIndex >= 0 && e.RowIndex < _rows.Count)
        {
            var row = _rows[e.RowIndex];
            e.ToolTipText = e.ColumnIndex switch
            {
                FileColumn => row.MediaPath,
                StatusColumn => row.Detail ?? string.Empty,
                _ => e.ToolTipText,
            };
        }
    }

    private static string StatusText(FileRow row) => row.Status switch
    {
        _ when !row.Selected && row.Status is RowStatus.Waiting or RowStatus.ExistsWillSkip => string.Empty,
        RowStatus.Waiting => UiText.RowWaiting,
        RowStatus.ExistsWillSkip => UiText.RowExistsWillSkip,
        RowStatus.Running => UiText.RowRunning(row.Fraction),
        RowStatus.Done => UiText.RowDone,
        RowStatus.Skipped => UiText.RowSkipped,
        RowStatus.Failed => UiText.RowFailed,
        RowStatus.Cancelled => UiText.RowCancelled,
        _ => string.Empty,
    };

    private static UiStyle.Badge StatusBadge(RowStatus status) => status switch
    {
        RowStatus.Running => UiStyle.Info,
        RowStatus.Done => UiStyle.Success,
        RowStatus.ExistsWillSkip or RowStatus.Skipped => UiStyle.Warning,
        RowStatus.Failed => UiStyle.Danger,
        _ => UiStyle.Neutral,
    };

    private void OnCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.Graphics is null)
            return;

        // Header of the checkbox column: a checkbox that shows whether all files are marked.
        if (e.RowIndex == -1 && e.ColumnIndex == CheckColumn)
        {
            e.PaintBackground(e.CellBounds, false);
            var state = _rows.Count > 0 && _rows.All(row => row.Selected) ? CheckBoxState.CheckedNormal
                : _rows.Any(row => row.Selected) ? CheckBoxState.MixedNormal
                : CheckBoxState.UncheckedNormal;
            var glyph = CheckBoxRenderer.GetGlyphSize(e.Graphics, state);
            var location = new Point(
                e.CellBounds.X + (e.CellBounds.Width - glyph.Width) / 2,
                e.CellBounds.Y + (e.CellBounds.Height - glyph.Height) / 2);
            CheckBoxRenderer.DrawCheckBox(e.Graphics, location, state);
            e.Handled = true;
            return;
        }

        // Status column: rounded badge instead of plain text.
        if (e.RowIndex >= 0 && e.RowIndex < _rows.Count && e.ColumnIndex == StatusColumn)
        {
            var row = _rows[e.RowIndex];
            var text = StatusText(row);
            e.PaintBackground(e.CellBounds, (e.State & DataGridViewElementStates.Selected) != 0);
            if (text.Length > 0 && e.CellStyle?.Font is { } font)
            {
                var textSize = TextRenderer.MeasureText(e.Graphics, text, font);
                var height = Math.Min(e.CellBounds.Height - LogicalToDeviceUnits(6), textSize.Height + LogicalToDeviceUnits(4));
                var bounds = new Rectangle(
                    e.CellBounds.X + LogicalToDeviceUnits(4),
                    e.CellBounds.Y + (e.CellBounds.Height - height) / 2,
                    Math.Min(textSize.Width + LogicalToDeviceUnits(14), e.CellBounds.Width - LogicalToDeviceUnits(8)),
                    height);
                UiStyle.DrawBadge(e.Graphics, bounds, text, font, StatusBadge(row.Status));
            }

            e.Handled = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
        }

        base.Dispose(disposing);
    }

    private enum RowStatus
    {
        Waiting,
        ExistsWillSkip,
        Running,
        Done,
        Skipped,
        Failed,
        Cancelled,
    }

    /// <summary>One file in the grid.</summary>
    private sealed class FileRow(int index, string mediaPath, string displayName)
    {
        public int Index { get; } = index;

        public string MediaPath { get; } = mediaPath;

        /// <summary>Path relative to the chosen folder (just the name unless subfolders are included).</summary>
        public string DisplayName { get; } = displayName;

        public bool Selected { get; set; } = true;

        public TimeSpan? Duration { get; set; }

        public bool DurationProbed { get; set; }

        public RowStatus Status { get; set; }

        public double? Fraction { get; set; }

        /// <summary>Error details, shown as tooltip of the status cell.</summary>
        public string? Detail { get; set; }
    }
}
