using Solution = Apollo.Contracts.Solutions.Solution;
using SolutionItem = Apollo.Contracts.Solutions.SolutionItem;

namespace Apollo.Components.Solutions;

public static class SolutionMapper
{
    public static Solution ToContract(this SolutionModel solutionModel)
    {
        return new Solution
        {
            Name = solutionModel.Name,
            Items = solutionModel.Files.Select(file => new SolutionItem
            {
                Path = file.Uri,
                Content = file.Data
            }).ToList(), 
            Type = solutionModel.ProjectType,
        };
    }
}