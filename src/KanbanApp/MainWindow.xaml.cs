using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KanbanApp.Models;
using KanbanApp.ViewModels;
using KanbanApp.Views;

namespace KanbanApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Point _dragStartPoint;
    private AdornerLayer? _adornerLayer;
    private DragAdorner? _dragAdorner;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2) return;
        if (sender is not Border { DataContext: TaskItem task }) return;
        if (DataContext is not MainViewModel viewModel) return;

        viewModel.OpenTaskDetail(task);
    }

    private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void Card_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not Border { DataContext: TaskItem task } border) return;

        var currentPosition = e.GetPosition(null);
        var diff = _dragStartPoint - currentPosition;

        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var root = (UIElement)Content;
        _adornerLayer = AdornerLayer.GetAdornerLayer(root);
        if (_adornerLayer is not null)
        {
            _dragAdorner = new DragAdorner(root, border, 0.6);
            _adornerLayer.Add(_dragAdorner);
        }

        try
        {
            DragDrop.DoDragDrop(border, task, DragDropEffects.Move);
        }
        finally
        {
            if (_dragAdorner is not null && _adornerLayer is not null)
            {
                _adornerLayer.Remove(_dragAdorner);
            }

            _dragAdorner = null;
            _adornerLayer = null;
        }
    }

    private void Window_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (_dragAdorner is null) return;

        var position = e.GetPosition((UIElement)Content);
        _dragAdorner.UpdatePosition(position.X + 12, position.Y + 12);
    }

    private void Card_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(TaskItem)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void Card_Drop(object sender, DragEventArgs e)
    {
        if (sender is not Border { DataContext: TaskItem targetTask }) return;
        if (e.Data.GetData(typeof(TaskItem)) is not TaskItem draggedTask) return;
        if (draggedTask == targetTask) { e.Handled = true; return; }
        if (DataContext is not MainViewModel viewModel) return;

        viewModel.MoveTask(draggedTask, targetTask.Column, targetTask);
        e.Handled = true;
    }

    private void Column_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(TaskItem)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void Column_Drop(object sender, DragEventArgs e)
    {
        if (e.Handled) return;
        if (sender is not Border { Tag: ColumnType targetColumn }) return;
        if (e.Data.GetData(typeof(TaskItem)) is not TaskItem task) return;
        if (DataContext is not MainViewModel viewModel) return;

        viewModel.MoveTask(task, targetColumn, null);
    }

    private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.CloseMenuCommand.Execute(null);
        }
    }
}
