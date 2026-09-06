using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace KanbanApp.Services;

public class AppSettings
{
    public int MaxToDo { get; set; } = 3;
    public int MaxInProgress { get; set; } = 3;
    public List<string> KnownIdentities { get; set; } = new();
}

public class SettingsService
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KanbanApp");

    private static readonly string FilePath = Path.Combine(DataDirectory, "settings.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public AppSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(DataDirectory);
        var json = JsonSerializer.Serialize(settings, Options);
        File.WriteAllText(FilePath, json);
    }
}
