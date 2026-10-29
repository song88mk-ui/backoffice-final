using Backoffice.API.ActionFilter;
using Backoffice.API.Controllers.Backoffice;
using Backoffice.Shared.Enums;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Xunit;

namespace Backoffice.API.Tests.Bet
{
    // BO-130: the API must require the same permission as the Back Office page that calls each action, not
    // only a login. These tests pin each action's claim, so dropping or changing one fails the build. Claim
    // names are AdminPermission names with '_' replaced by '.', as the Web policies build them.
    public class BetControllerAuthorizationTests
    {
        private static string ClaimName(AdminPermission permission) => permission.ToString().Replace('_', '.');

        private static Claim[] RequiredClaims(MemberInfo member) =>
            member.GetCustomAttributes<AuthorizeAttribute>(inherit: false)
                .Select(a => (Claim)typeof(AuthorizeAttribute)
                    .GetField("_requiredClaim", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(a))
                .ToArray();

        [Theory]
        [InlineData(nameof(BetController.ManualSettlement), AdminPermission.Admin_Settlements_ManualBetSettlement, Access.Edit)]
        [InlineData(nameof(BetController.GetResendSettlementBetDetails), AdminPermission.Admin_Settlements_ResendSettlement, Access.View)]
        [InlineData(nameof(BetController.ResendSettlement), AdminPermission.Admin_Settlements_ResendSettlement, Access.Edit)]
        [InlineData(nameof(BetController.ResendProviderCancellation), AdminPermission.Admin_Settlements_ResendSettlement, Access.Edit)]
        [InlineData(nameof(BetController.CancelBet), AdminPermission.Admin_CancelBets, Access.Edit)]
        public void Action_RequiresPagePermission(string action, AdminPermission permission, Access access)
        {
            var claim = Assert.Single(RequiredClaims(typeof(BetController).GetMethod(action)));

            Assert.NotNull(claim);
            Assert.Equal(ClaimName(permission), claim.Type);
            Assert.Equal(((int)access).ToString(), claim.Value);
        }

        // A class-level [Authorize] with a claim would also apply to every action and lock out users of the
        // other pages (see "Dedicated Translation Permission" in docs/08_TechnicalDebt.md), so it must stay claim-free.
        [Fact]
        public void Controller_ClassLevelAuthorize_HasNoClaim()
        {
            var claim = Assert.Single(RequiredClaims(typeof(BetController)));

            Assert.Null(claim);
        }

        // Shared by the Manual Settlement, Market Settlement and Cancel Bets pages, so no single claim fits.
        [Fact]
        public void GetManualBetDetails_HasNoActionLevelClaim()
        {
            Assert.Empty(RequiredClaims(typeof(BetController).GetMethod(nameof(BetController.GetManualBetDetails))));
        }
    }
}
