using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Apollo.Components.Solutions.Services;
using Apollo.Components.Solutions.Commands;

namespace Apollo.Components.Infrastructure.MessageBus;

public static class BusRegistrationExtensions
{
    public static void RegisterConsumers(this IServiceCollection services, Assembly assembly)
    {
        var consumerTypes = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .Select(i => new { ConsumerType = t, MessageType = i.GetGenericArguments()[0] }))
            .Where(c => !typeof(ComponentBase).IsAssignableFrom(c.ConsumerType)); 

        foreach (var consumer in consumerTypes)
        {
            services.AddTransient(consumer.ConsumerType);
        }
    }
    
    public static void RegisterConsumersToBus(this IMessageBus bus, Assembly assembly)
    {
        var consumerTypes = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                .Select(i => new { ConsumerType = t, MessageType = i.GetGenericArguments()[0] }))
            .Where(c => !typeof(ComponentBase).IsAssignableFrom(c.ConsumerType));

        foreach (var consumer in consumerTypes)
        {
            bus.RegisterConsumerType(consumer.MessageType, consumer.ConsumerType);
        }
    }

    public static void RegisterConsumerType(this IMessageBus bus, Type messageType, Type consumerType)
    {
        var method = typeof(IMessageBus).GetMethod(nameof(IMessageBus.RegisterConsumerType))?
            .MakeGenericMethod(messageType, consumerType);
        method?.Invoke(bus, null);
    }

    public static IServiceCollection AddMessageBus(this IServiceCollection services)
    {
        services.AddSingleton<IMessageBus, InMemoryMessageBus>();
        
        services.RegisterConsumers(Assembly.GetExecutingAssembly());
        
        return services;
    }
    
    public static IServiceCollection AddMessageBus(this IServiceCollection services,  params Assembly[] assemblies)
    {
        services.AddSingleton<IMessageBus, InMemoryMessageBus>();
        
        foreach (var assembly in assemblies)
        {
            services.RegisterConsumers(assembly);
        }
        
        return services;
    }

    public static IServiceProvider UseMessageBus(this IServiceProvider serviceProvider)
    {
        var bus = serviceProvider.GetService<IMessageBus>();
        bus.RegisterConsumersToBus(Assembly.GetExecutingAssembly());
        
        return serviceProvider;
    }
    
    public static IServiceProvider UseMessageBus(this IServiceProvider serviceProvider, params Assembly[] assemblies)
    {
        var bus = serviceProvider.GetService<IMessageBus>();
        foreach (var assembly in assemblies)
        {
            bus.RegisterConsumersToBus(assembly);
        }
        return serviceProvider;
    }
}