namespace KanbanApp.Models;

public class TaskComment
{
    public string Text { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.Now;
}
