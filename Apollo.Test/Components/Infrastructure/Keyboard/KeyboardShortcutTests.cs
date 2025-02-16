using Apollo.Components.Infrastructure.Keyboard;
using Microsoft.FluentUI.AspNetCore.Components;
using Shouldly;
using Xunit;

namespace Apollo.Test.Components.Infrastructure.Keyboard;

public class KeyboardShortcutTests
{
    [Fact]
    public void FromKeyCodeArgs_Should_Map_All_Properties()
    {
        // Arrange
        var args = new FluentKeyCodeEventArgs
        {
            Key = KeyCode.KeyB,
            CtrlKey = true,
            AltKey = true,
            ShiftKey = true,
            MetaKey = true
        };

        // Act
        var shortcut = KeyboardShortcut.FromKeyCodeArgs(args);

        // Assert
        shortcut.Key.ShouldBe(KeyCode.KeyB);
        shortcut.Ctrl.ShouldBeTrue();
        shortcut.Alt.ShouldBeTrue();
        shortcut.Shift.ShouldBeTrue();
        shortcut.Meta.ShouldBeTrue();
    }

    [Theory]
    [InlineData(KeyCode.KeyB, "B")]
    [InlineData(KeyCode.Enter, "Enter")]
    [InlineData(KeyCode.Escape, "Escape")]
    public void GetDisplayText_Should_Format_Keys_Correctly(KeyCode key, string expected)
    {
        // Arrange
        var shortcut = new KeyboardShortcut { Key = key };

        // Act
        var result = shortcut.GetDisplayText(key);

        // Assert
        result.ShouldBe(expected);
    }
} 