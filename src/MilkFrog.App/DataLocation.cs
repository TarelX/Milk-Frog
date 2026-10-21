using System;
using System.IO;
using Microsoft.Win32;

namespace MilkFrog.App;

internal static class DataLocation
{
    private const string KeyPath = @"Software\MilkFrog";
    private const string ValueName = "DataDirectory";
    internal static string Resolve()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            if (key?.GetValue(ValueName) is string configured && TryPrepare(configured, out var saved)) return saved;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }

        string portable = Path.Combine(AppContext.BaseDirectory, "UserData");
        if (TryPrepare(portable, out portable))
        {
            MigrateLegacy(portable);
            return portable;
        }
        string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MilkFrog");
        TryPrepare(fallback, out fallback);
        return fallback;
    }
    internal static void Save(string directory)
    {
        if (!TryPrepare(directory, out var full)) throw new IOException("选择的目录不可写");
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, true);
        key.SetValue(ValueName, full, RegistryValueKind.String);
    }
    internal static bool TryPrepare(string directory, out string full)
    {
        full = "";
        try
        {
            full = Path.GetFullPath(directory); Directory.CreateDirectory(full);
            string probe = Path.Combine(full, ".milkfrog-write-" + Guid.NewGuid().ToString("N"));
            using (File.Create(probe, 1, FileOptions.DeleteOnClose)) { }
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { return false; }
    }
    private static void MigrateLegacy(string destination)
    {
        string legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MilkFrog");
        if (Path.GetFullPath(legacy).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase)) return;
        string target = Path.Combine(destination, "state.json");
        if (File.Exists(target) || !File.Exists(Path.Combine(legacy, "state.json"))) return;
        try
        {
            File.Copy(Path.Combine(legacy, "state.json"), target, false);
            string backup = Path.Combine(legacy, "state.json.bak");
            if (File.Exists(backup)) File.Copy(backup, Path.Combine(destination, "state.json.bak"), false);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
