using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace CHDMounter.Views;

/// <summary>
///     Shows modal message dialogs. Calls made from background threads (for example the
///     mount worker) are marshaled to the UI thread and awaited, so the UI thread stays
///     free to run the dialog and process user input.
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
    internal static Task<bool> ConfirmAsync(string title, string message, string yesText = "Yes", string noText = "No")
    {
        if (Dispatcher.UIThread.CheckAccess())
            return ShowAsync(title, message, yesText, noText);

        return Dispatcher.UIThread.InvokeAsync(() => ShowAsync(title, message, yesText, noText));
    }

    /// <summary>
    ///     Shows a modal confirmation dialog without blocking the calling thread, invoking
    ///     <paramref name="onConfirmed" /> when the user confirms. Callers holding locks
    ///     return immediately: the dialog is queued to the UI thread and awaited there, so
    ///     no lock is held while the user answers and the UI thread remains responsive.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="onConfirmed">The action to run when the user confirms.</param>
    /// <param name="yesText">The text of the affirmative button.</param>
    /// <param name="noText">The text of the negative button.</param>
    internal static void ConfirmLater(string title, string message, Action onConfirmed,
        string yesText = "Yes", string noText = "No")
    {
        Dispatcher.UIThread.Post(() => _ = ConfirmLaterAsync(title, message, onConfirmed, yesText, noText));
    }

    /// <summary>
    ///     Shows a modal information dialog with a single OK button.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message to display.</param>
    internal static void Info(string title, string message)
    {
        Dispatcher.UIThread.Post(() => _ = ShowAsync(title, message, "OK", null));
    }

    private static async Task ConfirmLaterAsync(string title, string message, Action onConfirmed,
        string yesText, string noText)
    {
        if (await ShowAsync(title, message, yesText, noText))
            onConfirmed();
    }

    private static async Task<bool> ShowAsync(string title, string message, string yesText, string? noText)
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

        return await dialog.ShowDialog<bool>(owner);
    }

    private static Window? GetOwner()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.Windows.FirstOrDefault(static window => window.IsActive) ?? desktop.MainWindow;

        return null;
    }
}
