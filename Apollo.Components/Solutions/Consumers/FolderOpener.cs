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
        FileSystemFileHandle[]? fileHandles = null;
        try
        {
            OpenFilePickerOptionsStartInWellKnownDirectory options = new()
            {
                Multiple = true,
                StartIn = WellKnownDirectory.Downloads
            };
            fileHandles = await _fileSystem.ShowOpenFilePickerAsync(options);
        }
        catch (JSException ex)
        {
            _snackbar.AddApolloNotification("File system api not supported by current browser", Severity.Error);
        }
        finally
        {
            if (fileHandles != null &&  fileHandles?.Length > 0)
            {

                var solution = new SolutionModel();
                foreach (var fh in fileHandles)
                {
                    if (fh is not null)
                    {
                        var file = await fh.GetFileAsync();
                        var text = await file.TextAsync();
                        var name = await file.GetNameAsync();

                        if (string.IsNullOrWhiteSpace(solution.Name))
                        {
                            solution.Name = name;
                        }

                        solution.AddFile(name, text);
                    }
                }
            }
        }
    }
}