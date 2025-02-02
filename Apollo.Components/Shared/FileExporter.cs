using System.Text;
using Microsoft.JSInterop;

namespace Apollo.Components.Shared;

public static class FileExporter
{
    private static IJSObjectReference? _jsModule;

    public static async Task SaveAs(IJSRuntime js, string filename, byte[] data)
    {
        _jsModule ??= await js.InvokeAsync<IJSObjectReference>("import", "./_content/Apollo.Components/app.js");

        await _jsModule.InvokeVoidAsync(
            "saveAsFile",
            filename,
            Convert.ToBase64String(data));
    } 
}

public static class FileExporterExtensions
{
    public static async Task SaveAs(this IJSRuntime js, string filename, byte[] data)
        => await FileExporter.SaveAs(js, filename, data);
    
    public static async Task SaveAs(this IJSRuntime js, string filename, string data)
        => await FileExporter.SaveAs(js, filename, Encoding.UTF8.GetBytes(data));
}