using Backoffice.Web.Extensions;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Backoffice.Web.Tests.Services
{
    public class MetabaseIdleCleanupRegistrationTests
    {
        [Fact]
        public void AddServices_RegistersTheCleanupJobWithItsSettingsAndAdminClient()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Metabase:SiteUrl"] = "https://metabase.example",
                ["Metabase:ApiKey"] = "test-key",
                ["Metabase:IdleCleanup:IdleHours"] = "1.5",
                ["Metabase:IdleCleanup:GroupName"] = "Staff"
            });
            builder.AddHttpClients().AddServices();

            using var app = builder.Build();

            Assert.Contains(app.Services.GetServices<IHostedService>(), s => s is MetabaseIdleUserCleanupService);

            var options = app.Services.GetRequiredService<IOptions<MetabaseIdleCleanupOptions>>().Value;
            Assert.False(options.Enabled);
            Assert.Equal(1.5, options.IdleHours);
            Assert.Equal("Staff", options.GroupName);

            Assert.True(app.Services.GetRequiredService<IMetabaseAdminClient>().IsConfigured);
            Assert.NotNull(app.Services.GetRequiredService<IHttpClientFactory>().CreateClient(MetabaseAdminClient.HttpClientName));
        }

        [Fact]
        public void Options_AreOffAndUseTheTicketDefaultsUntilConfigured()
        {
            var options = new MetabaseIdleCleanupOptions();

            Assert.False(options.Enabled);
            Assert.Equal(3, options.IdleHours);
            Assert.Equal("Clients", options.GroupName);
        }
    }
}
