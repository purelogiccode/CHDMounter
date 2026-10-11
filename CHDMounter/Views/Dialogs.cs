using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace CHDMounter.Views;

/// <summary>
///     Shows modal message dialogs from any thread. Calls made from background threads
///     (for example the mount worker) are marshaled to the UI thread and block until the
///     dialog is dismissed.
/// </summary>
internal static class Dialogs
{
    /// <summary>
    ///     Shows a modal confirmation dialog with Yes and No buttons.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="yesText">The text of the affirmative button.</param>
    /// <param name="noText">The text of the negative button.</param>
    /// <returns><c>true</c> when the user confirms; otherwise <c>false</c>.</returns>
    internal static bool Confirm(string title, string message, string yesText = "Yes", string noText = "No")
    {
        return OnUiThread(() => Show(title, message, yesText, noText));
    }

    /// <summary>
    ///     Shows a modal information dialog with a single OK button.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message to display.</param>
    internal static void Info(string title, string message)
    {
        _ = OnUiThread(() => Show(title, message, "OK", null));
    }

    private static bool Show(string title, string message, string yesText, string? noText)
    {
        var dialog = new MessageDialog(title, message, yesText, noText);
        var owner = GetOwner();
        if (owner is null)
        {
            // No main window yet (for example during startup shutdown paths):
            // show non-modal rather than throwing.
            dialog.Show();
            return false;
        }

        return dialog.ShowDialog<bool>(owner).GetAwaiter().GetResult();
    }

    private static T OnUiThread<T>(Func<T> action)
    {
        return Dispatcher.UIThread.CheckAccess()
            ? action()
            : Dispatcher.UIThread.Invoke(action);
    }

    private static Window? GetOwner()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.Windows.FirstOrDefault(static window => window.IsActive) ?? desktop.MainWindow;

        return null;
    }
}
