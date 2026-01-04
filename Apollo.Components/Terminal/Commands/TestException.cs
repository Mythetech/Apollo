using Mythetech.Framework.Infrastructure.MessageBus;
using Microsoft.Extensions.Logging;

namespace Apollo.Components.Terminal.Commands;

public record TestException(string Message);

public class TestExceptionHandler : IConsumer<TestException>
{
    private readonly ILogger<TestExceptionHandler> _logger;

    public TestExceptionHandler(ILogger<TestExceptionHandler> logger)
    {
        _logger = logger;
    }
    
    public async Task Consume(TestException message)
    {
        try
        {
            throw new Exception(message.Message);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}