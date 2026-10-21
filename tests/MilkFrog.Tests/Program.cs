using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using MilkFrog.Core;
using MilkFrog.App;

internal static class Tests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 5, 0, 0, TimeSpan.Zero);
    private static int passed;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Test(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string root = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
            Test("new pet stays young at Lv.1", () => { var game = new GameEngine(new PetState()); Check(game.Level.Level == 1 && game.State.Stage == "young", "initial growth"); });
            Test("daily gift is idempotent", () => { var game = new GameEngine(new()); game.Visit(Now); int coins = game.State.Coins; game.Visit(Now); Check(game.State.Coins == coins && game.State.Experience == 5, "duplicate gift"); });
            Test("levels never change form", () => { var game = new GameEngine(new() { Experience = 5000, Stage = "standard" }); Check(game.Level.Level > 10 && game.State.Stage == "young", "unexpected adult state"); });
            Test("purchase charges once and adds item", () => { var game = new GameEngine(new()); var before = game.Count("milk"); game.Buy("milk", Now); Check(game.State.Coins == 110 && game.Count("milk") == before + 1, "purchase"); });
            Test("permanent toy cannot be bought twice", () => { var game = new GameEngine(new()); game.Buy("ball", Now); var again = game.Buy("ball", Now); Check(!again.Success && game.State.Coins == 90 && game.Count("ball") == 1, "toy duplicate"); });
            Test("insufficient funds preserve state", () => { var game = new GameEngine(new() { Coins = 0 }); int count = game.Count("milk"); Check(!game.Buy("milk", Now).Success && game.Count("milk") == count && game.State.Coins == 0, "negative wallet"); });
            Test("feeding updates inventory and caps values", () => { var game = new GameEngine(new() { Satiety = 85 }); var result = game.Use("milk", Now); Check(result.Success && result.Animation == "drink" && game.State.Satiety == 100 && game.Count("milk") == 1, "feeding"); });
            Test("full pet rejects food without consumption", () => { var game = new GameEngine(new() { Satiety = 95 }); Check(!game.Use("milk", Now).Success && game.Count("milk") == 2, "wasted food"); });
            Test("active activities require deliberate interruption", () => { var game = new GameEngine(new()); game.Start("count", Now); var result = game.Use("milk", Now); Check(result.NeedsConfirmation && game.Count("milk") == 2, "interruption"); });
            Test("work prerequisites are enforced", () => { var game = new GameEngine(new()); Check(game.Start("tidy", Now).Success, "starter income must remain available"); game.Stop(Now); Check(!game.Start("welcome", Now).Success, "level lock"); game.State.Experience = 100; Check(!game.Start("cloud", Now).Success, "skill lock"); });
            Test("exhausted pet enters tired restrictions", () => { var game = new GameEngine(new() { Energy = 0, Inventory = new() { ["ball"] = 1 } }); Check(game.IsTired && !game.Use("ball", Now).Success && game.Count("ball") == 1, "play while exhausted"); Check(!game.Start("tidy", Now).Success && game.Start("sleep", Now).Success, "activity tired lock"); });
            Test("high level unlocks extend to Lv.30", () => { var game = new GameEngine(new() { Experience = 5000, Skills = new() { ["专注"] = 10 } }); Check(game.Level.Level >= 30 && game.ActivityBlock(GameCatalog.FindActivity("night")!) is null, "high unlock"); });
            Test("study completes exactly once", () => { var game = new GameEngine(new()); game.Start("count", Now); var result = game.CompleteActivity(Now.AddMinutes(10)); Check(result?.Success == true && game.Skill("计算") == 1, "course completion"); Check(game.CompleteActivity(Now.AddMinutes(11)) is null && game.Skill("计算") == 1, "duplicate course"); });
            Test("work cancellation pays partial earnings", () => { var game = new GameEngine(new() { Experience = 40 }); game.Start("welcome", Now); game.Stop(Now.AddMinutes(2.5)); Check(game.State.Coins == 122 && game.State.Activity is null, "partial pay"); });
            Test("sleep recovers energy and stays nonnegative", () => { var game = new GameEngine(new() { Energy = 10 }); game.Start("sleep", Now); game.Advance(TimeSpan.FromMinutes(10), Now.AddMinutes(10)); Check(game.State.Energy == 30 && game.State.Activity is null, "sleep"); });
            Test("offline decay capped and low states not refilled", () => { var game = new GameEngine(new() { Satiety = 75 }); game.Advance(TimeSpan.FromDays(100), Now, true); Check(game.State.Satiety == 27, "offline cap"); game.State.Satiety = 5; game.Advance(TimeSpan.FromHours(1), Now.AddHours(1), true); Check(game.State.Satiety == 5, "offline refill exploit"); });
            Test("clock reversal grants no extra reward", () => { var game = new GameEngine(new()); game.Visit(Now); var coins = game.State.Coins; game.Advance(TimeSpan.FromMinutes(-30), Now.AddMinutes(-30)); game.Visit(Now.AddMinutes(-30)); Check(game.State.Coins == coins, "backwards clock"); });
            Test("petting cooldown limits growth", () => { var game = new GameEngine(new()); for (int i = 0; i < 50; i++) game.Pet(Now); Check(game.State.Experience == 2, "petting farm"); });
            Test("companion time earns both experience and milk beans", () => { var game = new GameEngine(new()); game.Advance(TimeSpan.FromMinutes(25), Now); Check(game.State.Experience == 3 && game.State.Coins == 122, "companion reward"); });
            Test("rescue package is daily and prevents empty-wallet trap", () => { var game = new GameEngine(new() { Coins = 0, Satiety = 10, Inventory = new() }); Check(game.Rescue(Now)?.Success == true && game.Rescue(Now) is null && game.Count("milk") == 1 && game.State.Coins == 10, "rescue"); });
            Test("atomic save, backup and damaged-record recovery", () =>
            {
                string dir = Path.Combine(root, "artifacts", "tests", Guid.NewGuid().ToString("N")); var store = new StateStore(dir); var state = new PetState { Coins = 73 };
                store.Save(state, Now); state.Coins = 99; store.Save(state, Now.AddMinutes(1)); Check(store.Load().Coins == 99 && File.Exists(store.FilePath + ".bak"), "round trip");
                File.WriteAllText(store.FilePath, "broken json"); Check(store.Load().Coins == 73 && store.RecoveryMessage is not null, "backup recovery");
            });
            var assets = new AnimationAssets(Path.Combine(root, "assets", "generated", "young", "animations"));
            Test("all 136 frames decode as 512 square PNGs", () =>
            {
                var files = assets.Catalog.Clips.Values.SelectMany(c => c.Frames).Select(f => f.File).Distinct().ToArray(); Check(files.Length == 136, "frame count");
                foreach (var file in files) { var sprite = assets.Get(file); Check(sprite.Width == 512 && sprite.Height == 512 && sprite.Alpha.Any(a => a == 0) && sprite.Alpha.Any(a => a > 240), "frame decode " + file); }
            });
            Test("share build reads protected embedded artwork", () =>
            {
                var embedded = new AnimationAssets();
                Check(embedded.Catalog.Clips.Count == 24 && embedded.Get("frames/drag/07.png").Width == 512, "embedded animation");
                Check(embedded.GetItem("milk-cup.png").PixelWidth > 0, "embedded item");
                using var icon = embedded.GetAppIcon(); Check(icon.Width > 0, "embedded icon");
            });
            Test("24 clips and native playback completion", () =>
            {
                Check(assets.Catalog.Clips.Count == 24 && assets.Catalog.Clips.ContainsKey("tired"), "clip count"); var player = new AnimationPlayer(assets); int completions = 0; player.Completed += _ => completions++;
                player.Play("wave", false); for (int i = 0; i < 60; i++) player.Step(100); Check(completions == 1, "one-shot repeat");
                player.Play("sleep"); for (int i = 0; i < 150; i++) player.Step(100); Check(completions == 1 && player.Name == "sleep", "sleep loop");
                player.Play("work", false); for (int i = 0; i < 60; i++) player.Step(100); Check(completions == 2, "loop override");
            });
            Test("native control styles construct without showing windows", () =>
            {
                var theme = typeof(FrogApplication).Assembly.GetType("MilkFrog.App.Theme")!;
                theme.GetMethod("Install", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { new ResourceDictionary() });
            });
            Test("native topmost survives layer changes and respects settings without focus theft", WindowLayerChecks.Run);
            Test("application icon contains visible multi-size images", () =>
            {
                var icon = File.ReadAllBytes(Path.Combine(root, "src", "MilkFrog.App", "Assets", "app.ico"));
                Check(icon.Length > 20000 && icon[2] == 1 && BitConverter.ToUInt16(icon, 4) == 4, "icon directory");
                using var preview = new System.Drawing.Bitmap(Path.Combine(root, "src", "MilkFrog.App", "Assets", "app-icon.png"));
                Check(preview.GetPixel(0, 0).A == 0 && preview.GetPixel(preview.Width / 2, preview.Height / 2).A > 240, "transparent icon background");
            });
            Console.WriteLine($"All {passed} checks passed"); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
