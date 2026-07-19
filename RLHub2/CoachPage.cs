using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;
using RLHub2.Services;

namespace RLHub2
{
    public partial class CoachPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "rl_bg.png";

        private readonly SettingsStore _settings = new();

        public CoachPage()
        {
            InitializeComponent();
            ApplyLanguage();

            // An AutoSize label ignores its docked width, so long advice lines ran off the
            // right edge. Cap the width to the panel so the text wraps instead.
            advicePanel.Resize += (s, e) => FitAdviceWidth();
            FitAdviceWidth();

            Load += async (s, e) => await LoadCoach();
        }

        private void FitAdviceWidth()
        {
            int w = advicePanel.ClientSize.Width - advicePanel.Padding.Horizontal;
            lblAdvice.MaximumSize = new Size(Math.Max(120, w), 0);
        }

        private bool Pl => Localization.IsPolish;
        private bool HasBallchasing => !string.IsNullOrWhiteSpace(_settings.LoadBallchasingKey());

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.T("coach_title");
            lblAdviceHeader.Text = Localization.T("coach_advice");
            tileOverall.Title = Pl ? "OCENA" : "OVERALL";
            tileAtk.Title = Pl ? "ATAK" : "ATTACK";
            tileDef.Title = Pl ? "OBRONA" : "DEFENSE";
            tileShot.Title = Pl ? "STRZAŁY" : "SHOOTING";
            tileBoost.Title = "BOOST";
            tilePos.Title = Pl ? "POZYCJA" : "POSITION";
        }

        private async Task LoadCoach()
        {
            RenderReport();

            if (HasBallchasing)
            {
                try { await Task.Run(() => new BallchasingSync().SyncAsync()); }
                catch { }
                if (!IsDisposed) RenderReport();
            }
            else
            {
                lblAdvice.Text = Pl
                    ? "Ustaw klucz Ballchasing w Ustawieniach, żeby Coach mógł analizować Twoje mecze."
                    : "Set your Ballchasing key in Settings so the Coach can analyze your matches.";
            }
        }

        private void RenderReport()
        {
            var r = CoachAnalysis.Build();
            lblAdvice.Text = r.Text;

            if (!r.HasData)
            {
                foreach (var t in new[] { tileOverall, tileAtk, tileDef, tileShot, tileBoost, tilePos })
                { t.Value = "—"; t.Subtitle = ""; }
                return;
            }

            tileOverall.Value = r.Overall.ToString();
            tileOverall.Subtitle = (Pl ? "Ocena " : "Grade ") + r.Grade;

            SetCat(tileAtk, r, "ATAK", "ATTACK");
            SetCat(tileDef, r, "OBRON", "DEF");
            SetCat(tileShot, r, "STRZA", "SHOOT");
            SetCat(tileBoost, r, "BOOST");
            SetCat(tilePos, r, "POZYC", "POSITION");
        }

        private static void SetCat(StatTile t, CoachReport r, params string[] keys)
        {
            var cat = r.Categories.FirstOrDefault(c =>
                keys.Any(k => c.Name.ToUpperInvariant().Contains(k)));

            if (!string.IsNullOrEmpty(cat.Name))
            {
                t.Value = cat.Score.ToString();
                t.Subtitle = cat.Val;
            }
            else
            {
                t.Value = "—";
                t.Subtitle = "";
            }
        }
    }
}
