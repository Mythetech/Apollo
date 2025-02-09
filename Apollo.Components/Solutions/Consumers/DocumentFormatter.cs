using Apollo.Components.Infrastructure;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Apollo.Components.Solutions.Consumers;


public class DocumentFormatter : IConsumer<FormatActiveDocument>
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<DocumentFormatter> _logger;

    public DocumentFormatter(IJSRuntime jsRuntime, ILogger<DocumentFormatter> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task Consume(FormatActiveDocument message)
    {
        _logger.LogInformation("Format Document Requested");
        await _jsRuntime.InvokeVoidAsync("blazorMonaco.editor.trigger", (object) "apollo-editor", (object) "apollo-editor", (object) "editor.action.formatDocument");
    }
}