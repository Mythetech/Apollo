using Apollo.Components.Console;
using MudBlazor;

namespace Apollo.Test.Components.Console;

public class TestConsole : BaseConsoleService
{
    public TestConsole(IJsApiService jsApiService, IScrollManager? scrollManager = null) : base(jsApiService, scrollManager)
    {
    }
}

public class DisabledTestConsole : BaseConsoleService
{
    public DisabledTestConsole(IJsApiService jsApiService, IScrollManager? scrollManager = null) : base(jsApiService, scrollManager)
    {
    }

    public override IReadOnlyList<ConsoleSeverity> SupportedSeverities { get; } = [];
}