using Apollo.Components.Infrastructure;
using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Shared.ApolloNotificationBar;
using Apollo.Components.Solutions.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;

namespace Apollo.Components.Solutions.Consumers;


public class DocumentFormatter : IConsumer<FormatActiveDocument>, IConsumer<FormatAllDocuments>
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<DocumentFormatter> _logger;
    private readonly ISnackbar _snackbar;
    private readonly SolutionsState _solutions;

    public DocumentFormatter(IJSRuntime jsRuntime, ILogger<DocumentFormatter> logger, ISnackbar snackbar, SolutionsState solutions)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
        _snackbar = snackbar;
        _solutions = solutions;
    }

    public async Task Consume(FormatActiveDocument message)
    {
        _logger.LogInformation("Format Document Requested");
        await TriggerEditorFormat();
    }

    public async Task Consume(FormatAllDocuments message)
    {
        _logger.LogInformation("Format Documents Requested");
        
        int reformattedFiles = 0;
        
        foreach (var file in _solutions.Project.Files)
        {
            string previousText = file.Data;

            var reformatted = await CSharpier.CodeFormatter.FormatAsync(previousText);

            if (reformatted.Code.Equals(previousText, StringComparison.Ordinal)) continue;
            
            reformattedFiles++;
                
            file.Data = reformatted.Code;
                
            _solutions.UpdateFile(file);
            
            if (file.Uri.Equals(_solutions.ActiveFile.Uri, StringComparison.OrdinalIgnoreCase))
            {
                await TriggerEditorFormat();
            }
        }

        if (reformattedFiles > 0)
        {
            _snackbar.AddApolloNotification($"Reformated {reformattedFiles} files", Severity.Success);
        }
    }
    
    private async Task TriggerEditorFormat() => await _jsRuntime.InvokeVoidAsync("blazorMonaco.editor.trigger", "apollo-editor", "apollo-editor", "editor.action.formatDocument");
}