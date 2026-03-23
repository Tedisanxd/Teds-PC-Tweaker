// ═══════════════════════════════════════════════════════════════════════
//  Ted's PC Tweaker
//  Developed by tedisanxd
// ═══════════════════════════════════════════════════════════════════════
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TedsPCTweaker.Services;

namespace TedsPCTweaker;

// ─────────────────────────────────────────────────────────────────────
//  Per-tweak view model
// ─────────────────────────────────────────────────────────────────────
public class TweakVM : INotifyPropertyChanged
{
    public string        Id              { get; init; } = string.Empty;
    public string        Title           { get; init; } = string.Empty;
    public string        Description     { get; init; } = string.Empty;
    public TweakCategory Category        { get; init; }
    public bool          RequiresReboot  { get; init; }
    public bool          IsCleanupAction { get; init; }
    public Brush         RiskColor       { get; init; } = Brushes.Gray;

    bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set { _isEnabled = value; OnProp(); }
    }

    bool _isApplied;
    public bool IsApplied
    {
        get => _isApplied;
        set { _isApplied = value; OnProp(); OnProp(nameof(AppliedBadgeVisible)); }
    }

    public Visibility ToggleVisible       => IsCleanupAction ? Visibility.Collapsed : Visibility.Visible;
    public Visibility CleanupVisible      => IsCleanupAction ? Visibility.Visible   : Visibility.Collapsed;
    public Visibility RebootBadgeVisible  => RequiresReboot  ? Visibility.Visible   : Visibility.Collapsed;
    public Visibility AppliedBadgeVisible => IsApplied       ? Visibility.Visible   : Visibility.Collapsed;

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnProp([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

// ─────────────────────────────────────────────────────────────────────
//  Shared easing functions
// ─────────────────────────────────────────────────────────────────────
static class Ease
{
    public static readonly IEasingFunction Out    = Make(new CubicEase { EasingMode = EasingMode.EaseOut });
    public static readonly IEasingFunction In     = Make(new CubicEase { EasingMode = EasingMode.EaseIn  });

    static IEasingFunction Make(EasingFunctionBase f) { f.Freeze(); return f; }
}

// ─────────────────────────────────────────────────────────────────────
//  Icon converter — maps category tag string → Segoe MDL2 Assets glyph
// ─────────────────────────────────────────────────────────────────────
public class CategoryIconConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter,
                          System.Globalization.CultureInfo culture)
        => value?.ToString() switch {
            "Performance" => "\uE945",
            "Privacy"     => "\uE72E",
            "Gaming"      => "\uE7FC",
            "Network"     => "\uE774",
            "Cleanup"     => "\uEF5F",
            _             => ""
        };

    public object ConvertBack(object value, Type targetType, object parameter,
                              System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

// ─────────────────────────────────────────────────────────────────────
//  Main window
// ─────────────────────────────────────────────────────────────────────
public partial class MainWindow : Window
{
    readonly List<TweakVM> _all = new();
    TweakCategory _currentCat = TweakCategory.Performance;
    bool _transitioning;
    bool _pendingReboot;

    static readonly Dictionary<TweakRisk, Color> RiskColors = new()
    {
        [TweakRisk.Safe]   = Color.FromRgb(0x1A, 0x8A, 0x4A),
        [TweakRisk.Low]    = Color.FromRgb(0x1A, 0x5A, 0x7A),
        [TweakRisk.Medium] = Color.FromRgb(0x7A, 0x5A, 0x00),
        [TweakRisk.High]   = Color.FromRgb(0x7A, 0x1C, 0x1C),
    };

    static readonly Dictionary<TweakCategory, (string Title, string Subtitle)> CatInfo = new()
    {
        [TweakCategory.Performance] = ("Performance", "Power plans, services, scheduling and system settings."),
        [TweakCategory.Privacy]     = ("Privacy",     "Block Microsoft telemetry, tracking and data collection."),
        [TweakCategory.Gaming]      = ("Gaming",      "Lower latency, better frame pacing and reduced overhead."),
        [TweakCategory.Network]     = ("Network",     "Reduce latency and improve throughput for games and downloads."),
        [TweakCategory.Cleanup]     = ("Cleanup",     "Remove temporary, cached and redundant files."),
    };

    public MainWindow()
    {
        InitializeComponent();

        // Pixel-perfect rendering
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);

        BuildTweakList();
        ApplyCategory(TweakCategory.Performance);   // items visible immediately — no initial stagger
        UpdateStats();
    }

    // ── Build ────────────────────────────────────────────────────────

    void BuildTweakList()
    {
        foreach (var def in TweakEngine.AllTweaks)
        {
            var brush = new SolidColorBrush(RiskColors[def.Risk]);
            brush.Freeze();
            var vm = new TweakVM
            {
                Id              = def.Id,
                Title           = def.Title,
                Description     = def.Description,
                Category        = def.Category,
                RequiresReboot  = def.RequiresReboot,
                IsCleanupAction = def.IsCleanupAction,
                RiskColor       = brush,
                IsApplied       = !def.IsCleanupAction && TweakEngine.IsApplied(def.Id),
            };
            vm.IsEnabled = vm.IsApplied;
            vm.PropertyChanged += (_, _) => UpdateStats();
            _all.Add(vm);
        }
    }

    // ── Category ─────────────────────────────────────────────────────

    void ApplyCategory(TweakCategory cat)
    {
        _currentCat = cat;
        var (title, sub) = CatInfo[cat];
        TxtCategoryTitle.Text    = title;
        TxtCategorySubtitle.Text = sub;
        TweakList.ItemsSource    = _all.Where(t => t.Category == cat).ToList();
    }

    void TransitionToCategory(TweakCategory cat)
    {
        if (_transitioning || cat == _currentCat) return;
        _transitioning = true;

        // Phase 1 — 90 ms fade-out
        var fadeOut = new DoubleAnimation(0d, Ms(90)) { EasingFunction = Ease.In };
        fadeOut.Completed += (_, _) =>
        {
            ApplyCategory(cat);

            // Phase 2 — after WPF lays out new items, stagger them in
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                var containers = GetItemContainers().ToList();

                // Pre-hide every container
                foreach (var c in containers)
                {
                    c.Opacity = 0;
                    SetTranslateY(c, 12);
                }

                // Release the held fade-out animation — ContentPanel snaps to opacity 1.
                // Containers are already opacity-0 so there is no flash.
                ContentPanel.BeginAnimation(OpacityProperty, null);

                // Stagger each container in (22 ms apart, 200 ms each)
                for (int i = 0; i < containers.Count; i++)
                {
                    var c     = containers[i];
                    var delay = TimeSpan.FromMilliseconds(Math.Min(i, 10) * 22);

                    c.BeginAnimation(OpacityProperty,
                        new DoubleAnimation(1d, Ms(200))
                        { BeginTime = delay, EasingFunction = Ease.Out });

                    GetTranslate(c)?.BeginAnimation(TranslateTransform.YProperty,
                        new DoubleAnimation(0d, Ms(200))
                        { BeginTime = delay, EasingFunction = Ease.Out });
                }

                _transitioning = false;
            });
        };

        ContentPanel.BeginAnimation(OpacityProperty, fadeOut);
    }

    // ── Visual-tree container lookup ─────────────────────────────────
    // ItemsControl does not support ItemContainerGenerator reliably.
    // Walking the visual tree directly is the correct approach.

    IEnumerable<FrameworkElement> GetItemContainers()
    {
        var panel = FindDescendant<Panel>(TweakList);
        if (panel is null) yield break;
        foreach (UIElement child in panel.Children)
            if (child is FrameworkElement fe)
                yield return fe;
    }

    static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        int n = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < n; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T hit) return hit;
            var found = FindDescendant<T>(child);
            if (found is not null) return found;
        }
        return null;
    }

    static void SetTranslateY(FrameworkElement el, double y)
    {
        if (el.RenderTransform is TranslateTransform tt) { tt.Y = y; return; }
        el.RenderTransform = new TranslateTransform(0, y);
    }

    static TranslateTransform? GetTranslate(FrameworkElement el)
        => el.RenderTransform as TranslateTransform;

    // ── Helpers ──────────────────────────────────────────────────────

    static Duration Ms(int ms) => new(TimeSpan.FromMilliseconds(ms));

    // ── Stats ────────────────────────────────────────────────────────

    void UpdateStats()
    {
        var nc      = _all.Where(t => !t.IsCleanupAction).ToList();
        int applied = nc.Count(t => t.IsApplied);
        int total   = nc.Count;
        TxtAppliedCount.Text = applied.ToString();
        TxtTotalCount.Text   = $" / {total}";
        PbProgress.Value     = total > 0 ? (double)applied / total : 0;
        TxtRebootNotice.Text = _pendingReboot ? "⚠  Restart required" : "";
    }

    // ── Status bar ───────────────────────────────────────────────────

    void SetStatus(string msg, bool isError = false)
    {
        var colour = isError
            ? Color.FromRgb(0xCC, 0x44, 0x44)
            : Color.FromRgb(0x00, 0x99, 0xBB);

        var fadeOut = new DoubleAnimation(0d, Ms(55));
        fadeOut.Completed += (_, _) =>
        {
            TxtStatus.Text       = msg;
            TxtStatus.Foreground = new SolidColorBrush(colour);
            TxtStatus.BeginAnimation(OpacityProperty,
                new DoubleAnimation(1d, Ms(130)) { EasingFunction = Ease.Out });
        };
        TxtStatus.BeginAnimation(OpacityProperty, fadeOut);
    }

    // ── Navigation ───────────────────────────────────────────────────

    void NavButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && Enum.TryParse<TweakCategory>(rb.Tag?.ToString(), out var cat))
            TransitionToCategory(cat);
    }

    // ── Apply / Revert ───────────────────────────────────────────────

    void BtnApplyAll_Click(object sender, RoutedEventArgs e)
    {
        var pending = _all.Where(t => !t.IsCleanupAction && t.IsEnabled && !t.IsApplied).ToList();
        if (pending.Count == 0) { SetStatus("Nothing to apply — toggle tweaks first."); return; }

        SetBusy(true);
        SetStatus($"Applying {pending.Count} tweak(s)…");

        var worker = new BackgroundWorker();
        worker.DoWork += (_, _) =>
        {
            foreach (var vm in pending)
            {
                var r = TweakEngine.Apply(vm.Id);
                Dispatcher.Invoke(() =>
                {
                    if (r.Success) { vm.IsApplied = true; if (r.RequiresReboot) _pendingReboot = true; SetStatus($"✓  {vm.Title}"); }
                    else             SetStatus($"✗  {vm.Title}: {r.Message}", true);
                    UpdateStats();
                });
            }
        };
        worker.RunWorkerCompleted += (_, _) =>
        {
            SetBusy(false);
            SetStatus($"Done — {pending.Count} tweak(s) applied." + (_pendingReboot ? " Restart recommended." : ""));
            UpdateStats();
        };
        worker.RunWorkerAsync();
    }

    void BtnRevertAll_Click(object sender, RoutedEventArgs e)
    {
        var applied = _all.Where(t => !t.IsCleanupAction && t.IsApplied).ToList();
        if (applied.Count == 0) { SetStatus("No tweaks are currently applied."); return; }

        if (MessageBox.Show($"Revert all {applied.Count} applied tweak(s) to Windows defaults?",
            "Confirm Revert", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        SetBusy(true);
        SetStatus($"Reverting {applied.Count} tweak(s)…");

        var worker = new BackgroundWorker();
        worker.DoWork += (_, _) =>
        {
            foreach (var vm in applied)
            {
                var r = TweakEngine.Revert(vm.Id);
                Dispatcher.Invoke(() =>
                {
                    vm.IsApplied = false; vm.IsEnabled = false;
                    SetStatus(r.Success ? $"✓  Reverted {vm.Title}" : $"✗  {vm.Title}: {r.Message}", !r.Success);
                    UpdateStats();
                });
            }
        };
        worker.RunWorkerCompleted += (_, _) =>
        {
            SetBusy(false); _pendingReboot = false;
            SetStatus("All tweaks reverted to Windows defaults.");
            UpdateStats();
        };
        worker.RunWorkerAsync();
    }

    void BtnRestorePoint_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true, BtnRestorePoint);
        SetStatus("Creating System Restore Point…");
        var worker = new BackgroundWorker();
        worker.DoWork += (_, _) =>
        {
            var psi = new ProcessStartInfo("powershell",
                "-NonInteractive -Command \"Checkpoint-Computer -Description 'TedsPCTweaker' -RestorePointType MODIFY_SETTINGS\"")
                { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi)!;
            p.WaitForExit(30_000);
            bool ok = p.ExitCode == 0;
            Dispatcher.Invoke(() => SetStatus(ok
                ? "Restore Point created." : "Could not create Restore Point (enable System Protection first).", !ok));
        };
        worker.RunWorkerCompleted += (_, _) => SetBusy(false, BtnRestorePoint);
        worker.RunWorkerAsync();
    }

    void CleanupRun_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var id = btn.Tag?.ToString() ?? "";
        btn.IsEnabled = false; btn.Content = "…";
        SetStatus("Running…");
        var worker = new BackgroundWorker();
        worker.DoWork += (_, _) =>
        {
            var r = TweakEngine.Apply(id);
            Dispatcher.Invoke(() => { SetStatus(r.Success ? $"✓  {r.Message}" : $"✗  {r.Message}", !r.Success); btn.Content = "Done"; btn.IsEnabled = true; });
        };
        worker.RunWorkerAsync();
    }

    void BtnSelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var t in _all.Where(t => t.Category == _currentCat && !t.IsCleanupAction)) t.IsEnabled = true;
    }

    void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var t in _all.Where(t => t.Category == _currentCat && !t.IsCleanupAction)) t.IsEnabled = false;
    }

    void SetBusy(bool busy, Button? btn = null)
    {
        if (btn != null) { btn.IsEnabled = !busy; return; }
        BtnApplyAll.IsEnabled = BtnRevertAll.IsEnabled = !busy;
    }
}
