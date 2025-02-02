namespace Apollo.Components.Solutions.Services;

public interface IGitHubService
{
    Task<(string Owner, string Repo)> ParseGitHubUrl(string url);
    Task<List<(string Path, string Content)>> GetRepositoryContents(string owner, string repo);
}