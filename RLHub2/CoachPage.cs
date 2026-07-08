using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    public partial class CoachPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "stadion1.jpg";

        private readonly ProfileServiceTracker _service = new();

        public CoachPage()
        {
            InitializeComponent();
            ApplyLanguage();
            Load += async (s, e) => await LoadCoach();
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.T("coach_title");
            lblAdviceHeader.Text = Localization.T("coach_advice");
            cardG.Title = Localization.T("coach_gpm");
            cardS.Title = Localization.T("coach_spm");
            cardA.Title = Localization.T("coach_apm");
            cardW.Title = Localization.T("coach_win");
        }

        private async System.Threading.Tasks.Task LoadCoach()
        {
            string nick = new SettingsStore().LoadTrackedNick();
            if (string.IsNullOrWhiteSpace(nick))
            {
                lblAdvice.Text = Localization.T("coach_no_nick");
                return;
            }

            lblAdvice.Text = Localization.T("coach_loading");
            try
            {
                var p = await _service.GetProfileAsync(nick);
                Analyze(p);
            }
            catch (ProfileServiceTracker.NoKeyException)
            {
                lblAdvice.Text = Localization.T("profile_no_key");
            }
            catch
            {
                lblAdvice.Text = Localization.T("profile_error");
            }
        }

        private void Analyze(Profile p)
        {
            int m = Math.Max(1, p.Matches);
            double gpm = p.Goals / (double)m;
            double spm = p.Saves / (double)m;
            double apm = p.Assists / (double)m;
            double win = p.Matches > 0 ? p.Wins * 100.0 / p.Matches : 0;

            cardG.Value = gpm.ToString("0.0");
            cardS.Value = spm.ToString("0.0");
            cardA.Value = apm.ToString("0.0");
            cardW.Value = win.ToString("0") + "%";

            var tips = new List<string>();
            if (spm < 1.2) tips.Add(Localization.T("coach_low_saves"));
            if (gpm < 0.8) tips.Add(Localization.T("coach_low_goals"));
            if (apm < 0.4) tips.Add(Localization.T("coach_low_assists"));
            if (gpm >= 1.3 && spm < 1.0) tips.Add(Localization.T("coach_aggressive"));
            if (tips.Count == 0) tips.Add(Localization.T("coach_good"));

            lblAdvice.Text = string.Join("\n\n", tips);
        }
    }
}
