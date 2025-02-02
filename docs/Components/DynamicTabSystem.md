# Dynamic Tab System

The Dynamic Tab System provides a flexible way to manage and display different UI components across multiple zones in the application. It leverages Blazor's `DynamicComponent` feature to render arbitrary components with their dependencies while maintaining a consistent tab management interface.

## Overview

The system allows components to be displayed in different zones:
- Left Panel
- Right Panel
- Bottom Panel
- Floating Windows
- Docked Panel

Each tab is a self-contained component that can be moved between these zones through drag-and-drop or menu selection.

## Architecture

### Core Components

- **TabDropContainer**: The main container that manages all tab zones
- **TabDropZone**: Individual zones that can host tabs
- **DynamicTabView**: Base class for all tab implementations
- **TabViewState**: Manages the state and location of all tabs

### Key Features

- Drag and drop between zones
- Floating windows with resize and drag capabilities
- Dockable panels
- State persistence
- Dynamic component loading with dependency injection

## Implementation

### Creating a New Tab

To implement a new tab, inherit from `DynamicTabView`:
```csharp
public class MyCustomTab : DynamicTabView
{
public override string Name => "My Custom Tab";
public override Type ComponentType => typeof(MyCustomTab);
// Optional: Additional properties and state
}
```

The system will automatically handle:
- Component instantiation
- Property injection
- State management
- Zone placement
- Window management

### Zone Management

Tabs can be placed in predefined zones using the `DropZones` constants:
- `DropZones.Left`
- `DropZones.Right`
- `DropZones.Bottom`
- `DropZones.Floating`
- `DropZones.Docked`

### State Management

The `TabViewState` manages:
- Tab locations
- Active/inactive states
- Zone configurations
- Window positions (for floating tabs)

## Benefits

1. **Flexibility**: Components can be rendered anywhere in the UI while maintaining their state and dependencies
2. **Consistency**: Common tab behaviors are handled by the base implementation
3. **Simplicity**: New tabs only need to implement minimal required properties
4. **Maintainability**: Clear separation between tab content and tab management
5. **Extensibility**: Easy to add new zones or tab features

## Example Usage
```csharp
csharp
// Define a new tab
public class CodeAnalysisTab : DynamicTabView
{
[Inject] public IAnalysisService AnalysisService { get; set; } = default!;
public override string Name => "Code Analysis";
public override Type ComponentType => typeof(CodeAnalysisTab);
}
// Register in TabViewStatetabs.Add(new CodeAnalysisTab
{
AreaIdentifier = DropZones.Right,
IsActive = true
});
```
The system handles all the complexity of rendering, state management, and zone placement while allowing the tab implementation to focus on its specific functionality.