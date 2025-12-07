using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Apollo.Components.NuGet;

public class NuGetService : INuGetService
{
    private readonly HttpClient _httpClient = new();
    private readonly ILogger<NuGetService> _logger;
    private const string SearchApiUrl = "https://azuresearch-usnc.nuget.org/query";
    private const string PackageBaseUrl = "https://api.nuget.org/v3-flatcontainer";
    private const string RegistrationBaseUrl = "https://api.nuget.org/v3/registration5-gz-semver2";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public NuGetService(ILogger<NuGetService> logger)
    {
        _logger = logger;
    }

    public async Task<List<NuGetPackage>> SearchPackagesAsync(string query, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        try
        {
            var url = $"{SearchApiUrl}?q={Uri.EscapeDataString(query)}&skip={skip}&take={take}&prerelease=false&semVerLevel=2.0.0";
            var response = await _httpClient.GetFromJsonAsync<NuGetSearchResponse>(url, JsonOptions, cancellationToken);

            if (response?.Data == null)
                return [];

            return response.Data.Select(r => new NuGetPackage
            {
                Id = r.Id,
                Version = r.Version,
                Title = string.IsNullOrEmpty(r.Title) ? r.Id : r.Title,
                Description = r.Description ?? string.Empty,
                Authors = r.Authors != null ? string.Join(", ", r.Authors) : string.Empty,
                IconUrl = r.IconUrl ?? string.Empty,
                ProjectUrl = r.ProjectUrl ?? string.Empty,
                LicenseUrl = r.LicenseUrl ?? string.Empty,
                TotalDownloads = r.TotalDownloads,
                Verified = r.Verified,
                Tags = r.Tags ?? [],
                Versions = r.Versions?.Select(v => v.Version).ToList() ?? [r.Version]
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search NuGet packages for query: {Query}", query);
            return [];
        }
    }

    public async Task<List<string>> GetPackageVersionsAsync(string packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{PackageBaseUrl}/{packageId.ToLowerInvariant()}/index.json";
            var response = await _httpClient.GetFromJsonAsync<PackageVersionsResponse>(url, JsonOptions, cancellationToken);
            
            var versions = response?.Versions ?? [];
            versions.Reverse();
            return versions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get versions for package: {PackageId}", packageId);
            return [];
        }
    }

    public async Task<NuGetPackageContent?> DownloadPackageAsync(string packageId, string version, CancellationToken cancellationToken = default)
    {
        var downloadedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return await DownloadPackageWithDependenciesAsync(packageId, version, downloadedPackages, cancellationToken);
    }

    private async Task<NuGetPackageContent?> DownloadPackageWithDependenciesAsync(
        string packageId, 
        string version, 
        HashSet<string> downloadedPackages,
        CancellationToken cancellationToken)
    {
        var packageKey = $"{packageId.ToLowerInvariant()}:{version.ToLowerInvariant()}";
        if (downloadedPackages.Contains(packageKey))
        {
            _logger.LogDebug("Skipping already downloaded package: {PackageId} v{Version}", packageId, version);
            return null;
        }
        downloadedPackages.Add(packageKey);

        try
        {
            var lowerId = packageId.ToLowerInvariant();
            var lowerVersion = version.ToLowerInvariant();
            var nupkgUrl = $"{PackageBaseUrl}/{lowerId}/{lowerVersion}/{lowerId}.{lowerVersion}.nupkg";

            _logger.LogInformation("Downloading package {PackageId} v{Version}", packageId, version);

            var nupkgBytes = await _httpClient.GetByteArrayAsync(nupkgUrl, cancellationToken);
            
            var content = await ExtractPackageAsync(packageId, version, nupkgBytes);
            if (content == null) return null;

            var dependencies = await GetPackageDependenciesAsync(packageId, version, cancellationToken);
            
            foreach (var dep in dependencies)
            {
                _logger.LogInformation("Downloading dependency: {PackageId} v{Version}", dep.Id, dep.Version);
                var depContent = await DownloadPackageWithDependenciesAsync(dep.Id, dep.Version, downloadedPackages, cancellationToken);
                if (depContent != null)
                {
                    content.Assemblies.AddRange(depContent.Assemblies);
                    content.TotalSizeBytes += depContent.TotalSizeBytes;
                }
            }

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download package: {PackageId} v{Version}", packageId, version);
            return null;
        }
    }

    private async Task<List<PackageDependency>> GetPackageDependenciesAsync(string packageId, string version, CancellationToken cancellationToken)
    {
        var dependencies = new List<PackageDependency>();
        
        try
        {
            var lowerId = packageId.ToLowerInvariant();
            var registrationUrl = $"{RegistrationBaseUrl}/{lowerId}/{version.ToLowerInvariant()}.json";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, registrationUrl);
            request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get registration for {PackageId} v{Version}: {Status}", packageId, version, response.StatusCode);
                return dependencies;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            
            var catalogEntry = doc.RootElement;
            
            if (doc.RootElement.TryGetProperty("catalogEntry", out var catalogEntryProp))
            {
                if (catalogEntryProp.ValueKind == JsonValueKind.String)
                {
                    var catalogUrl = catalogEntryProp.GetString();
                    if (!string.IsNullOrEmpty(catalogUrl))
                    {
                        var catalogResponse = await _httpClient.GetAsync(catalogUrl, cancellationToken);
                        if (catalogResponse.IsSuccessStatusCode)
                        {
                            await using var catalogStream = await catalogResponse.Content.ReadAsStreamAsync(cancellationToken);
                            using var catalogDoc = await JsonDocument.ParseAsync(catalogStream, cancellationToken: cancellationToken);
                            await ParseDependencyGroups(catalogDoc.RootElement, dependencies, cancellationToken);
                            return dependencies;
                        }
                    }
                }
                else if (catalogEntryProp.ValueKind == JsonValueKind.Object)
                {
                    catalogEntry = catalogEntryProp;
                }
            }

            await ParseDependencyGroups(catalogEntry, dependencies, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get dependencies for {PackageId} v{Version}", packageId, version);
        }

        return dependencies;
    }

    private async Task ParseDependencyGroups(JsonElement element, List<PackageDependency> dependencies, CancellationToken cancellationToken)
    {
        if (!element.TryGetProperty("dependencyGroups", out var depGroups))
            return;

        foreach (var group in depGroups.EnumerateArray())
        {
            var targetFramework = group.TryGetProperty("targetFramework", out var tf) ? tf.GetString() ?? "" : "";
            
            if (!string.IsNullOrEmpty(targetFramework) && !IsCompatibleFramework(targetFramework))
                continue;

            if (!group.TryGetProperty("dependencies", out var deps))
                continue;

            foreach (var dep in deps.EnumerateArray())
            {
                var depId = dep.TryGetProperty("id", out var id) ? id.GetString() : null;
                var depRange = dep.TryGetProperty("range", out var range) ? range.GetString() : null;
                
                if (string.IsNullOrEmpty(depId)) continue;
                
                if (dependencies.Any(d => d.Id.Equals(depId, StringComparison.OrdinalIgnoreCase)))
                    continue;
                
                var depVersion = ParseVersionRange(depRange);
                if (string.IsNullOrEmpty(depVersion))
                {
                    var versions = await GetPackageVersionsAsync(depId, cancellationToken);
                    depVersion = versions.FirstOrDefault() ?? "";
                }
                
                if (!string.IsNullOrEmpty(depVersion))
                {
                    dependencies.Add(new PackageDependency { Id = depId, Version = depVersion });
                    _logger.LogDebug("Found dependency: {Id} v{Version}", depId, depVersion);
                }
            }
            
            if (dependencies.Count > 0) break;
        }
    }

    private static string? ParseVersionRange(string? range)
    {
        if (string.IsNullOrEmpty(range)) return null;
        
        range = range.Trim('[', ']', '(', ')', ' ');
        var parts = range.Split(',');
        var version = parts[0].Trim();
        
        return string.IsNullOrEmpty(version) ? null : version;
    }

    private async Task<NuGetPackageContent?> ExtractPackageAsync(string packageId, string version, byte[] nupkgBytes)
    {
        var content = new NuGetPackageContent
        {
            PackageId = packageId,
            Version = version
        };

        try
        {
            using var memoryStream = new MemoryStream(nupkgBytes);
            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                if (!entry.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                var targetFramework = GetTargetFramework(entry.FullName);
                if (!IsCompatibleFramework(targetFramework))
                    continue;

                await using var entryStream = entry.Open();
                using var ms = new MemoryStream();
                await entryStream.CopyToAsync(ms);
                
                var assemblyData = new AssemblyData
                {
                    Name = Path.GetFileNameWithoutExtension(entry.Name),
                    FileName = entry.Name,
                    TargetFramework = targetFramework,
                    Data = ms.ToArray()
                };
                
                content.Assemblies.Add(assemblyData);
                _logger.LogInformation("Extracted assembly: {Name} ({Framework})", assemblyData.Name, targetFramework);
            }

            content.Assemblies = SelectBestFrameworkAssemblies(content.Assemblies);
            content.TotalSizeBytes = content.Assemblies.Sum(a => a.Data.Length);
            
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract package: {PackageId}", packageId);
            return null;
        }
    }

    private static string GetTargetFramework(string entryPath)
    {
        var parts = entryPath.Split('/');
        return parts.Length >= 2 ? parts[1] : "unknown";
    }

    private static bool IsCompatibleFramework(string framework)
    {
        var compatibleFrameworks = new[]
        {
            "netstandard2.0", "netstandard2.1",
            "net6.0", "net7.0", "net8.0", "net9.0",
            "netcoreapp3.1"
        };
        
        return compatibleFrameworks.Any(f => 
            framework.StartsWith(f, StringComparison.OrdinalIgnoreCase));
    }

    private static List<AssemblyData> SelectBestFrameworkAssemblies(List<AssemblyData> assemblies)
    {
        var frameworkPriority = new[]
        {
            "net8.0", "net7.0", "net6.0", 
            "netstandard2.1", "netstandard2.0",
            "netcoreapp3.1"
        };

        var grouped = assemblies.GroupBy(a => a.Name);
        var result = new List<AssemblyData>();

        foreach (var group in grouped)
        {
            var best = group
                .OrderBy(a => 
                {
                    var idx = Array.FindIndex(frameworkPriority, f => 
                        a.TargetFramework.StartsWith(f, StringComparison.OrdinalIgnoreCase));
                    return idx >= 0 ? idx : 999;
                })
                .First();
            
            result.Add(best);
        }

        return result;
    }
}

public class PackageVersionsResponse
{
    public List<string> Versions { get; set; } = [];
}

public class NuGetPackageContent
{
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public List<AssemblyData> Assemblies { get; set; } = [];
    public long TotalSizeBytes { get; set; }
}

public class AssemblyData
{
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
}

public class PackageDependency
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

