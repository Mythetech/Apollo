namespace Apollo.Contracts.Solutions
{
    public class Solution
    {
        public string Name { get; set; } = string.Empty;
        public List<SolutionItem> Items { get; set; } = [];
        public ProjectType Type { get; set; } = ProjectType.Console;
        public List<NuGetReference> NuGetReferences { get; set; } = [];
    }

    public class NuGetReference
    {
        public string PackageId { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
        public byte[] AssemblyData { get; set; } = [];
    }
}