using Microsoft.AspNetCore.Components;

namespace Apollo.Components.Infrastructure.MessageBus;

public abstract class ComponentConsumer<TMessage> : ComponentBase, IConsumer<TMessage>, IDisposable where TMessage : class
{
    [Inject]
    protected IMessageBus MessageBus { get; set; } = default!;
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        MessageBus.Subscribe(this);

    }

    public async Task Consume(TMessage message)
    {
        var cts = new CancellationTokenSource();
        await InvokeAsync(async () => await Consume(message, cts.Token)); 
    }
    
    protected abstract Task Consume(TMessage message, CancellationToken cancellationToken);

    public void Dispose()
    {
        MessageBus.Unsubscribe(this);
    }
}