using System.Drawing;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class GaragePage
    {
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private TableLayoutPanel columns;

        // Presets (left)
        private TableLayoutPanel leftLayout;
        private Label lblPresets;
        private TableLayoutPanel presetAdd;
        private TextBox txtPreset;
        private ComboBox cmbBody;
        private Button btnAddPreset;
        private DataGridView gridPresets;
        private Panel presetBar;
        private Button btnDeletePreset;
        private Button btnExport;

        // Achievements (right)
        private TableLayoutPanel rightLayout;
        private Label lblAch;
        private TableLayoutPanel achAdd;
        private TextBox txtAch;
        private Button btnAddAch;
        private DataGridView gridAch;
        private Panel achBar;
        private Button btnDeleteAch;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblTitle = new Label();
            columns = new TableLayoutPanel();

            leftLayout = new TableLayoutPanel();
            lblPresets = new Label();
            presetAdd = new TableLayoutPanel();
            txtPreset = new TextBox();
            cmbBody = new ComboBox();
            btnAddPreset = new Button();
            gridPresets = new DataGridView();
            presetBar = new Panel();
            btnDeletePreset = new Button();
            btnExport = new Button();

            rightLayout = new TableLayoutPanel();
            lblAch = new Label();
            achAdd = new TableLayoutPanel();
            txtAch = new TextBox();
            btnAddAch = new Button();
            gridAch = new DataGridView();
            achBar = new Panel();
            btnDeleteAch = new Button();

            var pageColor = Theme.PageBg;
            var panelColor = Theme.Surface;

            SuspendLayout();

            this.BackColor = pageColor;
            this.Dock = DockStyle.Fill;

            rootLayout.Dock = DockStyle.Fill;
            rootLayout.BackColor = pageColor;
            rootLayout.Padding = new Padding(20);
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 2;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblTitle.Text = "GARAGE";
            lblTitle.AutoSize = true;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);

            columns.Dock = DockStyle.Fill;
            columns.BackColor = pageColor;
            columns.Margin = new Padding(0, 6, 0, 0);
            columns.ColumnCount = 2;
            columns.RowCount = 1;
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ===== LEFT: PRESETS =====
            ColumnLayout(leftLayout, pageColor, new Padding(0, 0, 10, 0));
            SectionHeader(lblPresets, "CAR PRESETS");

            presetAdd.Dock = DockStyle.Fill;
            presetAdd.BackColor = pageColor;
            presetAdd.Margin = new Padding(0, 4, 0, 0);
            presetAdd.ColumnCount = 3;
            presetAdd.RowCount = 1;
            presetAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            presetAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
            presetAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
            presetAdd.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            StyleInput(txtPreset);
            cmbBody.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cmbBody.Margin = new Padding(0, 12, 10, 0);
            cmbBody.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBody.FlatStyle = FlatStyle.Flat;
            cmbBody.BackColor = Theme.SurfaceAlt;
            cmbBody.ForeColor = Theme.TextPrimary;
            cmbBody.Font = new Font("Segoe UI", 10F);
            cmbBody.Items.AddRange(new object[]
            {
                "Octane", "Fennec", "Dominus", "Breakout", "Batmobile", "Nimbus",
                "Merc", "Endo", "Centio", "Animus GP", "Guardian GXT", "Imperator DT5"
            });
            cmbBody.SelectedIndex = 0;
            StyleAccentBtn(btnAddPreset, "ADD");

            presetAdd.Controls.Add(txtPreset, 0, 0);
            presetAdd.Controls.Add(cmbBody, 1, 0);
            presetAdd.Controls.Add(btnAddPreset, 2, 0);

            StyleGrid(gridPresets, panelColor);
            gridPresets.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPName", HeaderText = "NAME", FillWeight = 55, ReadOnly = true });
            gridPresets.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPBody", HeaderText = "BODY", FillWeight = 45, ReadOnly = true });

            presetBar.Dock = DockStyle.Fill;
            presetBar.BackColor = pageColor;
            StyleDangerBtn(btnDeletePreset, "DELETE");
            btnDeletePreset.Location = new Point(0, 8);
            StyleAltBtn(btnExport, "⭳ EXPORT");
            btnExport.Location = new Point(126, 8);
            presetBar.Controls.Add(btnDeletePreset);
            presetBar.Controls.Add(btnExport);

            leftLayout.Controls.Add(lblPresets, 0, 0);
            leftLayout.Controls.Add(presetAdd, 0, 1);
            leftLayout.Controls.Add(gridPresets, 0, 2);
            leftLayout.Controls.Add(presetBar, 0, 3);

            // ===== RIGHT: ACHIEVEMENTS =====
            ColumnLayout(rightLayout, pageColor, new Padding(10, 0, 0, 0));
            SectionHeader(lblAch, "ACHIEVEMENTS");

            achAdd.Dock = DockStyle.Fill;
            achAdd.BackColor = pageColor;
            achAdd.Margin = new Padding(0, 4, 0, 0);
            achAdd.ColumnCount = 2;
            achAdd.RowCount = 1;
            achAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            achAdd.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
            achAdd.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            StyleInput(txtAch);
            StyleAccentBtn(btnAddAch, "ADD");
            achAdd.Controls.Add(txtAch, 0, 0);
            achAdd.Controls.Add(btnAddAch, 1, 0);

            StyleGrid(gridAch, panelColor);
            gridAch.Columns.Add(new DataGridViewCheckBoxColumn { Name = "colADone", HeaderText = "", FillWeight = 10, Resizable = DataGridViewTriState.False });
            gridAch.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAText", HeaderText = "ACHIEVEMENT", FillWeight = 90, ReadOnly = true });

            achBar.Dock = DockStyle.Fill;
            achBar.BackColor = pageColor;
            StyleDangerBtn(btnDeleteAch, "DELETE");
            btnDeleteAch.Location = new Point(0, 8);
            achBar.Controls.Add(btnDeleteAch);

            rightLayout.Controls.Add(lblAch, 0, 0);
            rightLayout.Controls.Add(achAdd, 0, 1);
            rightLayout.Controls.Add(gridAch, 0, 2);
            rightLayout.Controls.Add(achBar, 0, 3);

            columns.Controls.Add(leftLayout, 0, 0);
            columns.Controls.Add(rightLayout, 1, 0);

            rootLayout.Controls.Add(lblTitle, 0, 0);
            rootLayout.Controls.Add(columns, 0, 1);
            this.Controls.Add(rootLayout);

            ResumeLayout(false);
            PerformLayout();
        }

        private void ColumnLayout(TableLayoutPanel t, Color bg, Padding margin)
        {
            t.Dock = DockStyle.Fill;
            t.BackColor = bg;
            t.Margin = margin;
            t.ColumnCount = 1;
            t.RowCount = 4;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
        }

        private void SectionHeader(Label l, string text)
        {
            l.Text = text;
            l.Dock = DockStyle.Fill;
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.ForeColor = Theme.AccentSoft;
            l.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        }

        private void StyleInput(TextBox t)
        {
            t.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            t.Height = 32;
            t.Margin = new Padding(0, 12, 10, 0);
            t.BackColor = Theme.SurfaceAlt;
            t.ForeColor = Theme.TextPrimary;
            t.BorderStyle = BorderStyle.FixedSingle;
            t.Font = new Font("Segoe UI", 11F);
        }

        private void StyleAccentBtn(Button b, string text)
        {
            b.Anchor = AnchorStyles.Right;
            b.Size = new Size(84, 34);
            b.Margin = new Padding(0, 11, 0, 0);
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.ForeColor = Color.White;
            b.BackColor = Theme.Accent;
            b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }

        private void StyleDangerBtn(Button b, string text)
        {
            b.Size = new Size(116, 34);
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.ForeColor = Color.White;
            b.BackColor = Color.FromArgb(220, 60, 70);
            b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }

        private void StyleAltBtn(Button b, string text)
        {
            b.Size = new Size(116, 34);
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.ForeColor = Theme.TextPrimary;
            b.BackColor = Theme.SurfaceAlt;
            b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }

        private void StyleGrid(DataGridView grid, Color panelColor)
        {
            grid.Dock = DockStyle.Fill;
            grid.Margin = new Padding(0, 6, 0, 6);
            grid.BackgroundColor = panelColor;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 32;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.GridHeaderBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.DefaultCellStyle.BackColor = Theme.GridRowBg;
            grid.DefaultCellStyle.ForeColor = Theme.TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = Theme.Accent;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10.5F);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.GridAltBg;
            grid.GridColor = Theme.GridLines;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowTemplate.Height = 32;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
    }
}
