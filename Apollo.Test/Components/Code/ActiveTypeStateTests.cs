using System.Reflection;
using Apollo.Components.Code;
using Apollo.Components.Infrastructure.Environment;
using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Library.SampleProjects;
using Apollo.Components.Solutions.Events;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Apollo.Test.Components.Code;

public class ActiveTypeStateTests : ApolloBaseTestContext
{
    private ActiveTypeState _state;
    private IMessageBus _bus;

    public ActiveTypeStateTests()
    {
        Services.AddSingleton<IRuntimeEnvironment>(new TestRuntimeEnvironment());
        _bus = new InMemoryMessageBus(
            Services,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Enumerable.Empty<IMessagePipe>(),
            Enumerable.Empty<IConsumerFilter>());
        Services.AddSingleton<IMessageBus>(_bus);
        _state = new ActiveTypeState();
    }

    [Fact]
    public async Task Handles_BuildCompleted_AndSetsTypes()
    {
        // Arrange
        var project = SimpleLibraryProject.Create();
        _bus.Subscribe(_state);

        // Act
        var result = TestCompiler.Compile(project);
        var asm = Assembly.Load(result.Assembly);
        await _bus.PublishAsync(new BuildCompleted(new CompilationResult(result.Success, asm)));

        // Assert
        _state.Types.ShouldNotBeNull();
        _state.Types.Length.ShouldBeGreaterThan(0);
    }

    private sealed class TestRuntimeEnvironment : IRuntimeEnvironment
    {
        public string Name => "production";
        public Version Version => new(0, 0);
        public string BaseAddress => string.Empty;
    }
}