using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    public partial class GoalsPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "stadion2.jpg";

        private readonly GoalStore _store = new();
        private List<Goal> _goals = new();

        public GoalsPage()
        {
            InitializeComponent();
            ApplyLanguage();

            btnAdd.Click += (s, e) => AddGoal();
            btnDelete.Click += (s, e) => DeleteSelected();
            txtGoal.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; AddGoal(); }
            };

            grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewCheckBoxCell)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            grid.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex != 0) return;
                if (grid.Rows[e.RowIndex].Tag is Goal g)
                {
                    g.Done = Convert.ToBoolean(grid.Rows[e.RowIndex].Cells[0].Value);
                    _store.Save(_goals);
                    UpdateProgress();
                }
            };

            Load += (s, e) => LoadGoals();
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.T("goals_title");
            txtGoal.PlaceholderText = Localization.T("goals_add_ph");
            btnAdd.Text = Localization.T("goals_add");
            btnDelete.Text = Localization.T("goals_delete");
            grid.Columns["colGoal"].HeaderText = Localization.T("goals_col_goal");
        }

        private void LoadGoals()
        {
            _goals = _store.Load();
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            grid.Rows.Clear();
            foreach (var g in _goals)
            {
                int i = grid.Rows.Add(g.Done, g.Text);
                grid.Rows[i].Tag = g;
            }
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            int total = _goals.Count;
            int done = _goals.Count(g => g.Done);
            int pct = total > 0 ? (int)Math.Round(done * 100.0 / total) : 0;
            lblProgress.Text = total == 0
                ? Localization.T("goals_empty")
                : $"✓ {done} / {total}  •  {pct}% {Localization.T("goals_done_word")}";
        }

        private void AddGoal()
        {
            string text = txtGoal.Text.Trim();
            if (text.Length == 0) return;

            _goals.Add(new Goal { Text = text, Done = false });
            _store.Save(_goals);
            txtGoal.Clear();
            RefreshGrid();
            txtGoal.Focus();
        }

        private void DeleteSelected()
        {
            if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Tag is not Goal g)
                return;

            _goals.Remove(g);
            _store.Save(_goals);
            RefreshGrid();
        }
    }
}
