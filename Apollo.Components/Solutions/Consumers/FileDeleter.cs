using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;

namespace Apollo.Components.Solutions.Consumers;

public class FileDeleter : IConsumer<DeleteFile>
{
    private readonly SolutionsState _state;

    public FileDeleter(SolutionsState state)
    {
        _state = state;
    }

    public async Task Consume(DeleteFile message)
    {
        _state.DeleteFile(message.File);
    }
} 