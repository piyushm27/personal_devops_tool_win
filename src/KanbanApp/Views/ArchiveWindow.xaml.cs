using System.Collections;
using System.Windows;

namespace KanbanApp.Views;

public partial class ArchiveWindow : Window
{
    public ArchiveWindow(IEnumerable archivedTasks)
    {
        InitializeComponent();
        ArchiveList.ItemsSource = archivedTasks;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
