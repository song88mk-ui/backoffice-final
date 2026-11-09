using Backoffice.API.Repositories;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using OE.Shared.Data.Sportsbook;
using OE.Shared.Data.Sportsbook.SQLEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.SportsbookClients
{
    // BO-158: Market Groups Copy to several clients. The Trading API copy (one target per call) is replaced by
    // a fake here, so these cover the fan-out: one call and one cache message per target, partial failures,
    // the previous groups read for the activity log, and the requests rejected before anything is copied.
    public class ClientMarketTemplateGroupCopyRepositoryTests
    {
        private const string CacheRoutingKey = "r.oe.cachesync.onmarketsortorderchange";
        private const int Football = 1;
        private const int Basketball = 2;
        private const int Prematch = (int)FixtureStatus.Prematch;
        private const int Live = (int)FixtureStatus.Live;

        private static readonly Guid ClientAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ClientBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid ClientCId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private sealed class FakeTradingCopyRepository : SportsbookRepository
        {
            private readonly Func<ClientCopyMarketTemplateGroupRequest, BaseResponse<ClientMarketTemplateGroupResponse>> _reply;

            public List<(ClientCopyMarketTemplateGroupRequest Request, string Region)> Calls { get; } = new();

            public FakeTradingCopyRepository(
                Func<ClientCopyMarketTemplateGroupRequest, BaseResponse<ClientMarketTemplateGroupResponse>> reply,
                SportsbookContext context,
                IConfiguration configuration,
                IRabbitMqRepository rabbitMqRepository)
                : base(context, configuration, new Mock<IPlatformRepository>().Object, new Mock<IBackofficeActivityLogRepository>().Object, rabbitMqRepository, new Mock<IMemCache>().Object)
            {
                _reply = reply;
            }

            protected override Task<BaseResponse<ClientMarketTemplateGroupResponse>> PostCopyClientMarketTemplateGroup(ClientCopyMarketTemplateGroupRequest request, string region)
            {
                Calls.Add((request, region));
                return Task.FromResult(_reply(request));
            }
        }

        private static SportsbookContext BuildContext()
        {
            var opts = new DbContextOptionsBuilder<SportsbookContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new SportsbookContext(opts);
        }

        private static FakeTradingCopyRepository BuildSut(
            SportsbookContext context,
            List<MessageRequest> published,
            Func<ClientCopyMarketTemplateGroupRequest, BaseResponse<ClientMarketTemplateGroupResponse>> reply = null,
            Exception publishFailure = null)
        {
            var rabbitMq = new Mock<IRabbitMqRepository>();
            var publish = rabbitMq.Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()));

            if (publishFailure != null)
                publish.ThrowsAsync(publishFailure);
            else
                publish.Callback<MessageRequest, string>((request, _) => published.Add(request));

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Custom:MongoDB-Sportsbook"] = "mongodb://localhost:27017"
                })
                .Build();

            return new FakeTradingCopyRepository(reply ?? Copied, context, configuration, rabbitMq.Object);
        }

        // The Trading API's success reply: the target's groups after the copy.
        private static BaseResponse<ClientMarketTemplateGroupResponse> Copied(ClientCopyMarketTemplateGroupRequest request) =>
            new BaseResponse<ClientMarketTemplateGroupResponse>
            {
                StatusCode = StatusCode.Success,
                Message = "Success",
                Result = new ClientMarketTemplateGroupResponse
                {
                    ClientId = request.CopyToClientId,
                    SportId = request.CopyToSportId,
                    FixtureStatusId = request.CopyToFixtureStatusId,
                    ClientMarketTemplateGroups = new List<ClientMarketTemplateGroupDTO>
                    {
                        new ClientMarketTemplateGroupDTO { MarketTemplateGroupId = 900, Name = "Main", SortOrder = 1, MarketTemplateIds = "1,2", IsActive = true, FixtureStatusId = request.CopyToFixtureStatusId },
                        new ClientMarketTemplateGroupDTO { MarketTemplateGroupId = 901, Name = "Goals", SortOrder = 2, MarketTemplateIds = "3", IsActive = false, FixtureStatusId = request.CopyToFixtureStatusId },
                    }
                }
            };

        private static BaseResponse<ClientMarketTemplateGroupResponse> Failed(string message) =>
            new BaseResponse<ClientMarketTemplateGroupResponse> { StatusCode = StatusCode.Error, Message = message };

        private static ClientMarketTemplateGroup Group(Guid clientId, int id, string name, int sortOrder, int sportId = Football, int fixtureStatusId = Prematch) =>
            new ClientMarketTemplateGroup
            {
                ClientId = clientId,
                SportId = sportId,
                FixtureStatusId = fixtureStatusId,
                Id = id,
                Name = name,
                SortOrder = sortOrder,
                IsActive = true,
                MarketTemplates = $"{id}"
            };

        // Client A has two Football Prematch groups to copy; B has one of its own that the copy replaces.
        private static SportsbookContext Seeded()
        {
            var context = BuildContext();
            context.ClientMarketTemplateGroup.AddRange(
                Group(ClientAId, 1, "Main", 1),
                Group(ClientAId, 2, "Goals", 2),
                Group(ClientBId, 10, "B Old Second", 2),
                Group(ClientBId, 11, "B Old First", 1),
                Group(ClientBId, 12, "B Live", 1, fixtureStatusId: Live),
                Group(ClientBId, 13, "B Basketball", 1, sportId: Basketball));
            context.SaveChanges();
            return context;
        }

        private static ClientCopyMarketTemplateGroupRequest CopyRequest(Guid source, params Guid[] targets) =>
            new ClientCopyMarketTemplateGroupRequest
            {
                ClientId = source,
                SportId = Football,
                FixtureStatusId = Prematch,
                CopyToClientIds = targets.ToList(),
                CopyToSportId = Football,
                CopyToFixtureStatusId = Prematch
            };

        private static Guid CacheClientId(MessageRequest message) =>
            JsonSerializer.Deserialize<OnDeleteCacheMessage>(message.Payload).ClientId;

        // ---------------------------------------------------------------
        // Copy to several clients
        // ---------------------------------------------------------------

        [Fact]
        public async Task Copy_ToTwoClients_CallsTradingOncePerTarget()
        {
            var published = new List<MessageRequest>();
            var sut = BuildSut(Seeded(), published);

            var response = await sut.CopyClientMarketTemplateGroup(CopyRequest(ClientAId, ClientBId, ClientCId), "HK");

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(new[] { ClientBId, ClientCId }, sut.Calls.Select(c => c.Request.CopyToClientId));
            Assert.All(sut.Calls, c =>
            {
                Assert.Equal(ClientAId, c.Request.ClientId);
                Assert.Equal(Football, c.Request.SportId);
                Assert.Equal(Prematch, c.Request.FixtureStatusId);
                Assert.Equal(Football, c.Request.CopyToSportId);
                Assert.Equal(Prematch, c.Request.CopyToFixtureStatusId);
                Assert.Empty(c.Request.CopyToClientIds);
                Assert.Equal("HK", c.Region);
            });
        }

        [Fact]
        public async Task Copy_ToTwoClients_PublishesOneCacheMessagePerTarget()
        {
            var published = new List<MessageRequest>();
            var sut = BuildSut(Seeded(), published);

            await sut.CopyClientMarketTemplateGroup(CopyRequest(ClientAId, ClientBId, ClientCId), "HK");

            Assert.Equal(2, published.Count);
            Assert.All(published, m =>
            {
                Assert.Equal(CacheRoutingKey, m.RoutingKey);
                Assert.Equal("OnMarketSortOrderChange", m.MessageType);
            });
            Assert.Equal(new[] { ClientBId, ClientCId }, published.Select(CacheClientId));
        }

        [Fact]
        public async Task Copy_ReturnsEachTargetsPreviousAndCopiedGroups()
        {
            var sut = BuildSut(Seeded(), new List<MessageRequest>());

            var response = await sut.CopyClientMarketTemplateGroup(CopyRequest(ClientAId, ClientBId, ClientCId), "HK");

            var b = response.Result.Targets.Single(t => t.ClientId == ClientBId);
            var c = response.Result.Targets.Single(t => t.ClientId == ClientCId);

            Assert.Equal(StatusCode.Success, b.StatusCode);
            // Only B's Football Prematch groups, in sort order; its Live and Basketball groups are not part of the copy.
            Assert.Equal(new[] { "B Old First", "B Old Second" }, b.PreviousGroups.Select(g => g.Name));
            Assert.Equal(new[] { 11, 10 }, b.PreviousGroups.Select(g => g.MarketTemplateGroupId));
            Assert.Equal("11", b.PreviousGroups[0].MarketTemplateIds);
            Assert.Equal(new[] { "Main", "Goals" }, b.CopiedGroups.Select(g => g.Name));

            Assert.Empty(c.PreviousGroups);
            Assert.Equal(2, c.CopiedGroups.Count);
        }

        [Fact]
        public async Task Copy_DuplicateAndEmptyTargets_CopiesEachClientOnce()
        {
            var published = new List<MessageRequest>();
            var sut = BuildSut(Seeded(), published);

            var response = await sut.CopyClientMarketTemplateGroup(CopyRequest(ClientAId, ClientBId, Guid.Empty, ClientBId), "HK");

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Single(sut.Calls);
            Assert.Single(published);
            Assert.Single(response.Result.Targets);
        }

        [Fact]
        public async Task Copy_LegacySingleTarget_StillCopies()
        {
            var published = new List<MessageRequest>();
            var sut = BuildSut(Seeded(), published);

            var request = CopyRequest(ClientAId);
            request.CopyToClientIds = null;
            request.CopyToClientId = ClientBId;

            var response = await sut.CopyClientMarketTemplateGroup(request, "HK");

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(ClientBId, Assert.Single(sut.Calls).Request.CopyToClientId);
            Assert.Equal(ClientBId, CacheClientId(Assert.Single(published)));
        }

        // ---------------------------------------------------------------
        // Partial failure
        // ---------------------------------------------------------------

        [Fact]
        public async Task Copy_OneTargetFails_OthersAreStillCopiedAndReported()
        {
            var published = new List<MessageRequest>();
            var sut = BuildSut(Seeded(), published, r => r.CopyToClientId == ClientBId ? Failed("Error") : Copied(r));

            var response = await sut.CopyClientMarketTemplateGroup(CopyRequest(ClientAId, ClientBId, ClientCId), "HK");

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal("Market groups were copied to some clients only.", response.Message);
            Assert.Equal(2, sut.Calls.Count);

            var b = response.Result.Targets.Single(t => t.ClientId == ClientBId);
            var c = response.Result.Targets.Single(t => t.ClientId == ClientCId);
            Assert.Equal(StatusCode.Error, b.StatusCode);
            Assert.Equal("Error", b.Message);
            Assert.Empty(b.CopiedGroups);
            Assert.Equal(StatusCode.Success, c.StatusCode);

            // Only the copied client's cache is refreshed.
            Assert.Equal(ClientCId, CacheClientId(Assert.Single(published)));
        }

        [Fact]
        public async Task Copy_TradingThrowsForOneTarget_OthersAreStillCopied()
        {
            var published = new List<MessageRequest>();
            var sut = BuildSut(Seeded(), published, r => r.CopyToClientId == ClientBId ? throw new InvalidOperationException("boom") : Copied(r));

            var response = await sut.CopyClientMarketTemplateGroup(CopyRequest(ClientAId, ClientBId, ClientCId), "HK");

            var b = response.Result.Targets.Single(t => t.ClientId == ClientBId);
            Assert.Equal(StatusCode.Error, b.StatusCode);
            Assert.Contains("boom", b.Message);
            Assert.Equal(StatusCode.Success, response.Result.Targets.Single(t => t.ClientId == ClientCId).StatusCode);
            Assert.Equal(ClientCId, CacheClientId(Assert.Single(published)));
        }

        [Fact]
        public async Task Copy_CacheMessageFails_TargetStillCountsAsCopied()
        {
            var sut = BuildSut(Seeded(), new List<MessageRequest>(), publishFailure: new InvalidOperationException("rabbit down"));

            var response = await sut.CopyClientMarketTemplateGroup(CopyRequest(ClientAId, ClientBId), "HK");

            Assert.Equal(StatusCode.Success, response.StatusCode);
            var b = Assert.Single(response.Result.Targets);
            Assert.Equal(StatusCode.Success, b.StatusCode);
            Assert.Equal("Copied, but the market group cache could not be refreshed.", b.Message);
        }

        // ---------------------------------------------------------------
        // Rejected before anything is copied
        // ---------------------------------------------------------------

        private static async Task AssertRejected(Func<ClientCopyMarketTemplateGroupRequest, ClientCopyMarketTemplateGroupRequest> change, string message, SportsbookContext context = null)
        {
            var published = new List<MessageRequest>();
            var sut = BuildSut(context ?? Seeded(), published);

            var response = await sut.CopyClientMarketTemplateGroup(change(CopyRequest(ClientAId, ClientBId)), "HK");

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal(message, response.Message);
            Assert.Empty(response.Result.Targets);
            Assert.Empty(sut.Calls);
            Assert.Empty(published);
        }

        [Fact]
        public Task Copy_NoTargets_IsRejected() =>
            AssertRejected(r => { r.CopyToClientIds = new List<Guid>(); return r; }, "No target client selected");

        [Fact]
        public Task Copy_SourceAmongTargets_IsRejected() =>
            AssertRejected(r => { r.CopyToClientIds.Add(ClientAId); return r; }, "Source client cannot be a copy target");

        [Fact]
        public Task Copy_NoSourceClient_IsRejected() =>
            AssertRejected(r => { r.ClientId = Guid.Empty; return r; }, "Invalid Source Client");

        [Fact]
        public Task Copy_NoSport_IsRejected() =>
            AssertRejected(r => { r.CopyToSportId = 0; return r; }, "Invalid Sport");

        [Theory]
        [InlineData(-1)]
        [InlineData((int)FixtureStatus.Suspended)]
        public Task Copy_ScopeOtherThanPrematchOrLive_IsRejected(int fixtureStatusId) =>
            AssertRejected(r => { r.FixtureStatusId = fixtureStatusId; return r; }, "Invalid Scope");

        [Fact]
        public Task Copy_SourceWithoutGroups_IsRejectedSoTargetsAreNotWiped() =>
            // A has groups for Football Prematch only, so its Football Live has nothing to copy.
            AssertRejected(r => { r.FixtureStatusId = Live; r.CopyToFixtureStatusId = Live; return r; }, "Source client has no market groups to copy");
    }
}
