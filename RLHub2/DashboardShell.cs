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
                btnHome, btnMMR, btnRoad, btnCoach, btnSession, btnProfile, btnRecords, btnNews,
                btnTournaments, btnSeasons, btnSettings
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

            ApplyNavTexts();
            NavigateKey(startPage ?? config.LastPage);

            // Start the live Stats API listener once, on the UI thread, so matches
            // are logged whenever the app is open and Rocket League is running.
            // Also kick off a background ballchasing sync (upload replays + pull stats).
            Load += (s, e) =>
            {
                StatsApiClient.Instance.Start();
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
            switch ((key ?? "home").ToLowerInvariant())
            {
                case "mmr": Navigate("mmr", btnMMR, () => new MMRPage()); break;
                case "road": Navigate("road", btnRoad, () => new RoadPage()); break;
                case "coach": Navigate("coach", btnCoach, () => new CoachPage()); break;
                case "session": Navigate("session", btnSession, () => new SessionPage()); break;
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
            lblSecSocial.Visible = visible;
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

            // Undock the content and give it its FINAL width up front, so the page lays out and
            // repaints once instead of once per frame. A docked page would be resized by every
            // frame of the slide, and repainting a full page costs ~100 ms — that was the whole
            // reason the animation crawled. From here each frame only moves the panel sideways.
            if (ClientSize.Width > targetWidth)
            {
                panelContent.Dock = DockStyle.None;
                panelContent.Bounds = new Rectangle(
                    sidebar.Width, 0, ClientSize.Width - targetWidth, ClientSize.Height);
            }

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
        // The sidebar stays docked, so expanding it pushes the page across rather than
        // covering it — nothing on the page is ever hidden behind the menu.
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
                panelContent.Dock = DockStyle.Fill;   // hand the content back to the layout engine
                return;
            }

            int step = (int)(diff * 0.30f); // ease-out
            if (step == 0)
                step = Math.Sign(diff) * 2;

            sidebar.Width += step;
            // Content keeps its final width and only slides. Pin its left to max(bar, target):
            // when collapsing, that's the shrinking bar, so the page follows it left; when
            // expanding, it's the final width, so the page sits still and the bar grows up to
            // it — no strip of background ever shows between the two.
            panelContent.Left = Math.Max(sidebar.Width, targetWidth);
        }

        private static void EnableDoubleBuffer(Control c)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(c, true, null);
        }
    }
}
