using System.Text.Json;
using Apollo.Components.Analysis;
using Apollo.Components.Console;
using Apollo.Components.Hosting;
using Apollo.Components.NuGet;
using Apollo.Components.Solutions;
using Apollo.Components.Solutions.Services;
using Apollo.Contracts.Analysis;
using Apollo.Contracts.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Apollo.Test.Components.Analysis;

public class CodeAnalysisStateTests
{
    private const string SolutionName = "Demo";
    private const string ProgramUri = "virtual/Demo/Program.cs";
    private const string Code = "class Program { }";
    private const string CompletionRequest = "{}";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ICodeAnalysisWorker _worker = Substitute.For<ICodeAnalysisWorker>();
    private readonly List<string> _syncPayloads = [];
    private readonly SolutionsState _solutionsState;
    private readonly CodeAnalysisState _state;
    private Func<LogMessage, Task>? _logCallback;

    public CodeAnalysisStateTests()
    {
        _solutionsState = new SolutionsState(null!, new TestMessageBus(), Substitute.For<ISolutionSaveService>(), Substitute.For<IHostingService>());
        _solutionsState.Solutions.Add(new SolutionModel
        {
            Name = SolutionName,
            Items =
            [
                new Folder { Name = SolutionName, Uri = "virtual/Demo", Items = [] },
                new SolutionFile { Name = "Program.cs", Uri = ProgramUri, Data = Code }
            ]
        });
        _solutionsState.SwitchSolution(SolutionName);

        _worker.When(w => w.OnLog(Arg.Any<Func<LogMessage, Task>>()))
            .Do(call => _logCallback = call.Arg<Func<LogMessage, Task>>());
        _worker.UpdateSolutionAsync(Arg.Do<string>(payload => _syncPayloads.Add(payload))).Returns(Array.Empty<byte>());
        _worker.GetCompletionAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Array.Empty<byte>());
        _worker.GetDiagnosticsAsync(Arg.Any<string>()).Returns(Array.Empty<byte>());

        var factory = Substitute.For<ICodeAnalysisWorkerFactory>();
        factory.CreateAsync().Returns(_worker);

        var storage = Substitute.For<INuGetStorageService>();
        _state = new CodeAnalysisState(
            factory,
            new CodeAnalysisConsoleService(Substitute.For<IJsApiService>()),
            new TestMessageBus(),
            _solutionsState,
            new UserAssemblyStore(),
            new NuGetState(Substitute.For<INuGetService>(), storage, NullLogger<NuGetState>.Instance),
            storage);
    }

    [Fact(DisplayName = "The first completion syncs the solution before requesting completions")]
    public async Task FirstCompletion_SyncsSolutionFirst()
    {
        await StartWorkerAsync();

        await _state.GetCompletionAsync(Code, CompletionRequest);

        Received.InOrder(() =>
        {
            _worker.UpdateSolutionAsync(Arg.Any<string>());
            _worker.GetCompletionAsync(Code, CompletionRequest);
        });
    }

    [Fact(DisplayName = "A completion with no solution changes does not sync again")]
    public async Task SecondCompletion_WithoutChanges_DoesNotSyncAgain()
    {
        await StartWorkerAsync();

        await _state.GetCompletionAsync(Code, CompletionRequest);
        await _state.GetCompletionAsync(Code, CompletionRequest);

        await _worker.Received(1).UpdateSolutionAsync(Arg.Any<string>());
    }

    [Fact(DisplayName = "Adding a file makes the next completion sync the new file")]
    public async Task AddFile_SyncsOnNextCompletion()
    {
        await StartWorkerAsync();
        await _state.GetCompletionAsync(Code, CompletionRequest);

        _solutionsState.AddFile("NewFile.cs", "public class NewFile { }");
        await _state.GetCompletionAsync(Code, CompletionRequest);

        _syncPayloads.Count.ShouldBe(2);
        LastSyncRequest().Solution.Items.ShouldContain(i => i.Path.EndsWith("NewFile.cs"));
    }

    [Fact(DisplayName = "A diagnostics pass does not cancel a pending solution sync")]
    public async Task Diagnostics_DoNotCancelPendingSync()
    {
        await StartWorkerAsync();
        await _state.GetCompletionAsync(Code, CompletionRequest);

        _solutionsState.AddFile("NewFile.cs", "public class NewFile { }");
        await _state.GetDiagnosticsAsync(_solutionsState.Project.ToContract(), ProgramUri);
        await _state.GetCompletionAsync(Code, CompletionRequest);

        _syncPayloads.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "The sync names the active file when it is C#")]
    public async Task Sync_NamesActiveCSharpFile()
    {
        await StartWorkerAsync();

        await _state.GetCompletionAsync(Code, CompletionRequest);

        LastSyncRequest().ActiveDocumentPath.ShouldBe(ProgramUri);
    }

    [Fact(DisplayName = "The sync names no active file when the active file is not C#")]
    public async Task Sync_NamesNoActiveFile_WhenActiveFileIsRazor()
    {
        _solutionsState.AddFile("Counter.razor", "<h1>Counter</h1>");
        _solutionsState.ActiveFile = _solutionsState.Project.Files.First(f => f.Name == "Counter.razor");
        await StartWorkerAsync();

        await _state.GetCompletionAsync(Code, CompletionRequest);

        LastSyncRequest().ActiveDocumentPath.ShouldBeNull();
    }

    private async Task StartWorkerAsync()
    {
        var start = _state.StartAsync();
        _logCallback.ShouldNotBeNull();
        await _logCallback(new LogMessage("Analysis worker ready", LogSeverity.Debug, DateTimeOffset.UtcNow));
        await start;
    }

    private SolutionSyncRequest LastSyncRequest()
    {
        _syncPayloads.ShouldNotBeEmpty();
        var request = JsonSerializer.Deserialize<SolutionSyncRequest>(_syncPayloads[^1], JsonOptions);
        request.ShouldNotBeNull();
        return request;
    }
}
