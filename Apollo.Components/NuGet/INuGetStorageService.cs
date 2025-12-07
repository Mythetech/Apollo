namespace Apollo.Components.NuGet;

public interface INuGetStorageService
{
    Task StorePackageAsync(NuGetPackageContent content);
    Task<byte[]?> GetAssemblyDataAsync(string packageId, string version, string assemblyName);
    Task<List<InstalledPackage>> GetInstalledPackagesAsync();
    Task<bool> IsPackageInstalledAsync(string packageId, string? version = null);
    Task<InstalledPackage?> GetInstalledPackageAsync(string packageId);
    Task RemovePackageAsync(string packageId, string version);
}

