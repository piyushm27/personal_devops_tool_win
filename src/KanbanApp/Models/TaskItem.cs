namespace KanbanApp.Models;

public class TaskItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Title { get; set; }
    public ColumnType Column { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public int Order { get; set; }
}
