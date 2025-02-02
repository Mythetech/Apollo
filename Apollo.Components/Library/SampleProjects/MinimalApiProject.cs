using Apollo.Components.Solutions;
using Apollo.Contracts.Solutions;

namespace Apollo.Components.Library.SampleProjects;

public static class MinimalApiProject
{
    public static SolutionModel Create()
    {
        return new SolutionModel
        {
            Name = "MinimalApi",
            Description = "Example Minimal API web app",
            ProjectType = ProjectType.WebApi, 
            Items = new List<ISolutionItem>
            {
                new Folder
                {
                    Name = "MinimalApi",
                    Uri = "virtual/MinimalApi"
                },
                new SolutionFile
                {
                    Name = "Program.cs",
                    Uri = "virtual/MinimalApi/Program.cs",
                    Data = @"
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet(""/"", () => ""Hello World!"");

app.Run();
",
                    CreatedAt = DateTimeOffset.Now,
                    ModifiedAt = DateTimeOffset.Now,
                }
            }
        };
    }
}