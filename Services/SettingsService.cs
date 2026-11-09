using System.IO;
using System.Text.Json;

namespace OPoptimizer.Services;

public sealed class AppSettings
{
    public bool? OriginalClientAnimationsEnabled { get; set; }
}

public sealed class SettingsService
{
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OPoptimizer", "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath)) ?? new();
        }
        catch (Exception)
        {
            // Bad or inaccessible preferences should not prevent launching the app.
        }
        return new();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        string tempFile = _settingsPath + ".tmp";
        File.WriteAllText(tempFile, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tempFile, _settingsPath, overwrite: true);
    }
}
