using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MilkFrog.Core;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace MilkFrog.App;

public sealed class PetWindow : Window, IDisposable
{
    private readonly FrogApplication app;
    private readonly Image image = new() { Stretch = Stretch.Fill };
    private readonly Grid petSurface = new();
    private readonly Canvas sleepMarks = new() { IsHitTestVisible = false, Visibility = Visibility.Collapsed };
    private readonly List<TextBlock> zMarks = new();
    private readonly AnimationPlayer player;
    private readonly Popup toolbar = new() { AllowsTransparency = true, StaysOpen = true, Placement = PlacementMode.Bottom, VerticalOffset = -13 };
    private readonly Popup bubble = new() { AllowsTransparency = true, StaysOpen = true, Placement = PlacementMode.Top, VerticalOffset = 12, IsHitTestVisible = false };
    private readonly TextBlock bubbleText = Theme.Text("", 12);
    private readonly DispatcherTimer clock, bubbleTimer, hoverTimer;
    private readonly Stopwatch watch = Stopwatch.StartNew();
    private readonly Forms.NotifyIcon tray;
    private readonly Drawing.Icon trayIcon;
    private Forms.ContextMenuStrip? petMenu;
    private readonly Queue<string> sequence = new();
    private nint handle;
    private HwndSource? source;
    private DateTimeOffset lastGameTick = DateTimeOffset.UtcNow;
    private double lastAnimationTick;
    private DateTimeOffset nextIdle = DateTimeOffset.UtcNow.AddSeconds(30), nextSave = DateTimeOffset.UtcNow.AddSeconds(20);
    private NativeMethods.Point dragStart;
    private NativeMethods.Rect dragBounds;
    private bool pressed, dragging, disposed, manualHidden, tiredAnnounced;
    private Button? playButton;
    private readonly Random random = new();

    public PetWindow(FrogApplication application)
    {
        app = application;
        Title = "奶蛙"; Width = Height = app.Game.State.Settings.Size;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; AllowsTransparency = true;
        Background = Brushes.Transparent; ShowInTaskbar = false; ShowActivated = false;
        FontFamily = new FontFamily("Microsoft YaHei UI"); Cursor = Cursors.Hand;
        petSurface.Children.Add(image); petSurface.Children.Add(sleepMarks); Content = petSurface;
        BuildSleepMarks();
        toolbar.PlacementTarget = image; bubble.PlacementTarget = image;
        toolbar.Child = BuildToolbar();
        bubble.Child = new Border { Background = Theme.Brush("#FFFDF2"), CornerRadius = new CornerRadius(12), Padding = new Thickness(14, 9, 14, 9), BorderBrush = Theme.Line, BorderThickness = new Thickness(1), Child = bubbleText, MaxWidth = 300, IsHitTestVisible = false };
        bubble.Opened += (_, _) =>
        {
            if (PresentationSource.FromVisual(bubble.Child) is HwndSource popupSource)
                NativeMethods.SetWindowLong(popupSource.Handle, NativeMethods.GwlExStyle, NativeMethods.GetWindowLong(popupSource.Handle, NativeMethods.GwlExStyle) | NativeMethods.WsExNoActivate | 0x20);
        };
        player = new(app.Assets);
        player.FrameChanged += frame => { image.Source = frame.Bitmap; if (handle != 0 && !dragging) NativeMethods.ApplyRegion(handle, frame, false, sleepMarks.Visibility == Visibility.Visible); };
        player.Completed += _ => NextAnimation();
        SourceInitialized += (_, _) =>
        {
            handle = new WindowInteropHelper(this).Handle; source = HwndSource.FromHwnd(handle); source.AddHook(WindowHook);
            NativeMethods.SetWindowLong(handle, NativeMethods.GwlExStyle, NativeMethods.GetWindowLong(handle, NativeMethods.GwlExStyle) | NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate);
        };
        Loaded += (_, _) => { ApplySettings(); RestorePosition(); ResumeAnimation(); };
        SizeChanged += (_, _) => { PositionSleepMarks(); if (handle != 0 && player.Current is not null) NativeMethods.ApplyRegion(handle, player.Current, false, sleepMarks.Visibility == Visibility.Visible); };
        MouseEnter += (_, _) => { hoverTimer?.Stop(); if (!dragging) toolbar.IsOpen = true; };
        MouseLeave += (_, _) => hoverTimer?.Start();
        image.MouseLeftButtonDown += MouseDownPet; image.MouseMove += MouseMovePet; image.MouseLeftButtonUp += MouseUpPet;
        image.LostMouseCapture += (_, _) => { if (pressed) EndDrag(); };
        image.MouseRightButtonUp += (_, e) => { OpenMenu(); e.Handled = true; };
        Closing += (_, e) => { if (!app.Exiting) { e.Cancel = true; HidePet(); } };
        clock = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) }; clock.Tick += Tick;
        bubbleTimer = new() { Interval = TimeSpan.FromSeconds(6) }; bubbleTimer.Tick += (_, _) => { bubble.IsOpen = false; bubbleTimer.Stop(); };
        hoverTimer = new() { Interval = TimeSpan.FromMilliseconds(500) }; hoverTimer.Tick += (_, _) => { if (!IsMouseOver && !(toolbar.Child?.IsMouseOver ?? false)) toolbar.IsOpen = false; hoverTimer.Stop(); };
        if (toolbar.Child is FrameworkElement toolbarElement) { toolbarElement.MouseEnter += (_, _) => hoverTimer.Stop(); toolbarElement.MouseLeave += (_, _) => hoverTimer.Start(); }
        trayIcon = app.Assets.GetAppIcon();
        var trayMenu = BuildTrayMenu();
        trayMenu.Opening += (_, _) => UpdateTrayMenu(trayMenu);
        tray = new Forms.NotifyIcon { Icon = trayIcon, Text = "奶蛙 · 幼年陪伴", ContextMenuStrip = trayMenu, Visible = true };
        tray.DoubleClick += (_, _) => Dispatcher.BeginInvoke(RestorePet);
        clock.Start();
    }
    private void BuildSleepMarks()
    {
        foreach (var (size, delay) in new[] { (15.0, 0.0), (20.0, .65), (26.0, 1.3) })
        {
            var text = new TextBlock { Text = "Z", FontSize = size, FontWeight = FontWeights.SemiBold, Foreground = Theme.Brush("#6F8F61"), Opacity = 0, RenderTransform = new TranslateTransform() };
            zMarks.Add(text); sleepMarks.Children.Add(text);
            text.Tag = delay;
        }
        PositionSleepMarks();
    }
    private void PositionSleepMarks()
    {
        double size = ActualWidth > 0 ? ActualWidth : Width;
        for (int i = 0; i < zMarks.Count; i++)
        {
            Canvas.SetLeft(zMarks[i], size * (.62 + i * .09));
            Canvas.SetTop(zMarks[i], size * (.33 - i * .13));
        }
    }
    private void SetSleepMarks(bool visible)
    {
        if ((sleepMarks.Visibility == Visibility.Visible) == visible) return;
        sleepMarks.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        foreach (var mark in zMarks)
        {
            mark.BeginAnimation(OpacityProperty, null);
            if (mark.RenderTransform is TranslateTransform transform) transform.BeginAnimation(TranslateTransform.YProperty, null);
            if (!visible) { mark.Opacity = 0; continue; }
            var delay = TimeSpan.FromSeconds((double)mark.Tag);
            mark.BeginAnimation(OpacityProperty, new DoubleAnimationUsingKeyFrames
            {
                BeginTime = delay, Duration = TimeSpan.FromSeconds(2.7), RepeatBehavior = RepeatBehavior.Forever,
                KeyFrames = { new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)), new LinearDoubleKeyFrame(.9, KeyTime.FromPercent(.2)), new LinearDoubleKeyFrame(.75, KeyTime.FromPercent(.72)), new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1)) }
            });
            if (mark.RenderTransform is TranslateTransform movement) movement.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(9, -17, TimeSpan.FromSeconds(2.7)) { BeginTime = delay, RepeatBehavior = RepeatBehavior.Forever });
        }
        if (handle != 0 && player.Current is not null && !dragging) NativeMethods.ApplyRegion(handle, player.Current, false, visible);
    }
    private void Play(string name, bool? loopOverride = null)
    {
        player.Play(name, loopOverride);
        SetSleepMarks(player.Name == "sleep");
    }
    private UIElement BuildToolbar()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (name, action) in new (string, Action)[] { ("喂奶", () => UseItem("milk")), ("洗澡", () => UseItem("bubble")), ("玩球", () => UseItem("ball")), ("休息", SleepOrWake), ("背包", () => app.OpenPanel("背包")), ("更多", OpenMenu) })
        { var button = Theme.Button(name, action); if (name == "玩球") playButton = button; button.Padding = new Thickness(8, 7, 8, 7); button.Margin = new Thickness(2, 0, 2, 0); row.Children.Add(button); }
        return new Border { Child = row, Background = Theme.Brush("#FFFDF6"), Padding = new Thickness(6), CornerRadius = new CornerRadius(11), BorderBrush = Theme.Line, BorderThickness = new Thickness(1) };
    }
    private Forms.ContextMenuStrip BuildTrayMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.BackColor = Drawing.Color.FromArgb(255, 253, 246); menu.ForeColor = Drawing.Color.FromArgb(53, 70, 58); menu.Font = new Drawing.Font("Microsoft YaHei UI", 9); menu.Padding = new Forms.Padding(4);
        void Add(string key, string title, Action action, bool? check = null) { var item = new Forms.ToolStripMenuItem(title) { Name = key }; if (check.HasValue) item.Checked = check.Value; item.Click += (_, _) => Dispatcher.BeginInvoke(action); menu.Items.Add(item); }
        Add("visibility", "显示或隐藏奶蛙", () => { if (IsVisible) HidePet(); else RestorePet(); });
        menu.Items.Add(new Forms.ToolStripSeparator());
        Add("milk", "喂奶", () => UseItem("milk")); Add("bath", "洗澡", () => UseItem("bubble")); Add("ball", "玩球", () => UseItem("ball")); Add("sleep", "睡个小觉", SleepOrWake);
        menu.Items.Add(new Forms.ToolStripSeparator());
        foreach (var page in new[] { "小状态", "学习打工", "商店", "背包", "成长记录", "设置" }) { var selected = page; Add("page_" + selected, selected, () => { RestorePet(); app.OpenPanel(selected); }); }
        menu.Items.Add(new Forms.ToolStripSeparator());
        Add("topmost", "始终置顶", () => { app.Game.State.Settings.Topmost = !app.Game.State.Settings.Topmost; app.ApplySettings(); }, Topmost);
        Add("locked", "锁定位置", () => { app.Game.State.Settings.Locked = !app.Game.State.Settings.Locked; app.ApplySettings(); }, app.Game.State.Settings.Locked);
        Add("restore", "找回到主屏幕", () => { RestorePet(); RestorePosition(true); SavePosition(); app.Save(); });
        menu.Items.Add(new Forms.ToolStripSeparator()); Add("exit", "退出奶蛙", app.ExitPet); UpdateTrayMenu(menu); return menu;
    }
    private void UpdateTrayMenu(Forms.ContextMenuStrip menu)
    {
        if (menu.Items["visibility"] is Forms.ToolStripMenuItem visible) visible.Text = IsVisible ? "隐藏奶蛙" : "显示奶蛙";
        if (menu.Items["sleep"] is Forms.ToolStripMenuItem sleep) sleep.Text = app.Game.State.Activity?.Kind == "sleep" ? "叫它起床" : "睡个小觉";
        if (menu.Items["topmost"] is Forms.ToolStripMenuItem top) top.Checked = Topmost;
        if (menu.Items["locked"] is Forms.ToolStripMenuItem locked) locked.Checked = app.Game.State.Settings.Locked;
        if (menu.Items["ball"] is Forms.ToolStripMenuItem ball) ball.Enabled = app.Game.State.Energy >= 5;
    }
    private void OpenMenu()
    {
        toolbar.IsOpen = false;
        petMenu ??= new Forms.ContextMenuStrip
        {
            ShowImageMargin = false, ShowCheckMargin = false, BackColor = Drawing.Color.FromArgb(255, 253, 246),
            ForeColor = Drawing.Color.FromArgb(53, 70, 58), Font = new Drawing.Font("Microsoft YaHei UI", 9), Padding = new Forms.Padding(4), AutoSize = true
        };
        var menu = petMenu; menu.Items.Clear();
        void Add(string label, Action action) { var item = new Forms.ToolStripMenuItem(label) { Padding = new Forms.Padding(8, 5, 8, 5) }; item.Click += (_, _) => Dispatcher.BeginInvoke(action); menu.Items.Add(item); }
        foreach (var page in new[] { "小状态", "学习打工", "商店", "背包", "成长记录", "设置" }) { var selected = page; Add(page, () => app.OpenPanel(selected)); }
        menu.Items.Add(new Forms.ToolStripSeparator()); Add("隐藏奶蛙", HidePet); Add("退出", app.ExitPet);
        menu.Show(Forms.Cursor.Position);
    }
    public void Say(string message)
    {
        if (disposed || !app.Game.State.Settings.ShowBubbles || !IsVisible) return;
        bubbleText.Text = message; bubble.IsOpen = true; bubbleTimer.Stop(); bubbleTimer.Start();
    }
    public void Perform(GameResult result)
    {
        Say(result.Message);
        if ((result.Success || result.Animation == "tired") && result.Animation != "idle") PlaySequence(result.Animation);
        app.Changed();
    }
    public void PlaySequence(params string[] names)
    {
        sequence.Clear(); foreach (var name in names) sequence.Enqueue(name); NextAnimation();
    }
    private void NextAnimation()
    {
        if (sequence.Count > 0) Play(sequence.Dequeue(), false);
        else ResumeAnimation();
    }
    private void ResumeAnimation()
    {
        sequence.Clear(); Play(app.Game.State.Activity?.Kind ?? (app.Game.IsTired ? "tired" : "idle"));
    }
    public void Preview(string name)
    {
        if (app.Game.State.Activity is not null) { Say("先结束当前安排，再点播小动作吧"); return; }
        PlaySequence(name); Say(app.Assets.Catalog.Clips[name].Label);
    }
    public void UseItem(string id)
    {
        if (app.Game.Count(id) == 0) { app.OpenPanel("商店"); Say("背包里还没有它，去商店挑一份吧"); return; }
        bool waking = app.Game.State.Activity?.Kind == "sleep";
        if (!ConfirmStop()) return;
        var result = app.Game.Use(id, DateTimeOffset.UtcNow);
        if (result.Success) { app.Panel?.Hide(); PlaySequence(waking ? new[] { "wake", result.Animation } : new[] { result.Animation }); }
        else if (result.Animation == "tired") PlaySequence("tired");
        Say(result.Message); app.Changed();
    }
    public bool ConfirmStop()
    {
        var active = app.Game.State.Activity; if (active is null) return true;
        var result = MessageBox.Show($"奶蛙正在{active.Name}\n要先结束这次安排吗\n\n打工按当前进度结算部分奶豆", "奶蛙", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return false;
        var stopped = app.Game.Stop(DateTimeOffset.UtcNow); sequence.Clear();
        if (stopped.Animation == "idle") ResumeAnimation(); else PlaySequence(stopped.Animation);
        return true;
    }
    public void StartActivity(string id)
    {
        var result = app.Game.Start(id, DateTimeOffset.UtcNow);
        if (result.Success) { sequence.Clear(); Play(result.Animation); }
        Say(result.Message); app.Changed();
    }
    public void StopActivity()
    {
        if (app.Game.State.Activity is null) return;
        if (MessageBox.Show("结束这次安排吗", "奶蛙", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var result = app.Game.Stop(DateTimeOffset.UtcNow); sequence.Clear();
        if (result.Animation == "wake") PlaySequence("wake"); else ResumeAnimation(); Say(result.Message); app.Changed();
    }
    private void SleepOrWake() { if (app.Game.State.Activity?.Kind == "sleep") Perform(app.Game.Stop(DateTimeOffset.UtcNow)); else if (ConfirmStop()) StartActivity("sleep"); }
    private void MouseDownPet(object sender, MouseButtonEventArgs e)
    {
        NativeMethods.GetCursorPos(out dragStart); NativeMethods.GetWindowRect(handle, out dragBounds);
        pressed = true; dragging = false; image.CaptureMouse(); e.Handled = true;
    }
    private void MouseMovePet(object sender, MouseEventArgs e)
    {
        if (!pressed || app.Game.State.Settings.Locked) return;
        NativeMethods.GetCursorPos(out var cursor); int dx = cursor.X - dragStart.X, dy = cursor.Y - dragStart.Y;
        if (!dragging && Math.Abs(dx) + Math.Abs(dy) >= 6)
        {
            dragging = true; sequence.Clear(); toolbar.IsOpen = bubble.IsOpen = false;
            if (player.Current is not null) NativeMethods.ApplyRegion(handle, player.Current, true);
            Play("drag");
        }
        if (dragging) NativeMethods.SetWindowPos(handle, 0, dragBounds.Left + dx, dragBounds.Top + dy, 0, 0, NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);
    }
    private void MouseUpPet(object sender, MouseButtonEventArgs e) { if (pressed) EndDrag(); e.Handled = true; }
    private void EndDrag()
    {
        bool moved = dragging; pressed = dragging = false; image.ReleaseMouseCapture();
        if (moved) { ClampPosition(); SavePosition(); app.Save(); if (app.Game.State.Activity is null) PlaySequence("land"); else ResumeAnimation(); }
        else if (app.Game.State.Activity?.Kind == "sleep") { Perform(app.Game.Stop(DateTimeOffset.UtcNow)); }
        else if (app.Game.State.Activity is null && !player.IsOneShot) Perform(app.Game.Pet(DateTimeOffset.UtcNow));
        if (player.Current is not null) NativeMethods.ApplyRegion(handle, player.Current, false, sleepMarks.Visibility == Visibility.Visible);
    }
    public void ApplySettings()
    {
        Width = Height = app.Game.State.Settings.Size; Topmost = app.Game.State.Settings.Topmost;
        player.Speed = app.Game.State.Settings.Speed; player.ReducedMotion = app.Game.State.Settings.ReducedMotion; player.Refresh();
        if (!app.Game.State.Settings.ShowBubbles) bubble.IsOpen = false;
        Dispatcher.BeginInvoke(() => { if (handle != 0) ClampPosition(); });
    }
    private void RestorePosition(bool primary = false)
    {
        var area = Forms.Screen.PrimaryScreen!.WorkingArea;
        if (handle == 0) return;
        NativeMethods.GetWindowRect(handle, out var rect);
        int x = !primary && app.Game.State.Settings.X is int savedX ? savedX : area.Right - rect.Width - 60;
        int y = !primary && app.Game.State.Settings.Y is int savedY ? savedY : area.Bottom - rect.Height - 8;
        NativeMethods.SetWindowPos(handle, 0, x, y, 0, 0, NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate); ClampPosition();
    }
    private void ClampPosition()
    {
        if (handle == 0 || !NativeMethods.GetWindowRect(handle, out var rect)) return;
        var screen = Forms.Screen.FromRectangle(new Drawing.Rectangle(rect.Left, rect.Top, rect.Width, rect.Height)); var area = screen.WorkingArea;
        int x = Math.Clamp(rect.Left, area.Left, Math.Max(area.Left, area.Right - rect.Width));
        int y = Math.Clamp(rect.Top, area.Top, Math.Max(area.Top, area.Bottom - rect.Height));
        NativeMethods.SetWindowPos(handle, 0, x, y, 0, 0, NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);
    }
    public void SavePosition() { if (handle != 0 && NativeMethods.GetWindowRect(handle, out var r)) { app.Game.State.Settings.X = r.Left; app.Game.State.Settings.Y = r.Top; } }
    public void HidePet() { manualHidden = true; toolbar.IsOpen = bubble.IsOpen = false; Hide(); app.Panel?.Hide(); }
    public void RestorePet() { manualHidden = false; if (!IsVisible) Show(); ClampPosition(); }
    private nint WindowHook(nint hwnd, int message, nint w, nint l, ref bool handled)
    {
        if (message == NativeMethods.WmMouseActivate) { handled = true; return 3; }
        if (message == NativeMethods.WmDisplayChange || message == NativeMethods.WmDpiChanged) Dispatcher.BeginInvoke(() => { ClampPosition(); if (player.Current is not null) NativeMethods.ApplyRegion(handle, player.Current, false, sleepMarks.Visibility == Visibility.Visible); });
        return 0;
    }
    private void Tick(object? sender, EventArgs e)
    {
        double ms = watch.Elapsed.TotalMilliseconds; player.Step(ms - lastAnimationTick); lastAnimationTick = ms;
        var now = DateTimeOffset.UtcNow;
        if ((now - lastGameTick).TotalSeconds >= 1)
        {
            var completed = app.Game.Advance(now - lastGameTick, now); lastGameTick = now;
            if (completed is not null) { if (!dragging) Perform(completed); else app.Changed(); }
            if (playButton is not null)
            {
                playButton.IsEnabled = app.Game.State.Energy >= 5;
                playButton.ToolTip = playButton.IsEnabled ? null : "精力不足，需要先睡觉";
            }
            if (app.Game.IsTired && app.Game.State.Activity?.Kind != "sleep")
            {
                if (!tiredAnnounced) { tiredAnnounced = true; Say("精力耗尽啦，现在只想休息"); }
                if (!dragging && !player.IsOneShot && player.Name != "tired") { sequence.Clear(); Play("tired"); }
            }
            else if (tiredAnnounced && app.Game.State.Energy >= 5)
            {
                tiredAnnounced = false;
                if (!dragging && player.Name == "tired") ResumeAnimation();
            }
            app.Panel?.UpdateLiveStatus();
            if (now >= nextSave) { SavePosition(); app.Save(); nextSave = now.AddSeconds(20); }
            var rescue = app.Game.Rescue(now); if (rescue is not null) { Say(rescue.Message); app.Changed(); }
            if (now >= nextIdle)
            {
                nextIdle = now.AddSeconds(random.Next(28, 55));
                if (!dragging && !manualHidden && app.Game.State.Activity is null && !player.IsOneShot && !app.Game.State.Settings.ReducedMotion)
                {
                    string clip = app.Game.State.Health < 40 ? "sick" : app.Game.State.Energy < 25 ? "yawn" : app.Game.State.Mood < 30 ? "sad" : random.Next(3) == 0 ? "stretch" : "wave";
                    PlaySequence(clip);
                }
            }
        }
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true; clock.Stop(); hoverTimer.Stop(); bubbleTimer.Stop();
        toolbar.IsOpen = bubble.IsOpen = false; petMenu?.Dispose(); tray.Visible = false; tray.ContextMenuStrip?.Dispose(); tray.Dispose(); trayIcon.Dispose();
        source?.RemoveHook(WindowHook);
    }
}
