using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MilkFrog.App;

public sealed class AnimationCatalog
{
    [JsonPropertyName("clips")] public Dictionary<string, AnimationClip> Clips { get; set; } = new();
    [JsonPropertyName("aliases")] public Dictionary<string, string> Aliases { get; set; } = new();
}
public sealed class AnimationClip
{
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("frames")] public List<AnimationFrame> Frames { get; set; } = new();
    [JsonPropertyName("durationMs")] public int DurationMs { get; set; }
    [JsonPropertyName("loop")] public bool Loop { get; set; }
    [JsonPropertyName("loopStart")] public int LoopStart { get; set; }
}
public sealed class AnimationFrame
{
    [JsonPropertyName("file")] public string File { get; set; } = "";
    [JsonPropertyName("ms")] public int Milliseconds { get; set; }
}
public sealed record SpriteBitmap(BitmapSource Bitmap, byte[] Alpha, int Width, int Height);

public sealed class AnimationAssets
{
    private const string AnimationPrefix = "MilkFrog.Assets/animations/";
    private const string ItemPrefix = "MilkFrog.Assets/items/";
    private readonly Assembly? resourceAssembly;
    private readonly Dictionary<string, string>? resourceNames;
    public string Root { get; }
    public AnimationCatalog Catalog { get; }
    private readonly Dictionary<string, SpriteBitmap> cache = new();
    private readonly Queue<string> order = new();
    public AnimationAssets(string root)
    {
        Root = Path.GetFullPath(root);
        Catalog = JsonSerializer.Deserialize<AnimationCatalog>(System.IO.File.ReadAllText(Path.Combine(Root, "animations.json"))) ?? throw new InvalidDataException("动作清单为空");
        ValidateCatalog();
    }
    public AnimationAssets()
    {
        Root = "embedded://MilkFrog/Assets/animations";
        resourceAssembly = typeof(AnimationAssets).Assembly;
        resourceNames = resourceAssembly.GetManifestResourceNames().ToDictionary(Normalize, value => value, StringComparer.OrdinalIgnoreCase);
        using var catalog = OpenAnimation("animations.json");
        Catalog = JsonSerializer.Deserialize<AnimationCatalog>(catalog) ?? throw new InvalidDataException("动作清单为空");
        ValidateCatalog();
    }
    private void ValidateCatalog()
    {
        if (!Catalog.Clips.ContainsKey("tired")) Catalog.Clips["tired"] = new()
        {
            Label = "疲惫",
            Loop = true,
            Frames =
            [
                new() { File = "frames/stretch/01.png", Milliseconds = 500 },
                new() { File = "frames/stretch/02.png", Milliseconds = 700 },
                new() { File = "frames/stretch/03.png", Milliseconds = 850 },
                new() { File = "frames/stretch/02.png", Milliseconds = 500 },
                new() { File = "frames/stretch/01.png", Milliseconds = 500 },
                new() { File = "frames/sad/03.png", Milliseconds = 1800 },
                new() { File = "frames/sad/04.png", Milliseconds = 1800 },
                new() { File = "frames/sad/03.png", Milliseconds = 1200 },
            ]
        };
        foreach (var clip in Catalog.Clips.Values)
        {
            if (clip.Frames.Count == 0 || clip.Frames.Any(f => f.Milliseconds <= 0) || clip.LoopStart < 0 || clip.LoopStart >= clip.Frames.Count) throw new InvalidDataException("动作时序无效");
            clip.DurationMs = clip.Frames.Sum(f => f.Milliseconds);
            foreach (var frame in clip.Frames) if (!ExistsAnimation(frame.File)) throw new FileNotFoundException("缺少动作帧", frame.File);
        }
        if (!Catalog.Clips.ContainsKey("idle")) throw new InvalidDataException("缺少待机动作");
    }
    public string Resolve(string relative)
    {
        var path = Path.GetFullPath(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("动作路径无效");
        return path;
    }
    private static string Normalize(string value) => value.Replace('\\', '/');
    private bool ExistsAnimation(string relative) => resourceAssembly is null ? System.IO.File.Exists(Resolve(relative)) : resourceNames!.ContainsKey(Normalize(AnimationPrefix + relative));
    private Stream OpenAnimation(string relative)
    {
        if (resourceAssembly is null) return System.IO.File.OpenRead(Resolve(relative));
        return OpenEmbedded(AnimationPrefix + relative);
    }
    private Stream OpenEmbedded(string logicalName)
    {
        var normalized = Normalize(logicalName);
        if (resourceAssembly is null || resourceNames is null || !resourceNames.TryGetValue(normalized, out var actual)) throw new FileNotFoundException("缺少内嵌资源", logicalName);
        return resourceAssembly.GetManifestResourceStream(actual) ?? throw new FileNotFoundException("无法读取内嵌资源", logicalName);
    }
    public string Name(string name) => Catalog.Aliases.GetValueOrDefault(name, name);
    public SpriteBitmap Get(string file)
    {
        if (cache.TryGetValue(file, out var found)) return found;
        using var stream = OpenAnimation(file);
        var bitmap = LoadBitmap(stream);
        var pixels = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0); pixels.Freeze();
        int w = bitmap.PixelWidth, h = bitmap.PixelHeight;
        var bytes = new byte[w * h * 4]; pixels.CopyPixels(bytes, w * 4, 0);
        var alpha = new byte[w * h]; for (int i = 0; i < alpha.Length; i++) alpha[i] = bytes[i * 4 + 3];
        var entry = new SpriteBitmap(bitmap, alpha, w, h);
        while (order.Count >= 48) cache.Remove(order.Dequeue());
        cache[file] = entry; order.Enqueue(file); return entry;
    }
    public BitmapSource GetItem(string file)
    {
        if (resourceAssembly is null)
        {
            using var stream = System.IO.File.OpenRead(Path.Combine(Path.GetDirectoryName(Root)!, "items", file));
            return LoadBitmap(stream);
        }
        using var embedded = OpenEmbedded(ItemPrefix + file);
        return LoadBitmap(embedded);
    }
    public System.Drawing.Icon GetAppIcon()
    {
        using var stream = resourceAssembly is null
            ? System.IO.File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"))
            : OpenEmbedded("MilkFrog.Assets/app.ico");
        using var icon = new System.Drawing.Icon(stream);
        return (System.Drawing.Icon)icon.Clone();
    }
    private static BitmapImage LoadBitmap(Stream stream)
    {
        var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze(); return bitmap;
    }
}

public sealed class AnimationPlayer(AnimationAssets assets)
{
    public event Action<SpriteBitmap>? FrameChanged;
    public event Action<string>? Completed;
    public string Name { get; private set; } = "idle";
    public double Speed { get; set; } = 1;
    public bool ReducedMotion { get; set; }
    public SpriteBitmap? Current { get; private set; }
    private AnimationClip? clip;
    private bool loop;
    private double elapsed;
    private int lastIndex = -1;
    private bool finished;
    public bool IsOneShot => clip is not null && !loop && !finished;
    public void Play(string name, bool? loopOverride = null)
    {
        Name = assets.Name(name);
        if (!assets.Catalog.Clips.TryGetValue(Name, out clip)) { Name = "idle"; clip = assets.Catalog.Clips[Name]; }
        elapsed = 0; lastIndex = -1; finished = false; loop = loopOverride ?? clip.Loop; Draw();
    }
    public void Step(double milliseconds)
    {
        if (clip is null || finished) return;
        elapsed += Math.Clamp(milliseconds, 0, 250) * Math.Clamp(Speed, .5, 1.5);
        if (elapsed >= clip.DurationMs)
        {
            if (loop)
            {
                double prefix = clip.Frames.Take(clip.LoopStart).Sum(f => f.Milliseconds);
                elapsed = prefix + (elapsed - clip.DurationMs) % (clip.DurationMs - prefix);
            }
            else { elapsed = clip.DurationMs; finished = true; }
        }
        Draw();
        if (finished) Completed?.Invoke(Name);
    }
    public void Refresh() { lastIndex = -1; Draw(); }
    private void Draw()
    {
        if (clip is null) return;
        int index = 0; var time = elapsed;
        while (index < clip.Frames.Count - 1 && time >= clip.Frames[index].Milliseconds) { time -= clip.Frames[index].Milliseconds; index++; }
        if (ReducedMotion) index = Name == "sleep" ? clip.Frames.Count - 1 : clip.Frames.Count / 2;
        if (index == lastIndex) return;
        lastIndex = index; Current = assets.Get(clip.Frames[index].File); FrameChanged?.Invoke(Current);
    }
}
