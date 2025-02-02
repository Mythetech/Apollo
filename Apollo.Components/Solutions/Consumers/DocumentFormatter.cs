using Apollo.Components.Infrastructure;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;
using Microsoft.JSInterop;

namespace Apollo.Components.Solutions.Consumers;


public class DocumentFormatter : IConsumer<FormatActiveDocument>
{
    private readonly IJSRuntime _jsRuntime;

    public DocumentFormatter(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task Consume(FormatActiveDocument message)
    {
        await _jsRuntime.InvokeVoidAsync("blazorMonaco.editor.trigger", (object) "apollo-editor", (object) "apollo-editor", (object) "editor.action.formatDocument");
    }
}