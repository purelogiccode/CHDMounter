namespace CHDMounter.Core.Interfaces;

/// <summary>
///     Defines a service for managing application settings persistence.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    ///     Gets the current application settings.
    /// </summary>
    AppSettings Settings { get; }

    /// <summary>
    ///     Saves the current settings to persistent storage.
    /// </summary>
    void Save();
}