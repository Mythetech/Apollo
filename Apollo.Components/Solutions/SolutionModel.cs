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
            return false; // File already exists
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

        // Ensure each folder's items are not overwritten
        foreach (var folder in Items.OfType<Folder>())
        {
            // Add only items not already present in folder.Items
            var additionalItems = Items
                .Where(item => item.Uri.StartsWith($"{folder.Uri}/") && !folder.Items.Contains(item))
                .ToList();

            folder.Items.AddRange(additionalItems);
            hierarchy.Add(folder);
        }

        // Add root-level files that are not part of any folder
        var rootFiles = Items.OfType<SolutionFile>()
            .Where(file => !file.Uri.Contains("/"))
            .ToList();

        hierarchy.AddRange(rootFiles);

        return hierarchy;
    }
    
    public Folder GetLogicalSolutionStructure()
{
    // Enforce a single root folder
    var rootFolder = GetRootFolder();

    // Clear and rebuild the hierarchy for the root folder
    rootFolder.Items.Clear();

    foreach (var item in Items)
    {
        // Skip the root folder itself
        if (item.Uri == rootFolder.Uri)
        {
            //System.Console.WriteLine($"Skipping root folder: {item.Uri}");
            continue;
        }

        //System.Console.WriteLine($"Processing item: {item.Name}, URI: {item.Uri}");

        // Normalize URI by removing the prefix
        var relativeUri = item.Uri.Replace($"{Prefix}/", "").TrimStart('/');

        //System.Console.WriteLine($"Normalized URI: {relativeUri}");

        // Split the URI into parts
        var pathSegments = relativeUri.Split('/');
        var currentFolder = rootFolder;

        for (int i = 0; i < pathSegments.Length; i++)
        {
            var segment = pathSegments[i];
            //System.Console.WriteLine($"Segment: {segment}, Current Folder: {currentFolder.Name}");

            if (i == 0 && segment == rootFolder.Name)
            {
                // Skip the root folder segment
                //System.Console.WriteLine($"Skipping root folder segment: {segment}");
                continue;
            }

            if (i == pathSegments.Length - 1 && item is SolutionFile file)
            {
                //System.Console.WriteLine($"Adding file: {file.Name} to folder: {currentFolder.Name}");
                if (!currentFolder.Items.Contains(file))
                {
                    currentFolder.Items.Add(file);
                }
            }
            else
            {
                // Find or create folder
                var existingFolder = currentFolder.Items
                    .OfType<Folder>()
                    .FirstOrDefault(f => f.Name == segment);

                if (existingFolder == null)
                {
                    //System.Console.WriteLine($"Creating new folder: {segment}");
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
            // Start with the root or the current folder
            string parentUri = currentFolder?.Uri ?? Path;

            // Build the current URI correctly
            string currentUri = CreateUri(segment, parentUri);

            // Check for existing folder
            var existingFolder = Items.OfType<Folder>().FirstOrDefault(f => f.Uri == currentUri);
            if (existingFolder != null)
            {
                currentFolder = existingFolder;
            }
            else
            {
                // Create new folder
                var newFolder = new Folder
                {
                    Name = segment,
                    Uri = currentUri,
                    Items = new List<ISolutionItem>()
                };

                if (currentFolder == null)
                {
                    Items.Add(newFolder); // Add to root
                }
                else
                {
                    currentFolder.Items.Add(newFolder); // Add to current folder
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
        // Normalize path to remove extra prefixes or slashes
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