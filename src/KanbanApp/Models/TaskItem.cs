using CommunityToolkit.Mvvm.ComponentModel;

namespace KanbanApp.Models;

public partial class TaskItem : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public int Order { get; set; }

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private ColumnType column;

    public TaskItem(string title, ColumnType column)
    {
        this.title = title;
        this.column = column;
    }
}
