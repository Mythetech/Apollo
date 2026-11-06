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
using Solution = Apollo.Contracts.Solutions.Solution;

namespace Apollo.Test.Analysis;

public class UserTypeCompletionTests
{
    private const string ProgramPath = "virtual/Demo/Program.cs";

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
