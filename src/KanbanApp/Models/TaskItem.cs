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

    [JsonConstructor]
    public TaskItem(string title, ColumnType column)
    {
        this.title = title;
        this.column = column;
    }
}
