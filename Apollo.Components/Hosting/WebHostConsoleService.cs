using Apollo.Components.Console;
using MudBlazor;

namespace Apollo.Components.Hosting
{
    public class WebHostConsoleService(IJsApiService jsApiService, IScrollManager? scrollManager = null)
        : BaseConsoleService(jsApiService, scrollManager)
    {
        public override IReadOnlyList<ConsoleSeverity> SupportedSeverities { get; } =
        [
            ConsoleSeverity.Debug, 
            ConsoleSeverity.Trace, 
            ConsoleSeverity.Info, 
            ConsoleSeverity.Warning,
            ConsoleSeverity.Error
        ];
        
        public override IReadOnlyCollection<string> Selected { get; set; } = ["Info", "Warning", "Error"];
    }
}
