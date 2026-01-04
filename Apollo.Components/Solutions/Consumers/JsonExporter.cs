using System.Text.Json;
using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Shared;
using Apollo.Components.Solutions.Commands;
using Microsoft.JSInterop;

namespace Apollo.Components.Solutions.Consumers;

public class JsonExporter : IConsumer<ExportJsonFile>
{
    private readonly SolutionsState _solutionsState;
    private readonly IJSRuntime _jsRuntime;

    public JsonExporter(SolutionsState solutionsState, IJSRuntime jsRuntime)
    {
        _solutionsState = solutionsState;
        _jsRuntime = jsRuntime;
    }
    public async Task Consume(ExportJsonFile message)
    {
        var solution = _solutionsState.Project;
        
        var serialized = JsonSerializer.Serialize(solution, new JsonSerializerOptions()
        {
            WriteIndented = true,
        });
        
        await _jsRuntime.SaveAs(solution.Name + ".json", serialized);
    }
}