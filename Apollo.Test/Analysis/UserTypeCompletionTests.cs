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

    private const string FizzBuzzCs = "namespace Demo;\npublic class FizzBuzz\n{\n    public string GetValue(int i) => i.ToString();\n    public int Count { get; set; }\n}\npublic struct Point { public int X; }\npublic record Person(string Name);\n";

    private const string ValidProgram = "using Demo;\nclass Program\n{\n    static void Main()\n    {\n        var fizzBuzz = new FizzBuzz();\n        System.Console.WriteLine(fizzBuzz.GetValue(1));\n    }\n}\n";

    private const string PackageProgram = "using Pkg;\nclass Program\n{\n    static void Main()\n    {\n        var x = new FromP\n    }\n}\n";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly TestMetadataReferenceResolver _resolver = new();
    private readonly ILoggerProxy _logger = new TestLoggerProxy();

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
                new SolutionItem { Path = FizzBuzzPath, Content = FizzBuzzCs }
            ]
        };

        var bytes = await service.GetDiagnosticsAsync(ProgramPath, solution);

        var response = JsonSerializer.Deserialize<ResponsePayload>(bytes, JsonOptions);
        var diagnostics = JsonSerializer.Deserialize<List<Diagnostic>>(response!.Payload!.ToString()!, JsonOptions);
        diagnostics.ShouldNotBeNull();
        diagnostics.ShouldNotContain(d => d.Message.Contains("conflicts with the imported type"));
    }

    private async Task<MonacoService> CreateServiceAsync()
    {
        var service = new MonacoService(_resolver, _logger);
        await service.Init("test");
        return service;
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
