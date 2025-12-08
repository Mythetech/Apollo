using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Shared.ApolloNotificationBar;
using Apollo.Components.Solutions.Commands;
using KristofferStrube.Blazor.FileSystem;
using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.JSInterop;
using MudBlazor;

namespace Apollo.Components.Solutions.Consumers;

public class FolderOpener : IConsumer<PromptOpenFolder>
{
    private readonly IFileSystemAccessService _fileSystem;
    private readonly SolutionsState _state;
    private readonly ISnackbar _snackbar;

    public FolderOpener(IFileSystemAccessService fileSystem, SolutionsState state, ISnackbar snackbar)
    {
        _fileSystem = fileSystem;
        _state = state;
        _snackbar = snackbar;
    }

    public async Task Consume(PromptOpenFolder message)
    {
        FileSystemDirectoryHandle? directoryHandle = null;
        try
        {
            DirectoryPickerOptionsStartInWellKnownDirectory options = new()
            {
                StartIn = WellKnownDirectory.Documents
            };
            directoryHandle = await _fileSystem.ShowDirectoryPickerAsync(options);
        }
        catch (JSException)
        {
            _snackbar.AddApolloNotification("File system api not supported by current browser", Severity.Error);
            return;
        }

        if (directoryHandle is null)
        {
            return;
        }

        try
        {
            var solutionName = await directoryHandle.GetNameAsync();
            var solution = new SolutionModel(solutionName);

            await ProcessDirectoryAsync(directoryHandle, solution, string.Empty);

            if (solution.Files.Count > 0)
            {
                await _state.LoadSolutionAsync(solution);
            }
            else
            {
                _snackbar.AddApolloNotification("No files found in the selected folder", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            _snackbar.AddApolloNotification($"Error reading folder: {ex.Message}", Severity.Error);
        }
    }

    private async Task ProcessDirectoryAsync(
        FileSystemDirectoryHandle directoryHandle,
        SolutionModel solution,
        string relativePath)
    {
        var handles = await directoryHandle.ValuesAsync();
        foreach (var entry in handles)
        {
            if (entry is FileSystemFileHandle fileHandle)
            {
                try
                {
                    var file = await fileHandle.GetFileAsync();
                    var fileName = await fileHandle.GetNameAsync();
                    var text = await file.TextAsync();
                    
                    var folderPath = string.IsNullOrEmpty(relativePath) 
                        ? solution.Name 
                        : $"{solution.Name}/{relativePath}";
                    
                    solution.AddFile(fileName, folderPath, text);
                }
                catch
                {
                    // Skip files that can't be read as text
                }
            }
            else if (entry is FileSystemDirectoryHandle subDirectoryHandle)
            {
                var subDirectoryName = await subDirectoryHandle.GetNameAsync();
                var subDirectoryRelativePath = string.IsNullOrEmpty(relativePath) 
                    ? subDirectoryName 
                    : $"{relativePath}/{subDirectoryName}";
                
                await ProcessDirectoryAsync(subDirectoryHandle, solution, subDirectoryRelativePath);
            }
        }
    }
}