using System;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;
using RLHub2.Services;

namespace RLHub2
{
    public partial class DashboardShell : Form
    {
        private UserControl? currentPage;
        private NavButton[] navButtons;
        private readonly SettingsStore _store = new();

        private bool collapsed = false;
        private const int ExpandedWidth = 220;
        private const int CollapsedWidth = 64;

        private readonly System.Windows.Forms.Timer animTimer;
        private int targetWidth;
        private bool _animating;

        // Hover-peek: temporarily expands a collapsed sidebar while the cursor is over it.
        private readonly System.Windows.Forms.Timer _peekTimer;
        private bool _peeking;

        private readonly System.Windows.Forms.Timer _pageAnim;
        private UserControl? _animPage;

        private NavButton? _activeButton;
        private Func<UserControl>? _currentFactory;
        private readonly ToolTip _navTip = new() { InitialDelay = 300, ReshowDelay = 100 };

        // A picture of the page, shown in its place while the bar animates over it. See FreezePage.
        private Bitmap? _frozen;

        // startPage overrides the remembered last page. Picking a profile is the start of a
        // fresh session, so it lands on Home rather than wherever the previous run left off.
        public DashboardShell(string? startPage = null)
        {
            var config = _store.Load();
            Localization.Initialize(config.Language?.ToLowerInvariant() == "en"
                ? AppLanguage.English : AppLanguage.Polish);
            Theme.Initialize(config.Theme?.ToLowerInvariant() == "light"
                ? AppTheme.Light : AppTheme.Dark);
            Theme.InitializeAccent(_store.LoadAccent());

            InitializeComponent();
            DoubleBuffered = true;

            navButtons = new[]
            {
                btnHome, btnMMR, btnRoad, btnCoach, btnSession, btnCs2, btnProfile, btnRecords,
                btnNews, btnTournaments, btnSeasons, btnSettings
            };

            ApplyThemeColors();

            // Double-buffer the panels that resize during the sidebar animation — kills flicker.
            EnableDoubleBuffer(sidebar);
            EnableDoubleBuffer(navPanel);
            EnableDoubleBuffer(header);
            EnableDoubleBuffer(panelContent);

            try { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { /* ignore */ }

            btnToggle.Click += (s, e) => ToggleSidebar();

            btnHome.Click += (s, e) => NavigateKey("home");
            btnMMR.Click += (s, e) => NavigateKey("mmr");
            btnRoad.Click += (s, e) => NavigateKey("road");
            btnCoach.Click += (s, e) => NavigateKey("coach");
            btnSession.Click += (s, e) => NavigateKey("session");
            btnCs2.Click += (s, e) => NavigateKey("cs2");
            btnRecords.Click += (s, e) => NavigateKey("records");
            btnNews.Click += (s, e) => NavigateKey("news");
            btnProfile.Click += (s, e) => NavigateKey("profile");
            btnTournaments.Click += (s, e) => NavigateKey("tournaments");
            btnSeasons.Click += (s, e) => NavigateKey("seasons");
            btnSettings.Click += (s, e) => NavigateKey("settings");

            animTimer = new System.Windows.Forms.Timer { Interval = 10 };
            animTimer.Tick += AnimTick;

            _pageAnim = new System.Windows.Forms.Timer { Interval = 10 };
            _pageAnim.Tick += PageAnimTick;

            _peekTimer = new System.Windows.Forms.Timer { Interval = 120 };
            _peekTimer.Tick += (s, e) => HoverPeekTick();
            _peekTimer.Start();

            navPanel.SizeChanged += (s, e) => { if (!_animating) ResizeNav(); };

            // The page owns a FIXED area — it starts where the collapsed bar ends and never
            // moves or resizes. The bar floats above it and expands over the page instead of
            // pushing it. That is what makes the animation cheap: the page (a full tree of
            // child windows, each one costly to move or relayout) is untouched, so a frame is
            // just the bar repainting itself.
            //
            // Form padding reserves the collapsed bar's column for the docked page; the bar is
            // undocked, so it ignores that padding and can grow across it.
            Padding = new Padding(CollapsedWidth, 0, 0, 0);
            sidebar.Dock = DockStyle.None;
            sidebar.Location = Point.Empty;
            sidebar.Height = ClientSize.Height;
            sidebar.BringToFront();
            Resize += (s, e) =>
            {
                sidebar.Height = ClientSize.Height;
                DropFrozen();   // the page relaid out; the snapshot no longer matches
            };

            // Restore sidebar collapsed state (instant, no animation).
            collapsed = config.SidebarCollapsed;
            targetWidth = collapsed ? CollapsedWidth : ExpandedWidth;
            sidebar.Width = targetWidth;
            if (collapsed)
            {
                lblLogo.Visible = false;
                foreach (var b in navButtons) b.Collapsed = true;
                SetSectionHeadersVisible(false);
            }

            Localization.LanguageChanged += () =>
            {
                if (IsHandleCreated)
                    BeginInvoke(new Action(OnLanguageChanged));
            };

            // switching game swaps the whole nav and lands on that game's home page
            Games.ActiveChanged += () =>
            {
                if (IsHandleCreated)
                    BeginInvoke(new Action(() =>
                    {
                        ApplyGame();
                        ResizeNav();
                        NavigateKey(Games.HomePage(Games.Active));
                    }));
            };

            // switching account reloads the current page with that account's data
            Accounts.ActiveChanged += () =>
            {
                if (IsHandleCreated)
                    BeginInvoke(new Action(() =>
                    {
                        if (_currentFactory != null) SwitchPage(_currentFactory());
                    }));
            };

            Theme.ThemeChanged += () =>
            {
                if (IsHandleCreated)
                    BeginInvoke(new Action(OnThemeChanged));
            };

            FormClosed += (s, e) => DropFrozen();

            ApplyGame();

            ApplyNavTexts();
            NavigateKey(startPage ?? config.LastPage);

            // Start the live Stats API listener once, on the UI thread, so matches
            // are logged whenever the app is open and Rocket League is running.
            // Also kick off a background ballchasing sync (upload replays + pull stats).
            Load += (s, e) =>
            {
                StatsApiClient.Instance.Start();

                // CS2 only talks to apps it has a config for, and it reads that config at
                // startup — so write it now and listen; whatever is running today will be
                // picked up the next time the game starts.
                if (Cs2Install.IsInstalled && !Cs2Install.IsConfigured)
                    Cs2Install.WriteConfig(Cs2GsiClient.Port);
                Cs2GsiClient.Instance.Start();
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    try { await new BallchasingSync().SyncAsync(); } catch { }
                });
            };
        }

        // WS_EX_COMPOSITED used to be set here to stop the sidebar animation flickering. It
        // works by repainting the ENTIRE window off-screen on every invalidation, which cost
        // ~100 ms a frame on this window — the animation ran at under 10 fps. It was only needed
        // because each frame resized the content and forced a full repaint anyway; now the
        // content keeps its size and merely slides, Windows blits it, and the flicker it was
        // hiding is gone at the source. Measured: 277 ms -> 31 ms per frame.
        //
        //   protected override CreateParams CreateParams { ... ExStyle |= 0x02000000 ... }

        private HomePage CreateHome()
        {
            var home = new HomePage();
            home.OpenNewsRequested += (s, e) => NavigateKey("news");
            return home;
        }

        private void NavigateKey(string key)
        {
            key = (key ?? "home").ToLowerInvariant();
            if (!BelongsToActiveGame(key)) key = Games.HomePage(Games.Active);

            switch (key)
            {
                case "mmr": Navigate("mmr", btnMMR, () => new MMRPage()); break;
                case "road": Navigate("road", btnRoad, () => new RoadPage()); break;
                case "coach": Navigate("coach", btnCoach, () => new CoachPage()); break;
                case "session": Navigate("session", btnSession, () => new SessionPage()); break;
                case "cs2": Navigate("cs2", btnCs2, () => new Cs2Page()); break;
                case "records": Navigate("records", btnRecords, () => new RecordsPage()); break;
                case "news": Navigate("news", btnNews, () => new NewsPage()); break;
                case "profile": Navigate("profile", btnProfile, () => new ProfilePage()); break;
                case "tournaments": Navigate("tournaments", btnTournaments, () => new TournamentsPage()); break;
                case "seasons": Navigate("seasons", btnSeasons, () => new SeasonsPage()); break;
                case "settings": Navigate("settings", btnSettings, () => new SettingsPage()); break;
                default: Navigate("home", btnHome, CreateHome); break;
            }
        }

        private void Navigate(string key, NavButton button, Func<UserControl> factory)
        {
            _activeButton = button;
            _currentFactory = factory;
            SetActive(button);
            SwitchPage(factory());
            _store.SaveLastPage(key);
        }

        private void OnLanguageChanged()
        {
            ApplyNavTexts();
            if (_currentFactory != null)
                SwitchPage(_currentFactory());
        }

        private void OnThemeChanged()
        {
            ApplyThemeColors();
            if (_currentFactory != null)
                SwitchPage(_currentFactory());
        }

        // Show only the pages that belong to the game we're in. Each game's data, and most of
        // its pages, mean nothing in the other one — Rocket League news on a CS2 sidebar would
        // just be noise.
        private void ApplyGame()
        {
            var game = Games.Active;
            bool rl = game == GameId.RocketLeague;

            btnHome.Visible = rl;
            btnMMR.Visible = rl;
            btnRoad.Visible = rl;
            btnCoach.Visible = rl;
            btnSession.Visible = rl;
            btnProfile.Visible = rl;
            btnRecords.Visible = rl;
            btnTournaments.Visible = rl;
            btnNews.Visible = rl;
            btnSeasons.Visible = rl;
            lblSecSocial.Visible = rl;

            btnCs2.Visible = !rl;

            lblLogo.Text = rl ? "RL HUB" : "CS2 HUB";
        }

        // A page key only reachable in the other game (a remembered LastPage after switching)
        // would show a page whose nav item isn't even there.
        private bool BelongsToActiveGame(string key)
        {
            bool cs2Page = key == "cs2";
            return cs2Page == (Games.Active == GameId.Cs2) || key == "settings";
        }

        private void ApplyThemeColors()
        {
            this.BackColor = Theme.PageBg;
            sidebar.BackColor = Theme.Sidebar;
            header.BackColor = Theme.Sidebar;
            navPanel.BackColor = Theme.Sidebar;
            lblSecMain.ForeColor = Theme.TextMuted;
            lblSecSocial.ForeColor = Theme.TextMuted;
            panelContent.BackColor = Theme.PageBg;
            lblLogo.ForeColor = Theme.AccentSoft;
            btnToggle.ForeColor = Theme.TextPrimary;
            btnToggle.BackColor = Theme.Sidebar;
            foreach (var b in navButtons)
                b.Invalidate();
        }

        private void ResizeNav()
        {
            int w = navPanel.ClientSize.Width;
            foreach (Control c in navPanel.Controls)
                c.Width = Math.Max(10, w - c.Margin.Horizontal);
        }

        private void SetSectionHeadersVisible(bool visible)
        {
            lblSecMain.Visible = visible;
            // COMMUNITY only heads Rocket League items — in CS2 it would caption nothing.
            lblSecSocial.Visible = visible && Games.Active == GameId.RocketLeague;
        }

        private void ApplyNavTexts()
        {
            lblSecMain.Text = Localization.T("nav_sec_main");
            lblSecSocial.Text = Localization.T("nav_sec_social");
            btnHome.Text = Localization.T("nav_home");
            btnMMR.Text = Localization.T("nav_mmr");
            btnRoad.Text = Localization.T("nav_road");
            btnCoach.Text = Localization.T("nav_coach");
            btnSession.Text = Localization.T("nav_session");
            btnCs2.Text = "CS2";
            btnRecords.Text = Localization.T("nav_records");
            btnNews.Text = Localization.T("nav_news");
            btnProfile.Text = Localization.T("nav_profile");
            btnTournaments.Text = Localization.T("nav_tournaments");
            btnSeasons.Text = Localization.T("nav_seasons");
            btnSettings.Text = Localization.T("nav_settings");

            foreach (var b in navButtons)
                _navTip.SetToolTip(b, b.Text);
        }

        private void SetActive(NavButton button)
        {
            foreach (var b in navButtons)
                b.Active = b == button;
        }

        private void SwitchPage(UserControl page)
        {
            DropFrozen();
            _pageAnim.Stop();
            panelContent.Controls.Clear();
            currentPage?.Dispose();

            currentPage = page;

            // Slide-up transition: start slightly lowered, animate to top, then dock.
            page.Dock = DockStyle.None;
            page.Bounds = new Rectangle(0, 42,
                Math.Max(1, panelContent.ClientSize.Width),
                Math.Max(1, panelContent.ClientSize.Height));
            panelContent.Controls.Add(page);

            _animPage = page;
            _pageAnim.Start();
        }

        private void PageAnimTick(object? sender, EventArgs e)
        {
            if (_animPage == null || _animPage.IsDisposed)
            {
                _pageAnim.Stop();
                return;
            }

            int top = _animPage.Top;
            if (top <= 1)
            {
                _animPage.Top = 0;
                _animPage.Dock = DockStyle.Fill;
                _pageAnim.Stop();
                _animPage = null;
                return;
            }

            _animPage.Top = top - Math.Max(2, (int)(top * 0.20f)); // ease-out glide
        }

        // ===== PAGE FREEZING =====

        // Painting a page costs ~34 ms: the cards are semi-transparent, and WinForms renders a
        // transparent child by having its parent repaint that child's whole background — nine
        // containers deep, that adds up, and no amount of caching in ArenaControl avoids it.
        // It is the price of the look and it is fine when it happens once.
        //
        // It is not fine 25 times during an animation, which is what happened: each frame the
        // bar uncovered a few more pixels of the page and the whole thing repainted. So the page
        // is photographed once, hidden, and the photo shown in its place for the length of the
        // animation — uncovering a picture is one blit. The live page comes back at the end.
        private void FreezePage()
        {
            if (panelContent.Width < 2 || panelContent.Height < 2) return;

            // Reuse the buffer, but always re-shoot it: the page moves on its own (the season
            // countdown ticks, a sync lands) and a stale picture would flash old numbers.
            if (_frozen == null || _frozen.Size != panelContent.Size)
            {
                _frozen?.Dispose();
                _frozen = new Bitmap(panelContent.Width, panelContent.Height);
            }
            panelContent.DrawToBitmap(_frozen, new Rectangle(Point.Empty, panelContent.Size));
            panelContent.Visible = false;
        }

        private void ThawPage() => panelContent.Visible = true;

        // Anything that changes what the page looks like makes the snapshot a lie.
        private void DropFrozen()
        {
            _frozen?.Dispose();
            _frozen = null;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (_frozen != null && !panelContent.Visible)
                e.Graphics.DrawImageUnscaled(_frozen, panelContent.Left, panelContent.Top);
        }

        // ===== SIDEBAR COLLAPSE ANIMATION =====

        // Drives the sidebar to expanded/collapsed WITHOUT touching the saved preference,
        // so a hover-peek can reuse the exact same animation as the ☰ toggle.
        private void ApplySidebarState(bool expanded)
        {
            targetWidth = expanded ? ExpandedWidth : CollapsedWidth;

            lblLogo.Visible = expanded;
            foreach (var b in navButtons)
                b.Collapsed = !expanded;
            SetSectionHeadersVisible(expanded);

            // Set the nav item widths ONCE to their final value (not every frame) and turn
            // off scrolling during the slide, so nothing reflows/repaints per frame.
            _animating = true;
            navPanel.AutoScroll = false;
            foreach (Control c in navPanel.Controls)
                c.Width = Math.Max(10, targetWidth - c.Margin.Horizontal);

            FreezePage();
            animTimer.Start();
        }

        private void ToggleSidebar()
        {
            collapsed = !collapsed;
            _peeking = false;
            ApplySidebarState(!collapsed);
            _store.SaveSidebarCollapsed(collapsed);
        }

        // ===== HOVER PEEK =====
        // Hovering the collapsed bar floats it open over the page and moving away retracts it.
        // The page never reflows, so peeking costs nothing.
        private void HoverPeekTick()
        {
            if (!collapsed) { _peeking = false; return; }
            if (!IsHandleCreated || sidebar.Width <= 0) return;

            bool inside = sidebar.ClientRectangle.Contains(sidebar.PointToClient(Cursor.Position));

            if (inside && !_peeking)
            {
                _peeking = true;
                ApplySidebarState(true);
            }
            else if (!inside && _peeking)
            {
                _peeking = false;
                ApplySidebarState(false);
            }
        }

        private void AnimTick(object? sender, EventArgs e)
        {
            int diff = targetWidth - sidebar.Width;

            if (Math.Abs(diff) <= 2)
            {
                sidebar.Width = targetWidth;
                animTimer.Stop();
                _animating = false;
                navPanel.AutoScroll = true;
                ResizeNav();
                ThawPage();
                return;
            }

            int step = (int)(diff * 0.30f); // ease-out
            if (step == 0)
                step = Math.Sign(diff) * 2;

            sidebar.Width += step;   // the page is frozen and in place; only the bar moves
        }

        private static void EnableDoubleBuffer(Control c)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(c, true, null);
        }
    }
}
