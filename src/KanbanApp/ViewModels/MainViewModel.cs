using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KanbanApp.Models;
using KanbanApp.Services;
using KanbanApp.Views;

namespace KanbanApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly Regex MentionRegex = new(@"@(\w+)", RegexOptions.Compiled);

    private readonly TaskStorageService _taskStorage = new();
    private readonly SettingsService _settingsService = new();

    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<string> Identities { get; } = new();

    public ICollectionView ToDoTasks { get; }
    public ICollectionView InProgressTasks { get; }
    public ICollectionView DoneTasks { get; }
    public ICollectionView ParkedTasks { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddTaskCommand))]
    private string newTaskTitle = string.Empty;

    [ObservableProperty]
    private int maxInProgress = 3;

    [ObservableProperty]
    private int maxToDo = 3;

    [ObservableProperty]
    private int completedTodayCount;

    public MainViewModel()
    {
        var settings = _settingsService.Load();
        maxToDo = settings.MaxToDo;
        maxInProgress = settings.MaxInProgress;
        foreach (var identity in settings.KnownIdentities)
        {
            Identities.Add(identity);
        }
        _settingsService.Save(settings);

        Tasks.CollectionChanged += Tasks_CollectionChanged;

        var loadedTasks = _taskStorage.Load();
        if (loadedTasks.Count > 0)
        {
            foreach (var task in loadedTasks)
            {
                Tasks.Add(task);
            }
        }
        else
        {
            // First run: seed a few sample tasks so the board isn't empty.
            Tasks.Add(new TaskItem("Set up project structure", ColumnType.Done));
            Tasks.Add(new TaskItem("Wire up data binding", ColumnType.InProgress));
            Tasks.Add(new TaskItem("Add move buttons", ColumnType.ToDo));
            Tasks.Add(new TaskItem("Write WIP limit logic", ColumnType.ToDo));
        }

        // Normalizes Order to a clean 0..N-1 sequence per column, fixing
        // legacy/default values from before Order was tracked, or from a
        // hand-edited tasks.json.
        foreach (ColumnType column in Enum.GetValues<ColumnType>())
        {
            ReorderColumn(column);
        }

        ToDoTasks = CreateFilteredView(ColumnType.ToDo);
        InProgressTasks = CreateFilteredView(ColumnType.InProgress);
        DoneTasks = CreateFilteredView(ColumnType.Done);
        ParkedTasks = CreateFilteredView(ColumnType.Parked);

        RecomputeCompletedTodayCount();
    }

    private void Tasks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (TaskItem task in e.NewItems)
            {
                task.PropertyChanged += Task_PropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (TaskItem task in e.OldItems)
            {
                task.PropertyChanged -= Task_PropertyChanged;
            }
        }

        SaveTasks();
        RecomputeCompletedTodayCount();
    }

    private void Task_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveTasks();
        RecomputeCompletedTodayCount();
    }

    private void SaveTasks() => _taskStorage.Save(Tasks);

    private void RecomputeCompletedTodayCount()
    {
        var today = DateTime.Today;
        CompletedTodayCount = Tasks.Count(t => t.Column == ColumnType.Done && t.CompletedAt?.Date == today);
    }

    private void SaveSettings() =>
        _settingsService.Save(new AppSettings { MaxToDo = MaxToDo, MaxInProgress = MaxInProgress, KnownIdentities = Identities.ToList() });

    partial void OnMaxToDoChanged(int value) => SaveSettings();

    partial void OnMaxInProgressChanged(int value) => SaveSettings();

    /// Scans text for "@word" tokens and adds any not already known,
    /// so they show up as suggestions the next time "@" is typed anywhere.
    private void LearnMentions(string text)
    {
        var learnedAny = false;
        foreach (Match match in MentionRegex.Matches(text))
        {
            var name = match.Groups[1].Value;
            if (!Identities.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                Identities.Add(name);
                learnedAny = true;
            }
        }

        if (learnedAny)
        {
            SaveSettings();
        }
    }

    private ICollectionView CreateFilteredView(ColumnType column)
    {
        var view = new ListCollectionView(Tasks)
        {
            Filter = item => item is TaskItem task && task.Column == column,
            SortDescriptions = { new SortDescription(nameof(TaskItem.Order), ListSortDirection.Ascending) }
        };

        if (view is ICollectionViewLiveShaping liveShaping && liveShaping.CanChangeLiveFiltering)
        {
            liveShaping.LiveFilteringProperties.Add(nameof(TaskItem.Column));
            liveShaping.IsLiveFiltering = true;

            if (liveShaping.CanChangeLiveSorting)
            {
                liveShaping.LiveSortingProperties.Add(nameof(TaskItem.Order));
                liveShaping.IsLiveSorting = true;
            }
        }

        return view;
    }

    [RelayCommand(CanExecute = nameof(CanAddTask))]
    private void AddTask()
    {
        var column = ToDoHasRoom() ? ColumnType.ToDo : ColumnType.Parked;
        var title = NewTaskTitle.Trim();
        var newTask = new TaskItem(title, column);
        Tasks.Add(newTask);
        AppendToEnd(newTask);
        LearnMentions(title);
        NewTaskTitle = string.Empty;

        if (column == ColumnType.Parked)
        {
            MessageBox.Show(
                $"To Do is at your limit of {MaxToDo}. This task was parked instead.",
                "To Do limit reached",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private bool CanAddTask() => !string.IsNullOrWhiteSpace(NewTaskTitle);

    private bool ToDoHasRoom() => Tasks.Count(t => t.Column == ColumnType.ToDo) < MaxToDo;

    private void ApplyInProgressGate(TaskItem task)
    {
        int inProgressCount = Tasks.Count(t => t.Column == ColumnType.InProgress);
        if (inProgressCount < MaxInProgress)
        {
            task.Column = ColumnType.InProgress;
            return;
        }

        var inProgressTasks = Tasks.Where(t => t.Column == ColumnType.InProgress).ToList();
        var dialog = new WipLimitDialog(inProgressTasks, MaxInProgress)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true || dialog.SelectedTask is null)
        {
            return;
        }

        if (dialog.MarkAsDone)
        {
            dialog.SelectedTask.Column = ColumnType.Done;
        }
        else
        {
            ApplyToDoGate(dialog.SelectedTask, dialog.UpdatedTitle);
        }

        task.Column = ColumnType.InProgress;
    }

    private void ApplyToDoGate(TaskItem task, string? updatedTitle = null)
    {
        if (!string.IsNullOrWhiteSpace(updatedTitle))
        {
            UpdateTaskTitle(task, updatedTitle);
        }

        if (ToDoHasRoom())
        {
            task.Column = ColumnType.ToDo;
            return;
        }

        task.Column = ColumnType.Parked;
        MessageBox.Show(
            $"To Do is at your limit of {MaxToDo}. This task was parked instead.",
            "To Do limit reached",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// Decides the destination column, applying WIP-limit gating for To Do
    /// and In Progress; Done/Parked have no overflow concern going backward.
    private void MoveToColumnRespectingLimits(TaskItem task, ColumnType targetColumn)
    {
        if (task.Column == targetColumn)
        {
            return;
        }

        switch (targetColumn)
        {
            case ColumnType.ToDo:
                ApplyToDoGate(task);
                break;
            case ColumnType.InProgress:
                ApplyInProgressGate(task);
                break;
            case ColumnType.Done:
                task.Column = ColumnType.Done;
                break;
            case ColumnType.Parked:
                task.Column = ColumnType.Parked;
                break;
        }
    }

    /// Single entry point for every move: buttons, drag-and-drop between
    /// columns, and drag-and-drop reordering within a column all funnel
    /// through here so WIP gating and ordering stay consistent everywhere.
    /// insertBefore positions the task ahead of a specific card in the
    /// resulting column; null appends it to the end.
    public void MoveTask(TaskItem task, ColumnType targetColumn, TaskItem? insertBefore)
    {
        if (task.Column == targetColumn && insertBefore is null)
        {
            return;
        }

        var previousColumn = task.Column;
        MoveToColumnRespectingLimits(task, targetColumn);

        if (insertBefore is not null && insertBefore != task && insertBefore.Column == task.Column)
        {
            InsertBefore(task, insertBefore);
        }
        else
        {
            AppendToEnd(task);
        }

        if (previousColumn != task.Column)
        {
            ReorderColumn(previousColumn);
        }
    }

    private void InsertBefore(TaskItem task, TaskItem target)
    {
        var column = task.Column;
        var ordered = Tasks.Where(t => t.Column == column && t != task).OrderBy(t => t.Order).ToList();
        int targetIndex = ordered.IndexOf(target);
        ordered.Insert(targetIndex, task);

        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].Order = i;
        }
    }

    private void AppendToEnd(TaskItem task)
    {
        var column = task.Column;
        int maxOrder = Tasks.Where(t => t.Column == column && t != task)
            .Select(t => t.Order)
            .DefaultIfEmpty(-1)
            .Max();
        task.Order = maxOrder + 1;
    }

    private void ReorderColumn(ColumnType column)
    {
        var ordered = Tasks.Where(t => t.Column == column).OrderBy(t => t.Order).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].Order = i;
        }
    }

    [RelayCommand]
    private void MoveToInProgress(TaskItem task) => MoveTask(task, ColumnType.InProgress, null);

    [RelayCommand]
    private void MoveToDone(TaskItem task) => MoveTask(task, ColumnType.Done, null);

    [RelayCommand]
    private void MoveToToDo(TaskItem task) => MoveTask(task, ColumnType.ToDo, null);

    [RelayCommand]
    private void ShiftDown(TaskItem task)
    {
        var ordered = Tasks.Where(t => t.Column == task.Column).OrderBy(t => t.Order).ToList();
        int index = ordered.IndexOf(task);
        if (index < 0 || index == ordered.Count - 1)
        {
            return;
        }

        var next = ordered[index + 1];
        (task.Order, next.Order) = (next.Order, task.Order);
    }

    [RelayCommand]
    private void EditTask(TaskItem task)
    {
        var dialog = new EditTaskDialog(task.Title, Identities)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.UpdatedTitle is not null)
        {
            UpdateTaskTitle(task, dialog.UpdatedTitle);
        }
    }

    [RelayCommand]
    private void DeleteTask(TaskItem task)
    {
        var result = MessageBox.Show(
            $"Delete \"{task.Title}\"? This can't be undone.",
            "Delete task",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var column = task.Column;
        Tasks.Remove(task);
        ReorderColumn(column);
    }

    public void UpdateTaskTitle(TaskItem task, string newTitle)
    {
        task.Title = newTitle;
        LearnMentions(newTitle);
    }

    public void AddComment(TaskItem task, string text)
    {
        task.Comments.Add(new TaskComment { Text = text });
        LearnMentions(text);
        SaveTasks();
    }

    public void OpenTaskDetail(TaskItem task)
    {
        var window = new TaskDetailWindow(task, this)
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }
}
