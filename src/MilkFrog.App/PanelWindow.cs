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
using System.Windows.Media.Imaging;
using MilkFrog.Core;
using Forms = System.Windows.Forms;

namespace MilkFrog.App;

public sealed class PanelWindow : Window
{
    private static readonly string[] Pages = ["小状态", "学习打工", "商店", "背包", "成长记录", "设置"];
    private readonly FrogApplication app;
    private readonly ScrollViewer scroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, PanningMode = PanningMode.VerticalOnly };
    private readonly Button pickerButton = new() { Width = 110, HorizontalContentAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 10, 0) };
    private readonly Popup pickerPopup = new() { AllowsTransparency = true, StaysOpen = false, Placement = PlacementMode.Bottom, VerticalOffset = 5 };
    private readonly TextBlock wallet = Theme.Text("", 12, Theme.Green, true);
    private readonly List<(Func<double> Read, TextBlock Text, ProgressBar Bar)> liveStats = new();
    private TextBlock? activityClock;
    private ProgressBar? activityBar;
    private string page = "小状态", category = "全部";

    public PanelWindow(FrogApplication application)
    {
        app = application; Title = "奶蛙"; Width = 440; Height = 610; MinWidth = 360; MinHeight = 380;
        ShowInTaskbar = false; WindowStyle = WindowStyle.ToolWindow; Background = Theme.Paper;
        FontFamily = new FontFamily("Microsoft YaHei UI"); Foreground = Theme.Ink; Topmost = app.Game.State.Settings.Topmost;
        var layout = new DockPanel { Margin = new Thickness(18) };
        var chrome = new DockPanel { Margin = new Thickness(0, 0, 0, 14), LastChildFill = true };
        pickerButton.Click += (_, _) => pickerPopup.IsOpen = !pickerPopup.IsOpen;
        pickerPopup.PlacementTarget = pickerButton; pickerPopup.Child = BuildPicker();
        DockPanel.SetDock(pickerButton, Dock.Left); chrome.Children.Add(pickerButton);
        wallet.HorizontalAlignment = HorizontalAlignment.Right; wallet.VerticalAlignment = VerticalAlignment.Center; chrome.Children.Add(wallet);
        DockPanel.SetDock(chrome, Dock.Top); layout.Children.Add(chrome); layout.Children.Add(scroll); Content = layout;
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { if (pickerPopup.IsOpen) pickerPopup.IsOpen = false; else Hide(); e.Handled = true; } };
    }
    private UIElement BuildPicker()
    {
        var list = new StackPanel { Width = 132 };
        foreach (var name in Pages)
        {
            var selected = name; var button = Theme.Button(name, () => ShowPage(selected));
            button.HorizontalContentAlignment = HorizontalAlignment.Left; button.Margin = new Thickness(0, 2, 0, 2); list.Children.Add(button);
        }
        return new Border { Child = list, Background = Theme.Brush("#FFFDF6"), BorderBrush = Theme.Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(11), Padding = new Thickness(7), Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 15, ShadowDepth = 3, Opacity = .16, Color = Colors.DarkOliveGreen } };
    }
    public void ShowPage(string selected)
    {
        page = Pages.Contains(selected) ? selected : "小状态"; category = "全部"; pickerPopup.IsOpen = false;
        pickerButton.Content = page + "  ▾"; Title = app.Game.State.Name + " · " + page; Render(false);
    }
    public void PlaceNear(PetWindow pet)
    {
        var petHandle = new WindowInteropHelper(pet).Handle;
        if (!NativeMethods.GetWindowRect(petHandle, out var petRect)) return;
        var area = Forms.Screen.FromHandle(petHandle).WorkingArea;
        var hwnd = new WindowInteropHelper(this).Handle; NativeMethods.GetWindowRect(hwnd, out var rect);
        int x = petRect.Left - rect.Width - 15; if (x < area.Left) x = petRect.Right + 15;
        x = Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - rect.Width));
        int y = Math.Clamp(petRect.Top - rect.Height / 2, area.Top, Math.Max(area.Top, area.Bottom - rect.Height));
        NativeMethods.SetWindowPos(hwnd, 0, x, y, 0, 0, NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);
    }
    public void RefreshStatus() { if (page != "设置") Render(true); else UpdateLiveStatus(); }
    public void UpdateLiveStatus()
    {
        wallet.Text = $"{app.Game.State.Coins} 奶豆 · Lv.{app.Game.Level.Level}";
        foreach (var (read, text, bar) in liveStats) { double value = read(); text.Text = Math.Round(value) + " / 100"; bar.Value = value; }
        if (activityClock is not null)
        {
            var active = app.Game.State.Activity;
            if (active is null) { activityClock.Text = "当前安排已结束"; if (activityBar is not null) activityBar.Value = 100; }
            else
            {
                var remaining = active.EndsAt - DateTimeOffset.UtcNow;
                activityClock.Text = "还剩 " + (remaining > TimeSpan.Zero ? $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}" : "00:00");
                if (activityBar is not null) activityBar.Value = Math.Clamp(100 * (DateTimeOffset.UtcNow - active.StartedAt).TotalSeconds / Math.Max(1, (active.EndsAt - active.StartedAt).TotalSeconds), 0, 100);
            }
        }
    }
    private void Render(bool preserveScroll)
    {
        double offset = preserveScroll ? scroll.VerticalOffset : 0;
        liveStats.Clear(); activityClock = null; activityBar = null;
        var body = new StackPanel();
        switch (page)
        {
            case "商店": Items(body, false); break;
            case "背包": Items(body, true); break;
            case "学习打工": Activities(body); break;
            case "成长记录": Growth(body); break;
            case "设置": Settings(body); break;
            default: Status(body); break;
        }
        scroll.Content = body; UpdateLiveStatus(); Dispatcher.BeginInvoke(() => scroll.ScrollToVerticalOffset(offset));
    }
    private static void Heading(Panel panel, string title, string subtitle)
    { panel.Children.Add(Theme.Text(title, 20, bold:true)); var text = Theme.Text(subtitle, 11, Theme.Muted); text.Margin = new Thickness(0, 4, 0, 17); panel.Children.Add(text); }
    private ProgressBar Bar(double value) => new() { Minimum = 0, Maximum = 100, Value = Math.Clamp(value, 0, 100), Height = 6, Foreground = Theme.Brush("#91AA76"), Background = Theme.Mint, BorderThickness = new Thickness(0), Margin = new Thickness(0, 5, 0, 8) };
    private void Status(StackPanel body)
    {
        Heading(body, app.Game.State.Name + "的小状态", "好好吃饭，好好生活，保持幼年模样慢慢成长");
        var state = app.Game.State;
        if (app.Game.IsTired) body.Children.Add(Theme.Card(Theme.Text("精力已经耗尽，奶蛙进入疲惫状态\n现在不能玩耍、学习或打工，先让它睡一觉", 12, Theme.Brush("#9A663B"), true)));
        foreach (var (label, read) in new (string, Func<double>)[] { ("饱食", () => state.Satiety), ("清洁", () => state.Cleanliness), ("心情", () => state.Mood), ("健康", () => state.Health), ("精力", () => state.Energy) })
        {
            var row = new DockPanel(); var number = Theme.Text("", 11, Theme.Muted); DockPanel.SetDock(number, Dock.Right); row.Children.Add(number); row.Children.Add(Theme.Text(label, 12));
            var bar = Bar(read()); body.Children.Add(row); body.Children.Add(bar); liveStats.Add((read, number, bar));
        }
        var actions = new UniformGrid { Columns = 2, Margin = new Thickness(0, 10, 0, 10) };
        actions.Children.Add(Theme.Button("喂一杯奶", () => app.Pet?.UseItem("milk"))); actions.Children.Add(Theme.Button("洗个泡泡澡", () => app.Pet?.UseItem("bubble")));
        var play = Theme.Button("玩一会小球", () => app.Pet?.UseItem("ball")); play.IsEnabled = state.Energy >= 5; play.ToolTip = play.IsEnabled ? null : "精力不足，需要先睡觉"; actions.Children.Add(play);
        actions.Children.Add(Theme.Button(state.Activity?.Kind == "sleep" ? "叫它起床" : "睡个小觉", () => { if (state.Activity?.Kind == "sleep") app.Pet?.StopActivity(); else app.Pet?.StartActivity("sleep"); })); body.Children.Add(actions);
        var lv = app.Game.Level; body.Children.Add(Theme.Text($"Lv.{lv.Level} · 成长经验 {lv.Current} / {lv.Needed}", 13, Theme.Green, true)); body.Children.Add(Bar(100.0 * lv.Current / lv.Needed));
        ActiveCard(body); body.Children.Add(Theme.Text("等级持续解锁到 Lv.30，外观始终保持幼年形态", 11, Theme.Muted)); IncomeCard(body);
    }
    private void IncomeCard(StackPanel body)
    {
        var content = new StackPanel(); content.Children.Add(Theme.Text("奶豆怎么获得", 14, bold:true));
        content.Children.Add(Theme.Text("每日首次见面 +15\n每陪伴 25 分钟 +2，每日最多 6 次\nLv.1 可做整理奶豆罐 +5\n等级和技能越高，可解锁收益更高的工作\n低于 5 奶豆时，每天可获得一次 10 奶豆救助", 11, Theme.Muted));
        content.Children.Add(Theme.Button("去学习打工", () => ShowPage("学习打工"))); body.Children.Add(Theme.Card(content));
    }
    private void ActiveCard(StackPanel body)
    {
        if (app.Game.State.Activity is not { } active) return;
        var card = new StackPanel(); card.Children.Add(Theme.Text(active.Name, 14, bold:true)); activityClock = Theme.Text("", 12, Theme.Muted); card.Children.Add(activityClock);
        activityBar = Bar(0); card.Children.Add(activityBar); card.Children.Add(Theme.Button(active.Kind == "sleep" ? "叫它起床" : "结束安排", () => app.Pet?.StopActivity())); body.Children.Add(Theme.Card(card));
    }
    private void Items(StackPanel body, bool bag)
    {
        Heading(body, bag ? "奶蛙的百宝袋" : "小小商店", bag ? "想用的时候，拿出一点喜欢的小东西" : "一杯牛奶，一点泡泡，给它一个好心情");
        var filters = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        foreach (var name in new[] { "全部", "食物", "清洁", "玩具" }) { var selected = name; var button = Theme.Button(name, () => { category = selected; Render(false); }, category == name); filters.Children.Add(button); } body.Children.Add(filters);
        var grid = new UniformGrid { Columns = 2 };
        var items = GameCatalog.Items.Where(i => (category == "全部" || i.Category == category) && (!bag || app.Game.Count(i.Id) > 0)).ToArray();
        foreach (var item in items)
        {
            var stack = new StackPanel(); var bitmap = app.Assets.GetItem(item.Image);
            stack.Children.Add(new Image { Source = bitmap, Width = 122, Height = 112, Stretch = Stretch.Uniform, Margin = new Thickness(0, 0, 0, 8) });
            stack.Children.Add(Theme.Text(item.Name, 13, bold:true)); stack.Children.Add(Theme.Text(item.Effect, 11, Theme.Muted));
            stack.Children.Add(Theme.Text(bag ? $"拥有 {app.Game.Count(item.Id)} 件" : $"{item.Price} 奶豆", 11, Theme.Green));
            var button = Theme.Button(bag ? item.Permanent ? "一起玩" : "使用" : item.Permanent && app.Game.Count(item.Id) > 0 ? "已拥有" : "买一份", () =>
            {
                if (bag) app.Pet?.UseItem(item.Id);
                else { var result = app.Game.Buy(item.Id, DateTimeOffset.UtcNow); app.Pet?.Say(result.Message); app.Changed(); }
            });
            button.IsEnabled = bag ? item.Id != "ball" || app.Game.State.Energy >= 5 : !(item.Permanent && app.Game.Count(item.Id) > 0); stack.Children.Add(button);
            var border = Theme.Card(stack, 11); border.Margin = new Thickness(0, 0, 9, 10); grid.Children.Add(border);
        }
        body.Children.Add(grid);
        if (items.Length == 0) { body.Children.Add(Theme.Text("这里还空着，去商店挑一点吧", 13, Theme.Muted)); body.Children.Add(Theme.Button("逛逛商店", () => ShowPage("商店"))); }
        if (!bag) IncomeCard(body);
    }
    private void Activities(StackPanel body)
    {
        Heading(body, "今天安排一点什么", "学习新本领，也能做小工作赚取奶豆"); ActiveCard(body); IncomeCard(body);
        body.Children.Add(Theme.Text($"计算 {app.Game.Skill("计算")} · 社交 {app.Game.Skill("社交")} · 专注 {app.Game.Skill("专注")} · 艺术 {app.Game.Skill("艺术")}", 12, Theme.Green));
        foreach (var activity in GameCatalog.Activities)
        {
            var card = new StackPanel(); card.Children.Add(Theme.Text(activity.Name, 14, bold:true));
            string reward = activity.Coins > 0 ? $"{activity.Coins} 奶豆" : activity.Skill is not null ? $"{activity.Skill} +{activity.SkillGain}" : "精力 +20";
            card.Children.Add(Theme.Text($"Lv.{activity.Level} · {activity.Minutes} 分钟 · {reward}", 11, Theme.Muted));
            string? blocked = app.Game.ActivityBlock(activity); var button = Theme.Button(blocked ?? "开始安排", () => app.Pet?.StartActivity(activity.Id)); button.IsEnabled = blocked is null; card.Children.Add(button); body.Children.Add(Theme.Card(card));
        }
    }
    private void Growth(StackPanel body)
    {
        var state = app.Game.State; var lv = app.Game.Level; int days = Math.Max(1, (DateTimeOffset.UtcNow.LocalDateTime.Date - state.CreatedAt.LocalDateTime.Date).Days + 1);
        Heading(body, "我们的成长手记", $"一起度过第 {days} 天 · 幼年期");
        body.Children.Add(Theme.Text($"Lv.{lv.Level} · 累计 {state.Experience} 经验", 17, Theme.Green, true)); body.Children.Add(Bar(100.0 * lv.Current / lv.Needed));
        body.Children.Add(Theme.Text($"再获得 {Math.Max(0, lv.Needed - lv.Current)} 经验，就能升到下一等级", 12, Theme.Muted));
        body.Children.Add(Theme.Text("成长解锁", 14, bold:true));
        foreach (var milestone in GameCatalog.Milestones) body.Children.Add(Theme.Text($"{(lv.Level >= milestone.Level ? "已到达" : "待成长")}  Lv.{milestone.Level} · {milestone.Name}", 12));
        body.Children.Add(Theme.Text("Lv.30 后仍可继续提升等级，始终保持幼年外观", 11, Theme.Muted)); body.Children.Add(Theme.Text("最近的小小回忆", 14, bold:true));
        foreach (var memory in state.Memories.Take(24)) { body.Children.Add(Theme.Text(memory.Text, 12)); body.Children.Add(Theme.Text(memory.At.LocalDateTime.ToString("MM-dd HH:mm"), 11, Theme.Muted)); }
    }
    private void Settings(StackPanel body)
    {
        var settings = app.Game.State.Settings; Heading(body, "陪伴设置", "调整后立即在桌面奶蛙上生效");
        body.Children.Add(Theme.Text("奶蛙的名字")); var name = new TextBox { Text = app.Game.State.Name, MaxLength = 12, Margin = new Thickness(0, 5, 0, 14) }; body.Children.Add(name);
        void SaveName() { var value = name.Text.Trim(); if (value.Length == 0) return; app.Game.State.Name = value; Title = value + " · 设置"; app.Save(); }
        name.LostKeyboardFocus += (_, _) => SaveName(); name.KeyDown += (_, e) => { if (e.Key == Key.Enter) { SaveName(); Keyboard.ClearFocus(); e.Handled = true; } };
        void ToggleRow(string label, Func<bool> read, Action<bool> write)
        {
            var row = new DockPanel { Margin = new Thickness(0, 5, 0, 5) }; row.Children.Add(Theme.Text(label, 12));
            Button? button = null; button = Theme.Button(read() ? "开启" : "关闭", () => { write(!read()); button!.Content = read() ? "开启" : "关闭"; button.Background = read() ? Theme.Green : Theme.Mint; button.Foreground = read() ? Brushes.White : Theme.Ink; app.ApplySettings(); }, read());
            DockPanel.SetDock(button, Dock.Right); row.Children.Insert(0, button); body.Children.Add(row);
        }
        ToggleRow("始终置顶", () => settings.Topmost, value => settings.Topmost = value);
        ToggleRow("锁定当前位置", () => settings.Locked, value => settings.Locked = value);
        ToggleRow("轻柔动作", () => settings.ReducedMotion, value => settings.ReducedMotion = value);
        ToggleRow("显示临时气泡", () => settings.ShowBubbles, value => settings.ShowBubbles = value);
        body.Children.Add(Theme.Text("角色大小", 12, bold:true));
        var sizeText = Theme.Text(settings.Size + " px", 13, Theme.Green, true); var sizeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 15) };
        void SetSize(int value) { settings.Size = Math.Clamp(value, 150, 340); sizeText.Text = settings.Size + " px"; app.ApplySettings(); }
        sizeRow.Children.Add(Theme.Button("−", () => SetSize(settings.Size - 10))); sizeRow.Children.Add(sizeText); sizeText.Margin = new Thickness(12, 12, 12, 0); sizeRow.Children.Add(Theme.Button("+", () => SetSize(settings.Size + 10)));
        foreach (var value in new[] { 180, 230, 290 }) { var captured = value; sizeRow.Children.Add(Theme.Button(value == 180 ? "小" : value == 230 ? "中" : "大", () => SetSize(captured), settings.Size == value)); } body.Children.Add(sizeRow);
        body.Children.Add(Theme.Text("动作速度", 12, bold:true)); var speedRow = new WrapPanel { Margin = new Thickness(0, 5, 0, 14) };
        foreach (var entry in new[] { (.75, "轻慢 0.75×"), (1.0, "自然 1×"), (1.25, "活泼 1.25×") })
        { var captured = entry; speedRow.Children.Add(Theme.Button(entry.Item2, () => { settings.Speed = captured.Item1; app.ApplySettings(); Render(true); }, Math.Abs(settings.Speed - entry.Item1) < .01)); }
        body.Children.Add(speedRow); body.Children.Add(Theme.Button("找回奶蛙", () => app.Pet?.RestorePet()));
        body.Children.Add(Theme.Text("用户数据目录", 12, bold:true));
        var dataPath = Theme.Text(app.Store.DirectoryPath, 11, Theme.Muted); dataPath.Margin = new Thickness(0, 5, 0, 8); body.Children.Add(Theme.Card(dataPath, 10));
        var dataActions = new WrapPanel();
        dataActions.Children.Add(Theme.Button("更改目录", () =>
        {
            using var dialog = new Forms.FolderBrowserDialog { Description = "选择奶蛙用户数据保存目录", SelectedPath = app.Store.DirectoryPath, UseDescriptionForTitle = true, ShowNewFolderButton = true };
            if (dialog.ShowDialog() != Forms.DialogResult.OK) return;
            string result = app.ChangeDataDirectory(dialog.SelectedPath); dataPath.Text = app.Store.DirectoryPath; app.Pet?.Say(result);
        }, true));
        dataActions.Children.Add(Theme.Button("打开目录", () =>
        {
            try { Process.Start(new ProcessStartInfo("explorer.exe", app.Store.DirectoryPath) { UseShellExecute = true }); }
            catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException) { app.Pet?.Say("无法打开目录：" + error.Message); }
        }));
        body.Children.Add(dataActions);
        body.Children.Add(Theme.Text("默认保存在程序目录旁的 UserData 文件夹，旧版 C 盘记录会在首次启动时自动迁移", 11, Theme.Muted));
        body.Children.Add(Theme.Text("关闭面板会回到桌面陪伴，退出请使用托盘菜单", 11, Theme.Muted));
    }
}
