using Apollo.Components.Code;
using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Hosting;
using Apollo.Components.Infrastructure;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Library.SampleProjects;
using Apollo.Components.Solutions.Events;
using Apollo.Contracts.Solutions;

namespace Apollo.Components.Solutions;

public class SolutionsState
{
    private readonly IMessageBus _bus;
    private readonly CompilerState _compiler;
    private readonly ISolutionSaveService _solutionStorageService;
    private readonly IHostingService _hostingService;

    public SolutionsState(
        CompilerState compiler, 
        IMessageBus bus, 
        ISolutionSaveService solutionStorageService,
        IHostingService hostingService)
    {
        _compiler = compiler;
        _bus = bus;
        _solutionStorageService = solutionStorageService;
        _hostingService = hostingService;

        var untitledProject = UntitledProject.Create();
        var fizzBuzzProject = FizzBuzzProject.Create();
        var minimalApiProject = MinimalApiProject.Create();

        Project = untitledProject;
        Solutions = new List<SolutionModel>
        {
            untitledProject,
            fizzBuzzProject,
            minimalApiProject
        };

        ActiveFile = Project.Files.FirstOrDefault();
    }

    public SolutionModel Project { get; private set; }
    
    public bool HasActiveSolution => !string.IsNullOrWhiteSpace(Project?.Name);

    public List<SolutionModel> Solutions { get; private set; } = new();

    public SolutionFile? ActiveFile { get; set; }

    public event Action SolutionFilesChanged;

    public event Func<Task> SaveProjectRequested;

    public event Func<Task> BuildRequested;

    public event Action<SolutionFile> ActiveFileChangeRequested;

    public event Action<SolutionFile?> ActiveFileChanged;

    public event Action ProjectChanged;

    public async Task SaveProjectFilesAsync()
    {
      await SaveProjectRequested.Invoke();   
    }

    public void SwitchFile(SolutionFile file)
    {
        ActiveFileChangeRequested?.Invoke(ActiveFile);
        
        var updated = Project.Files.FirstOrDefault(x => x.Uri.Equals(ActiveFile.Uri));

        if (updated == null)
            return;
        
        updated.Data = file.Data;
        updated.ModifiedAt = DateTimeOffset.Now;
        
        ActiveFile = Project.Files.FirstOrDefault(x =>  x.Uri.Equals(file.Uri, StringComparison.Ordinal));
        ActiveFileChanged?.Invoke(file);
    }

    public async Task RenameFile(SolutionFile file, string name)
    {
        var f = Project.Files.FirstOrDefault(x => x.Uri.Equals(file.Uri, StringComparison.Ordinal));
        
        if(f == null) 
            return;
        
        await RenameItemAsync(file, name);
    }

    public async Task RenameItemAsync(ISolutionItem item, string name)
    {
        var oldUri = item.Uri;
        // Get the parent path and construct new URI properly, handling case sensitivity
        var lastSlashIndex = oldUri.LastIndexOf('/');
        var parentPath = lastSlashIndex > 0 ? oldUri[..lastSlashIndex] : "virtual";
        var newUri = $"{parentPath}/{name}";
        var isRootRename = false;
        
        var f = Project.Items.FirstOrDefault(x => x.Uri.Equals(item.Uri, StringComparison.OrdinalIgnoreCase));
        
        if(f == null) 
            return;
        
        var oldName = f.Name;  // Store old name before any changes
        
        // If this is a folder, update all child item URIs
        if (f is Folder folder)
        {
            // Store existing items before any changes
            var existingItems = new List<ISolutionItem>(folder.Items);
            
            // If this is the root folder, update solution name and references
            var normalizedFolderUri = folder.Uri.TrimEnd('/').ToLowerInvariant();
            var normalizedProjectPath = Project.Path.TrimEnd('/').ToLowerInvariant();
            if (normalizedFolderUri == normalizedProjectPath || 
                folder.Uri.Split('/').Last().Equals(Project.Name, StringComparison.OrdinalIgnoreCase))
            {
                isRootRename = true;
                
                Project.Name = name;
                folder.Name = name;
                folder.Uri = $"virtual/{name}";
                folder.ModifiedAt = DateTimeOffset.Now;
                
                // Update all child URIs to use new solution name
                foreach (var projectItem in Project.Items.Where(i => i != folder))
                {
                    projectItem.Uri = projectItem.Uri.Replace($"virtual/{oldName}/", $"virtual/{name}/", StringComparison.OrdinalIgnoreCase);
                }

                // Update solution in Solutions list
                var solutionIndex = Solutions.FindIndex(s => s.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase));
                if (solutionIndex >= 0)
                {
                    Solutions[solutionIndex] = Project;
                }

                ProjectChanged?.Invoke();
                SolutionFilesChanged?.Invoke();
                return;  // Exit early as we've handled everything for root rename
            }
            else
            {
                f.Name = name;
                f.ModifiedAt = DateTimeOffset.Now;
                f.Uri = newUri;

                // Update child URIs
                foreach (var projectItem in Project.Items.Where(i => i != f))
                {
                    if (projectItem.Uri.StartsWith(oldUri + "/"))
                    {
                        projectItem.Uri = projectItem.Uri.Replace(oldUri + "/", newUri + "/");
                    }
                }
            }

            Project.GetHierarchicalItems();
        }
        else
        {
            f.Name = name;
            f.ModifiedAt = DateTimeOffset.Now;
            f.Uri = newUri;
        }
        
        await _bus.PublishAsync(new ItemRenamed(item, oldName, name, isRootRename));
        
        SolutionFilesChanged?.Invoke();
    }

    public bool IsRootFolder(Folder folder)
    {
        // First, normalize both paths to a consistent format
        var folderPath = folder.Uri.TrimEnd('/').ToLowerInvariant();
        var projectPath = Project.Path.TrimEnd('/').ToLowerInvariant();
        
        // Direct path comparison
        if (folderPath == projectPath)
            return true;
        
        // Check if this is the root folder by comparing the last segment
        var folderName = folder.Uri.Split('/').Last().ToLowerInvariant();
        var projectName = Project.Name.ToLowerInvariant();
        
        return folderName == projectName;
    }

    public void UpdateActiveFile(string data)
    {
        if (ActiveFile == null)
            ActiveFile = Project.Files.FirstOrDefault();

        if (ActiveFile == null)
            return;
        
        var f = Project.Files.FirstOrDefault(x => x.Uri.Equals(ActiveFile.Uri, StringComparison.Ordinal));
        f.Data = data;
        f.ModifiedAt = DateTimeOffset.Now;
        
        ActiveFile = f;
        SolutionFilesChanged?.Invoke();
    }

    public void UpdateActiveFile(SolutionFile file)
    {
        var f = Project.Files.FirstOrDefault(x => x.Uri.Equals(ActiveFile.Uri, StringComparison.Ordinal));
        
        f.Data = file.Data;
        f.Uri = file.Uri;
        f.ModifiedAt = DateTimeOffset.Now;
        
        ActiveFile = f;
        SolutionFilesChanged?.Invoke();
    }

    public void AddFile(string name, string? data = "")
    {
        Project.AddFile(name, Project.GetRootFolder(), data);
        SolutionFilesChanged?.Invoke();
    }

    public void AddFile(string name, Folder folder)
    {
       Project.AddFile(name, folder);
        
        SolutionFilesChanged?.Invoke();
    }

    public Folder AddFolder(string name)
    {
        var f = Project.AddFolder(name);
        
        return f;
    }

    public Folder AddFolder(string name, Folder folder)
    {
        
        var f = Project.AddFolder(name, folder);
        SolutionFilesChanged?.Invoke();

        return f;
    }

    public Folder GetFolder(string path)
    {
        return Project.GetFolder(path);
    }

    public void AddFileToFolder(Folder folder, SolutionFile file)
        {
            Project.AddFile(file.Name, folder.Uri, file.Data);
            SolutionFilesChanged?.Invoke();
        }

    public async Task BuildAsync(CancellationToken? cancellationToken = default)
    {
        await _bus.PublishAsync(new FocusTab("Code Analysis Output"));
        await NotifyBuildRequested();
        await _compiler.BuildAsync(Project);
    }

    public async Task BuildAndRunAsync(CancellationToken? cancellationToken = default)
    {
        await NotifyBuildRequested();

        if (Project.ProjectType == ProjectType.Console)
        {
            await _bus.PublishAsync(new FocusTab("Console Output"));
            await _compiler.ExecuteAsync(Project);
        }
        else if (Project.ProjectType == ProjectType.WebApi)
        {
            await _bus.PublishAsync(new FocusTab("Web Host"));
            await _hostingService.RunAsync(Project);
        }
        else
        {
            throw new InvalidOperationException("Project must be a Console or Web API project");
        }
    }

    private async Task NotifyBuildRequested()
    {
        await BuildRequested.Invoke();
        await _bus.PublishAsync(new BuildRequested(Project));
    }

    public void SwitchSolution(string solutionName)
    {
        var solution = Solutions.FirstOrDefault(x => x.Name == solutionName);
        
        if (solution == null || (Project?.Name?.Equals(solutionName, StringComparison.OrdinalIgnoreCase) ?? false))
            return;
        
        Project = solution;
        ProjectChanged?.Invoke();

        ActiveFile = Project.Files.FirstOrDefault();
        if (ActiveFile != null)
        {
            SolutionFilesChanged?.Invoke();
            ActiveFileChanged?.Invoke(ActiveFile);
        }
    }
    

    public async Task LoadSolutionsFromStorageAsync()
    {
        var solutions = await _solutionStorageService.GetSolutionsAsync();

        if (solutions.Count < 1)
            return;
        
        Solutions = solutions;
        
        SwitchSolution(Solutions.First().Name);
    }

    public async Task LoadSolutionAsync(SolutionModel solution)
    {
        await _bus.PublishAsync(new SolutionCreated(solution));

        Solutions.Add(solution);
        SwitchSolution(solution.Name);
    }

    public async Task<SolutionModel> CreateNewSolutionAsync(string name, ProjectType projectType = ProjectType.Console, List<ISolutionItem>? solutionItems = default)
    {
        var solution = new SolutionModel
        {
            Name = name,
            ProjectType = projectType,
            Items = solutionItems ?? new List<ISolutionItem>
            {
                new Folder
                {
                    Name = name,
                    Uri = $"virtual/{name}"
                },
                new SolutionFile
                {
                    Name = "Program.cs",
                    Uri = $"virtual/{name}/Program.cs",
                    Data = GetDefaultProgramCs(projectType),
                    CreatedAt = DateTimeOffset.Now,
                    ModifiedAt = DateTimeOffset.Now
                }
            }
        };

        await _bus.PublishAsync(new SolutionCreated(solution));

        Solutions.Add(solution);
        SwitchSolution(name);
        
        return solution;
    }

    private string GetDefaultProgramCs(ProjectType projectType)
    {
        return projectType switch
        {
            ProjectType.Console => @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello World!"");
    }
}",
            ProjectType.WebApi => @"
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet(""/"", () => ""Hello World!"");

app.Run();",
            _ => throw new ArgumentOutOfRangeException(nameof(projectType))
        };
    }

    public void DeleteFile(SolutionFile file)
    {
        // Handle active file switching first
        if (ActiveFile?.Uri == file.Uri)
        {
            SwitchFile(Project.Files.FirstOrDefault(f => f != file));
        }

        // Remove from project
        Project.DeleteFile(file);
        
        SolutionFilesChanged?.Invoke();
    }

    public void DeleteFolder(Folder folder)
    {
        if (IsRootFolder(folder))
        {
            return;
        }

        // Switch active file first if it's in the folder being deleted
        if (ActiveFile != null && ActiveFile.Uri.StartsWith(folder.Uri))
        {
            var newActiveFile = Project.Files.FirstOrDefault(f => !f.Uri.StartsWith(folder.Uri));
            SwitchFile(newActiveFile);
        }

        // Then delete the folder and its contents
        Project.DeleteFolder(folder);
        
        SolutionFilesChanged?.Invoke();
    }

    public void MoveFile(SolutionFile file, Folder destinationFolder)
    {
        // Find current parent folder
        var currentParentFolder = Project.GetFolders()
            .FirstOrDefault(f => f.Items.Contains(file));
        
        // Remove from current folder if it exists
        currentParentFolder?.Items.Remove(file);

        // Create new URI for the file
        var newUri = Project.CreateUri(file.Name, destinationFolder.Uri);
        
        // Update file's URI
        file.Uri = newUri;
        
        // Add to destination folder
        destinationFolder.Items.Add(file);
        
        // Update folder structure
        Project.GetHierarchicalItems();
        
        SolutionFilesChanged?.Invoke();
    }

    public void CloseActiveSolution()
    {
        Project = default!;
        SolutionFilesChanged?.Invoke();
    }
}