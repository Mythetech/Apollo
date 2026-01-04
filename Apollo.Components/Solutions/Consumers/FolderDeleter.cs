using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;

namespace Apollo.Components.Solutions.Consumers;

public class FolderDeleter : IConsumer<DeleteFolder>
{
    private readonly SolutionsState _state;

    public FolderDeleter(SolutionsState state)
    {
        _state = state;
    }

    public async Task Consume(DeleteFolder message)
    {
        _state.DeleteFolder(message.Folder);
    }
} 