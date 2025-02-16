using Apollo.Components.Solutions;
using Apollo.Contracts.Solutions;

namespace Apollo.Components.Library.SampleProjects;

public static class SimpleLibraryProject
{
    public static SolutionModel Create()
    {
        var project = new SolutionModel
        {
            Name = "Simple Library",
            Description = "A basic class library project",
            ProjectType = ProjectType.ClassLibrary
        };

        project.Items = 
        [
            new Folder
            {
                Name = project.Name,
                Uri = project.Path
            },
            new SolutionFile
            {
                Name = "Calculator.cs",
                Uri = $"{project.Path}/Calculator.cs",
                Data = @"
namespace SimpleLibrary;

public class Calculator
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
    public int Multiply(int a, int b) => a * b;
    public double Divide(double a, double b) => a / b;
}",
                CreatedAt = DateTimeOffset.Now,
                ModifiedAt = DateTimeOffset.Now
            }
        ];

        return project;
    }
} 