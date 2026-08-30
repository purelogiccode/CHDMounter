using System.Windows;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Core.Views;

/// <summary>
///     A dialog window that prompts the user to select a console type for mounting a CHD file.
/// </summary>
public partial class ConsoleSelectionWindow
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsoleSelectionWindow" /> class.
    /// </summary>
    public ConsoleSelectionWindow()
    {
        InitializeComponent();

        var consoles = ParserFactory.GetAllSupportedConsoles()
            .OrderBy(static c => c.Name, StringComparer.Ordinal)
            .ToList();
        ConsoleComboBox.ItemsSource = consoles;
        ConsoleComboBox.DisplayMemberPath = "Name";
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
        ChdPathTextBlock.ToolTip = chdPath;
    }

    /// <summary>
    ///     Gets the console type selected by the user.
    /// </summary>
    public ConsoleType SelectedConsoleType { get; private set; } = ConsoleType.Unknown;

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (ConsoleComboBox.SelectedItem is ConsoleInfo ci)
        {
            SelectedConsoleType = ci.Type;
            DialogResult = true;
        }

        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}