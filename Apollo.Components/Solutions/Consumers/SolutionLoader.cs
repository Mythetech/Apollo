using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;

namespace Apollo.Components.Solutions.Consumers;

public class SolutionLoader : IConsumer<LoadSolution>
{
    private readonly SolutionsState _state;

    public SolutionLoader(SolutionsState state)
    {
        _state = state;
    }

    public async Task Consume(LoadSolution message)
    {
        string uniqueName = ResolveDuplicateName(message.Solution.Name);
        
        var solution = new SolutionModel(uniqueName)
        {
            ProjectType = message.Solution.ProjectType,
            Description = message.Solution.Description,
            Items = message.Solution.Items.ToList()
        };
        
        await _state.LoadSolutionAsync(solution);
    }

    private string ResolveDuplicateName(string name, int version = 0)
    {
        string nameToCheck = version == 0 ? name : UpdateName(name, version);
        
        if (_state.Solutions.Any(x => x.Name.Equals(nameToCheck, StringComparison.OrdinalIgnoreCase)))
        {
            return ResolveDuplicateName(name, ++version);
        }
        return nameToCheck;
    }

    private string UpdateName(string name, int version)
    {
        return $"{name} ({version})";
    }
}