using Apollo.Components.Library.SampleProjects;
using Apollo.Components.Library.Snippets;
using Apollo.Components.Solutions;

namespace Apollo.Components.Library;

public class LibraryState
{
    public List<SolutionFile> Snippets { get; set; } =
    [
        new SolutionFile()
        {
            Name = "FruitEmojis.cs",
            Data = Emojis.FruitArray,
        }
    ];

    public List<SolutionModel> Projects { get; set; } =
    [
        FizzBuzzProject.Create(),
        MinimalApiProject.Create(),
        UntitledProject.Create(),
        SimpleLibraryProject.Create()
    ];

}