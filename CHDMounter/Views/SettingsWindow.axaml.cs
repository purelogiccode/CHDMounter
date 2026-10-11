using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CHDMounter.Views;

/// <summary>
///     A dialog window that allows the user to configure application settings.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ISettingsService? _settingsService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SettingsWindow" /> class.
    /// </summary>
    public SettingsWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SettingsWindow" /> class.
    /// </summary>
    /// <param name="settingsService">The settings service providing access to application settings.</param>
    public SettingsWindow(ISettingsService? settingsService) : this()
    {
        _settingsService = settingsService;

        var isWindows = OperatingSystem.IsWindows();
        AutoOpenDriveCheckBox.IsChecked = settingsService?.Settings.AutoOpenMountedDrive ?? true;

        if (!isWindows)
            AutoOpenDriveCheckBox.Content = "Auto open the mounted drive in the file manager";

        // Only the backends supported by the current platform are selectable.
        DokanRadioButton.IsVisible = isWindows;
        WinFspRadioButton.IsVisible = isWindows;
        FuseRadioButton.IsVisible = !isWindows;

        var backend = settingsService?.Settings.MountBackend ?? MountBackend.Dokan;
        if (!isWindows) backend = MountBackend.Fuse;

        switch (backend)
        {
            case MountBackend.WinFsp:
                WinFspRadioButton.IsChecked = true;
                break;
            case MountBackend.Fuse:
                FuseRadioButton.IsChecked = true;
                break;
            default:
                DokanRadioButton.IsChecked = true;
                break;
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (_settingsService is null)
        {
            Close();
            return;
        }

        _settingsService.Settings.AutoOpenMountedDrive = AutoOpenDriveCheckBox.IsChecked == true;
        _settingsService.Settings.MountBackend = OperatingSystem.IsWindows()
            ? WinFspRadioButton.IsChecked == true ? MountBackend.WinFsp : MountBackend.Dokan
            : MountBackend.Fuse;
        _settingsService.Save();
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
