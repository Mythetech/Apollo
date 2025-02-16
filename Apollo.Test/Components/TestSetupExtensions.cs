using Bunit;
using MudBlazor;
using MudBlazor.Services;

namespace Apollo.Test.Components;

public static class TestSetupExtensions
{
    public static void AddDefaultTestServices(this TestContext ctx)
    {
        ctx.Services.AddMudServices(options =>
        {
            options.SnackbarConfiguration.ShowTransitionDuration = 0;
            options.SnackbarConfiguration.HideTransitionDuration = 0;
            options.PopoverOptions.CheckForPopoverProvider = false;
        });
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.SetupVoid("mudPopover.initialize", _ => true);
    }
}