using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;

namespace Apollo.Components.Solutions.Consumers;

public class RenameFile : IConsumer<RenameSolutionFile>
{
    private readonly SolutionsState _state;

    public RenameFile(SolutionsState state)
    {
        _state = state;
    }
    
    public async Task Consume(RenameSolutionFile message)
    {
        await _state.RenameFile(message.File, message.Name);
    }
}