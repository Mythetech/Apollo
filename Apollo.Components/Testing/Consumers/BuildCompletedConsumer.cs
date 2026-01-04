using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Events;

namespace Apollo.Components.Testing.Consumers;

public class BuildCompletedConsumer : IConsumer<BuildCompleted>
{
    private readonly TestingState _testingState;

    public BuildCompletedConsumer(TestingState testingState)
    {
        _testingState = testingState;
    }

    public async Task Consume(BuildCompleted message)
    {
        if (message?.Result.CompiledAssembly == null)
            return;
        
        if(message.Result.Success)
            _testingState.CacheBuild(message.Result.CompiledAssembly!);

        await _testingState.DiscoverTestsAsync();
    }
}