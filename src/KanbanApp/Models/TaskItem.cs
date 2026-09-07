using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KanbanApp.Models;

public partial class TaskItem : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public ObservableCollection<TaskComment> Comments { get; init; } = new();

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private ColumnType column;

    [ObservableProperty]
    private double originalEstimate;

    [ObservableProperty]
    private double completedWork;

    [ObservableProperty]
    private double remainingWork;

    [ObservableProperty]
    private int order;

    [ObservableProperty]
    private DateTime? completedAt;

    [ObservableProperty]
    private DateTime? archivedAt;

    [JsonConstructor]
    public TaskItem(string title, ColumnType column)
    {
        this.title = title;
        this.column = column;
    }

    partial void OnColumnChanged(ColumnType value)
    {
        if (value == ColumnType.Done)
        {
            CompletedAt = DateTime.Now;
        }
    }
}
