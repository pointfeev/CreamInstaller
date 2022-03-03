using System.Collections.Generic;

namespace CreamInstaller.Epic;

public class Manifest
{
#pragma warning disable IDE1006 // Naming Styles
    public int FormatVersion { get; set; }
    public bool bIsIncompleteInstall { get; set; }
    public string LaunchCommand { get; set; }
    public string LaunchExecutable { get; set; }
    public string ManifestLocation { get; set; }
    public bool bIsApplication { get; set; }
    public bool bIsExecutable { get; set; }
    public bool bIsManaged { get; set; }
    public bool bNeedsValidation { get; set; }
    public bool bRequiresAuth { get; set; }
    public bool bAllowMultipleInstances { get; set; }
    public bool bCanRunOffline { get; set; }
    public bool bAllowUriCmdArgs { get; set; }
    public List<string> BaseURLs { get; set; }
    public string BuildLabel { get; set; }
    public List<string> AppCategories { get; set; }
    public List<object> ChunkDbs { get; set; }
    public List<object> CompatibleApps { get; set; }
    public string DisplayName { get; set; }
    public string InstallationGuid { get; set; }
    public string InstallLocation { get; set; }
    public string InstallSessionId { get; set; }
    public List<object> InstallTags { get; set; }
    public List<object> InstallComponents { get; set; }
    public string HostInstallationGuid { get; set; }
    public List<string> PrereqIds { get; set; }
    public string StagingLocation { get; set; }
    public string TechnicalType { get; set; }
    public string VaultThumbnailUrl { get; set; }
    public string VaultTitleText { get; set; }
    public long InstallSize { get; set; }
    public string MainWindowProcessName { get; set; }
    public List<object> ProcessNames { get; set; }
    public List<object> BackgroundProcessNames { get; set; }
    public string MandatoryAppFolderName { get; set; }
    public string OwnershipToken { get; set; }
    public string CatalogNamespace { get; set; }
    public string CatalogItemId { get; set; }
    public string AppName { get; set; }
    public string AppVersionString { get; set; }
    public string MainGameCatalogNamespace { get; set; }
    public string MainGameCatalogItemId { get; set; }
    public string MainGameAppName { get; set; }
    public List<object> AllowedUriEnvVars { get; set; }
#pragma warning restore IDE1006 // Naming Styles
}
