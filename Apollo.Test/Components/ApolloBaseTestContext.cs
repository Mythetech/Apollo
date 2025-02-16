using Bunit;

namespace Apollo.Test.Components;

public abstract class ApolloBaseTestContext : TestContext
{
    protected ApolloBaseTestContext()
    {
        this.AddDefaultTestServices();
    }
} 