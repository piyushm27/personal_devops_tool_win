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

        ToDoTasks = CreateFilteredView(ColumnType.ToDo);
        InProgressTasks = CreateFilteredView(ColumnType.InProgress);
        DoneTasks = CreateFilteredView(ColumnType.Done);
        ParkedTasks = CreateFilteredView(ColumnType.Parked);
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
    }

    private void Task_PropertyChanged(object? sender, PropertyChangedEventArgs e) => SaveTasks();

    private void SaveTasks() => _taskStorage.Save(Tasks);

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
            Filter = item => item is TaskItem task && task.Column == column
        };

        if (view is ICollectionViewLiveShaping liveShaping && liveShaping.CanChangeLiveFiltering)
        {
            liveShaping.LiveFilteringProperties.Add(nameof(TaskItem.Column));
            liveShaping.IsLiveFiltering = true;
        }

        return view;
    }

    [RelayCommand(CanExecute = nameof(CanAddTask))]
    private void AddTask()
    {
        var column = ToDoHasRoom() ? ColumnType.ToDo : ColumnType.Parked;
        var title = NewTaskTitle.Trim();
        Tasks.Add(new TaskItem(title, column));
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

    [RelayCommand]
    private void MoveToInProgress(TaskItem task)
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
            RequeueToToDo(dialog.SelectedTask, dialog.UpdatedTitle);
        }

        task.Column = ColumnType.InProgress;
    }

    private void RequeueToToDo(TaskItem task, string? updatedTitle)
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
            $"To Do is also at your limit of {MaxToDo}, so this task was parked instead.",
            "To Do limit reached",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private void MoveToDone(TaskItem task) => task.Column = ColumnType.Done;

    [RelayCommand]
    private void MoveToToDo(TaskItem task)
    {
        if (!ToDoHasRoom())
        {
            MessageBox.Show(
                $"To Do is at your limit of {MaxToDo}. This task will stay parked for now.",
                "To Do limit reached",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        task.Column = ColumnType.ToDo;
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
