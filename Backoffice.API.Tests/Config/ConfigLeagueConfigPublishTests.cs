using Backoffice.API.Models;
using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Translation;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using SavedLeagueRow = Backoffice.Shared.Data.Trading.SQLEntities.ClientLeagueConfig;

namespace Backoffice.API.Tests.Config
{
    // BO-145: after the Config page saves league settings, Backoffice tells the odds engine which leagues changed
    // (OnLeagueConfig on r.uof.onleagueconfig), so it reads the saved settings again and updates the clients each league
    // is stamped with. The settings are saved first, so a message that cannot be sent never fails the save.
    public class ConfigLeagueConfigPublishTests
    {
        private static readonly string[] AllClients = { "ALL" };
        private static readonly Guid S88Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid OgId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private sealed class Harness
        {
            public Mock<ITradingRepository> Trading { get; } = new Mock<ITradingRepository>();
            public Mock<IRabbitMqRepository> Rabbit { get; } = new Mock<IRabbitMqRepository>();
            public List<MessageRequest> Published { get; } = new List<MessageRequest>();
            public BackofficeRepository Sut { get; }

            public Harness()
            {
                var name = Guid.NewGuid().ToString();
                var context = new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(name).Options);
                var translation = new TranslationContext(new DbContextOptionsBuilder<TranslationContext>().UseInMemoryDatabase(name).Options);

                Trading.Setup(t => t.GetClientLeagueConfigRows(It.IsAny<List<string>>()))
                    .ReturnsAsync(new BaseResponse<List<SavedLeagueRow>> { StatusCode = StatusCode.Success, Result = new List<SavedLeagueRow>() });
                Trading.Setup(t => t.UpsertClientLeagueConfigRows(It.IsAny<List<SavedLeagueRow>>())).ReturnsAsync(Ok());
                Trading.Setup(t => t.DeleteClientLeagueConfigRows(It.IsAny<List<SavedLeagueRow>>())).ReturnsAsync(Ok());

                Rabbit.Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                    .Callback<MessageRequest, string>((request, _) => Published.Add(request))
                    .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success });

                Sut = new BackofficeRepository(
                    context,
                    translation,
                    new Mock<IPlatformRepository>().Object,
                    new Mock<ISportsbookRepository>().Object,
                    Trading.Object,
                    new Mock<IMemCache>().Object,
                    Rabbit.Object);
            }

            public List<OnLeagueConfigMessage> Messages() => Published.Select(p => JsonSerializer.Deserialize<OnLeagueConfigMessage>(p.Payload)).ToList();
        }

        private static BaseResponse<bool> Ok() => new() { StatusCode = StatusCode.Success, Result = true };

        private static ConfigClientModel League(string client, Guid clientId, int league, int sport = 1, int segment = 10) => new()
        {
            ClientCode = client,
            ClientId = clientId.ToString(),
            ClientName = client,
            SportId = sport,
            SportName = "Soccer",
            SegmentId = segment,
            SegmentName = "England",
            LeagueId = league,
            LeagueName = $"League {league}"
        };

        private static ConfigSetLeagueStateRequest Request(bool restricted, params ConfigClientModel[] leagues) => new() { Restricted = restricted, Leagues = leagues.ToList() };

        private static RequesterInfo Requester() => new() { UserId = "u-1", UserName = "trader", Page = "Config" };

        [Fact]
        public async Task RestrictingALeague_PublishesOnLeagueConfig_WithTheLeagueAndIsActiveFalse()
        {
            var h = new Harness();

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 7)), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Success, response.StatusCode);

            var published = Assert.Single(h.Published);
            Assert.Equal("OnLeagueConfig", published.MessageType);
            Assert.Equal("r.uof.onleagueconfig", published.RoutingKey);
            Assert.False(string.IsNullOrWhiteSpace(published.CorrelationId));

            var message = Assert.Single(h.Messages());
            Assert.Equal((1, 10, 7), (message.SportId, message.SegmentId, message.LeagueId));
            Assert.Equal("S88", message.ClientCode);
            Assert.Equal(S88Id, message.ClientId);
            Assert.False(message.IsActive);
        }

        [Fact]
        public async Task AssigningAsActive_PublishesOneMessagePerLeague_WithIsActiveTrue()
        {
            var h = new Harness();

            await h.Sut.SetConfigState(ConfigState.Assigned, Request(false, League("S88", S88Id, 1), League("S88", S88Id, 2)), AllClients.ToList(), Requester());

            var messages = h.Messages();
            Assert.Equal(new[] { 1, 2 }, messages.Select(m => m.LeagueId).OrderBy(i => i));
            Assert.All(messages, m => Assert.True(m.IsActive));
        }

        [Fact]
        public async Task TheSameLeagueForSeveralClients_IsPublishedOnce()
        {
            var h = new Harness();

            await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1), League("OG", OgId, 1)), AllClients.ToList(), Requester());

            // the odds engine reads every client's saved setting for the league again, so one message names it
            var message = Assert.Single(h.Messages());
            Assert.Equal(1, message.LeagueId);
        }

        [Fact]
        public async Task LeaguesOfDifferentSportsAndSegments_AreDistinct()
        {
            var h = new Harness();

            await h.Sut.SetConfigState(ConfigState.Assigned,
                Request(true, League("S88", S88Id, 1, sport: 1, segment: 10), League("S88", S88Id, 1, sport: 2, segment: 10), League("S88", S88Id, 1, sport: 1, segment: 11)),
                AllClients.ToList(), Requester());

            Assert.Equal(3, h.Published.Count);
        }

        [Fact]
        public async Task Unassigning_PublishesOnLeagueConfig_WithIsActiveTrue_BecauseTheLeagueIsVisibleAgain()
        {
            var h = new Harness();

            await h.Sut.SetConfigState(ConfigState.UnAssigned, Request(true, League("S88", S88Id, 1)), AllClients.ToList(), Requester());

            var message = Assert.Single(h.Messages());
            Assert.True(message.IsActive);
        }

        [Fact]
        public async Task NothingIsPublished_WhenTheSaveFails()
        {
            var h = new Harness();
            h.Trading.Setup(t => t.UpsertClientLeagueConfigRows(It.IsAny<List<SavedLeagueRow>>()))
                .ReturnsAsync(new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = "write failed" });

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1)), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Empty(h.Published);
        }

        [Fact]
        public async Task NothingIsPublished_WhenTheRequestIsRefused()
        {
            var h = new Harness();

            var noLeagues = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true), AllClients.ToList(), Requester());
            var notAllowed = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("OG", OgId, 1)), new List<string> { "S88" }, Requester());

            Assert.Equal(StatusCode.Error, noLeagues.StatusCode);
            Assert.Equal(StatusCode.Error, notAllowed.StatusCode);
            Assert.Empty(h.Published);
        }

        [Fact]
        public async Task WhenThePublishThrows_TheSaveStillSucceeds_AndTheOtherLeaguesAreStillPublished()
        {
            var h = new Harness();
            var calls = 0;
            h.Rabbit.Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                .Returns<MessageRequest, string>((request, _) =>
                {
                    calls++;

                    if (calls == 1)
                        throw new InvalidOperationException("broker down");

                    h.Published.Add(request);
                    return Task.FromResult(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success });
                });

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1), League("S88", S88Id, 2)), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.True(response.Result);
            Assert.Equal(2, calls);
            Assert.Single(h.Published);
        }

        [Fact]
        public async Task WhenThePublishIsRefused_TheSaveStillSucceeds()
        {
            var h = new Harness();
            h.Rabbit.Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Error, Message = "refused" });

            var response = await h.Sut.SetConfigState(ConfigState.Assigned, Request(true, League("S88", S88Id, 1)), AllClients.ToList(), Requester());

            Assert.Equal(StatusCode.Success, response.StatusCode);
            h.Trading.Verify(t => t.UpsertClientLeagueConfigRows(It.IsAny<List<SavedLeagueRow>>()), Times.Once);
        }
    }
}
