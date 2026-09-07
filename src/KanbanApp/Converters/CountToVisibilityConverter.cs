using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KanbanApp.Converters;

/// Visible when the bound count is 0 (used to show an empty-column message),
/// Collapsed otherwise.
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int count && count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
