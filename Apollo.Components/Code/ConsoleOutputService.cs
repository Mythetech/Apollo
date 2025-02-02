using Apollo.Components.Console;
using Microsoft.JSInterop;
using MudBlazor;

namespace Apollo.Components.Code
{
    public class ConsoleOutputService : BaseConsoleService, IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private IJSObjectReference? _jsModule;
        
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;
        
        private DotNetObjectReference<ConsoleOutputService>? _dotNetObjectReference;
        
        public override IReadOnlyCollection<string> Selected { get; set; } = ["Info", "Warning", "Error", "Success"];
        
        public ConsoleOutputService(IJSRuntime jsRuntime, IJsApiService jsApiService, IScrollManager scrollManager = null) : base(jsApiService, scrollManager)
        {
            _jsRuntime = jsRuntime;
            _dotNetObjectReference = DotNetObjectReference.Create(this);
            
            InitializeAsync();
        }
        
        public async void InitializeAsync()
        {
            _jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Apollo.Components/app.js");

            await _jsModule.InvokeVoidAsync("captureConsoleOutput", _dotNetObjectReference);
        }
        
        [JSInvokable]
        public void OnConsoleLog(string message)
        {
            AddLog(message, ConsoleSeverity.Info); 
        }

        [JSInvokable]
        public void OnConsoleError(string message)
        {
            AddLog(message, ConsoleSeverity.Error); 
        }

        [JSInvokable]
        public void OnConsoleWarn(string message)
        {
            AddLog(message, ConsoleSeverity.Warning); 
        }
        
     
        public void ClearLogs()
        {
            ClearConsole();
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_jsModule is not null)
            {
                await _jsModule.DisposeAsync();
            }
        }
    }
}