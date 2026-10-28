using Backoffice.Web.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Backoffice.Web.Tests.Services
{
    public class DataProtectionKeysRegistrationTests : IDisposable
    {
        private const string Purpose = "BO-114.Test";
        private readonly string _keysPath = Path.Combine(Path.GetTempPath(), $"bo-web-keys-{Guid.NewGuid():N}");

        [Fact]
        public void AddDataProtectionKeys_CookieFromBeforeARestartCanStillBeRead()
        {
            string protectedPayload;
            using (var beforeRestart = BuildApp(_keysPath))
            {
                protectedPayload = beforeRestart.Services.GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector(Purpose).Protect("session-payload");
            }

            Assert.NotEmpty(Directory.GetFiles(_keysPath, "key-*.xml"));

            using var afterRestart = BuildApp(_keysPath);
            var unprotected = afterRestart.Services.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(Purpose).Unprotect(protectedPayload);

            Assert.Equal("session-payload", unprotected);
        }

        [Fact]
        public void AddDataProtectionKeys_UsesAFixedApplicationNameByDefault()
        {
            using var app = BuildApp(keysPath: null);

            var options = app.Services.GetRequiredService<IOptions<DataProtectionOptions>>().Value;

            Assert.Equal(WebApplicationBuilderExtensions.DefaultDataProtectionApplicationName, options.ApplicationDiscriminator);
        }

        private static WebApplication BuildApp(string? keysPath)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataProtection:KeysPath"] = keysPath
            });
            builder.AddDataProtectionKeys();

            return builder.Build();
        }

        public void Dispose()
        {
            if (Directory.Exists(_keysPath))
                Directory.Delete(_keysPath, recursive: true);
        }
    }
}
