using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;
using MudBlazor;

namespace Apollo.Components.Solutions.Consumers;

public class ActiveFileCopier : IConsumer<CopyActiveFileToClipboard>
{
    private readonly SolutionsState _state;
    private readonly IJsApiService _jsApiService;

    public ActiveFileCopier(SolutionsState state, IJsApiService jsApiService)
    {
        _state = state;
        _jsApiService = jsApiService;
    }

    public async Task Consume(CopyActiveFileToClipboard message)
    {
        await _state.SaveProjectFilesAsync();
        var content = _state.ActiveFile.Data;

        await _jsApiService.CopyToClipboardAsync(content);
    }
}