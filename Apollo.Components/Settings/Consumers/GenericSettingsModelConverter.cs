using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Settings.Events;

namespace Apollo.Components.Settings.Consumers;

public class GenericSettingsModelConverter(IMessageBus bus) : IConsumer<SettingsModelChanged>
{
    public async Task Consume(SettingsModelChanged message)
    {
        var genericMessageType = typeof(SettingsModelChanged<>).MakeGenericType(message.Type);
        var instance = Activator.CreateInstance(genericMessageType, message.Model);
        
        await bus.PublishAsync(genericMessageType, instance!);
    }
}