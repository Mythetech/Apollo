using System.Reflection;
using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Events;

namespace Apollo.Components.Code;

public class ActiveTypeState : IConsumer<BuildCompleted>
{
    public Type[]? Types { get; private set; }
    public Dictionary<Type, object?> Instances { get; } = new();
    public object? LastResult { get; set; }
    public string? LastError { get; set; }

    public event Action? StateChanged;
    
    public void NotifyStateChanged() => StateChanged?.Invoke();
    
    public void AddTypeInstance(Type type, object? instance)
    {
        Instances[type] = instance;
        NotifyStateChanged();   
    }

    public Task Consume(BuildCompleted message)
    {
        HandleBuildCompleted(message.Result.CompiledAssembly);
        return Task.CompletedTask;
    }

    private void HandleBuildCompleted(Assembly? assembly)
    {
        Types = assembly?.GetExportedTypes() ?? [];
        Instances.Clear();
        LastResult = null;
        LastError = null;
        NotifyStateChanged();
    }
} 