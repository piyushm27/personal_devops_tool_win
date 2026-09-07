using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KanbanApp.Views;

/// A plain TextBox that pops up a filtered suggestion list from Identities
/// whenever the word under the caret starts with '@'.
public partial class MentionTextBox : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(MentionTextBox),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextPropertyChanged));

    public static readonly DependencyProperty IdentitiesProperty = DependencyProperty.Register(
        nameof(Identities), typeof(IEnumerable<string>), typeof(MentionTextBox), new PropertyMetadata(null));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IEnumerable<string>? Identities
    {
        get => (IEnumerable<string>?)GetValue(IdentitiesProperty);
        set => SetValue(IdentitiesProperty, value);
    }

    public MentionTextBox()
    {
        InitializeComponent();
    }

    public void FocusAndSelectAll()
    {
        InnerTextBox.Focus();
        InnerTextBox.SelectAll();
    }

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MentionTextBox)d;
        var newText = (string?)e.NewValue ?? string.Empty;
        if (control.InnerTextBox.Text != newText)
        {
            control.InnerTextBox.Text = newText;
        }
    }

    private void InnerTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Text = InnerTextBox.Text;
        UpdateSuggestions();
    }

    private void UpdateSuggestions()
    {
        var caret = InnerTextBox.CaretIndex;
        var text = InnerTextBox.Text;
        var wordStart = caret;
        while (wordStart > 0 && !char.IsWhiteSpace(text[wordStart - 1]))
        {
            wordStart--;
        }

        var word = text[wordStart..caret];
        if (!word.StartsWith('@'))
        {
            SuggestionPopup.IsOpen = false;
            return;
        }

        var filter = word[1..];
        var matches = (Identities ?? Enumerable.Empty<string>())
            .Where(id => id.StartsWith(filter, System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(id => id)
            .ToList();

        if (matches.Count == 0)
        {
            SuggestionPopup.IsOpen = false;
            return;
        }

        SuggestionList.ItemsSource = matches;
        SuggestionList.SelectedIndex = 0;
        SuggestionPopup.IsOpen = true;
    }

    private void InnerTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!SuggestionPopup.IsOpen)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Down:
                SuggestionList.SelectedIndex = System.Math.Min(SuggestionList.SelectedIndex + 1, SuggestionList.Items.Count - 1);
                e.Handled = true;
                break;
            case Key.Up:
                SuggestionList.SelectedIndex = System.Math.Max(SuggestionList.SelectedIndex - 1, 0);
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Tab:
                if (SuggestionList.SelectedItem is string selected)
                {
                    ApplySuggestion(selected);
                }
                e.Handled = true;
                break;
            case Key.Escape:
                SuggestionPopup.IsOpen = false;
                e.Handled = true;
                break;
        }
    }

    private void SuggestionList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (SuggestionList.SelectedItem is string selected)
        {
            ApplySuggestion(selected);
        }
    }

    private void ApplySuggestion(string identity)
    {
        var caret = InnerTextBox.CaretIndex;
        var text = InnerTextBox.Text;
        var wordStart = caret;
        while (wordStart > 0 && !char.IsWhiteSpace(text[wordStart - 1]))
        {
            wordStart--;
        }

        var newText = text[..wordStart] + "@" + identity + " " + text[caret..];
        InnerTextBox.Text = newText;
        InnerTextBox.CaretIndex = wordStart + identity.Length + 2;
        SuggestionPopup.IsOpen = false;
        InnerTextBox.Focus();
    }
}
