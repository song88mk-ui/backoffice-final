using Bunit;
using Xunit;

namespace Backoffice.Web.Tests
{
    // bUnit 2 disposes its service container synchronously from Dispose(), which throws for
    // services that only implement IAsyncDisposable (HubService). xUnit 2.x only calls Dispose()
    // on test classes, so IAsyncLifetime makes it call DisposeAsync() first (BO-120).
    public abstract class BunitTestContext : BunitContext, IAsyncLifetime
    {
        Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

        Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
    }
}
