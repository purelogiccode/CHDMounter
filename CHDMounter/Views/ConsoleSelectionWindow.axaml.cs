using Avalonia.Controls;
using Avalonia.Interactivity;
using CHDMounter.Services;

namespace CHDMounter.Views;

/// <summary>
///     A dialog window that prompts the user to select a console type for mounting a CHD file.
/// </summary>
public partial class ConsoleSelectionWindow : Window
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsoleSelectionWindow" /> class.
    /// </summary>
    public ConsoleSelectionWindow()
    {
        InitializeComponent();

        var formats = MountFormatRegistry.All
            .OrderBy(static format => format.DisplayName, StringComparer.Ordinal)
            .ToList();
        ConsoleComboBox.ItemsSource = formats;
        ConsoleComboBox.SelectedIndex = 0;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsoleSelectionWindow" /> class with the specified CHD file path.
    /// </summary>
    /// <param name="chdPath">The path to the CHD file being mounted.</param>
    public ConsoleSelectionWindow(string chdPath) : this()
    {
        var fileName = Path.GetFileName(chdPath);
        ChdPathTextBlock.Text = !string.IsNullOrEmpty(fileName) ? fileName : chdPath;
        ToolTip.SetTip(ChdPathTextBlock, chdPath);
    }

    /// <summary>
    ///     Gets the console type selected by the user.
    /// </summary>
    public ConsoleType SelectedConsoleType { get; private set; } = ConsoleType.Unknown;

    /// <summary>
    ///     Gets a value indicating whether the selected format requests 2448-byte CD frames.
    /// </summary>
    internal bool SelectedIncludeSubchannel { get; private set; }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        if (ConsoleComboBox.SelectedItem is MountFormatInfo format)
        {
            SelectedConsoleType = format.ConsoleType;
            SelectedIncludeSubchannel = format.IncludeSubchannel;
            Close(true);
            return;
        }

        Close(false);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
