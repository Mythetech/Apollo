using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;

namespace Apollo.Components.Terminal.CommandServices;

public class RunCommand : ITerminalCommand
{
    private readonly IMessageBus _bus;
    private readonly TerminalState _state;

    public RunCommand(IMessageBus bus, TerminalState state)
    {
        _bus = bus;
        _state = state;
    }

    public string Name => "run";
    public string Description => "Run the current solution";
    public string[] Aliases => [];
    public async Task ExecuteAsync(string[] args)
    {
        _state.AddEntry("Run solution requested...");
        await _bus.PublishAsync(new RunSolution());
    }
}