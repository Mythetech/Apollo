namespace Apollo.Components.NuGet;

public class NuGetPackage
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Authors { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string ProjectUrl { get; set; } = string.Empty;
    public string LicenseUrl { get; set; } = string.Empty;
    public long TotalDownloads { get; set; }
    public bool Verified { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<string> Versions { get; set; } = [];
}

public class NuGetSearchResponse
{
    public int TotalHits { get; set; }
    public List<NuGetSearchResult> Data { get; set; } = [];
}

public class NuGetSearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Authors { get; set; } = [];
    public string IconUrl { get; set; } = string.Empty;
    public string ProjectUrl { get; set; } = string.Empty;
    public string LicenseUrl { get; set; } = string.Empty;
    public long TotalDownloads { get; set; }
    public bool Verified { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<NuGetVersionInfo> Versions { get; set; } = [];
}

public class NuGetVersionInfo
{
    public string Version { get; set; } = string.Empty;
    public long Downloads { get; set; }
}

public class InstalledPackage
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime InstalledAt { get; set; }
    public List<string> AssemblyNames { get; set; } = [];
    public long SizeBytes { get; set; }
}

