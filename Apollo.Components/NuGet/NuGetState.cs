using Microsoft.Extensions.Logging;

namespace Apollo.Components.NuGet;

public class NuGetState
{
    private readonly INuGetService _nuGetService;
    private readonly INuGetStorageService _storageService;
    private readonly ILogger<NuGetState> _logger;
    
    private List<InstalledPackage> _installedPackages = [];
    private bool _initialized;

    public event Action? OnPackagesChanged;
    public event Action<string>? OnInstallStarted;
    public event Action<string, bool>? OnInstallCompleted;

    public IReadOnlyList<InstalledPackage> InstalledPackages => _installedPackages;
    public bool IsInitialized => _initialized;

    public NuGetState(
        INuGetService nuGetService, 
        INuGetStorageService storageService,
        ILogger<NuGetState> logger)
    {
        _nuGetService = nuGetService;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            _installedPackages = await _storageService.GetInstalledPackagesAsync();
            _initialized = true;
            _logger.LogInformation("NuGet state initialized with {Count} installed packages", _installedPackages.Count);
            OnPackagesChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize NuGet state");
        }
    }

    public async Task<List<NuGetPackage>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        return await _nuGetService.SearchPackagesAsync(query, cancellationToken: cancellationToken);
    }

    public async Task<List<string>> GetVersionsAsync(string packageId, CancellationToken cancellationToken = default)
    {
        return await _nuGetService.GetPackageVersionsAsync(packageId, cancellationToken);
    }

    public async Task<bool> InstallPackageAsync(string packageId, string version, CancellationToken cancellationToken = default)
    {
        OnInstallStarted?.Invoke(packageId);
        
        try
        {
            var content = await _nuGetService.DownloadPackageAsync(packageId, version, cancellationToken);
            
            if (content == null || content.Assemblies.Count == 0)
            {
                _logger.LogWarning("Package {PackageId} v{Version} has no compatible assemblies", packageId, version);
                OnInstallCompleted?.Invoke(packageId, false);
                return false;
            }

            await _storageService.StorePackageAsync(content);
            
            _installedPackages = await _storageService.GetInstalledPackagesAsync();
            
            _logger.LogInformation("Installed package {PackageId} v{Version}", packageId, version);
            OnPackagesChanged?.Invoke();
            OnInstallCompleted?.Invoke(packageId, true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install package {PackageId} v{Version}", packageId, version);
            OnInstallCompleted?.Invoke(packageId, false);
            return false;
        }
    }

    public async Task<bool> UninstallPackageAsync(string packageId)
    {
        try
        {
            var installed = _installedPackages.FirstOrDefault(p => p.Id == packageId);
            if (installed == null) return false;

            await _storageService.RemovePackageAsync(packageId, installed.Version);
            _installedPackages = await _storageService.GetInstalledPackagesAsync();
            
            _logger.LogInformation("Uninstalled package {PackageId}", packageId);
            OnPackagesChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall package {PackageId}", packageId);
            return false;
        }
    }

    public bool IsInstalled(string packageId) 
        => _installedPackages.Any(p => p.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase));

    public InstalledPackage? GetInstalledPackage(string packageId)
        => _installedPackages.FirstOrDefault(p => p.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase));

    public async Task<byte[]?> GetAssemblyDataAsync(string packageId, string assemblyName)
    {
        var installed = GetInstalledPackage(packageId);
        if (installed == null) return null;
        
        return await _storageService.GetAssemblyDataAsync(packageId, installed.Version, assemblyName);
    }
}

