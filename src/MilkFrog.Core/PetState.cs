namespace MilkFrog.Core;

public sealed class PetState
{
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "奶蛙";
    public string Stage { get; set; } = "young";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSavedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Experience { get; set; }
    public int Coins { get; set; } = 120;
    public double Satiety { get; set; } = 75;
    public double Cleanliness { get; set; } = 80;
    public double Mood { get; set; } = 70;
    public double Health { get; set; } = 100;
    public double Energy { get; set; } = 75;
    public double LowNeedsMinutes { get; set; }
    public double CompanionMinutes { get; set; }
    public Dictionary<string, int> Inventory { get; set; } = new() { ["milk"] = 2, ["pudding"] = 1, ["bubble"] = 2 };
    public Dictionary<string, int> Skills { get; set; } = new() { ["计算"] = 0, ["社交"] = 0, ["专注"] = 0, ["艺术"] = 0 };
    public string RewardDay { get; set; } = "";
    public string LastVisitDay { get; set; } = "";
    public string LastRescueDay { get; set; } = "";
    public Dictionary<string, int> Daily { get; set; } = new();
    public DateTimeOffset? LastPetRewardAt { get; set; }
    public ActivityState? Activity { get; set; }
    public List<MemoryEntry> Memories { get; set; } = new();
    public PetSettings Settings { get; set; } = new();
}

public sealed class PetSettings
{
    public int Size { get; set; } = 230;
    public bool Topmost { get; set; } = true;
    public bool Locked { get; set; }
    public bool ReducedMotion { get; set; }
    public bool ShowBubbles { get; set; } = true;
    public double Speed { get; set; } = 1;
    public int? X { get; set; }
    public int? Y { get; set; }
}

public sealed class ActivityState
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
}

public sealed record MemoryEntry(DateTimeOffset At, string Text);
public sealed record GameResult(bool Success, string Message, string Animation = "idle", bool NeedsConfirmation = false);
public sealed record Item(string Id, string Name, string Image, int Price, string Category, string Effect, string Animation,
    int Satiety = 0, int Cleanliness = 0, int Mood = 0, int Health = 0, int Energy = 0, bool Permanent = false);
public sealed record Activity(string Id, string Name, string Kind, int Minutes, int Level, int Coins = 0,
    string? Skill = null, int SkillGain = 0, string? RequiredSkill = null, int RequiredSkillLevel = 0);
public sealed record Milestone(int Level, string Name);

public static class GameCatalog
{
    public static readonly Item[] Items =
    [
        new("milk", "小杯牛奶", "milk-cup.png", 10, "食物", "饱食 +30 · 心情 +5", "drink", Satiety:30, Mood:5),
        new("pudding", "奶香布丁", "milk-pudding.png", 18, "食物", "饱食 +20 · 心情 +15", "pudding", Satiety:20, Mood:15),
        new("dumplings", "青草团子", "grass-dumplings.png", 14, "食物", "饱食 +25 · 健康 +3", "snack", Satiety:25, Health:3),
        new("bubble", "小泡泡", "bubble-wand.png", 8, "清洁", "清洁 +25", "bath", Cleanliness:25),
        new("wash", "奶香浴液", "bath-wash.png", 15, "清洁", "清洁 +45 · 心情 +5", "bath", Cleanliness:45, Mood:5),
        new("ball", "薄荷小球", "toy-ball.png", 30, "玩具", "心情 +15 · 精力 −5", "play", Mood:15, Energy:-5, Permanent:true),
    ];
    public static readonly Activity[] Activities =
    [
        new("tidy", "整理奶豆罐", "work", 3, 1, Coins:5),
        new("count", "数数课", "study", 10, 1, Skill:"计算", SkillGain:1),
        new("manners", "礼貌课", "study", 10, 1, Skill:"社交", SkillGain:1),
        new("welcome", "奶站迎宾", "work", 5, 2, Coins:8),
        new("cloud", "云朵擦拭员", "work", 10, 3, Coins:18, RequiredSkill:"社交", RequiredSkillLevel:2),
        new("stamps", "邮票整理员", "work", 15, 4, Coins:30, RequiredSkill:"计算", RequiredSkillLevel:3),
        new("music", "音乐课", "study", 15, 5, Skill:"艺术", SkillGain:1),
        new("focus", "一起专注", "study", 25, 7, Skill:"专注", SkillGain:2),
        new("performer", "午后小乐手", "work", 20, 12, Coins:50, RequiredSkill:"艺术", RequiredSkillLevel:3),
        new("library", "图书管理员", "work", 25, 15, Coins:65, RequiredSkill:"计算", RequiredSkillLevel:5),
        new("weather", "云端气象员", "work", 25, 20, Coins:85, RequiredSkill:"专注", RequiredSkillLevel:6),
        new("radio", "奶蛙电台主持", "work", 30, 25, Coins:110, RequiredSkill:"社交", RequiredSkillLevel:8),
        new("night", "星星守夜员", "work", 30, 30, Coins:140, RequiredSkill:"专注", RequiredSkillLevel:10),
        new("sleep", "睡一个暖暖的小觉", "sleep", 10, 1),
    ];
    public static readonly Milestone[] Milestones =
    [
        new(2, "奶站迎宾"), new(3, "云朵擦拭员"), new(4, "邮票整理员"), new(5, "音乐课"),
        new(7, "一起专注"), new(12, "午后小乐手"), new(15, "图书管理员"), new(20, "云端气象员"),
        new(25, "奶蛙电台主持"), new(30, "星星守夜员")
    ];
    public static Item? FindItem(string id) => Items.FirstOrDefault(x => x.Id == id);
    public static Activity? FindActivity(string id) => Activities.FirstOrDefault(x => x.Id == id);
}
