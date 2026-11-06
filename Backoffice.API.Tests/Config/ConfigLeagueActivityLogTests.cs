using Backoffice.API.Models;
using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Translation;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
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
    // BO-155: the league changes made on Config > Assigned / Unassigned are written to the activity log (BO-151) and
    // read back by the Logs dialog of those pages. The rows must always go to the same page. They used to take the
    // page the web service had last set, which stays set for the whole session, so a league row could land among the
    // template rows of SBC.OE.Config, or under a translation page.
    public class ConfigLeagueActivityLogTests
    {
        private static readonly string[] AllClients = { "ALL" };
        private static readonly Guid S88Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private sealed class Harness
        {
            public Mock<ITradingRepository> Trading { get; } = new Mock<ITradingRepository>();
            public BackofficeContext Context { get; }
            public BackofficeRepository Sut { get; }

            public Harness(params SavedLeagueRow[] savedRows)
            {
                var name = Guid.NewGuid().ToString();
                Context = new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(name).Options);
                var translation = new TranslationContext(new DbContextOptionsBuilder<TranslationContext>().UseInMemoryDatabase(name).Options);

                Trading.Setup(t => t.GetClientLeagueConfigRows(It.IsAny<List<string>>()))
                    .ReturnsAsync(new BaseResponse<List<SavedLeagueRow>> { StatusCode = StatusCode.Success, Result = savedRows.ToList() });
                Trading.Setup(t => t.UpsertClientLeagueConfigRows(It.IsAny<List<SavedLeagueRow>>())).ReturnsAsync(Ok());
                Trading.Setup(t => t.DeleteClientLeagueConfigRows(It.IsAny<List<SavedLeagueRow>>())).ReturnsAsync(Ok());

                var rabbit = new Mock<IRabbitMqRepository>();
                rabbit.Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                    .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success });

                Sut = new BackofficeRepository(
                    Context,
                    translation,
                    new Mock<IPlatformRepository>().Object,
                    new Mock<ISportsbookRepository>().Object,
                    Trading.Object,
                    new Mock<IMemCache>().Object,
                    rabbit.Object);
            }
        }

        private static BaseResponse<bool> Ok() => new() { StatusCode = StatusCode.Success, Result = true };

        private static ConfigSetLeagueStateRequest Request(bool restricted) => new()
        {
            Restricted = restricted,
            Leagues = new List<ConfigClientModel>
            {
                new()
                {
                    ClientCode = "S88",
                    ClientId = S88Id.ToString(),
                    ClientName = "Song 88",
                    SportId = 1,
                    SportName = "Soccer",
                    SegmentId = 10,
                    SegmentName = "England",
                    LeagueId = 7,
                    LeagueName = "Premier League"
                }
            }
        };

        private static RequesterInfo Requester(string page) => new() { UserId = "u-1", UserName = "trader", Page = page };

        private static SavedLeagueRow Saved(bool isActive) => new()
        {
            ClientCode = "S88",
            ClientId = S88Id,
            SportId = 1,
            SegmentId = 10,
            LeagueId = 7,
            IsActive = isActive
        };

        [Fact]
        public void TheLeaguesPage_IsTheNameTheLogsDialogReads()
        {
            Assert.Equal("SBC.OE.Config.Leagues", BackofficeRepository.ConfigLeagueActivityLogPage);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Config")]
        [InlineData("SBC.OE.Config")]
        [InlineData("Translations.Dynamic")]
        [InlineData("Translations.Static")]
        public async Task AssigningALeague_IsLoggedOnTheLeaguesPage_WhateverPageTheCallerSent(string callerPage)
        {
            var h = new Harness();

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(restricted: false), AllClients.ToList(), Requester(callerPage));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            var log = Assert.Single(h.Context.ActivityLog);
            Assert.Equal("SBC.OE.Config.Leagues", log.Page);
            Assert.Equal("S88.1.10.7", log.Payload);
            Assert.Equal(Activity.Create.ToString(), log.Activity);
            Assert.Equal("Unassigned", log.From);
            Assert.Equal("Active", log.To);
            Assert.Equal("u-1", log.UserId);
            Assert.Equal("trader", log.UserName);
        }

        [Fact]
        public async Task ChangingTheState_IsLoggedOnTheLeaguesPage_FromTheOldStateToTheNew()
        {
            var h = new Harness(Saved(isActive: true));

            await h.Sut.SetConfigState(ConfigState.Assigned, Request(restricted: true), AllClients.ToList(), Requester("SBC.OE.Config"));

            var log = Assert.Single(h.Context.ActivityLog);
            Assert.Equal("SBC.OE.Config.Leagues", log.Page);
            Assert.Equal(Activity.Update.ToString(), log.Activity);
            Assert.Equal("Active", log.From);
            Assert.Equal("Restricted", log.To);
        }

        [Fact]
        public async Task UnassigningALeague_IsLoggedOnTheLeaguesPage_AsDeleteToUnassigned()
        {
            var h = new Harness(Saved(isActive: false));

            await h.Sut.SetConfigState(ConfigState.UnAssigned, Request(restricted: false), AllClients.ToList(), Requester(null));

            var log = Assert.Single(h.Context.ActivityLog);
            Assert.Equal("SBC.OE.Config.Leagues", log.Page);
            Assert.Equal(Activity.Delete.ToString(), log.Activity);
            Assert.Equal("Restricted", log.From);
            Assert.Equal("Unassigned", log.To);
        }

        [Fact]
        public async Task WhenNothingChanges_NothingIsLogged()
        {
            var h = new Harness(Saved(isActive: true));

            await h.Sut.SetConfigState(ConfigState.Assigned, Request(restricted: false), AllClients.ToList(), Requester("Config"));

            Assert.Empty(h.Context.ActivityLog);
        }
    }
}
