using Apollo.Components.DynamicTabs;
using Apollo.Components.Editor;
using Apollo.Test.Components.Infrastructure;
using Bunit;
using Shouldly;
using Xunit;
using static Apollo.Test.Components.DynamicTabs.TestDynamicTabs;

namespace Apollo.Test.Components.DynamicTabs;

public class TabViewStateTests : TestContext
{
    private TabViewState _state;
    private List<DynamicTabView> _testTabs;
    private bool _stateChangedCalled;

    public TabViewStateTests()
    {
        _state = new TabViewState(TestingRuntimeEnvironment.Instance);
        _state.TabViewStateChanged += () => _stateChangedCalled = true;
        
        // Initialize with test tabs in different zones
        _testTabs = new List<DynamicTabView>
        {
            new TestDynamicTab1 { AreaIdentifier = DropZones.Left, IsActive = true },
            new TestDynamicTab2 { AreaIdentifier = DropZones.Right, IsActive = true },
            new TestDynamicTab3 { AreaIdentifier = DropZones.Bottom },
            new TestDynamicTab4 { AreaIdentifier = DropZones.Docked },
            new TestDynamicTab5 { AreaIdentifier = DropZones.Floating },
            new TestDynamicTab6 { AreaIdentifier = DropZones.None }
        };
        
        // Replace default tabs with test tabs
        typeof(TabViewState)
            .GetField("_tabs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_state, _testTabs);
    }

    [Fact(DisplayName = "Moving tab to new location deactivates existing tabs in that location")]
    public void UpdateTabLocation_DeactivatesOtherTabsInNewLocation()
    {
        // Arrange
        var tab = new TestDynamicTab3 { AreaIdentifier = DropZones.Left };
        _testTabs.Add(tab);

        // Act
        _state.UpdateTabLocation(tab, DropZones.Right);

        // Assert
        tab.IsActive.ShouldBeTrue();
        _testTabs.First(t => t.AreaIdentifier == DropZones.Right && t.TabId != tab.TabId)
            .IsActive.ShouldBeFalse();
        _stateChangedCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Moving tab activates next available tab in previous location")]
    public void UpdateTabLocation_ActivatesNextTabInPreviousLocation()
    {
        // Arrange
        var tab1 = _testTabs.First(t => t.AreaIdentifier == DropZones.Left);
        var tab2 = new TestDynamicTab1 { AreaIdentifier = DropZones.Left, IsActive = false };
        _testTabs.Add(tab2);

        // Act
        _state.UpdateTabLocation(tab1, DropZones.Right);

        // Assert
        tab2.IsActive.ShouldBeTrue();
        _stateChangedCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Focusing hidden tab restores it to its default area")]
    public void FocusTab_RestoresTabFromHiddenState()
    {
        // Arrange
        var tab = _testTabs.First(t => t.AreaIdentifier == DropZones.None);

        // Act
        _state.FocusTab(tab.Name);

        // Assert
        tab.AreaIdentifier.ShouldBe(tab.DefaultArea);
        tab.IsActive.ShouldBeTrue();
        _stateChangedCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Hiding active tab activates remaining tab in same zone")]
    public void HideTab_ActivatesRemainingTabInZone()
    {
        // Arrange
        var tab1 = _testTabs.First(t => t.AreaIdentifier == DropZones.Right);
        var tab2 = new TestDynamicTab2 { AreaIdentifier = DropZones.Right, IsActive = false };
        _testTabs.Add(tab2);

        // Act
        _state.HideTab(tab1.Name);

        // Assert
        tab1.AreaIdentifier.ShouldBe(DropZones.None);
        tab1.IsActive.ShouldBeFalse();
        tab2.IsActive.ShouldBeTrue();
        _stateChangedCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Toggle tab visibility switches between hidden and default area")]
    public void ToggleTabVisibility_HidesAndRestoresTab()
    {
        // Arrange
        var tab = _testTabs.First(t => t.AreaIdentifier == DropZones.Right);

        // Act - Hide
        _state.ToggleTabVisibility(tab);

        // Assert - Hidden
        tab.AreaIdentifier.ShouldBe(DropZones.None);
        tab.IsActive.ShouldBeFalse();

        // Act - Restore
        _state.ToggleTabVisibility(tab);

        // Assert - Restored
        tab.AreaIdentifier.ShouldBe(tab.DefaultArea);
        tab.IsActive.ShouldBeTrue();
        _stateChangedCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Dock state properly manages docked tabs and dock visibility")]
    public void DockState_ManagesDockedTabs()
    {
        // Arrange
        var dockedTab = _testTabs.First(t => t.AreaIdentifier == DropZones.Docked);

        // Act - Close
        _state.CloseDock();

        // Assert - Closed
        dockedTab.IsActive.ShouldBeFalse();
        _state.DockOpen.ShouldBeFalse();

        // Act - Open
        _state.OpenDock();

        // Assert - Opened
        dockedTab.IsActive.ShouldBeTrue();
        _state.DockOpen.ShouldBeTrue();
        _stateChangedCalled.ShouldBeTrue();
    }
}