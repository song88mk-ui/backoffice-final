using System.Net;
using Backoffice.Web.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Backoffice.Web.Tests.Pipeline
{
    /// <summary>
    /// BO-132: Program.cs ran the developer exception page and forced CircuitOptions.DetailedErrors on in
    /// every environment, so STG and PRD sent stack traces, file paths and headers to the browser.
    /// Deployed pods run as Production (no ASPNETCORE_ENVIRONMENT in argocd), so detail now needs
    /// Development or the DetailedErrors key.
    /// </summary>
    public class ErrorPagesTests
    {
        private const string SecretInException = "BO-132 secret connection string";
        private const string GenericErrorBody = "An error occurred while processing your request.";

        [Theory]
        [InlineData("Production", null, false)]
        [InlineData("Production", "false", false)]
        [InlineData("Production", "true", true)]
        [InlineData("Production", "True", true)]
        [InlineData("Production", "1", true)]
        [InlineData("Development", null, true)]
        public async Task UseErrorPages_ShowsExceptionDetailOnlyWhenDetailedErrorsIsOn(string environment, string? setting, bool expectDetail)
        {
            await using var app = await StartApp(environment, setting);

            var response = await app.GetTestClient().GetAsync("/boom");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(expectDetail, body.Contains(SecretInException));
            Assert.Equal(!expectDetail, body == GenericErrorBody);
        }

        [Theory]
        [InlineData("Production", null, false)]
        [InlineData("Production", "true", true)]
        public void Circuit_DetailedErrorsFollowsTheSetting(string environment, string? setting, bool expected)
        {
            var builder = CreateBuilder(environment, setting);
            builder.Services.AddServerSideBlazor();
            using var app = builder.Build();

            var circuit = app.Services.GetRequiredService<IOptions<CircuitOptions>>().Value;

            Assert.Equal(expected, circuit.DetailedErrors);
        }

        private static async Task<WebApplication> StartApp(string environment, string? setting)
        {
            var builder = CreateBuilder(environment, setting);
            builder.WebHost.UseTestServer();

            var app = builder.Build();
            app.UseErrorPages();
            app.MapGet("/boom", (Func<string>)(() => throw new InvalidOperationException(SecretInException)));
            // Stands in for Pages/Error.cshtml, which the exception handler re-executes.
            app.MapGet("/Error", () => Results.Content(GenericErrorBody));

            await app.StartAsync();
            return app;
        }

        private static WebApplicationBuilder CreateBuilder(string environment, string? setting)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WebApplicationExtensions.DetailedErrorsKey] = setting
            });

            return builder;
        }
    }
}
