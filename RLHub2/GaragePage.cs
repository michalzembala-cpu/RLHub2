using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    public partial class GaragePage : Controls.ArenaControl
    {
        protected override string ArenaFile => "rl_bg.png";

        private readonly GarageStore _store = new();
        private List<CarPreset> _presets = new();
        private List<Achievement> _achievements = new();

        public GaragePage()
        {
            InitializeComponent();
            ApplyLanguage();

            btnAddPreset.Click += (s, e) => AddPreset();
            btnDeletePreset.Click += (s, e) => DeletePreset();
            btnExport.Click += (s, e) => ExportPresets();
            txtPreset.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; AddPreset(); }
            };

            btnAddAch.Click += (s, e) => AddAchievement();
            btnDeleteAch.Click += (s, e) => DeleteAchievement();
            txtAch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; AddAchievement(); }
            };

            gridAch.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (gridAch.IsCurrentCellDirty && gridAch.CurrentCell is DataGridViewCheckBoxCell)
                    gridAch.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            gridAch.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex != 0) return;
                if (gridAch.Rows[e.RowIndex].Tag is Achievement a)
                {
                    a.Done = Convert.ToBoolean(gridAch.Rows[e.RowIndex].Cells[0].Value);
                    _store.SaveAchievements(_achievements);
                }
            };

            Load += (s, e) => LoadData();
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.T("garage_title");
            lblPresets.Text = Localization.T("garage_presets");
            lblAch.Text = Localization.T("garage_ach");
            txtPreset.PlaceholderText = Localization.T("garage_preset_ph");
            txtAch.PlaceholderText = Localization.T("garage_ach_ph");
            btnAddPreset.Text = Localization.T("garage_add");
            btnAddAch.Text = Localization.T("garage_add");
            btnDeletePreset.Text = Localization.T("garage_delete");
            btnDeleteAch.Text = Localization.T("garage_delete");
            btnExport.Text = Localization.T("garage_export");
            gridPresets.Columns["colPName"].HeaderText = Localization.T("garage_col_name");
            gridPresets.Columns["colPBody"].HeaderText = Localization.T("garage_col_body");
            gridAch.Columns["colAText"].HeaderText = Localization.T("garage_col_ach");
        }

        private void LoadData()
        {
            _presets = _store.LoadPresets();
            _achievements = _store.LoadAchievements();
            RefreshPresets();
            RefreshAchievements();
        }

        // ===== PRESETS =====
        private void RefreshPresets()
        {
            gridPresets.Rows.Clear();
            foreach (var p in _presets)
            {
                int i = gridPresets.Rows.Add(p.Name, p.Body);
                gridPresets.Rows[i].Tag = p;
            }
        }

        private void AddPreset()
        {
            string name = txtPreset.Text.Trim();
            if (name.Length == 0) return;
            _presets.Add(new CarPreset { Name = name, Body = cmbBody.SelectedItem?.ToString() ?? "Octane" });
            _store.SavePresets(_presets);
            txtPreset.Clear();
            RefreshPresets();
            txtPreset.Focus();
        }

        private void DeletePreset()
        {
            if (gridPresets.SelectedRows.Count == 0 || gridPresets.SelectedRows[0].Tag is not CarPreset p) return;
            _presets.Remove(p);
            _store.SavePresets(_presets);
            RefreshPresets();
        }

        private void ExportPresets()
        {
            using var dlg = new SaveFileDialog { Filter = "JSON|*.json", FileName = "rl_presets.json" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _store.ExportPresets(dlg.FileName, _presets);
                Toast.Show(this, Localization.T("garage_exported"), ToastKind.Success);
            }
        }

        // ===== ACHIEVEMENTS =====
        private void RefreshAchievements()
        {
            gridAch.Rows.Clear();
            foreach (var a in _achievements)
            {
                int i = gridAch.Rows.Add(a.Done, a.Text);
                gridAch.Rows[i].Tag = a;
            }
        }

        private void AddAchievement()
        {
            string text = txtAch.Text.Trim();
            if (text.Length == 0) return;
            _achievements.Add(new Achievement { Text = text, Done = false });
            _store.SaveAchievements(_achievements);
            txtAch.Clear();
            RefreshAchievements();
            txtAch.Focus();
        }

        private void DeleteAchievement()
        {
            if (gridAch.SelectedRows.Count == 0 || gridAch.SelectedRows[0].Tag is not Achievement a) return;
            _achievements.Remove(a);
            _store.SaveAchievements(_achievements);
            RefreshAchievements();
        }
    }
}
