using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using KanbanApp.Models;

namespace KanbanApp.Services;

public class TaskStorageService
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KanbanApp");

    private static readonly string FilePath = Path.Combine(DataDirectory, "tasks.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public List<TaskItem> Load()
    {
        if (!File.Exists(FilePath))
        {
            return new List<TaskItem>();
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<TaskItem>>(json, Options) ?? new List<TaskItem>();
        }
        catch (JsonException)
        {
            // A hand-edited tasks.json can end up malformed; start fresh
            // rather than crash on launch.
            return new List<TaskItem>();
        }
    }

    public void Save(IEnumerable<TaskItem> tasks)
    {
        Directory.CreateDirectory(DataDirectory);
        var json = JsonSerializer.Serialize(tasks, Options);
        File.WriteAllText(FilePath, json);
    }
}
