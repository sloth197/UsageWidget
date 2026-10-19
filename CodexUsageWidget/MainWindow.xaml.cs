using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CodexUsageWidget;

public partial class MainWindow : Window
{
    enum Edge { None, Left, Top, Right, Bottom }
    Edge edge;
    Point grab;
    double dent;
    double dentAtRelease;
    double velocity;
    readonly List<JellyBar> jellyBars = [];
    readonly DispatcherTimer refresh = new() { Interval = TimeSpan.FromMinutes(1) };
    readonly DispatcherTimer spring = new() { Interval = TimeSpan.FromMilliseconds(16) };

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => { PlaceWindow(); DrawBlob(); await RefreshUsage(); refresh.Start(); };
        SizeChanged += (_, _) => DrawBlob();
        refresh.Tick += async (_, _) => await RefreshUsage();
        spring.Tick += (_, _) => SpringTick();
    }

    void PlaceWindow()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 24;
        Top = area.Top + 24;
    }

    void Root_Down(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        grab = e.GetPosition(Root);
        edge = HitEdge(grab);
        if (edge == Edge.None) { DragMove(); return; }
        spring.Stop();
        Root.CaptureMouse();
    }

    void Root_Move(object sender, MouseEventArgs e)
    {
        if (edge == Edge.None || e.LeftButton != MouseButtonState.Pressed) return;
        var p = e.GetPosition(Root);
        dent = edge switch
        {
            Edge.Left => Math.Clamp(p.X - grab.X, 0, ActualWidth * .55),
            Edge.Right => Math.Clamp(grab.X - p.X, 0, ActualWidth * .55),
            Edge.Top => Math.Clamp(p.Y - grab.Y, 0, ActualHeight * .55),
            Edge.Bottom => Math.Clamp(grab.Y - p.Y, 0, ActualHeight * .55),
            _ => 0
        };
        DrawBlob();
    }

    void Root_Up(object sender, MouseButtonEventArgs e)
    {
        if (edge == Edge.None) return;
        Root.ReleaseMouseCapture();
        dentAtRelease = dent;
        velocity = 0;
        spring.Start();
    }

    Edge HitEdge(Point p)
    {
        const double hit = 16;
        if (p.X <= hit) return Edge.Left;
        if (p.X >= ActualWidth - hit) return Edge.Right;
        if (p.Y <= hit) return Edge.Top;
        if (p.Y >= ActualHeight - hit) return Edge.Bottom;
        return Edge.None;
    }

    void SpringTick()
    {
        velocity = (velocity + (0 - dent) * .22) * .74;
        dent += velocity;
        if (Math.Abs(dent) < .08 && Math.Abs(velocity) < .08)
        {
            dent = 0; velocity = 0; spring.Stop(); edge = Edge.None;
        }
        DrawBlob();
    }

    void DrawBlob()
    {
        var w = ActualWidth > 0 ? ActualWidth : Width;
        var h = ActualHeight > 0 ? ActualHeight : Height;
        const double r = 22;
        var x = Math.Clamp(grab.X, r + 38, w - r - 38);
        var y = Math.Clamp(grab.Y, r + 34, h - r - 34);
        var g = new StreamGeometry();
        using (var c = g.Open())
        {
            c.BeginFigure(new Point(r, 0), true, true);
            EdgeLine(c, Edge.Top, new Point(r, 0), new Point(w - r, 0), x, dent);
            c.ArcTo(new Point(w, r), new Size(r, r), 0, false, SweepDirection.Clockwise, true, false);
            EdgeLine(c, Edge.Right, new Point(w, r), new Point(w, h - r), y, dent);
            c.ArcTo(new Point(w - r, h), new Size(r, r), 0, false, SweepDirection.Clockwise, true, false);
            EdgeLine(c, Edge.Bottom, new Point(w - r, h), new Point(r, h), x, dent);
            c.ArcTo(new Point(0, h - r), new Size(r, r), 0, false, SweepDirection.Clockwise, true, false);
            EdgeLine(c, Edge.Left, new Point(0, h - r), new Point(0, r), y, dent);
            c.ArcTo(new Point(r, 0), new Size(r, r), 0, false, SweepDirection.Clockwise, true, false);
        }
        g.Freeze();
        Blob.Data = g;
        foreach (var bar in jellyBars) bar.InvalidateVisual();
    }

    internal Point DeformPoint(Point point)
    {
        if (edge == Edge.None || Math.Abs(dent) < .01) return point;
        const double alongSpread = 48;
        const double depthSpread = 95;
        double Along(double distance) => Math.Exp(-(distance * distance) / (2 * alongSpread * alongSpread));
        return edge switch
        {
            Edge.Left => new Point(point.X + dent * Math.Exp(-point.X / depthSpread) * Along(point.Y - grab.Y), point.Y),
            Edge.Right => new Point(point.X - dent * Math.Exp(-(ActualWidth - point.X) / depthSpread) * Along(point.Y - grab.Y), point.Y),
            Edge.Top => new Point(point.X, point.Y + dent * Math.Exp(-point.Y / depthSpread) * Along(point.X - grab.X)),
            Edge.Bottom => new Point(point.X, point.Y - dent * Math.Exp(-(ActualHeight - point.Y) / depthSpread) * Along(point.X - grab.X)),
            _ => point
        };
    }

    void EdgeLine(StreamGeometryContext c, Edge which, Point start, Point end, double center, double amount)
    {
        if (edge != which || Math.Abs(amount) < .01) { c.LineTo(end, true, false); return; }
        var spread = Math.Max(38, Math.Abs(amount) * 1.15);
        if (which is Edge.Top or Edge.Bottom)
        {
            var sign = which == Edge.Top ? 1 : -1;
            c.LineTo(new Point(center - spread, start.Y), true, false);
            c.BezierTo(new Point(center - 20, start.Y), new Point(center - 18, start.Y + sign * amount), new Point(center, start.Y + sign * amount), true, false);
            c.BezierTo(new Point(center + 18, start.Y + sign * amount), new Point(center + 20, start.Y), new Point(center + spread, start.Y), true, false);
        }
        else
        {
            var sign = which == Edge.Left ? 1 : -1;
            var dir = Math.Sign(end.Y - start.Y);
            c.LineTo(new Point(start.X, center - dir * spread), true, false);
            c.BezierTo(new Point(start.X, center - dir * 20), new Point(start.X + sign * amount, center - dir * 18), new Point(start.X + sign * amount, center), true, false);
            c.BezierTo(new Point(start.X + sign * amount, center + dir * 18), new Point(start.X, center + dir * 20), new Point(start.X, center + dir * spread), true, false);
        }
        c.LineTo(end, true, false);
    }

    async Task RefreshUsage()
    {
        Status.Text = "새로고침 중…";
        try
        {
            var data = await CodexUsage.ReadAsync();
            UsageRows.Children.Clear();
            jellyBars.Clear();
            foreach (var item in data.Windows) UsageRows.Children.Add(MakeRow(item));
            if (data.Windows.Count == 0) UsageRows.Children.Add(new TextBlock { Text = data.Unlimited ? "고정 사용량 한도 없음" : "표시할 시간 한도가 없습니다", Foreground = Brush("#BEC6D6"), Margin = new Thickness(0, 12, 0, 0) });
            var credits = data.Unlimited ? " · 무제한" : data.HasCredits && data.Balance != null ? $" · 크레딧 {data.Balance}" : "";
            Status.Text = $"{data.Plan.ToUpperInvariant()}{credits} · {DateTime.Now:HH:mm} 갱신";
        }
        catch { Status.Text = "조회 실패 · ↻로 다시 시도"; }
    }

    UIElement MakeRow(UsageWindow item)
    {
        var grid = new Grid { Height = 42, Margin = new Thickness(0, 0, 0, 3) };
        grid.Children.Add(new TextBlock { Text = item.Name, Foreground = Brush("#DCE1EB"), FontFamily = new FontFamily("Segoe UI"), FontWeight = FontWeights.SemiBold, FontSize = 12 });
        var reset = DateTimeOffset.FromUnixTimeSeconds(item.ResetAt).ToLocalTime().ToString("M/d HH:mm");
        grid.Children.Add(new TextBlock { Text = $"{100 - item.Used:0}% 남음  ·  {reset}", Foreground = Brush("#7D8798"), FontFamily = new FontFamily("Segoe UI"), FontSize = 10.7, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 2, 0, 0) });
        var fillColor = item.Used < 75 ? "#5C87FF" : item.Used < 90 ? "#F5B446" : "#F05F69";
        var bar = new JellyBar(this, item.Used, Brush(fillColor)) { Width = 274, Height = 9, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 24, 0, 0) };
        jellyBars.Add(bar);
        grid.Children.Add(bar);
        return grid;
    }

    static Brush Brush(string value) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
    void Opacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) { if (IsLoaded) Opacity = e.NewValue; }
    async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshUsage();
    void Close_Click(object sender, RoutedEventArgs e) => Close();
}

sealed class JellyBar(MainWindow owner, double value, Brush fill) : FrameworkElement
{
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        DrawShape(dc, ActualWidth, Brush("#2D303B"));
        DrawShape(dc, Math.Max(1, ActualWidth * value / 100), fill);
    }

    void DrawShape(DrawingContext dc, double width, Brush brush)
    {
        var points = RoundedRectPoints(width, ActualHeight, Math.Min(4.5, width / 2));
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            for (var i = 0; i < points.Count; i++)
            {
                var global = TranslatePoint(points[i], owner.Root);
                var moved = owner.DeformPoint(global);
                var local = owner.Root.TranslatePoint(moved, this);
                if (i == 0) context.BeginFigure(local, true, true); else context.LineTo(local, true, false);
            }
        }
        geometry.Freeze();
        dc.DrawGeometry(brush, null, geometry);
    }

    static List<Point> RoundedRectPoints(double w, double h, double r)
    {
        var p = new List<Point>();
        AddArc(p, r, r, r, 180, 270);
        for (var x = r; x <= w - r; x += Math.Max(2, w / 18)) p.Add(new Point(x, 0));
        AddArc(p, w - r, r, r, 270, 360);
        for (var y = r; y <= h - r; y += Math.Max(1, h / 4)) p.Add(new Point(w, y));
        AddArc(p, w - r, h - r, r, 0, 90);
        for (var x = w - r; x >= r; x -= Math.Max(2, w / 18)) p.Add(new Point(x, h));
        AddArc(p, r, h - r, r, 90, 180);
        for (var y = h - r; y >= r; y -= Math.Max(1, h / 4)) p.Add(new Point(0, y));
        return p;
    }

    static void AddArc(List<Point> points, double cx, double cy, double r, int start, int end)
    {
        for (var degree = start; degree <= end; degree += 10)
        {
            var a = degree * Math.PI / 180;
            points.Add(new Point(cx + Math.Cos(a) * r, cy + Math.Sin(a) * r));
        }
    }

    static Brush Brush(string value) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
}

record UsageWindow(string Name, double Used, long ResetAt);
record UsageData(List<UsageWindow> Windows, string Plan, bool Unlimited, bool HasCredits, string? Balance);

static class CodexUsage
{
    public static async Task<UsageData> ReadAsync()
    {
        var exe = FindCodex();
        var psi = new ProcessStartInfo(exe, "app-server --stdio") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        using var p = Process.Start(psi) ?? throw new Exception();
        try
        {
            await p.StandardInput.WriteLineAsync("{\"id\":1,\"method\":\"initialize\",\"params\":{\"clientInfo\":{\"name\":\"usage-widget-jelly\",\"version\":\"1.0\"},\"capabilities\":null}}");
            await p.StandardInput.FlushAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            while (true) { var line = await p.StandardOutput.ReadLineAsync(timeout.Token) ?? throw new Exception(); using var d = JsonDocument.Parse(line); if (d.RootElement.TryGetProperty("id", out var id) && id.GetInt32() == 1) break; }
            await p.StandardInput.WriteLineAsync("{\"method\":\"initialized\"}");
            await p.StandardInput.WriteLineAsync("{\"id\":2,\"method\":\"account/rateLimits/read\",\"params\":{\"excludeResetCreditDetails\":true,\"supportsLunaReserve\":false}}");
            await p.StandardInput.FlushAsync();
            while (true)
            {
                var line = await p.StandardOutput.ReadLineAsync(timeout.Token) ?? throw new Exception(); using var d = JsonDocument.Parse(line); var root = d.RootElement;
                if (!root.TryGetProperty("id", out var id) || id.GetInt32() != 2) continue;
                var limits = root.GetProperty("result").GetProperty("rateLimits"); var windows = new List<UsageWindow>(); Add(limits, "primary", windows); Add(limits, "secondary", windows);
                var unlimited = false; var has = false; string? balance = null;
                if (limits.TryGetProperty("credits", out var cr) && cr.ValueKind == JsonValueKind.Object) { unlimited = cr.TryGetProperty("unlimited", out var u) && u.ValueKind == JsonValueKind.True; has = cr.TryGetProperty("hasCredits", out var hc) && hc.ValueKind == JsonValueKind.True; if (cr.TryGetProperty("balance", out var b) && b.ValueKind == JsonValueKind.String) balance = b.GetString(); }
                return new(windows, limits.TryGetProperty("planType", out var plan) ? plan.GetString() ?? "Codex" : "Codex", unlimited, has, balance);
            }
        }
        finally
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
        }
    }
    static void Add(JsonElement limits, string key, List<UsageWindow> list)
    {
        if (!limits.TryGetProperty(key, out var w) || w.ValueKind != JsonValueKind.Object) return;
        if (!w.TryGetProperty("usedPercent", out var used) || used.ValueKind != JsonValueKind.Number || !w.TryGetProperty("resetsAt", out var reset) || reset.ValueKind != JsonValueKind.Number) return;
        var mins = w.TryGetProperty("windowDurationMins", out var m) && m.ValueKind == JsonValueKind.Number ? m.GetInt32() : 0;
        var name = mins switch { 300 => "5시간", 10080 => "주간", >= 1440 when mins % 1440 == 0 => $"{mins / 1440}일", >= 60 when mins % 60 == 0 => $"{mins / 60}시간", _ => key == "primary" ? "단기 한도" : "장기 한도" };
        list.Add(new(name, used.GetDouble(), reset.GetInt64()));
    }
    static string FindCodex()
    {
        var overridePath = Environment.GetEnvironmentVariable("CODEX_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath)) return overridePath;
        foreach (var folder in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(folder, "codex.exe");
            if (File.Exists(candidate)) return candidate;
        }
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenAI", "Codex", "bin");
        return Directory.Exists(root) ? Directory.GetFiles(root, "codex.exe", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault() ?? "codex.exe" : "codex.exe";
    }
}
