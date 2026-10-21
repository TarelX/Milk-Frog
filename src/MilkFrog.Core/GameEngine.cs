namespace MilkFrog.Core;

public sealed class GameEngine
{
    public PetState State { get; }
    public GameEngine(PetState state) { State = state; Normalize(); }

    public (int Level, int Current, int Needed) Level
    {
        get
        {
            int level = 1, remaining = State.Experience;
            while (level < 99 && remaining >= 20 + level * 10) { remaining -= 20 + level * 10; level++; }
            return (level, remaining, 20 + level * 10);
        }
    }
    public int Count(string id) => State.Inventory.GetValueOrDefault(id);
    public int Skill(string name) => State.Skills.GetValueOrDefault(name);
    public bool IsTired => State.Energy <= 0.01;
    private static double Clamp(double value) => double.IsFinite(value) ? Math.Clamp(value, 0, 100) : 70;
    private static string Day(DateTimeOffset now) => now.LocalDateTime.ToString("yyyy-MM-dd");
    private void Normalize()
    {
        State.Stage = "young";
        State.Name = string.IsNullOrWhiteSpace(State.Name) ? "奶蛙" : State.Name.Trim()[..Math.Min(12, State.Name.Trim().Length)];
        State.Experience = Math.Clamp(State.Experience, 0, 100000);
        State.Coins = Math.Clamp(State.Coins, 0, 10000000);
        State.Satiety = Clamp(State.Satiety); State.Cleanliness = Clamp(State.Cleanliness);
        State.Mood = Clamp(State.Mood); State.Health = Clamp(State.Health); State.Energy = Clamp(State.Energy);
        State.Inventory ??= new(); State.Skills ??= new(); State.Daily ??= new(); State.Memories ??= new(); State.Settings ??= new();
        foreach (var skill in new[] { "计算", "社交", "专注", "艺术" }) State.Skills.TryAdd(skill, 0);
        foreach (var id in State.Inventory.Keys.ToArray()) State.Inventory[id] = Math.Clamp(State.Inventory[id], 0, 9999);
        foreach (var id in State.Skills.Keys.ToArray()) State.Skills[id] = Math.Clamp(State.Skills[id], 0, 9999);
        State.Settings.Size = Math.Clamp(State.Settings.Size, 150, 340);
        State.Settings.Speed = double.IsFinite(State.Settings.Speed) ? Math.Clamp(State.Settings.Speed, .5, 1.5) : 1;
        State.CompanionMinutes = double.IsFinite(State.CompanionMinutes) ? Math.Clamp(State.CompanionMinutes, 0, 24.99) : 0;
        State.LowNeedsMinutes = double.IsFinite(State.LowNeedsMinutes) ? Math.Clamp(State.LowNeedsMinutes, 0, 60) : 0;
        if (State.Activity is { } a)
        {
            var definition = GameCatalog.FindActivity(a.Id);
            if (definition is null || a.EndsAt <= a.StartedAt || a.EndsAt - a.StartedAt > TimeSpan.FromHours(4)) State.Activity = null;
            else { a.Kind = definition.Kind; a.Name = definition.Name; }
        }
    }
    public void Remember(string text, DateTimeOffset now)
    {
        State.Memories.Insert(0, new(now, text));
        if (State.Memories.Count > 80) State.Memories.RemoveRange(80, State.Memories.Count - 80);
    }
    private void EnsureDay(DateTimeOffset now)
    {
        if (State.RewardDay == Day(now)) return;
        State.RewardDay = Day(now); State.Daily.Clear(); State.CompanionMinutes = 0;
    }
    private void AddExperience(int amount) => State.Experience = Math.Min(100000, State.Experience + amount);
    public GameResult Visit(DateTimeOffset now)
    {
        EnsureDay(now);
        if (State.LastVisitDay == Day(now)) return new(false, "今天也一起慢慢长大");
        State.LastVisitDay = Day(now); State.Coins += 15; AddExperience(5);
        Remember("今天又见到了你，真开心", now);
        return new(true, "今天的见面礼：15 奶豆 · 成长 +5", "wave");
    }
    public GameResult Pet(DateTimeOffset now)
    {
        EnsureDay(now); State.Mood = Clamp(State.Mood + 4);
        int earned = 0;
        if ((State.LastPetRewardAt is null || now - State.LastPetRewardAt >= TimeSpan.FromMinutes(15)) && State.Daily.GetValueOrDefault("pet") < 5)
        {
            earned = Math.Min(State.Daily.GetValueOrDefault("pet") == 0 ? 2 : 1, 5 - State.Daily.GetValueOrDefault("pet"));
            AddExperience(earned); State.Daily["pet"] = State.Daily.GetValueOrDefault("pet") + earned; State.LastPetRewardAt = now;
        }
        return new(true, earned > 0 ? $"被你摸摸就很开心 · 成长 +{earned}" : "嘿嘿，被你摸摸就很开心", "happy");
    }
    public GameResult Buy(string id, DateTimeOffset now)
    {
        var item = GameCatalog.FindItem(id);
        if (item is null) return new(false, "没有找到这件物品");
        if (item.Permanent && Count(id) > 0) return new(false, "这个玩具已经在背包里了");
        if (Count(id) >= 9999) return new(false, "背包里的这件物品已经很多啦");
        if (State.Coins < item.Price) return new(false, "奶豆有点不够，安排一份小工作吧");
        State.Coins -= item.Price; State.Inventory[id] = Count(id) + 1;
        return new(true, $"{item.Name}已放进背包 · −{item.Price} 奶豆");
    }
    public GameResult Use(string id, DateTimeOffset now)
    {
        EnsureDay(now);
        var item = GameCatalog.FindItem(id);
        if (item is null) return new(false, "没有找到这件物品");
        if (Count(id) <= 0) return new(false, $"背包里没有{item.Name}，先去商店看看");
        if (State.Activity is not null) return new(false, "先结束当前安排，再照顾奶蛙", NeedsConfirmation:true);
        if (item.Id == "ball" && State.Energy < 5) return new(false, "精力已经耗尽啦，先睡个小觉再玩", "tired");
        if (item.Satiety > 0 && State.Satiety > 90) return new(false, "肚子已经圆圆的啦，晚一点再吃");
        if (item.Cleanliness > 0 && State.Cleanliness >= 95) return new(false, "已经香香净净啦，留着下次用");
        if (!item.Permanent) State.Inventory[id] = Count(id) - 1;
        State.Satiety = Clamp(State.Satiety + item.Satiety); State.Cleanliness = Clamp(State.Cleanliness + item.Cleanliness);
        State.Mood = Clamp(State.Mood + item.Mood); State.Health = Clamp(State.Health + item.Health); State.Energy = Clamp(State.Energy + item.Energy);
        var rewardKey = item.Category == "食物" ? "food" : item.Category == "清洁" ? "bath" : "play";
        int earned = State.Daily.GetValueOrDefault(rewardKey) < 3 ? 3 : 0;
        if (earned > 0) { State.Daily[rewardKey] = State.Daily.GetValueOrDefault(rewardKey) + 1; AddExperience(earned); }
        Remember(item.Category == "清洁" ? "和你一起洗了个香香的澡" : $"和你一起享用了{item.Name}", now);
        return new(true, item.Name + " · " + item.Effect + (earned > 0 ? " · 成长 +3" : ""), item.Animation);
    }
    public string? ActivityBlock(Activity activity)
    {
        if (State.Activity is not null) return "已经有一项安排在进行";
        if (Level.Level < activity.Level) return $"Lv.{activity.Level} 解锁";
        if (activity.RequiredSkill is not null && Skill(activity.RequiredSkill) < activity.RequiredSkillLevel) return $"{activity.RequiredSkill} {activity.RequiredSkillLevel} 解锁";
        if (activity.Kind != "sleep" && State.Health < 40) return "先休息，照顾好身体";
        if (activity.Kind != "sleep" && State.Energy <= 0) return "精力已经耗尽，先睡个小觉";
        if (activity.Kind != "sleep" && State.Energy < 20) return "精力不足 20，先休息一下";
        return null;
    }
    public GameResult Start(string id, DateTimeOffset now)
    {
        var activity = GameCatalog.FindActivity(id);
        if (activity is null) return new(false, "没有找到这项安排");
        var block = ActivityBlock(activity); if (block is not null) return new(false, block);
        State.Activity = new() { Id = id, Name = activity.Name, Kind = activity.Kind, StartedAt = now, EndsAt = now.AddMinutes(activity.Minutes) };
        Remember("开始了" + activity.Name, now);
        return new(true, $"{activity.Name}开始啦 · {activity.Minutes} 分钟", activity.Kind);
    }
    public GameResult Stop(DateTimeOffset now)
    {
        var active = State.Activity;
        if (active is null) return new(false, "现在没有进行中的安排");
        if (now >= active.EndsAt) return CompleteActivity(now)!;
        int pay = 0;
        var activity = GameCatalog.FindActivity(active.Id)!;
        var ratio = Math.Clamp((now - active.StartedAt).TotalSeconds / Math.Max(1, (active.EndsAt - active.StartedAt).TotalSeconds), 0, 1);
        if (activity.Coins > 0) { pay = (int)Math.Floor(activity.Coins * ratio * .5); State.Coins += pay; }
        if (activity.Kind == "sleep") State.Energy = Clamp(State.Energy + 20 * ratio);
        State.Activity = null;
        return new(true, activity.Coins > 0 ? $"休息一下 · 本次结算 {pay} 奶豆" : "休息一下，再继续吧", activity.Kind == "sleep" ? "wake" : "idle");
    }
    public GameResult? CompleteActivity(DateTimeOffset now)
    {
        var active = State.Activity;
        if (active is null || now < active.EndsAt) return null;
        var activity = GameCatalog.FindActivity(active.Id)!;
        State.Activity = null; State.Coins += activity.Coins;
        if (activity.Skill is not null) State.Skills[activity.Skill] = Skill(activity.Skill) + activity.SkillGain;
        if (activity.Kind == "sleep") { State.Energy = Clamp(State.Energy + 20); State.Health = Clamp(State.Health + 4); }
        else AddExperience(5);
        Remember("完成了" + activity.Name + "，又长大了一点", now);
        return new(true, activity.Name + "完成啦" + (activity.Coins > 0 ? $" · +{activity.Coins} 奶豆" : activity.Skill is not null ? $" · {activity.Skill} +{activity.SkillGain}" : " · 精力 +20"), activity.Kind == "sleep" ? "wake" : "victory");
    }
    public GameResult? Advance(TimeSpan elapsed, DateTimeOffset now, bool offline = false)
    {
        EnsureDay(now);
        var minutes = Math.Max(0, Math.Min(elapsed.TotalMinutes, offline ? 480 : 60));
        var active = State.Activity;
        var activeMinutes = active is null ? 0 : Math.Clamp((active.EndsAt - (now - TimeSpan.FromMinutes(minutes))).TotalMinutes, 0, minutes);
        double weighted = minutes + activeMinutes * (active?.Kind == "work" ? .5 : active?.Kind == "study" ? .25 : 0);
        State.Satiety = Math.Max(offline ? Math.Min(20, State.Satiety) : 0, State.Satiety - weighted * .1);
        State.Cleanliness = Math.Max(offline ? Math.Min(20, State.Cleanliness) : 0, State.Cleanliness - weighted / 15);
        if (active?.Kind != "sleep") State.Energy = Math.Max(offline ? Math.Min(20, State.Energy) : 0, State.Energy - weighted / 12);
        State.Mood += Math.Sign(60 - State.Mood) * Math.Min(Math.Abs(60 - State.Mood), minutes / 30);
        if (!offline)
        {
            if (State.Satiety < 20 || State.Cleanliness < 20) State.LowNeedsMinutes += minutes; else State.LowNeedsMinutes = 0;
            if (State.LowNeedsMinutes > 30) State.Health = Clamp(State.Health - minutes / 10);
            else if (State.Satiety >= 40 && State.Cleanliness >= 40) State.Health = Clamp(State.Health + minutes / 30);
            State.CompanionMinutes += minutes;
            while (State.CompanionMinutes >= 25) { State.CompanionMinutes -= 25; if (State.Daily.GetValueOrDefault("companion") < 6) { State.Daily["companion"] = State.Daily.GetValueOrDefault("companion") + 1; AddExperience(3); State.Coins += 2; } }
        }
        var completed = CompleteActivity(now);
        if (completed is not null) return completed;
        if (!offline && State.Health < 40 && State.Activity?.Kind is "study" or "work")
        { var stopped = Stop(now); return stopped with { Message = "先休息一下，照顾好身体" }; }
        if (!offline && IsTired && State.Activity?.Kind is "study" or "work")
        { var stopped = Stop(now); return stopped with { Message = "精力已经耗尽啦，先进入疲惫休息状态", Animation = "tired" }; }
        return null;
    }
    public GameResult? Rescue(DateTimeOffset now)
    {
        if (State.Coins >= 5 || State.LastRescueDay == Day(now)) return null;
        State.LastRescueDay = Day(now); State.Coins += 10;
        if (State.Satiety < 30) State.Inventory["milk"] = Count("milk") + 1;
        if (State.Cleanliness < 30) State.Inventory["bubble"] = Count("bubble") + 1;
        return new(true, "收到每日低余额救助：10 奶豆" + (State.Satiety < 30 || State.Cleanliness < 30 ? "和照料用品" : ""), "wave");
    }
}
