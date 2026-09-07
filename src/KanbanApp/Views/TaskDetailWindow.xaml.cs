using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using KanbanApp.Models;
using KanbanApp.ViewModels;

namespace KanbanApp.Views;

public partial class TaskDetailWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly TaskItem _task;

    public TaskDetailWindow(TaskItem task, MainViewModel viewModel)
    {
        InitializeComponent();
        _task = task;
        _viewModel = viewModel;

        TitleBox.Identities = viewModel.Identities;
        NewCommentBox.Identities = viewModel.Identities;

        TitleBox.Text = task.Title;
        OriginalEstimateBox.Text = task.OriginalEstimate.ToString(CultureInfo.InvariantCulture);
        CompletedWorkBox.Text = task.CompletedWork.ToString(CultureInfo.InvariantCulture);
        RemainingWorkBox.Text = task.RemainingWork.ToString(CultureInfo.InvariantCulture);
        CommentsList.ItemsSource = task.Comments;

        Closing += (_, _) => SaveFields();
    }

    private void SaveFields()
    {
        if (!string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            _viewModel.UpdateTaskTitle(_task, TitleBox.Text.Trim());
        }

        if (double.TryParse(OriginalEstimateBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var estimate))
        {
            _task.OriginalEstimate = estimate;
        }

        if (double.TryParse(CompletedWorkBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var completed))
        {
            _task.CompletedWork = completed;
        }

        if (double.TryParse(RemainingWorkBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var remaining))
        {
            _task.RemainingWork = remaining;
        }
    }

    private void AddComment_Click(object sender, RoutedEventArgs e)
    {
        var text = NewCommentBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _viewModel.AddComment(_task, text);
        NewCommentBox.Text = string.Empty;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
