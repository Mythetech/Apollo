using System.IO.Compression;
using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Shared;
using Apollo.Components.Solutions.Commands;
using Apollo.Contracts.Solutions;
using Microsoft.JSInterop;

namespace Apollo.Components.Solutions.Consumers;

public class VisualStudioExporter : IConsumer<ExportVisualStudioSolution>
{
    private readonly SolutionsState _solutionsState;
    private readonly IJSRuntime _jsRuntime;

    public VisualStudioExporter(SolutionsState solutionsState, IJSRuntime jsRuntime)
    {
        _solutionsState = solutionsState;
        _jsRuntime = jsRuntime;
    }

    public async Task Consume(ExportVisualStudioSolution message)
    {
        var solution = _solutionsState.Project;
        
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Add .gitignore
            var gitignoreEntry = archive.CreateEntry($"{solution.Name}/.gitignore");
            using (var writer = new StreamWriter(gitignoreEntry.Open()))
            {
                await writer.WriteAsync(@"bin/
obj/
+.idea/
*.user
*.DotSettings");
            }

            // Add solution file
            var slnEntry = archive.CreateEntry($"{solution.Name}.sln");
            using (var writer = new StreamWriter(slnEntry.Open()))
            {
                await writer.WriteAsync(GenerateSolutionFile(solution));
            }

            // Add project file
            var projEntry = archive.CreateEntry($"{solution.Name}/{solution.Name}.csproj");
            using (var writer = new StreamWriter(projEntry.Open()))
            {
                await writer.WriteAsync(GenerateProjectFile(solution));
            }

            // Add source files maintaining folder structure
            foreach (var file in solution.Files)
            {
                var relativePath = file.Uri.Replace("virtual/", "").Replace($"{solution.Name}/", "");
                var entry = archive.CreateEntry($"{solution.Name}/{relativePath}");
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream);
                await writer.WriteAsync(file.Data);
            }
        }

        var bytes = memoryStream.ToArray();
        var suffix = message.IsRider ? "rider" : "vs";
        await _jsRuntime.SaveAs($"{solution.Name}.{suffix}.zip", bytes);
    }

    private string GenerateSolutionFile(SolutionModel solution)
    {
        var projectGuid = Guid.NewGuid().ToString("B").ToUpper();
        return $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{solution.Name}"", ""{solution.Name}\{solution.Name}.csproj"", ""{{{projectGuid}}}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{{{projectGuid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{{projectGuid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{{projectGuid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{{projectGuid}}}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal";
    }

    private string GenerateProjectFile(SolutionModel solution)
    {
        var targetFramework = "net8.0";
        var outputType = "Exe";  // Both console and web are executables
        var sdk = solution.ProjectType == ProjectType.WebApi ? "Microsoft.NET.Sdk.Web" : "Microsoft.NET.Sdk";

        var aspNetImports = solution.ProjectType == ProjectType.WebApi ? 
            @"
  <ItemGroup>
    <Using Include=""System.Net.Http""/>
    <Using Include=""Microsoft.AspNetCore.Builder""/>
    <Using Include=""Microsoft.AspNetCore.Hosting""/>
    <Using Include=""Microsoft.AspNetCore.Http""/>
    <Using Include=""Microsoft.Extensions.Hosting""/>
  </ItemGroup>" : "";

        return $@"<Project Sdk=""{sdk}"">
  <PropertyGroup>
    <OutputType>{outputType}</OutputType>
    <TargetFramework>{targetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>{aspNetImports}
</Project>";
    }
} 