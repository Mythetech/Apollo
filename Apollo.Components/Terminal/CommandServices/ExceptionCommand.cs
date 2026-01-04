using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Terminal.Commands;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Apollo.Components.Terminal.CommandServices;

public class ExceptionCommand : ITerminalCommand
{
    private readonly IMessageBus _bus;

    public ExceptionCommand(IMessageBus bus)
    {
        _bus = bus;
    }

    public string Name { get; } = "exception";
    public string Description => "Throws a handled exception";
    public string[] Aliases => ["yeet"];
    public async Task ExecuteAsync(string[] args)
    {
        string message = "Uh oh! Something went wrong.";
        if (args.Length >= 1)
        {
            message = args[0];
        }
        await _bus.PublishAsync(new TestException(message));
    }
}

