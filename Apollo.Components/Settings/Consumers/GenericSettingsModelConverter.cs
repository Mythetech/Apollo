using System.Reflection;
using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Settings.Events;

namespace Apollo.Components.Settings.Consumers;

public class GenericSettingsModelConverter(IMessageBus bus) : IConsumer<SettingsModelChanged>
{
    private static readonly MethodInfo PublishAsyncMethod = typeof(IMessageBus)
        .GetMethods()
        .First(m => m.Name == nameof(IMessageBus.PublishAsync) &&
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 1);

    public async Task Consume(SettingsModelChanged message)
    {
        var genericMessageType = typeof(SettingsModelChanged<>).MakeGenericType(message.Type);
        var instance = Activator.CreateInstance(genericMessageType, message.Model);

        // Use reflection to call PublishAsync<T> with the runtime type
        var method = PublishAsyncMethod.MakeGenericMethod(genericMessageType);
        var task = (Task)method.Invoke(bus, [instance!])!;
        await task;
    }
}