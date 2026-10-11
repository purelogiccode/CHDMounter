using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CHDMounter.Views;

/// <summary>
///     A small modal dialog with a message and either Yes/No or a single OK button.
/// </summary>
public partial class MessageDialog : Window
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageDialog" /> class.
    /// </summary>
    public MessageDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageDialog" /> class with content.
    /// </summary>
    /// <param name="title">The window title.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="yesText">The text of the primary button.</param>
    /// <param name="noText">The text of the secondary button, or <c>null</c> for an OK-only dialog.</param>
    public MessageDialog(string title, string message, string yesText, string? noText) : this()
    {
        Title = title;
        MessageText.Text = message;
        YesButton.Content = yesText;
        if (noText is null)
            NoButton.IsVisible = false;
        else
            NoButton.Content = noText;
    }

    private void Yes_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void No_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
