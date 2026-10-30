using Backoffice.API.Models;
using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Translation;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SavedLeagueRow = Backoffice.Shared.Data.Trading.SQLEntities.ClientLeagueConfig;

namespace Backoffice.API.Tests.Config
{
    // BO-151: a user assigns leagues to a client as Active or Restricted, or unassigns them. The league settings are saved
    // in Trading.dbo.ClientLeagueConfig (IsActive = 0 is Restricted), only for the clients the user can see, and every
    // change is written to the activity log.
    public class ConfigSetStateTests
    {
        private static readonly string[] AllClients = { "ALL" };
        private static readonly Guid S88Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid OgId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private sealed class Harness
        {
            public BackofficeContext Context { get; }
            public Mock<ITradingRepository> Trading { get; } = new Mock<ITradingRepository>();
            public List<SavedLeagueRow> Upserted { get; } = new List<SavedLeagueRow>();
            public List<SavedLeagueRow> Deleted { get; } = new List<SavedLeagueRow>();
            public BackofficeRepository Sut { get; }

            public Harness()
            {
                var name = Guid.NewGuid().ToString();
                Context = new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(name).Options);
                var translation = new TranslationContext(new DbContextOptionsBuilder<TranslationContext>().UseInMemoryDatabase(name).Options);

                Trading.Setup(t => t.GetClientLeagueConfigRows(It.IsAny<List<string>>())).ReturnsAsync(Rows());
                Trading
                    .Setup(t => t.UpsertClientLeagueConfigRows(It.IsAny<List<SavedLeagueRow>>()))
                    .Callback<List<SavedLeagueRow>>(rows => Upserted.AddRange(rows))
                    .ReturnsAsync(Ok());
                Trading
                    .Setup(t => t.DeleteClientLeagueConfigRows(It.IsAny<List<SavedLeagueRow>>()))
                    .Callback<List<SavedLeagueRow>>(rows => Deleted.AddRange(rows))
                    .ReturnsAsync(Ok());

                Sut = new BackofficeRepository(
                    Context,
                    translation,
                    new Mock<IPlatformRepository>().Object,
                    new Mock<ISportsbookRepository>().Object,
                    Trading.Object,
                    new Mock<IMemCache>().Object,
                    new Mock<IRabbitMqRepository>().Object);
            }

            public void Saved(params SavedLeagueRow[] rows) =>
                Trading.Setup(t => t.GetClientLeagueConfigRows(It.IsAny<List<string>>())).ReturnsAsync(Rows(rows));

            public List<Backoffice.Shared.Data.Backoffice.Entities.ActivityLog> Logs() => Context.ActivityLog.AsNoTracking().OrderBy(l => l.Payload).ToList();
        }

        private static BaseResponse<bool> Ok() => new() { StatusCode = StatusCode.Success, Result = true };

        private static BaseResponse<List<SavedLeagueRow>> Rows(params SavedLeagueRow[] rows) => new() { StatusCode = StatusCode.Success, Result = rows.ToList() };

        private static SavedLeagueRow Saved(string client, Guid clientId, int league, bool isActive) => new()
        {
            ClientCode = client,
            ClientId = clientId,
            SportId = 1,
            SegmentId = 10,
            LeagueId = league,
            IsActive = isActive
        };

        private static ConfigClientModel League(string client, Guid clientId, int league) => new()
        {
            ClientCode = client,
            ClientId = clientId.ToString(),
            ClientName = client,
            SportId = 1,
            SportName = "Soccer",
            SegmentId = 10,
            SegmentName = "England",
            LeagueId = league,
            LeagueName = $"League {league}"
        };

        private static ConfigSetLeagueStateRequest Request(bool restricted, params ConfigClientModel[] leagues) => new() { Restricted = restricted, Leagues = leagues.ToList() };

        private static RequesterInfo Requester() => new() { UserId = "u-1", UserName = "trader", Page = "Config" };

        // ---- Assign ------------------------------------------------------------------------------------------

        [Fact]
        public async Task AssignAsActive_SavesTheLeaguesWithIsActive_AndLogsTheNewAssignment()
        {
            var h = new Harness();

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(false, League("S88", S88Id, 1), League("S88", S88Id, 2)), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.All(h.Upserted, r => Assert.True(r.IsActive));
            Assert.Equal(new[] { 1, 2 }, h.Upserted.Select(r => r.LeagueId).OrderBy(i => i));
            Assert.All(h.Upserted, r => Assert.Equal(S88Id, r.ClientId));

            var logs = h.Logs();
            Assert.Equal(2, logs.Count);
            Assert.All(logs, l =>
            {
                Assert.Equal("Create", l.Activity);
                Assert.Equal("Unassigned", l.From);
                Assert.Equal("Active", l.To);
                Assert.Equal("u-1", l.UserId);
                Assert.Equal("trader", l.UserName);
                Assert.Equal("Config", l.Page);
            });
            Assert.Equal("S88.1.10.1", logs[0].Payload);
        }

        [Fact]
        public async Task AssignAsRestricted_SavesTheLeaguesWithIsActiveFalse()
        {
            var h = new Harness();

            await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1)), AllClients.ToList(), Requester());

            Assert.False(Assert.Single(h.Upserted).IsActive);
            Assert.Equal("Restricted", Assert.Single(h.Logs()).To);
        }

        [Fact]
        public async Task ARequestThatDoesNotSayRestricted_AssignsActive_NeverRestricted()
        {
            // an older Web build does not send the new field, and the default must keep the leagues visible
            var request = new ConfigSetLeagueStateRequest { Leagues = new List<ConfigClientModel> { League("S88", S88Id, 1) } };
            var h = new Harness();

            await h.Sut.SetConfigState(ConfigState.Assigned, request, AllClients.ToList(), Requester());

            Assert.True(Assert.Single(h.Upserted).IsActive);
        }

        [Fact]
        public async Task ChangingTheState_IsLoggedAsAnUpdate_WithTheOldAndTheNewState()
        {
            var h = new Harness();
            h.Saved(Saved("S88", S88Id, 1, isActive: true));

            await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1)), AllClients.ToList(), Requester());

            var log = Assert.Single(h.Logs());
            Assert.Equal("Update", log.Activity);
            Assert.Equal("Active", log.From);
            Assert.Equal("Restricted", log.To);
        }

        [Fact]
        public async Task AssigningTheSameStateAgain_SavesIt_ButLogsNothing()
        {
            var h = new Harness();
            h.Saved(Saved("S88", S88Id, 1, isActive: false));

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1)), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Single(h.Upserted);
            Assert.Empty(h.Logs());
        }

        [Fact]
        public async Task TheSameLeagueTwiceInOneRequest_IsLoggedOnce()
        {
            var h = new Harness();

            await h.Sut.SetConfigState(ConfigState.Assigned, Request(false, League("S88", S88Id, 1), League("s88", S88Id, 1)), AllClients.ToList(), Requester());

            Assert.Single(h.Logs());
        }

        // ---- Unassign ----------------------------------------------------------------------------------------

        [Fact]
        public async Task Unassign_RemovesTheRows_AndLogsTheDeletion()
        {
            var h = new Harness();
            h.Saved(Saved("S88", S88Id, 1, isActive: false));

            var response = await h.Sut.SetConfigState(ConfigState.UnAssigned, Request(false, League("S88", S88Id, 1)), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(1, Assert.Single(h.Deleted).LeagueId);
            Assert.Empty(h.Upserted);

            var log = Assert.Single(h.Logs());
            Assert.Equal("Delete", log.Activity);
            Assert.Equal("Restricted", log.From);
            Assert.Equal("Unassigned", log.To);
        }

        [Fact]
        public async Task UnassigningALeagueThatIsNotAssigned_IsSuccess_ButLogsNothing()
        {
            var h = new Harness();

            var response = await h.Sut.SetConfigState(ConfigState.UnAssigned, Request(false, League("S88", S88Id, 1)), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Empty(h.Logs());
        }

        // ---- What is refused ---------------------------------------------------------------------------------

        [Fact]
        public async Task NoLeagues_IsAnError_AndNothingIsSaved()
        {
            var h = new Harness();

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(false), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Error, response.StatusCode);
            h.Trading.Verify(t => t.UpsertClientLeagueConfigRows(It.IsAny<List<SavedLeagueRow>>()), Times.Never);
        }

        [Fact]
        public async Task ALeagueWithoutAValidClient_IsAnError_AndNothingIsSaved()
        {
            var h = new Harness();
            var bad = League("S88", S88Id, 1);
            bad.ClientId = "not-a-guid";

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(false, League("S88", S88Id, 2), bad), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Empty(h.Upserted);
        }

        [Fact]
        public async Task AClientTheUserCannotSee_IsAnError_ForTheWholeRequest()
        {
            var h = new Harness();

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1), League("OG", OgId, 1)), new List<string> { "S88" }, Requester());

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Contains("OG", response.Message);
            Assert.Empty(h.Upserted); // not even the league of the client the user can see
            Assert.Empty(h.Logs());
        }

        [Fact]
        public async Task TheClientCodesOfTheUserAreMatchedIgnoringCase()
        {
            var h = new Harness();

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1)), new List<string> { "s88" }, Requester());

            Assert.Equal(StatusCode.Success, response.StatusCode);
        }

        [Fact]
        public async Task AUserWithNoClientCodes_CannotChangeAnything()
        {
            var h = new Harness();

            var none = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1)), new List<string>(), Requester());
            var missing = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1)), null, Requester());

            Assert.Equal(StatusCode.Error, none.StatusCode);
            Assert.Equal(StatusCode.Error, missing.StatusCode);
            Assert.Empty(h.Upserted);
        }

        [Fact]
        public async Task AnUnknownState_IsAnError()
        {
            var h = new Harness();

            var response = await h.Sut.SetConfigState((ConfigState)99, Request(false, League("S88", S88Id, 1)), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Error, response.StatusCode);
        }

        // ---- Failures ----------------------------------------------------------------------------------------

        [Fact]
        public async Task WhenTheSavedSettingsCannotBeRead_IsAnError_AndNothingIsSaved()
        {
            var h = new Harness();
            h.Trading.Setup(t => t.GetClientLeagueConfigRows(It.IsAny<List<string>>()))
                .ReturnsAsync(new BaseResponse<List<SavedLeagueRow>> { StatusCode = StatusCode.Error, Message = "Trading database is down" });

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1)), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal("Trading database is down", response.Message);
            Assert.Empty(h.Upserted);
        }

        [Fact]
        public async Task WhenTheSaveFails_TheErrorIsReturned_AndNothingIsLogged()
        {
            var h = new Harness();
            h.Trading.Setup(t => t.UpsertClientLeagueConfigRows(It.IsAny<List<SavedLeagueRow>>()))
                .ReturnsAsync(new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = "Violation of PRIMARY KEY" });

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1)), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal("Violation of PRIMARY KEY", response.Message);
            Assert.Empty(h.Logs());
        }

        [Fact]
        public async Task WhenOnlyTheActivityLogFails_TheChangeIsStillASuccess_AndSaysSo()
        {
            var h = new Harness();
            h.Context.Dispose(); // the log cannot be written, the Trading save does not use this context

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1)), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Contains("activity log could not be written", response.Message);
            Assert.Single(h.Upserted);
        }
    }
}
