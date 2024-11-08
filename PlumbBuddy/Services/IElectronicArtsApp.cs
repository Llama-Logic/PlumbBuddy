namespace PlumbBuddy.Services;

/// <summary>
/// Represents the a platform's implementation of interoperation with EA's App, which way have installed The Sims 4
/// </summary>
public interface IElectronicArtsApp
{
    /// <summary>
    /// Gets whether the EA App is installed on this computer
    /// </summary>
    /// <returns><see langword="true"/> if the EA App is installed on this computer; otherwise, <see langword="false"/></returns>
    Task<bool> GetIsElectronicArtsAppInstalledAsync();

    /// <summary>
    /// Gets the installation directory for The Sims 4 if it was installed by the EA App on this computer
    /// </summary>
    /// <returns>A <see cref="DirectoryInfo"/> object encapsulating The Sims 4 installation directory as installed by the EA App if it was installed by the EA App; otherwise, <see langword="null"/></returns>
    Task<DirectoryInfo?> GetTS4InstallationDirectoryAsync();

    /// <summary>
    /// Gets the version of The Sims 4 if it was installed by the EA App on this computer
    /// </summary>
    /// <returns>A <see cref="Version"/> object encapsulating the version of The Sims 4 installed by the EA App if it was installed by the EA App; otherwise, <see langword="null"/></returns>
    Task<Version?> GetTS4InstallationVersionAsync();
}
