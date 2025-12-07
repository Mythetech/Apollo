using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace Apollo.Components.NuGet;

public class NuGetStorageService : INuGetStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<NuGetStorageService> _logger;
    private IJSObjectReference? _module;
    private const string DbName = "ApolloNuGetCache";
    private const string StoreName = "packages";

    public NuGetStorageService(IJSRuntime jsRuntime, ILogger<NuGetStorageService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    private async Task EnsureModuleLoadedAsync()
    {
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Apollo.Components/nuget-storage.js");
    }

    public async Task StorePackageAsync(NuGetPackageContent content)
    {
        try
        {
            await EnsureModuleLoadedAsync();

            var packageData = new StoredPackageData
            {
                PackageId = content.PackageId,
                Version = content.Version,
                InstalledAt = DateTime.UtcNow,
                TotalSizeBytes = content.TotalSizeBytes,
                Assemblies = content.Assemblies.Select(a => new StoredAssemblyInfo
                {
                    Name = a.Name,
                    FileName = a.FileName,
                    TargetFramework = a.TargetFramework
                }).ToList()
            };

            await _module!.InvokeVoidAsync("storePackageMetadata", DbName, StoreName, packageData);

            foreach (var assembly in content.Assemblies)
            {
                var key = GetAssemblyKey(content.PackageId, content.Version, assembly.Name);
                await _module!.InvokeVoidAsync("storeAssemblyData", DbName, "assemblies", key, assembly.Data);
            }

            _logger.LogInformation("Stored package {PackageId} v{Version} with {Count} assemblies", 
                content.PackageId, content.Version, content.Assemblies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store package {PackageId} v{Version}", content.PackageId, content.Version);
            throw;
        }
    }

    public async Task<byte[]?> GetAssemblyDataAsync(string packageId, string version, string assemblyName)
    {
        try
        {
            await EnsureModuleLoadedAsync();
            var key = GetAssemblyKey(packageId, version, assemblyName);
            return await _module!.InvokeAsync<byte[]?>("getAssemblyData", DbName, "assemblies", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get assembly {AssemblyName} from {PackageId} v{Version}", 
                assemblyName, packageId, version);
            return null;
        }
    }

    public async Task<List<InstalledPackage>> GetInstalledPackagesAsync()
    {
        try
        {
            await EnsureModuleLoadedAsync();
            var packages = await _module!.InvokeAsync<List<StoredPackageData>?>("getAllPackages", DbName, StoreName);
            
            return packages?.Select(p => new InstalledPackage
            {
                Id = p.PackageId,
                Version = p.Version,
                InstalledAt = p.InstalledAt,
                SizeBytes = p.TotalSizeBytes,
                AssemblyNames = p.Assemblies.Select(a => a.Name).ToList()
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get installed packages");
            return [];
        }
    }

    public async Task<bool> IsPackageInstalledAsync(string packageId, string? version = null)
    {
        try
        {
            await EnsureModuleLoadedAsync();
            return await _module!.InvokeAsync<bool>("isPackageInstalled", DbName, StoreName, packageId, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if package {PackageId} is installed", packageId);
            return false;
        }
    }

    public async Task<InstalledPackage?> GetInstalledPackageAsync(string packageId)
    {
        try
        {
            await EnsureModuleLoadedAsync();
            var package = await _module!.InvokeAsync<StoredPackageData?>("getPackage", DbName, StoreName, packageId);
            
            if (package == null) return null;

            return new InstalledPackage
            {
                Id = package.PackageId,
                Version = package.Version,
                InstalledAt = package.InstalledAt,
                SizeBytes = package.TotalSizeBytes,
                AssemblyNames = package.Assemblies.Select(a => a.Name).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get installed package {PackageId}", packageId);
            return null;
        }
    }

    public async Task RemovePackageAsync(string packageId, string version)
    {
        try
        {
            await EnsureModuleLoadedAsync();
            
            var package = await _module!.InvokeAsync<StoredPackageData?>("getPackage", DbName, StoreName, packageId);
            if (package != null)
            {
                foreach (var assembly in package.Assemblies)
                {
                    var key = GetAssemblyKey(packageId, version, assembly.Name);
                    await _module!.InvokeVoidAsync("removeAssemblyData", DbName, "assemblies", key);
                }
            }

            await _module!.InvokeVoidAsync("removePackage", DbName, StoreName, packageId);
            _logger.LogInformation("Removed package {PackageId} v{Version}", packageId, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove package {PackageId} v{Version}", packageId, version);
            throw;
        }
    }

    private static string GetAssemblyKey(string packageId, string version, string assemblyName) 
        => $"{packageId}_{version}_{assemblyName}";
}

internal class StoredPackageData
{
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime InstalledAt { get; set; }
    public long TotalSizeBytes { get; set; }
    public List<StoredAssemblyInfo> Assemblies { get; set; } = [];
}

internal class StoredAssemblyInfo
{
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;
}

