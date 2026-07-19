using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    public partial class MMRPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "rl_bg.png";

        private readonly MmrStore _store = new();
        private List<MmrEntry> _entries = new();
        private readonly Stack<List<MmrEntry>> _undo = new();

        private string _range = "MONTH";
        private string _mode = "2v2";
        private MmrEntry? _editing;

        public MMRPage()
        {
            InitializeComponent();
            ApplyLanguage();

            card1v1.Click += (s, e) => OpenMode("1v1");
            card2v2.Click += (s, e) => OpenMode("2v2");
            card3v3.Click += (s, e) => OpenMode("3v3");

            btnFetch.Click += async (s, e) => await FetchMmrAsync();
            btnExport.Click += (s, e) => ExportData();
            btnImport.Click += (s, e) => ImportData();
            btnFolder.Click += (s, e) => OpenFolder();

            btnBack.Click += (s, e) => ShowSelection();

            btnWeek.Click += (s, e) => SetRange("WEEK");
            btnMonth.Click += (s, e) => SetRange("MONTH");
            btnSeason.Click += (s, e) => SetRange("SEASON");
            btnAll.Click += (s, e) => SetRange("ALL");

            btnEdit.Click += (s, e) => BeginEdit();
            btnDelete.Click += (s, e) => DeleteSelected();
            btnUndo.Click += (s, e) => Undo();
            btnCancelEdit.Click += (s, e) => CancelEdit();
            grid.CellDoubleClick += (s, e) => BeginEdit();

            txtMmr.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    AddOrSave();
                }
            };

            historyPanel.SizeChanged += (s, e) => ApplyRoundedRegion(historyPanel, 14);
            formPanel.SizeChanged += (s, e) => ApplyRoundedRegion(formPanel, 14);

            Load += (s, e) => LoadEntries();
        }

        private void ApplyLanguage()
        {
            lblSelTitle.Text = Localization.T("mmr_title");
            lblSelSub.Text = Localization.T("mmr_choose");

            btnBack.Text = Localization.T("mmr_back");
            btnWeek.Text = Localization.T("mmr_week");
            btnMonth.Text = Localization.T("mmr_month");
            btnSeason.Text = Localization.T("mmr_season");
            btnAll.Text = Localization.T("mmr_all");

            btnEdit.Text = Localization.T("mmr_edit_btn");
            btnDelete.Text = Localization.T("mmr_delete");
            btnUndo.Text = Localization.T("mmr_undo");
            btnCancelEdit.Text = Localization.T("mmr_cancel");

            btnFetch.Text = Localization.T("mmr_fetch");
            btnExport.Text = Localization.T("mmr_export");
            btnImport.Text = Localization.T("mmr_import");
            btnFolder.Text = Localization.T("mmr_folder");

            lblFormTitle.Text = _editing != null
                ? Localization.T("mmr_edit_title")
                : Localization.T("mmr_add_title");
            lblHint.Text = _editing != null
                ? Localization.T("mmr_hint_save")
                : Localization.T("mmr_hint_add");
            lblMmr.Text = Localization.T("mmr_col_mmr");

            if (grid.Columns["colDate"] is DataGridViewColumn cDate)
                cDate.HeaderText = Localization.T("mmr_col_date");
            if (grid.Columns["colMmr"] is DataGridViewColumn cMmr)
                cMmr.HeaderText = Localization.T("mmr_col_mmr");

            chart.EmptyTitle = Localization.T("mmr_empty");
            chart.EmptySub = Localization.T("mmr_empty_sub");
        }

        private static void ApplyRoundedRegion(Control c, int radius)
        {
            if (c.Width <= 0 || c.Height <= 0)
                return;

            var r = c.ClientRectangle;
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseAllFigures();
            c.Region = new Region(path);
        }

        private void LoadEntries()
        {
            _entries = _store.LoadForActive();
            ShowSelection();
        }

        // ===== VIEW SWITCHING =====
        private void ShowSelection()
        {
            CancelEdit();
            card1v1.Value = LatestText("1v1");
            card2v2.Value = LatestText("2v2");
            card3v3.Value = LatestText("3v3");

            detailView.Visible = false;
            selectionView.Visible = true;
        }

        private void OpenMode(string mode)
        {
            _mode = mode;

            var (yMin, yMax) = RangeForMode(mode);
            chart.Configure(yMin, yMax, AccentForMode(mode));

            lblDetailTitle.Text = $"MMR — {mode.ToUpperInvariant()}";

            var latest = LatestEntry(mode);
            txtMmr.Text = (latest?.Value ?? (yMin + yMax) / 2).ToString();

            CancelEdit();
            selectionView.Visible = false;
            detailView.Visible = true;

            SetRange("MONTH");
        }

        private static (int Min, int Max) RangeForMode(string mode) => mode switch
        {
            "1v1" => (500, 900),
            "2v2" => (700, 1100),
            "3v3" => (500, 900),
            _ => (0, 2000)
        };

        private static Color AccentForMode(string mode) => mode switch
        {
            "1v1" => Color.FromArgb(0, 140, 255),
            "2v2" => Color.FromArgb(150, 90, 255),
            "3v3" => Color.FromArgb(0, 200, 180),
            _ => Color.FromArgb(120, 60, 255)
        };

        private MmrEntry? LatestEntry(string mode) =>
            _entries.Where(e => e.Mode == mode).OrderBy(e => e.Timestamp).LastOrDefault();

        private string LatestText(string mode)
        {
            var e = LatestEntry(mode);
            return e != null ? e.Value.ToString() : "—";
        }

        // ===== RANGE =====
        private void SetRange(string range)
        {
            _range = range;

            foreach (var (b, key) in new[]
                     {
                         (btnWeek, "WEEK"), (btnMonth, "MONTH"),
                         (btnSeason, "SEASON"), (btnAll, "ALL")
                     })
            {
                bool on = key == range;
                b.BackColor = on ? Theme.Accent : Theme.SurfaceAlt;
                b.ForeColor = on ? Color.White : Theme.TextPrimary;
            }

            RefreshChart();
            RefreshGrid();
            UpdateStats();
        }

        private static readonly (string Name, int Mmr)[] Tiers =
        {
            ("Silver", 400), ("Gold", 550), ("Platinum", 700), ("Diamond", 850),
            ("Champion", 1000), ("Grand Champion", 1200), ("Supersonic Legend", 1400)
        };

        private void UpdateStats()
        {
            var ranged = CurrentEntries().OrderBy(e => e.Timestamp).ToList();
            var modeAll = _entries.Where(e => e.Mode == _mode).OrderBy(e => e.Timestamp).ToList();
            if (modeAll.Count == 0) { lblStats.Text = ""; return; }

            int peak = ranged.Count > 0 ? ranged.Max(e => e.Value) : modeAll.Max(e => e.Value);
            int avg = ranged.Count > 0 ? (int)Math.Round(ranged.Average(e => e.Value)) : modeAll[^1].Value;
            int change = ranged.Count > 0 ? ranged[^1].Value - ranged[0].Value : 0;

            int allPeak = modeAll.Max(e => e.Value);
            int current = modeAll[^1].Value;

            var weekAgo = DateTime.Now.AddDays(-7);
            var week = modeAll.Where(e => e.Timestamp >= weekAgo).ToList();
            int weekly = week.Count > 0 ? current - week[0].Value : 0;

            static string S(int v) => v > 0 ? "+" + v : v.ToString();

            string prediction = "";
            var next = Tiers.FirstOrDefault(t => t.Mmr > current);
            if (next.Name != null)
            {
                if (weekly > 0)
                {
                    int weeks = (int)Math.Ceiling((next.Mmr - current) / (double)weekly);
                    prediction = $"{Localization.T("mmr_next")}: {next.Name} ~{weeks}w";
                }
                else
                {
                    prediction = $"{Localization.T("mmr_next")}: {next.Name} ({Localization.T("mmr_next_none")})";
                }
            }

            lblStats.Text =
                $"{Localization.T("mmr_peak")} {peak}   {Localization.T("mmr_avg")} {avg}   Δ {S(change)}\n" +
                $"{Localization.T("mmr_peak_all")} {allPeak}   {Localization.T("mmr_week")} {S(weekly)}\n" +
                prediction;
        }

        // ===== AUTO-FETCH MMR FROM TRACKER.GG =====
        private async System.Threading.Tasks.Task FetchMmrAsync()
        {
            string nick = Accounts.ActiveName;
            if (string.IsNullOrWhiteSpace(nick))
            {
                Toast.Show(this, Localization.T("mmr_fetch_nonick"), ToastKind.Info, 4000);
                return;
            }

            btnFetch.Enabled = false;
            try
            {
                var profile = await new ProfileServiceTracker().GetProfileAsync(nick);

                PushUndo();
                int added = 0;
                foreach (var rank in profile.Ranks)
                {
                    if (rank.Mmr > 0 && (rank.Mode == "1v1" || rank.Mode == "2v2" || rank.Mode == "3v3"))
                    {
                        _entries.Add(new MmrEntry(DateTime.Now, rank.Mmr, rank.Mode) { Account = Accounts.ActiveName });
                        added++;
                    }
                }

                if (added > 0)
                {
                    _store.SaveForActive(_entries);
                    ShowSelection();
                    Toast.Show(this, Localization.T("mmr_fetched"), ToastKind.Success);
                }
                else
                {
                    if (_undo.Count > 0) _undo.Pop();
                    Toast.Show(this, Localization.T("mmr_fetch_fail"), ToastKind.Error);
                }
            }
            catch (ProfileServiceTracker.NoKeyException)
            {
                Toast.Show(this, Localization.T("profile_no_key"), ToastKind.Info, 4000);
            }
            catch
            {
                Toast.Show(this, Localization.T("mmr_fetch_fail"), ToastKind.Error);
            }
            finally
            {
                btnFetch.Enabled = true;
            }
        }

        // ===== DATA: EXPORT / IMPORT / FOLDER =====
        private void ExportData()
        {
            _store.SaveForActive(_entries);
            using var dlg = new SaveFileDialog { Filter = "JSON|*.json", FileName = "mmr_entries.json" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                System.IO.File.Copy(_store.FilePath, dlg.FileName, true);
                Toast.Show(this, Localization.T("mmr_exported"), ToastKind.Success);
            }
        }

        private void ImportData()
        {
            using var dlg = new OpenFileDialog { Filter = "JSON|*.json" };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                PushUndo();
                _entries = _store.ImportFrom(dlg.FileName);
                _store.SaveForActive(_entries);
                ShowSelection();
                Toast.Show(this, Localization.T("mmr_imported"), ToastKind.Success);
            }
            catch
            {
                Toast.Show(this, Localization.IsPolish ? "Błąd importu" : "Import error", ToastKind.Error);
            }
        }

        private void OpenFolder()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _store.DirectoryPath,
                    UseShellExecute = true
                });
            }
            catch { /* ignore */ }
        }

        private IEnumerable<MmrEntry> CurrentEntries()
        {
            DateTime now = DateTime.Now;
            var byMode = _entries.Where(e => e.Mode == _mode);

            return _range switch
            {
                "WEEK" => byMode.Where(e => e.Timestamp >= now.AddDays(-7)),
                "MONTH" => byMode.Where(e => e.Timestamp >= now.AddDays(-30)),
                "SEASON" => byMode.Where(e => e.Timestamp >= now.AddDays(-90)),
                _ => byMode
            };
        }

        // ===== REFRESH =====
        private void RefreshChart()
        {
            chart.SetEntries(CurrentEntries());
        }

        private void RefreshGrid()
        {
            grid.Rows.Clear();

            foreach (var entry in CurrentEntries().OrderByDescending(e => e.Timestamp))
            {
                int i = grid.Rows.Add(entry.Timestamp.ToString("yyyy-MM-dd"), entry.Value);
                grid.Rows[i].Tag = entry;
            }
        }

        // ===== ADD / EDIT / DELETE =====
        private void AddOrSave()
        {
            if (!int.TryParse(txtMmr.Text.Trim(), out int value) || value < 0)
            {
                Toast.Show(this, Localization.T("mmr_invalid"), ToastKind.Error);
                txtMmr.Focus();
                txtMmr.SelectAll();
                return;
            }

            PushUndo();

            if (_editing != null)
            {
                // Keep the original entry date; only the value changes.
                _editing.Value = value;
                CancelEdit();
            }
            else
            {
                _entries.Add(new MmrEntry(DateTime.Now, value, _mode) { Account = Accounts.ActiveName });
            }

            _store.SaveForActive(_entries);
            RefreshChart();
            RefreshGrid();
        }

        private void BeginEdit()
        {
            if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Tag is not MmrEntry entry)
                return;

            _editing = entry;
            txtMmr.Text = entry.Value.ToString();

            lblFormTitle.Text = Localization.T("mmr_edit_title");
            lblHint.Text = Localization.T("mmr_hint_save");
            btnCancelEdit.Visible = true;
            lblStats.Visible = false;
        }

        private void CancelEdit()
        {
            _editing = null;
            lblFormTitle.Text = Localization.T("mmr_add_title");
            lblHint.Text = Localization.T("mmr_hint_add");
            btnCancelEdit.Visible = false;
            lblStats.Visible = true;
        }

        private void DeleteSelected()
        {
            if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Tag is not MmrEntry entry)
                return;

            var confirm = MessageBox.Show(
                Localization.T("mmr_confirm_delete"), "MMR",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            PushUndo();
            _entries.Remove(entry);
            if (_editing == entry)
                CancelEdit();

            _store.SaveForActive(_entries);
            RefreshChart();
            RefreshGrid();
        }

        // ===== UNDO =====
        private void PushUndo()
        {
            _undo.Push(_entries
                .Select(e => new MmrEntry(e.Timestamp, e.Value, e.Mode) { Account = e.Account })
                .ToList());
        }

        private void Undo()
        {
            if (_undo.Count == 0)
                return;

            _entries = _undo.Pop();
            CancelEdit();
            _store.SaveForActive(_entries);
            RefreshChart();
            RefreshGrid();
        }

    }
}
