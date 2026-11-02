using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Pages.Authentication;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.JSInterop;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;
using IAuthenticationService = Backoffice.Web.Services.IAuthenticationService;

namespace Backoffice.Web.Tests.Pages.Authentication
{
    public class LoginModelTests
    {
        private const string Password = "Wr0ngPassw0rd!";

        private sealed class ListSink : ILogEventSink
        {
            public List<LogEvent> Events { get; } = new();

            public void Emit(LogEvent logEvent) => Events.Add(logEvent);
        }

        private static LoginModel BuildPage(BaseResponse<Tuple<bool, List<string>, TokenModel>> loginResult)
        {
            var authenticationService = new Mock<IAuthenticationService>();
            authenticationService.Setup(x => x.Login(It.IsAny<LogInModel>())).ReturnsAsync(loginResult);

            var translationService = new Mock<ITranslationService>();
            translationService.Setup(x => x.GetTranslationsAsync(It.IsAny<LanguageCode>())).ReturnsAsync(new Dictionary<string, string>());

            var httpContext = new DefaultHttpContext();

            return new LoginModel(authenticationService.Object, new HttpContextAccessor { HttpContext = httpContext },
                Mock.Of<IJSRuntime>(), translationService.Object)
            {
                PageContext = new PageContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
                LogInModel = new LogInModel
                {
                    Username = "qa.user",
                    Password = Password,
                    IpAddress = "10.0.0.1",
                    Country = "PH",
                    UserAgent = "test-agent"
                }
            };
        }

        [Fact]
        public async Task OnPostLogin_Failed_DoesNotPutLoginDetailsInPage_AndLogsWithoutPassword()
        {
            var page = BuildPage(new BaseResponse<Tuple<bool, List<string>, TokenModel>>
            {
                StatusCode = StatusCode.Error,
                Message = "Incorrect password",
                Result = new Tuple<bool, List<string>, TokenModel>(false, new List<string>(), null)
            });

            var sink = new ListSink();
            var previousLogger = Log.Logger;
            Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(sink).CreateLogger();
            try
            {
                await page.OnPostLogin();
            }
            finally
            {
                Log.Logger = previousLogger;
            }

            Assert.False(page.TempData.ContainsKey("LogError"));
            Assert.All(page.TempData.Values, value => Assert.DoesNotContain(Password, value?.ToString() ?? string.Empty));
            Assert.Equal(true, page.TempData["HasError"]);

            var error = Assert.Single(page.ModelState[string.Empty]!.Errors);
            Assert.Equal("Username or Password is incorrect", error.ErrorMessage);

            var failure = Assert.Single(sink.Events, e => e.Level == LogEventLevel.Warning);
            var rendered = failure.RenderMessage();
            Assert.Contains("qa.user", rendered);
            Assert.DoesNotContain(Password, rendered);
            Assert.All(failure.Properties.Values, value => Assert.DoesNotContain(Password, value.ToString()));
        }
    }
}
