using Apollo.Components.Analysis;
using Apollo.Components.Code;
using Apollo.Components.DynamicTabs;
using Apollo.Components.Hosting;
using Apollo.Components.Infrastructure.Environment;
using Apollo.Components.Infrastructure.Logging;
using Apollo.Components.Library;
using Apollo.Components.Solutions;
using Apollo.Components.Terminal;
using Apollo.Components.Testing;

namespace Apollo.Components.Editor;

public class TabViewState
{
    private readonly IRuntimeEnvironment _environment;
    public event Action? TabViewStateChanged;
    
    private List<DynamicTabView> _tabs;

    public TabViewState(IRuntimeEnvironment environment)
    {
        _environment = environment;
        _tabs = InitializeDefaultLayout();
    }

    private bool _dockOpen = false;

    public bool DockOpen
    {
        get => _dockOpen;
        set { _dockOpen = value; NotifyStateChanged(); }
    }

    public void CloseDock()
    {
        _dockOpen = false;
        InactivateTabsInArea(DropZones.Docked);
    }

    public void OpenDock()
    {
        if (_dockOpen)
            return;
        
        _dockOpen = true;
        if (!Tabs.Any(x => x.AreaIdentifier.Equals(DropZones.Docked) && x.IsActive))
        {
            var docked = Tabs.FirstOrDefault(x => x.AreaIdentifier.Equals(DropZones.Docked));
            if(docked != null)
                docked.IsActive = true;
        }
        NotifyStateChanged();
    }

    public IReadOnlyList<DynamicTabView> Tabs => _tabs.AsReadOnly();
    
    public List<DynamicTabView> TabList => _tabs;

    public void UpdateTabLocation(DynamicTabView tab, string newLocation)
    {
        var previousLocation = tab.AreaIdentifier;
        var existingTab = _tabs.FirstOrDefault(t => t.TabId == tab.TabId);
        if (existingTab != null)
        {
            existingTab.AreaIdentifier = newLocation;
            existingTab.IsActive = true;
            
            foreach (var otherTab in _tabs.Where(t => 
                t.AreaIdentifier == newLocation && t.TabId != tab.TabId))
            {
                otherTab.IsActive = false;
            }

            if (previousLocation != newLocation && 
                !_tabs.Any(t => t.AreaIdentifier == previousLocation && t.IsActive))
            {
                var nextTab = _tabs
                    .Where(t => t.AreaIdentifier == previousLocation)
                    .OrderByDescending(t => t.AreaIndex)
                    .FirstOrDefault();

                if (nextTab != null)
                {
                    nextTab.IsActive = true;
                }
            }
            
            NotifyStateChanged();
        }
    }

    public void InactivateTabsInArea(string areaIdentifier)
    {
        HideTabsInArea(areaIdentifier);
        NotifyStateChanged();
    }

    private void HideTabsInArea(string areaIdentifier)
    {
        if (areaIdentifier.Equals(DropZones.Floating))
            return;
        
        foreach(var tab in _tabs.Where(x => x.AreaIdentifier.Equals(areaIdentifier)))
        {
            tab.IsActive = false;
        }
    }

    public void FocusTab(string tabName)
    {
        var existingTab = _tabs.FirstOrDefault(t => t.Name.Equals(tabName, StringComparison.OrdinalIgnoreCase));
        
        if(existingTab == null) return;
        
        if (existingTab.IsActive) return;
        
        if (existingTab.AreaIdentifier == DropZones.None)
        {
            existingTab.AreaIdentifier = existingTab.DefaultArea;
        }
      
        HideTabsInArea(existingTab.AreaIdentifier);
        existingTab.IsActive = true;

        if (existingTab.AreaIdentifier == DropZones.Docked && !_dockOpen)
        {
            OpenDock();
        }
        
        NotifyStateChanged();
    }

    public void HideTab(string tabName)
    {
        var existingTab = _tabs.FirstOrDefault(t => t.Name.Equals(tabName, StringComparison.OrdinalIgnoreCase));
        
        if(existingTab == null) return;
        
        var previousZone = existingTab.AreaIdentifier;
        
        existingTab.IsActive = false;
        existingTab.AreaIdentifier = DropZones.None;
        
        var remaining = _tabs.FirstOrDefault(t => t.AreaIdentifier == previousZone);

        if (remaining != null)
        {
            remaining.IsActive = true;
        }
        
        NotifyStateChanged();
    }

    public void ToggleTabVisibility(DynamicTabView tab)
    {
        var existingTab = _tabs.FirstOrDefault(t => t.TabId == tab.TabId);
        if (existingTab != null)
        {
            if (existingTab.AreaIdentifier != DropZones.None)
            {
                existingTab.AreaIdentifier = DropZones.None;
                existingTab.IsActive = false;
            }
            else
            {
                existingTab.AreaIdentifier = tab.DefaultArea;
                existingTab.IsActive = true;
            }
            NotifyStateChanged();
        }
    }

    private List<DynamicTabView> InitializeDefaultLayout()
    {
        List<DynamicTabView> defaultTabs = 
        [
            new ConsoleOutputTab()
            {
                AreaIdentifier = DropZones.Bottom,
                IsActive = true
            },
            new SolutionExplorerTab()
            {
                AreaIdentifier = DropZones.Left,
                IsActive = true
            },
            new LibraryTab()
            {
                AreaIdentifier = DropZones.Left,
                IsActive = false,
            },
            new CodeAnalysisOutputTab()
            {
                AreaIdentifier = DropZones.Right,
                IsActive = true
            },

            new TestingTab()
            {
                AreaIdentifier = DropZones.Right,
                IsActive = false,
            },
            new TestConsoleOutputTab()
            {
                AreaIdentifier = DropZones.Bottom,
            },

            new WebHostOutputTab()
            {
                AreaIdentifier = DropZones.Bottom
            },
            new WebApiRouteTab()
            {
                AreaIdentifier = DropZones.Right
            },
            new TerminalTab()
            {
                AreaIdentifier = DropZones.Docked
            }
        ];
        
        if(_environment.IsDevelopment())
            defaultTabs.Add(new SystemLogViewer());

        return defaultTabs;
    }

    private void NotifyStateChanged() => TabViewStateChanged?.Invoke();
}