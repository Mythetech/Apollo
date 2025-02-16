using Apollo.Components.Infrastructure.Keyboard;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Shouldly;
using Xunit;

namespace Apollo.Test.Components.Infrastructure.Keyboard;

public class KeyboardIconButtonTests : ApolloBaseTestContext
{
    [Fact]
    public void Should_Handle_Click()
    {
        // Arrange
        var clicked = false;

        // Act
        var cut = RenderComponent<KeyboardIconButton>(parameters => parameters
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true))
        );

        // Act
        cut.FindComponent<MudIconButton>().Find("button").Click();

        // Assert
        clicked.ShouldBeTrue();
    }

    [Fact]
    public void Should_Handle_Disabled_State()
    {
        // Arrange & Act
        var cut = RenderComponent<KeyboardIconButton>(parameters => parameters
            .Add(p => p.Disabled, true)
        );

        // Assert
        cut.FindComponent<MudIconButton>().Instance.Disabled.ShouldBeTrue();
    }
} 