using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Shared.ApolloNotificationBar;
using Apollo.Components.Solutions.Commands;
using KristofferStrube.Blazor.FileSystem;
using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.JSInterop;
using MudBlazor;

namespace Apollo.Components.Solutions.Consumers;

public class FileOpener : IConsumer<PromptOpenFile>
{
    private readonly IFileSystemAccessService _fileSystem;
    private readonly SolutionsState _state;
    private readonly ISnackbar _snackbar;

    public FileOpener(IFileSystemAccessService fileSystem, SolutionsState state, ISnackbar snackbar)
    {
        _fileSystem = fileSystem;
        _state = state;
        _snackbar = snackbar;
    }

    public async Task Consume(PromptOpenFile message)
    {
        FileSystemFileHandle? fileHandle = null;
        try
        {
            OpenFilePickerOptionsStartInWellKnownDirectory options = new()
            {
                Multiple = false,
                StartIn = WellKnownDirectory.Downloads
            };
            var fileHandles = await _fileSystem.ShowOpenFilePickerAsync(options);
            fileHandle = fileHandles.Single();
        }
        catch (JSException ex)
        {
            _snackbar.AddApolloNotification("File system api not supported by current browser", Severity.Error);
        }
        finally
        {
            if (fileHandle is not null)
            {
                var file = await fileHandle.GetFileAsync();
                var text = await file.TextAsync();
                _state.AddFile(await file.GetNameAsync(), text);
            }
        }
    }
}