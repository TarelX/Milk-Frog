using System.Text.Json;

namespace MilkFrog.Core;

public sealed class StateStore(string directory)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public string DirectoryPath { get; } = directory;
    public string FilePath => Path.Combine(DirectoryPath, "state.json");
    public string? RecoveryMessage { get; private set; }
    public PetState Load()
    {
        if (!File.Exists(FilePath)) return new();
        try { return JsonSerializer.Deserialize<PetState>(File.ReadAllText(FilePath)) ?? throw new JsonException("Empty state"); }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
        {
            if (File.Exists(FilePath + ".bak"))
            {
                try { var backup = JsonSerializer.Deserialize<PetState>(File.ReadAllText(FilePath + ".bak")); if (backup is not null) { RecoveryMessage = "已从最近一次备份恢复奶蛙记录"; return backup; } }
                catch (Exception backupError) when (backupError is JsonException or IOException or UnauthorizedAccessException) { }
            }
            try { File.Copy(FilePath, FilePath + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"), false); }
            catch (IOException) { }
            RecoveryMessage = "旧记录已保留副本，已为奶蛙建立新记录";
            return new();
        }
    }
    public void Save(PetState state, DateTimeOffset now)
    {
        Directory.CreateDirectory(DirectoryPath); state.LastSavedAt = now;
        var temporary = Path.Combine(DirectoryPath, "state-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            { JsonSerializer.Serialize(stream, state, Options); stream.Flush(true); }
            if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak", true);
            else File.Move(temporary, FilePath);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
