using System;
using System.IO;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using MilkFrog.Core;

namespace MilkFrog.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var identity = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        var name = "Local\\MilkFrog_" + identity.Replace('\\', '_');
        using var mutex = new Mutex(true, name, out bool first);
        using var restore = new EventWaitHandle(false, EventResetMode.AutoReset, name + "_Restore");
        if (!first) { restore.Set(); return; }
        try
        {
            var app = new FrogApplication();
            var wait = ThreadPool.RegisterWaitForSingleObject(restore, (_, _) => app.Dispatcher.BeginInvoke(() => app.Pet?.RestorePet()), null, Timeout.Infinite, false);
            try { app.Run(); } finally { wait.Unregister(null); }
        }
        catch (Exception error)
        {
            var log = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MilkFrog", "startup.log");
            try { Directory.CreateDirectory(Path.GetDirectoryName(log)!); File.WriteAllText(log, error.ToString()); } catch (IOException) { }
            MessageBox.Show("奶蛙暂时没能启动\n\n" + error.Message + "\n\n诊断记录：" + log, "奶蛙", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally { mutex.ReleaseMutex(); }
    }
}

public sealed class FrogApplication : Application
{
    public GameEngine Game { get; private set; } = null!;
    public StateStore Store { get; private set; } = null!;
    public AnimationAssets Assets { get; private set; } = null!;
    public PetWindow? Pet { get; private set; }
    public PanelWindow? Panel { get; private set; }
    public bool Exiting { get; private set; }
    private DateTimeOffset lastSaveError;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e); ShutdownMode = ShutdownMode.OnExplicitShutdown;
        Theme.Install(Resources);
        Store = new(DataLocation.Resolve());
        Game = new(Store.Load());
        var now = DateTimeOffset.UtcNow;
        var offline = now - Game.State.LastSavedAt;
        var completion = offline > TimeSpan.Zero ? Game.Advance(offline, now, true) : null;
        var visit = Game.Visit(now);
        Assets = new();
        Pet = new(this); MainWindow = Pet; Pet.Show();
        Pet.Dispatcher.InvokeAsync(() =>
        {
            if (Store.RecoveryMessage is not null) Pet.Say(Store.RecoveryMessage);
            else if (completion is not null) Pet.Perform(completion);
            else if (visit.Success) Pet.Perform(visit);
            else Pet.Say("今天也要一起，慢慢长大呀");
        }, DispatcherPriority.ApplicationIdle);
        Save();
    }
    public void OpenPanel(string page)
    {
        if (Pet is null) return;
        if (Panel is null) { Panel = new(this); Panel.Closed += (_, _) => Panel = null; }
        Panel.ShowPage(page);
        if (!Panel.IsVisible) { Panel.Owner = Pet; Panel.Show(); Panel.PlaceNear(Pet); }
        Panel.Activate();
    }
    public void Changed() { Panel?.RefreshStatus(); Save(); }
    public void Save()
    {
        try { Store.Save(Game.State, DateTimeOffset.UtcNow); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            if (DateTimeOffset.UtcNow - lastSaveError > TimeSpan.FromMinutes(2)) { lastSaveError = DateTimeOffset.UtcNow; Pet?.Say("暂时无法保存记录，请检查数据目录的写入权限"); }
        }
    }
    public void ApplySettings(bool save = true)
    {
        Pet?.ApplySettings();
        if (Panel is not null) Panel.Topmost = Game.State.Settings.Topmost;
        if (save) Save();
    }
    public string ChangeDataDirectory(string directory)
    {
        try
        {
            if (!DataLocation.TryPrepare(directory, out var full)) return "这个目录不可写，请选择其他位置";
            if (Path.GetFullPath(Store.DirectoryPath).Equals(full, StringComparison.OrdinalIgnoreCase)) return "当前已经使用这个目录";
            var target = new StateStore(full); target.Save(Game.State, DateTimeOffset.UtcNow); DataLocation.Save(full); Store = target;
            return "用户数据已经迁移到新目录";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { return "迁移失败：" + error.Message; }
    }
    public void ExitPet()
    {
        if (Exiting) return;
        Exiting = true; Pet?.SavePosition(); Save(); Panel?.Close(); Pet?.Dispose(); Shutdown();
    }
    protected override void OnExit(ExitEventArgs e) { if (!Exiting) { Exiting = true; Save(); Pet?.Dispose(); } base.OnExit(e); }
}
