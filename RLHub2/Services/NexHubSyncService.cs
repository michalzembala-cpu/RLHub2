using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RLHub2.Helpers;

namespace RLHub2.Services
{
    // Cron w tle — co ~5 min bierze najnowszy MMR z lokalnego store i push'uje do NexHub.
    // Timer w prostej pętli — nic egzotycznego, WinForms Timer nie zdałby przy zamkniętym oknie.
    public class NexHubSyncService : IDisposable
    {
        private readonly SettingsStore _settings = new();
        private readonly MmrStore _mmrStore = new();
        private CancellationTokenSource? _cts;

        public void Start()
        {
            if (_cts != null) return;
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => LoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        public void Dispose() => Stop();

        private async Task LoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await SyncOnceAsync();
                try { await Task.Delay(TimeSpan.FromMinutes(5), ct); }
                catch (TaskCanceledException) { break; }
            }
        }

        public async Task<bool> SyncOnceAsync()
        {
            var cfg = _settings.Load();
            if (!cfg.HubAutoSync || string.IsNullOrWhiteSpace(cfg.HubUrl) || string.IsNullOrWhiteSpace(cfg.HubToken))
                return false;

            var entries = _mmrStore.LoadForActive();
            if (entries.Count == 0) return false;

            int? m1 = LatestForMode(entries, "1v1");
            int? m2 = LatestForMode(entries, "2v2");
            int? m3 = LatestForMode(entries, "3v3");
            string rank = HighestOf(m1, m2, m3);

            // Winrate — z ostatnich N pozycji tego samego trybu (proxy: rosnie/spadło)
            var today = entries.Where(e => e.Timestamp.Date == DateTime.UtcNow.Date).ToList();

            var client = new NexHubClient(cfg.HubUrl, cfg.HubToken);
            var ok = await client.PutRocketLeagueAsync(
                mmr1v1: m1, mmr2v2: m2, mmr3v3: m3,
                rank: rank,
                gamesToday: today.Count > 0 ? today.Count : (int?)null);

            if (ok && !string.IsNullOrWhiteSpace(Accounts.ActiveName))
                await client.PutMetaAsync(pilotName: Accounts.ActiveName);

            return ok;
        }

        private static int? LatestForMode(System.Collections.Generic.List<Models.MmrEntry> entries, string mode)
        {
            var lastest = entries.Where(e => e.Mode == mode)
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefault();
            return lastest?.Value;
        }

        // Zamień MMR na rangę wg tabeli Rocket League Season 15+.
        private static string HighestOf(int? m1, int? m2, int? m3)
        {
            int best = new[] { m1 ?? 0, m2 ?? 0, m3 ?? 0 }.Max();
            if (best == 0) return "Unranked";
            return best switch
            {
                < 175 => "Bronze I",
                < 265 => "Bronze II",
                < 355 => "Bronze III",
                < 445 => "Silver I",
                < 535 => "Silver II",
                < 625 => "Silver III",
                < 715 => "Gold I",
                < 805 => "Gold II",
                < 895 => "Gold III",
                < 985 => "Platinum I",
                < 1075 => "Platinum II",
                < 1165 => "Platinum III",
                < 1255 => "Diamond I",
                < 1345 => "Diamond II",
                < 1435 => "Diamond III",
                < 1525 => "Champion I",
                < 1615 => "Champion II",
                < 1705 => "Champion III",
                < 1855 => "Grand Champion I",
                < 2005 => "Grand Champion II",
                < 2155 => "Grand Champion III",
                _ => "Supersonic Legend",
            };
        }
    }
}
