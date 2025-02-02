using Apollo.Components.Analysis;
using Apollo.Components.Code;
using Apollo.Components.Hosting;
using Apollo.Components.Solutions;

namespace Apollo.Components.Editor;

public class EditorState
{
    private readonly SolutionsState _solutionsState;
    private readonly CodeAnalysisState _codeAnalysisState;
    private readonly CompilerState _compilerState;
    private readonly IHostingService _hostingService;

    public EditorState(SolutionsState solutionsState, CodeAnalysisState codeAnalysisState, CompilerState compilerState,
        IHostingService hostingService)
    {
        _solutionsState = solutionsState;
        _codeAnalysisState = codeAnalysisState;
        _compilerState = compilerState;
        _hostingService = hostingService;
    }
    
    
}