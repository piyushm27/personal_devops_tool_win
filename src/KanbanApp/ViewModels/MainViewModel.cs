using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using KanbanApp.Models;

namespace KanbanApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<TaskItem> Tasks { get; } = new();

    public ICollectionView ToDoTasks { get; }
    public ICollectionView InProgressTasks { get; }
    public ICollectionView DoneTasks { get; }
    public ICollectionView ParkedTasks { get; }

    public MainViewModel()
    {
        // Sample data for now; replaced by JSON load/save in a later step.
        Tasks.Add(new TaskItem("Set up project structure", ColumnType.Done));
        Tasks.Add(new TaskItem("Wire up data binding", ColumnType.InProgress));
        Tasks.Add(new TaskItem("Add move buttons", ColumnType.ToDo));
        Tasks.Add(new TaskItem("Write WIP limit logic", ColumnType.ToDo));

        ToDoTasks = CreateFilteredView(ColumnType.ToDo);
        InProgressTasks = CreateFilteredView(ColumnType.InProgress);
        DoneTasks = CreateFilteredView(ColumnType.Done);
        ParkedTasks = CreateFilteredView(ColumnType.Parked);
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
}
