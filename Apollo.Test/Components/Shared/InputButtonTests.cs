using Apollo.Components.Shared;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Services;
using Shouldly;
using Xunit;

namespace Apollo.Test.Components.Shared;

public class InputButtonTests : ApolloBaseTestContext
{
    [Fact]
    public void Should_Start_In_Button_Mode()
    {
        // Arrange & Act
        var cut = RenderComponent<InputButton>();

        // Assert
        cut.FindComponent<ApolloIconButton>().ShouldNotBeNull();
        cut.FindComponents<MudTextField<string>>().Count.ShouldBe(0);
    }

    [Fact]
    public void Should_Switch_To_Edit_Mode_When_Clicked()
    {
        // Arrange
        var cut = RenderComponent<InputButton>();
        
        // Act
        cut.FindComponent<ApolloIconButton>().Find("button").Click();
        
        // Assert
        cut.FindComponent<MudTextField<string>>().ShouldNotBeNull();
        cut.FindComponents<ApolloIconButton>().Count.ShouldBe(2); // Save and Cancel buttons
    }

    [Fact]
    public async Task Should_Update_Value_On_Save()
    {
        // Arrange
        var currentValue = "";
        var cut = RenderComponent<InputButton>(parameters => parameters
            .Add(p => p.Value, currentValue)
            .Add(p => p.ValueChanged, (string v) => currentValue = v)
        );
        
        // Act
        cut.FindComponent<ApolloIconButton>().Find("button").Click();
        var textField = cut.FindComponent<MudTextField<string>>();
        await textField.InvokeAsync(() => textField.Instance.ValueChanged.InvokeAsync("test value"));
        
        var saveButton = cut.FindComponents<ApolloIconButton>()
            .First(b => b.Instance.Tooltip == "Save");
        await saveButton.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

        // Assert
        currentValue.ShouldBe("test value");
        cut.FindComponents<MudTextField<string>>().Count.ShouldBe(0); // Back to button mode
    }

    [Fact]
    public async Task Should_Not_Update_Value_On_Cancel()
    {
        // Arrange
        var currentValue = "original";
        var cut = RenderComponent<InputButton>(parameters => parameters
            .Add(p => p.Value, currentValue)
            .Add(p => p.ValueChanged, (string v) => currentValue = v)
        );
        
        // Act
        cut.FindComponent<ApolloIconButton>().Find("button").Click();
        var textField = cut.FindComponent<MudTextField<string>>();
        await textField.InvokeAsync(() => textField.Instance.ValueChanged.InvokeAsync("test value"));
        
        var cancelButton = cut.FindComponents<ApolloIconButton>()
            .First(b => b.Instance.Tooltip == "Cancel");
        await cancelButton.InvokeAsync(() => cancelButton.Instance.OnClick.InvokeAsync());

        // Assert
        currentValue.ShouldBe("original");
        cut.FindComponents<MudTextField<string>>().Count.ShouldBe(0); // Back to button mode
    }

    [Fact]
    public async Task Should_Handle_Enter_Key_As_Save()
    {
        // Arrange
        var currentValue = "";
        var cut = RenderComponent<InputButton>(parameters => parameters
            .Add(p => p.Value, currentValue)
            .Add(p => p.ValueChanged, (string v) => currentValue = v)
        );
        
        // Act
        cut.FindComponent<ApolloIconButton>().Find("button").Click();
        var textField = cut.FindComponent<MudTextField<string>>();
        await textField.InvokeAsync(() => textField.Instance.ValueChanged.InvokeAsync("test value"));
        
        await textField.InvokeAsync(() => textField.Instance.OnKeyDown.InvokeAsync(
            new KeyboardEventArgs { Key = "Enter" }));

        // Assert
        currentValue.ShouldBe("test value");
        cut.FindComponents<MudTextField<string>>().Count.ShouldBe(0); // Back to button mode
    }

    [Fact]
    public async Task Should_Handle_Escape_Key_As_Cancel()
    {
        // Arrange
        var currentValue = "original";
        var cut = RenderComponent<InputButton>(parameters => parameters
            .Add(p => p.Value, currentValue)
            .Add(p => p.ValueChanged, (string v) => currentValue = v)
        );
        
        // Act
        cut.FindComponent<ApolloIconButton>().Find("button").Click();
        var textField = cut.FindComponent<MudTextField<string>>();
        await textField.InvokeAsync(() => textField.Instance.ValueChanged.InvokeAsync("test value"));
        
        await textField.InvokeAsync(() => textField.Instance.OnKeyDown.InvokeAsync(
            new KeyboardEventArgs { Key = "Escape" }));

        // Assert
        currentValue.ShouldBe("original");
        cut.FindComponents<MudTextField<string>>().Count.ShouldBe(0); // Back to button mode
    }

    [Fact]
    public async Task Should_Cancel_On_Blur()
    {
        // Arrange
        var currentValue = "original";
        var cut = RenderComponent<InputButton>(parameters => parameters
            .Add(p => p.Value, currentValue)
            .Add(p => p.ValueChanged, (string v) => currentValue = v)
        );
        
        // Act
        cut.FindComponent<ApolloIconButton>().Find("button").Click();
        var textField = cut.FindComponent<MudTextField<string>>();
        await textField.InvokeAsync(() => textField.Instance.ValueChanged.InvokeAsync("test value"));
        
        await textField.InvokeAsync(() => textField.Instance.OnBlur.InvokeAsync());

        // Assert
        currentValue.ShouldBe("original");
        cut.FindComponents<MudTextField<string>>().Count.ShouldBe(0); // Back to button mode
    }
}