using System.Reflection;
using System.Text;
using System.Text.Json;
using Apollo.Analysis;
using Apollo.Contracts.Analysis;
using Apollo.Contracts.Solutions;
using Apollo.Infrastructure.Workers;
using Apollo.Test.Workers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using OmniSharp.Models.v1.Completion;
using Shouldly;
using Xunit;
using CompletionItem = OmniSharp.Models.v1.Completion.CompletionItem;
using Diagnostic = Apollo.Contracts.Analysis.Diagnostic;
using Solution = Apollo.Contracts.Solutions.Solution;

namespace Apollo.Test.Analysis;

public class UserTypeCompletionTests
{
    private const string ProgramPath = "virtual/Demo/Program.cs";
    private const string FizzBuzzPath = "virtual/Demo/FizzBuzz.cs";
    private const string RazorPath = "virtual/Demo/Counter.razor";
    private const string JsonPath = "virtual/Demo/appsettings.json";

    private const string FizzBuzzCs = "namespace Demo;\npublic class FizzBuzz\n{\n    public string GetValue(int i) => i.ToString();\n    public int Count { get; set; }\n}\npublic struct Point { public int X; }\npublic record Person(string Name);\n";

    private const string ProgramMember = "using Demo;\nclass Program\n{\n    static void Main()\n    {\n        var fizzBuzz = new FizzBuzz();\n        fizzBuzz.\n    }\n}\n";

    private const string ProgramType = "using Demo;\nclass Program\n{\n    static void Main()\n    {\n        var x = new Fiz\n    }\n}\n";

    private const string ValidProgram ="using Demo;\nclass Program\n{\n    static void Main()\n    {\n        var fizzBuzz = new FizzBuzz();\n        System.Console.WriteLine(fizzBuzz.GetValue(1));\n    }\n}\n";

    private const string PackageProgram = "using Pkg;\nclass Program\n{\n    static void Main()\n    {\n        var x = new FromP\n    }\n}\n";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly TestMetadataReferenceResolver _resolver = new();
    private readonly ILoggerProxy _logger = new TestLoggerProxy();

    [Fact(DisplayName = "Solution sync exposes members of types defined in other files")]
    public async Task SolutionSync_ExposesMembersFromOtherFiles()
    {
        var service = await CreateServiceAsync();
        await SyncAsync(service, Demo(ProgramMember), ProgramPath);

        var items = await GetCompletionsAsync(service, ProgramPath, ProgramMember, line: 6, column: 17, triggerCharacter: '.');

        items.ShouldContain(i => i.Label == "GetValue" && i.Kind == CompletionItemKind.Method);
        items.ShouldContain(i => i.Label == "Count" && i.Kind == CompletionItemKind.Property);
    }

    [Fact(DisplayName = "Solution sync exposes type names defined in other files")]
    public async Task SolutionSync_ExposesTypeNamesFromOtherFiles()
    {
        var service = await CreateServiceAsync();
        await SyncAsync(service, Demo(ProgramType), ProgramPath);

        var items = await GetCompletionsAsync(service, ProgramPath, ProgramType, line: 5, column: 23);

        items.ShouldContain(i => i.Label == "FizzBuzz" && i.Kind == CompletionItemKind.Class);
        items.ShouldContain(i => i.Label == "Point" && i.Kind == CompletionItemKind.Struct);
        items.ShouldContain(i => i.Label == "Person");
    }

    [Fact(DisplayName = "Solution sync keeps the active document's live text")]
    public async Task SolutionSync_KeepsActiveDocumentText()
    {
        var projectService = await CreateProjectServiceAsync();
        const string liveText = "class Program { void Typed() { } }";
        const string staleText = "class Program { }";
        projectService.UpdateDocument(ProgramPath, liveText);

        projectService.UpdateSolution(Demo(staleText), ProgramPath);
        (await projectService.GetDocument(ProgramPath)!.GetTextAsync()).ToString().ShouldBe(liveText);

        projectService.UpdateSolution(Demo(staleText), activeDocumentPath: null);
        (await projectService.GetDocument(ProgramPath)!.GetTextAsync()).ToString().ShouldBe(staleText);
    }

    [Fact(DisplayName = "Updating a document with unchanged text keeps its version")]
    public async Task UpdateDocument_UnchangedText_KeepsVersion()
    {
        var projectService = await CreateProjectServiceAsync();
        projectService.UpdateDocument(FizzBuzzPath, FizzBuzzCs);
        var before = await projectService.GetDocument(FizzBuzzPath)!.GetTextVersionAsync();

        projectService.UpdateDocument(FizzBuzzPath, FizzBuzzCs);

        var after = await projectService.GetDocument(FizzBuzzPath)!.GetTextVersionAsync();
        after.ShouldBe(before);
    }

    [Fact(DisplayName = "Solution sync removes documents that are no longer in the solution")]
    public async Task SolutionSync_RemovesDeletedDocuments()
    {
        var service = await CreateServiceAsync();
        await SyncAsync(service, Demo(ProgramType), ProgramPath);

        await SyncAsync(service, new Solution
        {
            Name = "Demo",
            Items = [new SolutionItem { Path = ProgramPath, Content = ProgramType }]
        }, ProgramPath);

        var items = await GetCompletionsAsync(service, ProgramPath, ProgramType, line: 5, column: 23);
        items.ShouldNotContain(i => i.Label == "FizzBuzz");
    }

    [Fact(DisplayName = "Solution sync does not add non-C# items to the workspace")]
    public async Task SolutionSync_SkipsNonCSharpItems()
    {
        var projectService = await CreateProjectServiceAsync();

        projectService.UpdateSolution(WithNonCSharpItems(), ProgramPath);

        projectService.GetDocument(RazorPath).ShouldBeNull();
        projectService.GetDocument(JsonPath).ShouldBeNull();
    }

    [Fact(DisplayName = "Types parsed out of Razor markup are not offered as completions")]
    public async Task SolutionSync_DoesNotOfferTypesFromRazorMarkup()
    {
        var service = await CreateServiceAsync();
        await SyncAsync(service, WithNonCSharpItems(), ProgramPath);

        var items = await GetCompletionsAsync(service, ProgramPath, ProgramType, line: 5, column: 23);

        items.ShouldContain(i => i.Label == "FizzBuzz");
        items.ShouldNotContain(i => i.Label == "Inner");
    }

    [Fact(DisplayName = "Extension methods in source are not ambiguous with the last build's user assembly")]
    public async Task UserAssembly_DoesNotMakeExtensionMethodsAmbiguous()
    {
        const string extensions = "namespace Demo;\npublic static class Ext { public static int Twice(this int x) => x * 2; }\n";
        const string program = "using Demo;\nclass Program\n{\n    static void Main()\n    {\n        System.Console.WriteLine(2.Twice());\n    }\n}\n";
        var service = await CreateServiceAsync();
        service.SetUserAssemblyReference(CompileAssembly("UserAssembly", extensions));

        var diagnostics = await GetDiagnosticsAsync(service, new Solution
        {
            Name = "Demo",
            Items =
            [
                new SolutionItem { Path = ProgramPath, Content = program },
                new SolutionItem { Path = "virtual/Demo/Ext.cs", Content = extensions }
            ]
        });

        diagnostics.ShouldNotContain(d => d.Message.Contains("The call is ambiguous"));
    }

    [Fact(DisplayName = "Solutions with Razor files still see component types from the last build")]
    public async Task UserAssembly_ProvidesComponentTypesToRazorSolutions()
    {
        const string program = "using Demo;\nclass Program\n{\n    static void Main()\n    {\n        var counter = new Counter();\n    }\n}\n";
        var service = await CreateServiceAsync();
        service.SetUserAssemblyReference(CompileAssembly("UserAssembly", "namespace Demo; public class Counter {}"));

        var diagnostics = await GetDiagnosticsAsync(service, new Solution
        {
            Name = "Demo",
            Items =
            [
                new SolutionItem { Path = ProgramPath, Content = program },
                new SolutionItem { Path = RazorPath, Content = "<h1>Counter</h1>" }
            ]
        });

        diagnostics.ShouldNotContain(d => d.Severity == 8);
    }

    [Fact(DisplayName = "Completion marks user constants with the constant kind")]
    public async Task Completion_MarksConstants()
    {
        const string code = "class Limits { public const int Max = 10; }\nclass Program\n{\n    static void Main()\n    {\n        var max = Limits.\n    }\n}\n";
        var service = await CreateServiceAsync();

        var items = await GetCompletionsAsync(service, ProgramPath, code, line: 5, column: 25, triggerCharacter: '.');

        items.ShouldContain(i => i.Label == "Max" && i.Kind == CompletionItemKind.Constant);
    }

    [Fact(DisplayName = "Completion marks type parameters with the type parameter kind")]
    public async Task Completion_MarksTypeParameters()
    {
        const string code = "class Program\n{\n    static void Use<TItem>()\n    {\n        TIt\n    }\n}\n";
        var service = await CreateServiceAsync();

        var items = await GetCompletionsAsync(service, ProgramPath, code, line: 4, column: 11);

        items.ShouldContain(i => i.Label == "TItem" && i.Kind == CompletionItemKind.TypeParameter);
    }

    [Fact(DisplayName = "NuGet references stay in completions after the user assembly updates")]
    public async Task NuGetReferences_SurviveUserAssemblyUpdate()
    {
        var service = await CreateServiceAsync();
        var solution = new Solution
        {
            Name = "Demo",
            Items = [new SolutionItem { Path = ProgramPath, Content = PackageProgram }],
            NuGetReferences =
            [
                new NuGetReference
                {
                    PackageId = "Pkg",
                    AssemblyName = "Pkg.dll",
                    AssemblyData = CompileAssembly("Pkg", "namespace Pkg; public class FromPackage {}")
                }
            ]
        };
        await service.GetDiagnosticsAsync(ProgramPath, solution);

        service.SetUserAssemblyReference(CompileAssembly("UserAssembly", "namespace Demo; public class FizzBuzz {}"));

        var items = await GetCompletionsAsync(service, ProgramPath, PackageProgram, line: 5, column: 25);
        items.ShouldContain(i => i.Label == "FromPackage");
    }

    [Fact(DisplayName = "Source types do not conflict with the same types in the last build's user assembly")]
    public async Task UserAssembly_DoesNotReportTypeConflicts()
    {
        var service = await CreateServiceAsync();
        service.SetUserAssemblyReference(CompileAssembly("UserAssembly", FizzBuzzCs));
        var solution = new Solution
        {
            Name = "Demo",
            Items =
            [
                new SolutionItem { Path = ProgramPath, Content = ValidProgram },
                new SolutionItem { Path = FizzBuzzPath, Content = FizzBuzzCs },
                new SolutionItem { Path = RazorPath, Content = "<h1>Counter</h1>" }
            ]
        };

        var diagnostics = await GetDiagnosticsAsync(service, solution);

        diagnostics.ShouldNotContain(d => d.Message.Contains("conflicts with the imported type"));
    }

    private async Task<MonacoService> CreateServiceAsync()
    {
        var service = new MonacoService(_resolver, _logger);
        await service.Init("test");
        return service;
    }

    private async Task<RoslynProjectService> CreateProjectServiceAsync()
    {
        var projectService = new RoslynProjectService(_resolver, _logger);
        await projectService.InitializeAsync();
        return projectService;
    }

    private static Solution Demo(string programText) => new()
    {
        Name = "Demo",
        Items =
        [
            new SolutionItem { Path = ProgramPath, Content = programText },
            new SolutionItem { Path = FizzBuzzPath, Content = FizzBuzzCs }
        ]
    };

    private static Solution WithNonCSharpItems()
    {
        var solution = Demo(ProgramType);
        solution.Items.Add(new SolutionItem { Path = RazorPath, Content = "<h1>@count</h1>\n@code { int count; public class Inner {} }" });
        solution.Items.Add(new SolutionItem { Path = JsonPath, Content = "{ \"Logging\": { } }" });
        return solution;
    }

    private static async Task SyncAsync(MonacoService service, Solution solution, string? activeDocumentPath)
    {
        var request = new SolutionSyncRequest { Solution = solution, ActiveDocumentPath = activeDocumentPath };
        var bytes = await service.HandleSolutionSyncAsync(JsonSerializer.Serialize(request, JsonOptions));
        Encoding.UTF8.GetString(bytes).ShouldContain("\"success\":true");
    }

    private static async Task<List<CompletionItem>> GetCompletionsAsync(
        MonacoService service, string path, string code, int line, int column, char? triggerCharacter = null)
    {
        var request = new CompletionRequest
        {
            FileName = path,
            Line = line,
            Column = column,
            CompletionTrigger = triggerCharacter == null ? CompletionTriggerKind.Invoked : CompletionTriggerKind.TriggerCharacter,
            TriggerCharacter = triggerCharacter
        };

        var bytes = await service.GetCompletionAsync(code, JsonSerializer.Serialize(request));
        if (bytes.Length == 0)
            return [];

        var response = JsonSerializer.Deserialize<ResponsePayload>(Encoding.UTF8.GetString(bytes), JsonOptions);
        var wrapper = JsonSerializer.Deserialize<CompletionResponseWrapper>(response!.Payload!.ToString()!, JsonOptions);
        return wrapper?.Payload?.Items?.ToList() ?? [];
    }

    private static async Task<List<Diagnostic>> GetDiagnosticsAsync(MonacoService service, Solution solution)
    {
        var bytes = await service.GetDiagnosticsAsync(ProgramPath, solution);
        var response = JsonSerializer.Deserialize<ResponsePayload>(bytes, JsonOptions);
        var diagnostics = JsonSerializer.Deserialize<List<Diagnostic>>(response!.Payload!.ToString()!, JsonOptions);
        diagnostics.ShouldNotBeNull();
        return diagnostics;
    }

    private static byte[] CompileAssembly(string assemblyName, string source)
    {
        var references = new[]
        {
            typeof(object).Assembly.Location,
            typeof(Console).Assembly.Location,
            typeof(Enumerable).Assembly.Location,
            Assembly.Load("System.Runtime").Location
        }.Select(location => MetadataReference.CreateFromFile(location));

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.ShouldBeTrue(string.Join("\n", result.Diagnostics));
        return stream.ToArray();
    }
}
