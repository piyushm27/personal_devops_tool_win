using System.Windows;

namespace KanbanApp.Views;

public partial class EditTaskDialog : Window
{
    public string? UpdatedTitle { get; private set; }

    public EditTaskDialog(string currentTitle)
    {
        InitializeComponent();
        TitleBox.Text = currentTitle;
        TitleBox.Focus();
        TitleBox.SelectAll();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var trimmed = TitleBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        UpdatedTitle = trimmed;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
