using Backoffice.API.Repositories;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Backoffice.API.Tests.ActivityLog
{
    // BO-104: the in-memory search filter shared by every "View Logs" modal. GetActivityLogs itself
    // reads through a stored procedure (FromSql), which the EF in-memory provider cannot execute,
    // so the filter is tested directly.
    public class ActivityLogSearchFilterTests
    {
        private const string ClientGuid = "11111111-2222-3333-4444-555555555555";

        private static ActivityLogModel Log(string userName, string payload, string from = "{}", string to = "{}") => new ActivityLogModel
        {
            Page = "Admin.ClientSiteConfig",
            Activity = "Update",
            From = from,
            To = to,
            UserName = userName,
            Payload = payload
        };

        [Fact]
        public void EmptySearchText_ReturnsAllRows()
        {
            var logs = new List<ActivityLogModel> { Log("jane", "jane.x"), Log("john", "john.y") };

            var result = BackofficeRepository.FilterActivityLogsBySearch(logs, new ActivityLogRequest { SearchText = "" });

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void PayloadSearchTerm_MatchesRowByClientGuidInPayload()
        {
            var logs = new List<ActivityLogModel>
            {
                Log("jane", $"jane.{ClientGuid.ToUpper()}"),
                Log("john", "john.99999999-8888-7777-6666-555555555555")
            };

            var result = BackofficeRepository.FilterActivityLogsBySearch(logs, new ActivityLogRequest
            {
                SearchText = "Acme",
                PayloadSearchTerms = new List<string> { ClientGuid }
            });

            Assert.Equal("jane", Assert.Single(result).UserName);
        }

        [Fact]
        public void SearchText_IsNotMatchedAgainstPayloadWithoutPayloadSearchTerms()
        {
            // Payload holds IPs/ids for other pages; matching raw SearchText against it would change
            // results in every other View Logs modal.
            var logs = new List<ActivityLogModel> { Log("jane", "jane.acme") };

            var result = BackofficeRepository.FilterActivityLogsBySearch(logs, new ActivityLogRequest { SearchText = "acme" });

            Assert.Empty(result);
        }

        [Fact]
        public void NullFields_DoNotThrow_AndStillMatchOtherFields()
        {
            var logs = new List<ActivityLogModel>
            {
                Log("jane", null, from: null, to: null),
                Log(null, "x", from: null, to: "{\"DarkMode\":true}")
            };

            var result = BackofficeRepository.FilterActivityLogsBySearch(logs, new ActivityLogRequest
            {
                SearchText = "darkmode",
                PayloadSearchTerms = null
            });

            Assert.Equal("x", Assert.Single(result).Payload);
        }

        [Fact]
        public void SearchText_StillMatchesFromToAndUserName_CaseInsensitive()
        {
            var logs = new List<ActivityLogModel>
            {
                Log("Jane.Doe", "a"),
                Log("x", "b", from: "{\"FullCashout\":true}"),
                Log("y", "c", to: "{\"FullCashout\":false}"),
                Log("z", "d")
            };

            var byUser = BackofficeRepository.FilterActivityLogsBySearch(logs, new ActivityLogRequest { SearchText = "jane" });
            var byJson = BackofficeRepository.FilterActivityLogsBySearch(logs, new ActivityLogRequest { SearchText = "fullcashout" });

            Assert.Equal("a", Assert.Single(byUser).Payload);
            Assert.Equal(new[] { "b", "c" }, byJson.Select(l => l.Payload).ToArray());
        }
    }
}
