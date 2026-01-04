using Apollo.Components.Settings;
using Apollo.Components.Settings.Consumers;
using Apollo.Components.Settings.Events;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using NSubstitute;
using Shouldly;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Apollo.Test.Components.Settings;

public class SettingsModelChangedTests : TestContext
{
    private IMessageBus _bus;

    public SettingsModelChangedTests()
    {
        _bus = new InMemoryMessageBus(
            Services,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Enumerable.Empty<IMessagePipe>(),
            Enumerable.Empty<IConsumerFilter>());
        Services.AddSingleton<IMessageBus>(_bus);

        // Register the converter that transforms SettingsModelChanged -> SettingsModelChanged<T>
        Services.AddSingleton<GenericSettingsModelConverter>();
        _bus.RegisterConsumerType<SettingsModelChanged, GenericSettingsModelConverter>();
    }

    [Fact(DisplayName = "When SettingsModelChanged is published, typed consumers receive SettingsModelChanged<T>")]
    public async Task TypedConsumer_Receives_SettingsModelChanged()
    {
        // Arrange
        var consumer = new TestSettingsConsumer();
        _bus.Subscribe(consumer);

        // Also register it so the bus knows about this consumer type
        Services.AddSingleton(consumer);
        _bus.RegisterConsumerType<SettingsModelChanged<TestSettings>, TestSettingsConsumer>();

        var settings = new TestSettings { TestValue = "Hello World" };

        // Act - publish the non-generic event (as SettingsProvider.ApplyStoredSettingsAsync does)
        await _bus.PublishAsync(new SettingsModelChanged(typeof(TestSettings), settings));

        // Assert - the typed consumer should have received the converted event
        consumer.ReceivedModel.ShouldNotBeNull();
        consumer.ReceivedModel.TestValue.ShouldBe("Hello World");
    }

    [Fact(DisplayName = "Direct typed publish works")]
    public async Task DirectTypedPublish_Works()
    {
        // Arrange
        var consumer = new TestSettingsConsumer();
        _bus.Subscribe(consumer);
        Services.AddSingleton(consumer);
        _bus.RegisterConsumerType<SettingsModelChanged<TestSettings>, TestSettingsConsumer>();

        var settings = new TestSettings { TestValue = "Direct Test" };

        // Act - publish directly as typed (this should work)
        await _bus.PublishAsync(new SettingsModelChanged<TestSettings>(settings));

        // Assert
        consumer.ReceivedModel.ShouldNotBeNull();
        consumer.ReceivedModel.TestValue.ShouldBe("Direct Test");
    }
}

/// <summary>
/// Test settings model that extends SettingsBase
/// </summary>
public class TestSettings : SettingsBase
{
    public string TestValue { get; set; } = string.Empty;

    public override string Section => "Test";
    public override Type Type => typeof(TestSettings);
    public override object Model => this;
}

/// <summary>
/// Consumer that listens for SettingsModelChanged<TestSettings>
/// </summary>
public class TestSettingsConsumer : IConsumer<SettingsModelChanged<TestSettings>>
{
    public TestSettings? ReceivedModel { get; private set; }

    public Task Consume(SettingsModelChanged<TestSettings> message)
    {
        ReceivedModel = message.Model;
        return Task.CompletedTask;
    }
}
