using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Backoffice.Web.Tests.Common
{
    // Component tests for PermissionRow, covering permission updates via the checkbox
    // (OnCheckboxClicked) and the BO-76 regression where re-rendering the admin's
    // "Sub Users" row kept resetting that permission's saved value back to disabled.
    public class PermissionRowTests : TestContext
    {
        public PermissionRowTests()
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
            Services.AddSingleton(sp => new PageState(
                new Mock<IAuthenticationService>().Object,
                sp.GetRequiredService<NavigationManager>()));
        }

        private static string ClaimType(SportsbookClientPermission permission) =>
            permission.ToString().Replace("_", ".");

        [Fact]
        public void CheckingCheckbox_UpdatesClaimAndCorrespondingPermissionInList()
        {
            var claimType = ClaimType(SportsbookClientPermission.SBC_Dashboard);
            var permissions = new List<PermissionModel>
            {
                new PermissionModel { ClaimType = claimType, ClaimValue = "0" }
            };
            var claim = new PermissionTreeModel.PermissionClaim
            {
                Name = "Dashboard",
                ClaimType = claimType,
                ClaimValue = "0"
            };

            var cut = RenderComponent<PermissionRow>(ps => ps
                .Add(p => p.Claim, claim)
                .Add(p => p.Permissions, permissions)
                .Add(p => p.IsAdmin, false)
                .Add(p => p.IsSubUser, false));

            cut.Find("input.form-check-input").Change(true);

            Assert.Equal("1", claim.ClaimValue);
            Assert.Equal("1", Assert.Single(permissions).ClaimValue);
        }

        [Fact]
        public void UncheckingCheckbox_UpdatesClaimAndCorrespondingPermissionInList()
        {
            var claimType = ClaimType(SportsbookClientPermission.SBC_Dashboard);
            var permissions = new List<PermissionModel>
            {
                new PermissionModel { ClaimType = claimType, ClaimValue = "1" }
            };
            var claim = new PermissionTreeModel.PermissionClaim
            {
                Name = "Dashboard",
                ClaimType = claimType,
                ClaimValue = "1"
            };

            var cut = RenderComponent<PermissionRow>(ps => ps
                .Add(p => p.Claim, claim)
                .Add(p => p.Permissions, permissions)
                .Add(p => p.IsAdmin, false)
                .Add(p => p.IsSubUser, false));

            cut.Find("input.form-check-input").Change(false);

            Assert.Equal("0", claim.ClaimValue);
            Assert.Equal("0", Assert.Single(permissions).ClaimValue);
        }

        [Fact]
        public void RenderingAdminSubUsersRow_DoesNotResetStoredPermissionValue_RegressionForBO76()
        {
            // Before the BO-76 fix, OnParametersSetAsync additionally looked up the SubUsers
            // entry in the saved Permissions list and forced it to "0" every time this row
            // rendered for an admin -- silently wiping out an already-granted sub user
            // permission whenever the parent tree re-rendered (e.g. editing any other claim).
            var claimType = ClaimType(SportsbookClientPermission.SBC_SubUsers);
            var permissions = new List<PermissionModel>
            {
                new PermissionModel { ClaimType = claimType, ClaimValue = "1" }
            };
            var claim = new PermissionTreeModel.PermissionClaim
            {
                Name = "Sub Users",
                ClaimType = claimType,
                ClaimValue = "1"
            };

            var cut = RenderComponent<PermissionRow>(ps => ps
                .Add(p => p.Claim, claim)
                .Add(p => p.Permissions, permissions)
                .Add(p => p.IsAdmin, true)
                .Add(p => p.IsSubUser, false));

            // Simulate the parent re-rendering this row again, as happens when an unrelated
            // permission elsewhere in the tree is toggled.
            cut.SetParametersAndRender(ps => ps
                .Add(p => p.Claim, claim)
                .Add(p => p.Permissions, permissions)
                .Add(p => p.IsAdmin, true)
                .Add(p => p.IsSubUser, false));

            Assert.Equal("1", Assert.Single(permissions).ClaimValue);

            // The row's own displayed claim is still correctly forced off/read-only for admins.
            Assert.Equal("0", claim.ClaimValue);
            Assert.True(claim.IsReadOnly);
        }
    }
}
