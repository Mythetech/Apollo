namespace Apollo.Components.NuGet;

public interface INuGetService
{
    Task<List<NuGetPackage>> SearchPackagesAsync(string query, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<List<string>> GetPackageVersionsAsync(string packageId, CancellationToken cancellationToken = default);
    Task<NuGetPackageContent?> DownloadPackageAsync(string packageId, string version, CancellationToken cancellationToken = default);
}

