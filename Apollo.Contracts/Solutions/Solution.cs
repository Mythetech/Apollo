namespace Apollo.Contracts.Solutions
{
    public class Solution
    {
        public string Name { get; set; } = string.Empty;
        public List<SolutionItem> Items { get; set; } = [];
        public ProjectType Type { get; set; } = ProjectType.Console;
    }
}