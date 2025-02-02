using Apollo.Components.Console;
using Bunit;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Apollo.Test.Components.Console;

public class BaseConsoleTests : TestContext
{
    private TestConsole _testConsole;
    private IJsApiService _jsApiService;
    
    public BaseConsoleTests()
    {
        _jsApiService = Substitute.For<IJsApiService>();
        _testConsole = new TestConsole(_jsApiService);
    }

    [Theory(DisplayName = "Can add a log entry to the console")]
    [InlineData(ConsoleSeverity.Debug)]
    [InlineData(ConsoleSeverity.Trace)]
    [InlineData(ConsoleSeverity.Info)]
    [InlineData(ConsoleSeverity.Warning)]
    [InlineData(ConsoleSeverity.Error)]
    [InlineData(ConsoleSeverity.Success)]
    public void Can_Add_LogOfType(ConsoleSeverity severity)
    {
        // Act
        _testConsole.AddLog("Test", severity);
        
        // Assert
        _testConsole.Logs.Count.ShouldBe(1);
        _testConsole.Logs[0].Severity.ShouldBe(severity);
        _testConsole.Logs[0].Message.ShouldBe("Test");
    }
    
    [Theory(DisplayName = "Does not log for type when disabled by console service")]
    [InlineData(ConsoleSeverity.Debug)]
    [InlineData(ConsoleSeverity.Trace)]
    [InlineData(ConsoleSeverity.Info)]
    [InlineData(ConsoleSeverity.Warning)]
    [InlineData(ConsoleSeverity.Error)]
    [InlineData(ConsoleSeverity.Success)]
    public void Only_Logs_WhenEnabled_ForType(ConsoleSeverity severity)
    {
        // Act
        var testConsole = new DisabledTestConsole(Substitute.For<IJsApiService>())
        {
            Selected = [],
            LogEnabledOnly = true
        };
        
        testConsole.AddLog("Test", severity);
        
        // Assert
        testConsole.Logs.Count.ShouldBe(0);
    }
    
    [Fact(DisplayName = "Auto scrolls when adding logs only if enabled")]
    public async Task Respects_AutoScroll_Enabled()
    {
        // Arrange
        var sm = Substitute.For<IScrollManager>();
        _testConsole.ScrollManager = sm;
        _testConsole.AutoScroll = true;
        
        // Act
        await _testConsole.AddLogAsync("Test", ConsoleSeverity.Debug);
        
        // Assert
        sm.Received().ScrollToListItemAsync(Arg.Any<string>());
    }
    
    [Fact(DisplayName = "Does not auto scrolls when adding logs if disabled")]
    public async Task Respects_AutoScroll_Disabled()
    {
        // Arrange
        var sm = Substitute.For<IScrollManager>();
        _testConsole.ScrollManager = sm;
        _testConsole.AutoScroll = false;
        
        // Act
        await _testConsole.AddLogAsync("Test", ConsoleSeverity.Debug);
        
        // Assert
        sm.DidNotReceive().ScrollToListItemAsync(Arg.Any<string>());
    }

    [Theory(DisplayName = "Filter should only return logs matching selected severities")]
    [InlineData(new[] { "Debug" }, new[] { ConsoleSeverity.Debug }, 1)]
    [InlineData(new[] { "Error", "Warning" }, new[] { ConsoleSeverity.Debug, ConsoleSeverity.Error, ConsoleSeverity.Warning }, 2)]
    [InlineData(new[] { "Success" }, new[] { ConsoleSeverity.Debug, ConsoleSeverity.Error }, 0)]
    public void Filter_ShouldOnlyReturn_SelectedSeverities(string[] selected, ConsoleSeverity[] logTypes, int expectedCount)
    {
        // Arrange
        _testConsole.Selected = selected;
        foreach (var severity in logTypes)
        {
            _testConsole.AddLog($"Test {severity}", severity);
        }

        // Act
        var filtered = _testConsole.Filter(_testConsole.Logs);

        // Assert
        filtered.Count.ShouldBe(expectedCount);
        filtered.ShouldAllBe(log => selected.Contains(log.Severity.ToString()));
    }

    [Fact(DisplayName = "Filter should handle empty selection")]
    public void Filter_ShouldHandleEmptySelection()
    {
        // Arrange
        _testConsole.Selected = Array.Empty<string>();
        _testConsole.AddLog("Test Debug", ConsoleSeverity.Debug);
        _testConsole.AddLog("Test Error", ConsoleSeverity.Error);

        // Act
        var filtered = _testConsole.Filter(_testConsole.Logs);

        // Assert
        filtered.Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Filter should handle invalid severity strings")]
    public void Filter_ShouldHandleInvalidSeverities()
    {
        // Arrange
        _testConsole.Selected = new[] { "Debug", "InvalidSeverity", "Error" };
        _testConsole.AddLog("Test Debug", ConsoleSeverity.Debug);
        _testConsole.AddLog("Test Error", ConsoleSeverity.Error);

        // Act
        var filtered = _testConsole.Filter(_testConsole.Logs);

        // Assert
        filtered.Count.ShouldBe(2);
        filtered.ShouldContain(log => log.Severity == ConsoleSeverity.Debug);
        filtered.ShouldContain(log => log.Severity == ConsoleSeverity.Error);
    }

    [Fact(DisplayName = "Filter should be case insensitive")]
    public void Filter_ShouldBeCaseInsensitive()
    {
        // Arrange
        _testConsole.Selected = new[] { "debug", "ERROR", "Warning" };
        _testConsole.AddLog("Test Debug", ConsoleSeverity.Debug);
        _testConsole.AddLog("Test Error", ConsoleSeverity.Error);
        _testConsole.AddLog("Test Warning", ConsoleSeverity.Warning);
        _testConsole.AddLog("Test Info", ConsoleSeverity.Info);

        // Act
        var filtered = _testConsole.Filter(_testConsole.Logs);

        // Assert
        filtered.Count.ShouldBe(3);
        filtered.Select(l => l.Severity).ShouldBe([
            ConsoleSeverity.Debug, 
            ConsoleSeverity.Error, 
            ConsoleSeverity.Warning
        ]);
    }

    [Fact(DisplayName = "Copy to clipboard should copy logs")]
    public async Task Can_CopyLogs_ToClipboard()
    {
        // Arrange 
        _testConsole.AddLog("Test Info", ConsoleSeverity.Info);

        // Act
       await _testConsole.CopyLogsAsync();

       // Assert
       _jsApiService.Received().CopyToClipboardAsync("Test Info");
    }
    
    [Fact(DisplayName = "Copy filtered clipboard should copy only filtered logs")]
    public async Task Can_CopyFilteredLogsOnly_ToClipboard()
    {
        // Arrange 
        _testConsole.Selected = ["Info"];
        _testConsole.AddLog("Test1", ConsoleSeverity.Info);
        _testConsole.AddLog("Test2", ConsoleSeverity.Info);
        _testConsole.AddLog("Test3", ConsoleSeverity.Debug);
        _testConsole.AddLog("Test4", ConsoleSeverity.Info);
        _testConsole.AddLog("Test5", ConsoleSeverity.Trace);

        // Act
        await _testConsole.CopyVisibleLogsAsync();

        // Assert
        _jsApiService.Received().CopyToClipboardAsync(string.Join(Environment.NewLine, "Test1", "Test2", "Test4"));
    }
}