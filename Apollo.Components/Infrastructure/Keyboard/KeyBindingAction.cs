using System.ComponentModel;

namespace Apollo.Components.Infrastructure.Keyboard;

public enum KeyBindingAction
{
    [Description("No action")]
    None,
    
    [Description("Build solution")]
    BuildSolution,
    
    [Description("Run solution")]
    RunSolution,
    
    [Description("Save active solution")]
    SaveActiveSolution,
    
    [Description("Save solution as")]
    PromptSaveSolutionAs,
    
    [Description("Open settings")]
    OpenSettingsDialog,
    
    [Description("Open about dialog")]
    OpenAboutDialog,
    
    [Description("Format active document")]
    FormatActiveDocument,
    
    [Description("Format all documents")]
    FormatAllDocuments,
    
    [Description("Create new file")]
    PromptCreateNewFile,
    
    [Description("Create new solution")]
    PromptCreateNewSolution,
    
    [Description("Open file")]
    PromptOpenFile,
    
    [Description("Open folder")]
    PromptOpenFolder,
    
    [Description("Open GitHub repository")]
    PromptOpenGitHubRepo,
    
    [Description("Close solution")]
    CloseSolution,
    
    [Description("Copy active file to clipboard")]
    CopyActiveFileToClipboard,
    
    [Description("Export as Base64")]
    ExportBase64String,
    
    [Description("Export as JSON")]
    ExportJsonFile,
    
    [Description("Export as ZIP")]
    ExportZipFile,
    
    [Description("Share solution")]
    ShareSolution,
    
    [Description("Focus terminal")]
    FocusTerminal,
    
    [Description("Focus console")]
    FocusConsole,
    
    [Description("Focus editor")]
    FocusEditor,
    
    [Description("Focus solution explorer")]
    FocusSolutionExplorer,
    
    [Description("Close dock")]
    CloseDock,
    
    [Description("Open dock")]
    OpenDock,
    
    [Description("Close all floating windows")]
    CloseAllFloatingWindows,
    
    [Description("Start web host")]
    StartRunning,
    
    [Description("Stop web host")]
    Shutdown,
}

