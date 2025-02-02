using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Apollo.Components.Solutions.Services
{
    public class GitHubService : IGitHubService
    {
        private readonly HttpClient _httpClient;

        public GitHubService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.github.com/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Apollo-IDE");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        }

        public Task<(string Owner, string Repo)> ParseGitHubUrl(string url)
        {
            // Remove .git if present
            url = url.Replace(".git", "");
            
            var uri = new Uri(url);
            var parts = uri.AbsolutePath.Trim('/').Split('/');
            
            if (parts.Length < 2)
                throw new ArgumentException("Invalid GitHub URL");
                
            return Task.FromResult((parts[0], parts[1]));
        }

        public async Task<List<(string Path, string Content)>> GetRepositoryContents(string owner, string repo)
        {
            var contents = new List<(string, string)>();
            
            await GetContentsRecursively(owner, repo, "", contents);
            
            return contents;
        }
        
        private async Task GetContentsRecursively(string owner, string repo, string path, List<(string, string)> contents)
        {
            var requestPath = string.IsNullOrEmpty(path) ? "" : $"/{path}";
            var response = await _httpClient.GetFromJsonAsync<List<GitHubContent>>($"repos/{owner}/{repo}/contents{requestPath}");
            
            foreach (var item in response)
            {
                if (item.Type == "dir")
                {
                    await GetContentsRecursively(owner, repo, item.Path, contents);
                }
                else if (item.Type == "file" && item.Name.EndsWith(".cs"))
                {
                    var content = await GetFileContent(item.DownloadUrl);
                    contents.Add((item.Path, content));
                }
            }
        }
        
        private async Task<string> GetFileContent(string downloadUrl)
        {
            // Convert the API URL to a raw content URL
            // From: https://api.github.com/repos/owner/repo/contents/path/file.cs
            // To:   https://raw.githubusercontent.com/owner/repo/master/path/file.cs
            var rawUrl = downloadUrl.Replace("https://api.github.com/repos", "https://raw.githubusercontent.com")
                                   .Replace("/contents", "")
                                   .Replace("/master", "master");

            var response = await _httpClient.GetAsync(rawUrl);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        
        private class GitHubContent
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string Type { get; set; }
            public string DownloadUrl { get; set; }
        }
    }
} 