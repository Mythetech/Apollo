using System.IO.Compression;
using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Shared;
using Apollo.Components.Solutions.Commands;
using Microsoft.JSInterop;

namespace Apollo.Components.Solutions.Consumers;

public class ZipExporter : IConsumer<ExportZipFile>
{
    private readonly SolutionsState _solutionsState;
    private readonly IJSRuntime _jsRuntime;

    public ZipExporter(SolutionsState solutionsState, IJSRuntime jsRuntime)
    {
        _solutionsState = solutionsState;
        _jsRuntime = jsRuntime;
    }

    public async Task Consume(ExportZipFile message)
    {
        var solution = _solutionsState.Project;
        
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var file in solution.Files)
            {
                var entry = archive.CreateEntry(file.Name);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream);
                await writer.WriteAsync(file.Data);
            }
        }

        var bytes = memoryStream.ToArray();
        await _jsRuntime.SaveAs($"{solution.Name}.zip", bytes);
    }
} 