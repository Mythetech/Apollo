using Apollo.Contracts.Solutions;
using Microsoft.CodeAnalysis;
using Console = System.Console;

namespace Apollo.Components.Solutions;
public class SolutionModel
{
    public SolutionModel()
    {
        Name = "";
    }

    public SolutionModel(string projectName)
    {
        Name = projectName;
    }
    
    public string Name { get; set; }
    
    public string? Description { get; set; }
    
    public ProjectType ProjectType { get; set; } = ProjectType.Console;
    public List<ISolutionItem> Items { get; set; } = new List<ISolutionItem>();

    public List<SolutionFile> Files => GetFiles();

    public string Path => $"{Prefix}/{Name}";

    public string Prefix { get; private set; } = "virtual";

    public bool DuplicateFileInFolder(string path, string name) => FilesInPath(path).Any(x => x.Name.Equals(name));

    public IEnumerable<ISolutionItem> FilesInPath(string path) => Items.Where(item => item.Uri.Contains(path));

    public bool AddFile(string name, string folderPath, string data = "")
    {
        var folder = GetOrCreateFolder(folderPath);

        if (DuplicateFileInFolder(folderPath, name))
        {
            return false; 
        }

        var file = new SolutionFile
        {
            Name = name,
            Uri = CreateUri(name, folder.Uri),
            Data = data
        };

        Items.Add(file);
        folder.Items.Add(file);

        return true;
    }

    public void AddFile(string name, string data = "")
    {
        AddFile(name, GetRootFolder(), data);
    }

    public void AddFile(string name, Folder folder, string data = "")
    {
        folder.Items ??= new List<ISolutionItem>();
        var file = new SolutionFile
        {
            Name = name,
            Uri = CreateUri(name, folder.Uri), 
            Data = data
        };

        Items.Add(file);
        folder.Items.Add(file);
    }

    public List<ISolutionItem> GetHierarchicalItems()
    {
        var hierarchy = new List<ISolutionItem>();

        foreach (var folder in Items.OfType<Folder>())
        {
            var additionalItems = Items
                .Where(item => item.Uri.StartsWith($"{folder.Uri}/") && !folder.Items.Contains(item))
                .ToList();

            folder.Items.AddRange(additionalItems);
            hierarchy.Add(folder);
        }

        var rootFiles = Items.OfType<SolutionFile>()
            .Where(file => !file.Uri.Contains("/"))
            .ToList();

        hierarchy.AddRange(rootFiles);

        return hierarchy;
    }
    
    public Folder GetLogicalSolutionStructure()
{
    var rootFolder = GetRootFolder();

    rootFolder.Items.Clear();

    foreach (var item in Items)
    {
        if (item.Uri == rootFolder.Uri)
        {
            continue;
        }
        
        var relativeUri = item.Uri.Replace($"{Prefix}/", "").TrimStart('/');

        var pathSegments = relativeUri.Split('/');
        var currentFolder = rootFolder;

        for (int i = 0; i < pathSegments.Length; i++)
        {
            var segment = pathSegments[i];

            if (i == 0 && segment == rootFolder.Name)
            {
                continue;
            }

            if (i == pathSegments.Length - 1 && item is SolutionFile file)
            {
                if (!currentFolder.Items.Contains(file))
                {
                    currentFolder.Items.Add(file);
                }
            }
            else
            {
                var existingFolder = currentFolder.Items
                    .OfType<Folder>()
                    .FirstOrDefault(f => f.Name == segment);

                if (existingFolder == null)
                {
                    var newFolder = new Folder
                    {
                        Name = segment,
                        Uri = CreateUri(segment, currentFolder.Uri),
                        Items = new List<ISolutionItem>(),
                        CreatedAt = DateTime.Now,
                        ModifiedAt = DateTime.Now
                    };

                    currentFolder.Items.Add(newFolder);
                    currentFolder = newFolder;
                }
                else
                {
                    currentFolder = existingFolder;
                }
            }
        }
    }

    return rootFolder;
}
    
    public Folder AddFolder(string name)
    {
        return AddFolder(name, GetRootFolder());
    }
    
    public Folder AddFolder(string name, Folder parentFolder)
    {
        var folderUri = CreateUri(name, parentFolder.Uri);

        var newFolder = new Folder
        {
            Name = name,
            Uri = folderUri,
            Items = new List<ISolutionItem>()
        };

        Items.Add(newFolder);
        parentFolder.Items.Add(newFolder);

        return newFolder;
    }

    public Folder? GetFolder(string name)
    {
        return Items.OfType<Folder>()
            .FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public Folder GetRootFolder()
    {
        var rootFolder = Items.OfType<Folder>().FirstOrDefault(f => f.Uri == Path);
        if (rootFolder == null)
        {
            rootFolder = new Folder { Name = Name, Uri = Path };
            Items.Add(rootFolder);
        }

        if (rootFolder.Uri != Path)
        {
            throw new InvalidOperationException($"Root folder URI mismatch. Expected {Path}, got {rootFolder.Uri}");
        }

        return rootFolder;
    }

    public Folder GetOrCreateFolder(string folderPath)
    {
        var segments = folderPath.Split('/').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        Folder? currentFolder = null;

        foreach (var segment in segments)
        {
            string parentUri = currentFolder?.Uri ?? Path;

            string currentUri = CreateUri(segment, parentUri);

            var existingFolder = Items.OfType<Folder>().FirstOrDefault(f => f.Uri == currentUri);
            if (existingFolder != null)
            {
                currentFolder = existingFolder;
            }
            else
            {
                var newFolder = new Folder
                {
                    Name = segment,
                    Uri = currentUri,
                    Items = new List<ISolutionItem>()
                };

                if (currentFolder == null)
                {
                    Items.Add(newFolder); 
                }
                else
                {
                    currentFolder.Items.Add(newFolder); 
                }

                currentFolder = newFolder;
            }
        }

        return currentFolder!;
    }

    public List<Folder> GetFolders()
    {
        return Items.OfType<Folder>().ToList();
    }

    public List<SolutionFile> GetFiles()
    {
        return Items.OfType<SolutionFile>().ToList();
    }

    public string CreateUri(string name, string path)
    {
        if (path.StartsWith(Prefix))
        {
            path = path.Substring(Prefix.Length).TrimStart('/');
        }

        return $"{Prefix}/{path}/{name}".Replace("//", "/").TrimStart('/');
    }

    public void DeleteFile(SolutionFile file)
    {
        Items.Remove(file);
    }

    public void DeleteFolder(Folder folder)
    {
        var itemsToRemove = Items
            .Where(item => item.Uri.StartsWith(folder.Uri, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var item in itemsToRemove)
        {
            Items.Remove(item);
        }
    }

    public override string ToString()
    {
        return Name;
    }
}