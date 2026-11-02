using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Administration.ManageUserGroups;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text.Json;
using Xunit;

namespace Backoffice.Web.Tests.Common
{
    // BO-134: text typed by users or received from the hub must render as text, not as HTML.
    public class MarkupEncodingTests : BunitTestContext
    {
        private const string HostileName = "<img src=x onerror=alert(1)>";

        private readonly ToastService _toastService = new ToastService();

        public MarkupEncodingTests()
        {
            var translationService = new Mock<ITranslationService>();
            translationService
                .Setup(s => s.GetTranslationsAsync(It.IsAny<LanguageCode>()))
                .ReturnsAsync(new Dictionary<string, string>());

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["API:HubAPI"] = "http://localhost/hub" })
                .Build();

            Services.AddSingleton<IConfiguration>(configuration);
            Services.AddSingleton(new Mock<IPreRenderService>().Object);
            Services.AddSingleton(new TranslationState(translationService.Object));
            Services.AddSingleton<UserState>();
            Services.AddSingleton<AlertToastService>();
            Services.AddSingleton<HubService>();
            Services.AddSingleton(new Mock<IModalService>().Object);
            Services.AddSingleton(new Mock<ITradingService>().Object);
            Services.AddSingleton(new Mock<IProviderMappingService>().Object);
            Services.AddSingleton(new Mock<IBackofficeService>().Object);
            Services.AddSingleton(new Mock<IAuthenticationService>().Object);
            Services.AddSingleton(new Mock<ITableauService>().Object);
            Services.AddSingleton(new Mock<ICommonService>().Object);
            Services.AddSingleton(_toastService);
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));
        }

        [Fact]
        public void DeleteUserGroupModal_ShowsGroupNameAsText()
        {
            var parameters = new ModalParameters();
            parameters.Add("UserGroup", new UserGroupResponse { UserGroupId = "1", UserGroupName = HostileName });

            var cut = Render<DeleteUserGroupModal>(ps => ps.AddCascadingValue(parameters));

            Assert.Empty(cut.FindAll("img"));
            Assert.Equal(HostileName, cut.Find("span.fw-bold").TextContent);
        }

        [Fact]
        public void UserGroupLog_ShowsGroupNameAsText()
        {
            var from = JsonSerializer.Serialize(new UserGroupResponse { UserGroupId = "1", UserGroupName = "Traders" });
            var to = JsonSerializer.Serialize(new UserGroupResponse { UserGroupId = "1", UserGroupName = HostileName });

            var activity = CommonService.GetActivity<AdminPermission>(from, to, AdminPermission.Admin_ManageUserGroups, Activity.Update.ToString(), false);

            Assert.DoesNotContain("<img", activity);
            Assert.Contains("UserGroupName: &lt;img src=x onerror=alert(1)&gt;", activity);
        }

        [Fact]
        public void Toast_ShowsInjectedHtmlAsText()
        {
            _toastService.AddToast(Toast.NewToast("Notification", HostileName, true));

            var cut = Render<ToastComponent>();

            Assert.Empty(cut.FindAll(".toast-body img"));
            Assert.Equal(HostileName, cut.Find(".toast-body").TextContent.Trim());
        }

        [Fact]
        public void Toast_KeepsProducerFormatting()
        {
            // Formats sent by Backoffice.Utility (excel ready) and Backoffice.API (boosted settings).
            _toastService.AddToast(Toast.NewToast("Export", "Your excel file is now ready: <a onmouseout=\"this.style.color = '#404E67'\" onmouseover=\"this.style.color = 'blue'\" href=\"https://bucket.s3.amazonaws.com/report.xlsx?X-Amz-Signature=a&b=c\">report.xlsx</a>", true));
            _toastService.AddToast(Toast.NewToast("Boost", "<b>New Boosted Settings</b> for <b>Client A</b> - 10.00%.", true));

            var cut = Render<ToastComponent>();

            var link = cut.Find(".toast-body a");
            Assert.Equal("report.xlsx", link.TextContent);
            Assert.StartsWith("https://bucket.s3.amazonaws.com/report.xlsx", link.GetAttribute("href"));
            Assert.Null(link.GetAttribute("onmouseover"));
            Assert.Equal(new[] { "New Boosted Settings", "Client A" }, cut.FindAll(".toast-body b").Select(b => b.TextContent));
        }

        [Theory]
        [InlineData("<a href=\"javascript:alert(1)\">x</a>")]
        [InlineData("<b onclick=\"alert(1)\">x</b>")]
        [InlineData("<svg onload=alert(1)>")]
        [InlineData("<a href=\"https://x\" onclick=\"alert(1)\">x</a><img src=x onerror=alert(1)>")]
        public void ToSafeMarkup_DropsAnythingButPlainTagsAndHttpLinks(string message)
        {
            var markup = MarkupHelper.ToSafeMarkup(message);

            // Every tag left in the output is one the helper rebuilt itself.
            var tags = System.Text.RegularExpressions.Regex.Matches(markup, "<[^>]*>").Select(m => m.Value);
            Assert.All(tags, tag => Assert.Matches("^(<b>|</b>|<strong>|</strong>|<br>|</a>|<a href=\"https?://[^\"]*\">)$", tag));
        }

        [Fact]
        public void ToSafeMarkup_EncodesLinkTextAndKeepsLineBreaks()
        {
            var markup = MarkupHelper.ToSafeMarkup("a<br/>b<BR>c <a href=\"https://x/y?a=1&b=2\">&lt;file&gt;</a>");

            Assert.Equal("a<br>b<br>c <a href=\"https://x/y?a=1&amp;b=2\">&amp;lt;file&amp;gt;</a>", markup);
        }
    }
}
