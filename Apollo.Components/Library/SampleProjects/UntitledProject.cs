using Apollo.Components.Code;
using Apollo.Components.Infrastructure;
using Apollo.Components.Solutions;
using Apollo.Contracts.Solutions; // Adjust namespaces as needed

namespace Apollo.Components.Library.SampleProjects;

public static class UntitledProject
{
    public static SolutionModel Create()
    {
        var project = new SolutionModel
        {
            Name = "Untitled Project",
            Description = "Simple console app",
            ProjectType = ProjectType.Console
        };

        // Populate folder and files
        project.Items = new List<ISolutionItem>
        {
            new Folder
            {
                Name = project.Name,
                Uri = project.Path
            },
            new SolutionFile
            {
                Name = "Program.cs",
                Uri = $"{project.Path}/Program.cs",
                Data = @"using System;
var instance = new DemoClass();
Console.WriteLine(instance.Name);",
                CreatedAt = DateTimeOffset.Now,
                ModifiedAt = DateTimeOffset.Now,
            },
            new SolutionFile
            {
                Name = "DemoClass.cs",
                Uri = $"{project.Path}/DemoClass.cs",
                Data = @"
public class DemoClass
{
    public string Name { get; set; } = ""Apollo"";
}
"
            }
        };

        return project;
    }
}