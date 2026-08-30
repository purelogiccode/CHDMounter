using System.Windows;

namespace CHDMounter.Core.Views;

/// <summary>
///     A dialog window that allows the user to configure application settings.
/// </summary>
public partial class SettingsWindow
{
    private readonly ISettingsService _settingsService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SettingsWindow" /> class.
    /// </summary>
    /// <param name="settingsService">The settings service providing access to application settings.</param>
    public SettingsWindow(ISettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        AutoOpenDriveCheckBox.IsChecked = _settingsService.Settings.AutoOpenMountedDrive;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.Settings.AutoOpenMountedDrive = AutoOpenDriveCheckBox.IsChecked == true;
        _settingsService.Save();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}