using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using KanbanApp.Models;

namespace KanbanApp.Views;

public partial class WipLimitDialog : Window
{
    public TaskItem? SelectedTask { get; private set; }
    public bool MarkAsDone { get; private set; }
    public string? UpdatedTitle { get; private set; }

    public WipLimitDialog(IEnumerable<TaskItem> inProgressTasks, int limit)
    {
        InitializeComponent();
        HeaderText.Text = $"In Progress is at your limit of {limit}. Mark a task Done, or update one that's only partly finished and send it back to To Do.";
        TaskListBox.ItemsSource = inProgressTasks.ToList();
    }

    private void TaskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = TaskListBox.SelectedItem is TaskItem;
        DoneButton.IsEnabled = hasSelection;
        RequeueButton.IsEnabled = hasSelection;
        EditTitleBox.IsEnabled = hasSelection;

        if (TaskListBox.SelectedItem is TaskItem task)
        {
            EditTitleBox.Text = task.Title;
        }
    }

    private void MarkDone_Click(object sender, RoutedEventArgs e)
    {
        SelectedTask = TaskListBox.SelectedItem as TaskItem;
        MarkAsDone = true;
        DialogResult = true;
    }

    private void Requeue_Click(object sender, RoutedEventArgs e)
    {
        SelectedTask = TaskListBox.SelectedItem as TaskItem;
        MarkAsDone = false;
        UpdatedTitle = EditTitleBox.Text.Trim();
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
