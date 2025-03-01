using Apollo.Components.Infrastructure.MessageBus;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Apollo.Test.Components.Infrastructure.MessageBus;

public class MessageBusTests : TestContext
{
    private IMessageBus _bus;
    
    public MessageBusTests()
    {
        Services.AddSingleton<TestConsumer>();
        _bus = new InMemoryMessageBus(this.Services, Substitute.For<ILogger<InMemoryMessageBus>>());
        Services.AddSingleton<IMessageBus>(_bus);
    }

    [Fact(DisplayName = "Can send messages to be received by services")]
    public async Task Can_Message_Services()
    {
        // Arrange
        _bus.RegisterConsumerType<TestCommand, TestConsumer>();
        
        // Act
        await _bus.PublishAsync(new TestCommand("HelloWorld!"));
        
        // Assert
        var consumer = Services.GetService<TestConsumer>();
        consumer.Text.ShouldBe("HelloWorld!");
    }
    
    [Fact(DisplayName = "Can send messages to be received by component consumers")]
    public async Task Can_Message_Components()
    {
        // Arrange
        _bus.RegisterConsumerType<TestCommand, TestComponentConsumer>();
        var cut = RenderComponent<TestComponentConsumer>();
        
        // Act
        await _bus.PublishAsync(new TestCommand("HelloWorld!"));
        
        // Assert
        cut.Markup.MarkupMatches("<p>HelloWorld!</p>");
    }

    [Fact(DisplayName = "When a consumer throws an exception, other consumers still receive the message")]
    public async Task Exception_In_One_Consumer_Does_Not_Affect_Others()
    {
        // Arrange
        _bus.RegisterConsumerType<TestCommand, TestComponentConsumer>();
        _bus.RegisterConsumerType<TestCommand, TestExceptionThrowingConsumer>();
        
        var normalConsumer = RenderComponent<TestComponentConsumer>();
        var throwingConsumer = RenderComponent<TestExceptionThrowingConsumer>();
        
        // Act 
        await _bus.PublishAsync(new TestCommand("HelloWorld!"));
        
        // Assert
        normalConsumer.Markup.MarkupMatches("<p>HelloWorld!</p>");
    }
    
    [Fact(DisplayName = "When multiple consumers throw an exception, other consumers still receive the message")]
    public async Task Exceptions_In_Multiple_Consumers_Does_Not_Affect_Others()
    {
        // Arrange
        _bus.RegisterConsumerType<TestCommand, TestComponentConsumer>();
        _bus.RegisterConsumerType<TestCommand, TestExceptionThrowingConsumer>();
        
        var initialThrowingConsumer = RenderComponent<TestExceptionThrowingConsumer>();
        var throwingConsumer = RenderComponent<TestExceptionThrowingConsumer>();
        var normalConsumer = RenderComponent<TestComponentConsumer>();
        
        // Act 
        await _bus.PublishAsync(new TestCommand("HelloWorld!"));
        
        // Assert
        normalConsumer.Markup.MarkupMatches("<p>HelloWorld!</p>");
    }
}

public record TestCommand(string Text);

public class TestConsumer : IConsumer<TestCommand>
{
    public string? Text { get; set; }
    
    public Task Consume(TestCommand message)
    {
        Text = message.Text;
        return Task.CompletedTask;
    }
}