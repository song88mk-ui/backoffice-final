using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Trading;
using Backoffice.Shared.Data.Trading.SQLEntities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OE.Shared.Data.Sportsbook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.Config
{
    // BO-144: the saved league settings of a client (Trading.dbo.ClientLeagueConfig), which decide which leagues are
    // unassigned on the OE Config page. A row with IsActive = 0 restricts the league for that client.
    public class TradingRepositoryLeagueConfigTests
    {
        private static (TradingRepository Sut, TradingContext Context) Build()
        {
            var context = new TradingContext(new DbContextOptionsBuilder<TradingContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

            var configuration = new Mock<IConfiguration>();
            configuration.Setup(c => c["Custom:MongoDB-Sportsbook"]).Returns("mongodb://localhost:27017");

            var sut = new TradingRepository(
                configuration.Object,
                new Mock<IBackofficeActivityLogRepository>().Object,
                new Mock<IMemCache>().Object,
                NullLogger<TradingRepository>.Instance,
                new Mock<IRabbitMqRepository>().Object,
                context,
                new SportsbookContext(new DbContextOptionsBuilder<SportsbookContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options),
                new Mock<IPlatformRepository>().Object);

            return (sut, context);
        }

        private static ClientLeagueConfig ARow(string client, int league, bool isActive) => new()
        {
            ClientCode = client,
            ClientId = Guid.NewGuid(),
            SportId = 1,
            SegmentId = 10,
            LeagueId = league,
            IsActive = isActive
        };

        [Fact]
        public async Task ReturnsTheRowsOfTheRequestedClientsOnly()
        {
            var (sut, context) = Build();
            context.ClientLeagueConfig.AddRange(ARow("S88", 1, false), ARow("S88", 2, true), ARow("OG", 1, false), ARow("QTE", 3, false));
            await context.SaveChangesAsync();

            var response = await sut.GetClientLeagueConfigRows(new List<string> { "S88", "OG" });

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(new[] { "OG:1", "S88:1", "S88:2" }, response.Result.Select(r => $"{r.ClientCode}:{r.LeagueId}").OrderBy(s => s));
        }

        [Fact]
        public async Task KeepsTheRestrictedAndTheAllowedState()
        {
            var (sut, context) = Build();
            context.ClientLeagueConfig.AddRange(ARow("S88", 1, false), ARow("S88", 2, true));
            await context.SaveChangesAsync();

            var response = await sut.GetClientLeagueConfigRows(new List<string> { "S88" });

            Assert.False(response.Result.Single(r => r.LeagueId == 1).IsActive);
            Assert.True(response.Result.Single(r => r.LeagueId == 2).IsActive);
        }

        [Fact]
        public async Task NoClientsOrNoRows_IsAnEmptySuccess_NotAnError()
        {
            var (sut, context) = Build();
            context.ClientLeagueConfig.Add(ARow("S88", 1, false));
            await context.SaveChangesAsync();

            var none = await sut.GetClientLeagueConfigRows(new List<string>());
            var missing = await sut.GetClientLeagueConfigRows(null);
            var otherClient = await sut.GetClientLeagueConfigRows(new List<string> { "OG" });

            Assert.All(new[] { none, missing, otherClient }, r =>
            {
                Assert.Equal(StatusCode.Success, r.StatusCode);
                Assert.Empty(r.Result);
            });
        }

        [Fact]
        public async Task WhenTheDatabaseFails_IsAnError_WithTheReason()
        {
            var (sut, context) = Build();
            context.Dispose(); // every query on it now throws, like a missing table or an unreachable server

            var response = await sut.GetClientLeagueConfigRows(new List<string> { "S88" });

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(response.Message));
            Assert.Null(response.Result);
        }

        // ---- BO-151: assigning and unassigning ----------------------------------------------------------------

        [Fact]
        public async Task Upsert_AddsARowForEachNewLeague()
        {
            var (sut, context) = Build();

            var response = await sut.UpsertClientLeagueConfigRows(new List<ClientLeagueConfig> { ARow("S88", 1, true), ARow("S88", 2, false), ARow("OG", 1, false) });

            Assert.Equal(StatusCode.Success, response.StatusCode);
            var saved = context.ClientLeagueConfig.AsNoTracking().ToList();
            Assert.Equal(new[] { "OG:1:False", "S88:1:True", "S88:2:False" }, saved.Select(r => $"{r.ClientCode}:{r.LeagueId}:{r.IsActive}").OrderBy(s => s));
        }

        [Fact]
        public async Task Upsert_AssigningALeagueAgain_ChangesItsState_AndNeverAddsASecondRow()
        {
            var (sut, context) = Build();
            context.ClientLeagueConfig.Add(ARow("S88", 1, true));
            await context.SaveChangesAsync();

            var newClientId = Guid.NewGuid();
            var restricted = ARow("S88", 1, false);
            restricted.ClientId = newClientId;
            var response = await sut.UpsertClientLeagueConfigRows(new List<ClientLeagueConfig> { restricted });

            Assert.Equal(StatusCode.Success, response.StatusCode);
            var row = Assert.Single(context.ClientLeagueConfig.AsNoTracking().ToList());
            Assert.False(row.IsActive);
            Assert.Equal(newClientId, row.ClientId);
        }

        [Fact]
        public async Task Upsert_TheSameLeagueTwiceInOneRequest_IsOneRow_AndTheLastOneWins()
        {
            var (sut, context) = Build();

            await sut.UpsertClientLeagueConfigRows(new List<ClientLeagueConfig> { ARow("S88", 1, true), ARow("s88", 1, false) });

            var row = Assert.Single(context.ClientLeagueConfig.AsNoTracking().ToList());
            Assert.False(row.IsActive);
        }

        [Fact]
        public async Task Upsert_LeavesTheOtherRowsAlone()
        {
            var (sut, context) = Build();
            context.ClientLeagueConfig.AddRange(ARow("S88", 1, true), ARow("S88", 2, false), ARow("OG", 1, false));
            await context.SaveChangesAsync();

            await sut.UpsertClientLeagueConfigRows(new List<ClientLeagueConfig> { ARow("S88", 1, false) });

            var saved = context.ClientLeagueConfig.AsNoTracking().ToList();
            Assert.Equal(3, saved.Count);
            Assert.True(saved.Single(r => r.ClientCode == "OG").IsActive == false);
            Assert.False(saved.Single(r => r.ClientCode == "S88" && r.LeagueId == 2).IsActive);
        }

        [Fact]
        public async Task Upsert_NothingToSave_IsASuccess()
        {
            var (sut, _) = Build();

            Assert.Equal(StatusCode.Success, (await sut.UpsertClientLeagueConfigRows(new List<ClientLeagueConfig>())).StatusCode);
            Assert.Equal(StatusCode.Success, (await sut.UpsertClientLeagueConfigRows(null)).StatusCode);
        }

        [Fact]
        public async Task Delete_RemovesOnlyTheGivenLeagues()
        {
            var (sut, context) = Build();
            context.ClientLeagueConfig.AddRange(ARow("S88", 1, false), ARow("S88", 2, true), ARow("OG", 1, false));
            await context.SaveChangesAsync();

            var response = await sut.DeleteClientLeagueConfigRows(new List<ClientLeagueConfig> { ARow("S88", 1, true) }); // the state in the request does not matter

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(new[] { "OG:1", "S88:2" }, context.ClientLeagueConfig.AsNoTracking().ToList().Select(r => $"{r.ClientCode}:{r.LeagueId}").OrderBy(s => s));
        }

        [Fact]
        public async Task Delete_ALeagueThatIsNotAssigned_IsASuccess()
        {
            var (sut, context) = Build();
            context.ClientLeagueConfig.Add(ARow("S88", 2, true));
            await context.SaveChangesAsync();

            var response = await sut.DeleteClientLeagueConfigRows(new List<ClientLeagueConfig> { ARow("S88", 1, true), ARow("QTE", 1, true) });

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Single(context.ClientLeagueConfig.AsNoTracking().ToList());
        }

        [Fact]
        public async Task UpsertAndDelete_WhenTheDatabaseFails_AreAnError_WithTheReason()
        {
            var (sut, context) = Build();
            context.Dispose();

            var upsert = await sut.UpsertClientLeagueConfigRows(new List<ClientLeagueConfig> { ARow("S88", 1, true) });
            var delete = await sut.DeleteClientLeagueConfigRows(new List<ClientLeagueConfig> { ARow("S88", 1, true) });

            Assert.Equal(StatusCode.Error, upsert.StatusCode);
            Assert.Equal(StatusCode.Error, delete.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(upsert.Message));
        }
    }
}
