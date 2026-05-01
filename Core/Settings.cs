using System.Text.Json;
using System.Text.Json.Serialization;

namespace RBX_AntiAFK.Core;

public class Settings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly string SettingsFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Roblox-AntiAFK", "settings.json");

    private static readonly object FileLock = new();

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ActionTypeEnum ActionType { get; set; } = ActionTypeEnum.CameraShift;

    public bool EnableWindowMaximization { get; set; } = false;
    public int WindowMaximizationDelaySeconds { get; set; } = 3;
    public bool HideWindowContentsOnMaximizing { get; set; } = false;
    public int DelayBeforeWindowInteractionMilliseconds { get; set; } = 50;
    public int DelayBetweenKeyPressMilliseconds { get; set; } = 45;

    public void Load()
    {
        try
        {
            lock (FileLock)
            {
                if (!File.Exists(SettingsFile))
                {
                    SaveInternal();
                    return;
                }

                var json = File.ReadAllText(SettingsFile);
                var loaded = JsonSerializer.Deserialize<Settings>(json, JsonOptions);
                if (loaded == null)
                {
                    SaveInternal();
                    return;
                }

                ActionType = loaded.ActionType;
                EnableWindowMaximization = loaded.EnableWindowMaximization;
                WindowMaximizationDelaySeconds = loaded.WindowMaximizationDelaySeconds;
                HideWindowContentsOnMaximizing = loaded.HideWindowContentsOnMaximizing;
                DelayBeforeWindowInteractionMilliseconds = loaded.DelayBeforeWindowInteractionMilliseconds;
                DelayBetweenKeyPressMilliseconds = loaded.DelayBetweenKeyPressMilliseconds;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }
    }

    public void Save()
    {
        lock (FileLock)
        {
            SaveInternal();
        }
    }

    private void SaveInternal()
    {
        try
        {
            var file = SettingsFile;
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(file, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    public Settings Clone() => (Settings)MemberwiseClone();
}
