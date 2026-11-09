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
    // BO-159: Market Sort Order copy to several clients.
    public class ClientMarketSortOrderCopyRepositoryTests
    {
        private const string RoutingKey = "r.oe.cachesync.onmarketsortorderchange";
        private const int Football = 1;
        private const int Basketball = 2;
        private const int PreMatch = 0;
        private const int Live = 1;

        private static readonly Guid ClientAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ClientBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid ClientCId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid UnknownId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static SportsbookContext BuildContext()
        {
            var opts = new DbContextOptionsBuilder<SportsbookContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new SportsbookContext(opts);
        }

        private sealed class Sut
        {
            public SportsbookRepository Repository;
            public List<MessageRequest> Published = new List<MessageRequest>();
            public List<OnActivityLogMessage> AuditLogs = new List<OnActivityLogMessage>();
        }

        private static Sut BuildSut(SportsbookContext context, Guid? failingClientId = null, Exception publishFailure = null, Exception auditFailure = null)
        {
            var sut = new Sut();

            var platform = new Mock<IPlatformRepository>();
            platform
                .Setup(p => p.GetClients(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<List<PlatformClientResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<PlatformClientResponse>
                    {
                        new PlatformClientResponse { ClientId = ClientAId, ClientCode = "QTE-A", ClientName = "Client A" },
                        new PlatformClientResponse { ClientId = ClientBId, ClientCode = "QTE-B", ClientName = "Client B" },
                        new PlatformClientResponse { ClientId = ClientCId, ClientCode = "QTE-C", ClientName = "Client C" },
                    }
                });

            var rabbitMq = new Mock<IRabbitMqRepository>();
            var publishSetup = rabbitMq.Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()));
            if (publishFailure != null)
                publishSetup.ThrowsAsync(publishFailure);
            else
                publishSetup
                    .Callback<MessageRequest, string>((request, _) => sut.Published.Add(request));

            var activityLog = new Mock<IBackofficeActivityLogRepository>();
            var logSetup = activityLog.Setup(a => a.ProcessActivityLog(It.IsAny<OnActivityLogListMessage>(), It.IsAny<string>()));
            if (auditFailure != null)
                logSetup.ThrowsAsync(auditFailure);
            else
                logSetup
                    .Callback<OnActivityLogListMessage, string>((message, _) => sut.AuditLogs.AddRange(message.OnActivityLogMessages))
                    .Returns(Task.CompletedTask);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Custom:MongoDB-Sportsbook"] = "mongodb://localhost:27017"
                })
                .Build();

            sut.Repository = new InMemoryGroupsSportsbookRepository(
                failingClientId, context, configuration, platform.Object, activityLog.Object, rabbitMq.Object, new Mock<IMemCache>().Object);

            return sut;
        }

        // The real group insert is raw SQL (IDENTITY Id), which EF InMemory cannot run.
        private sealed class InMemoryGroupsSportsbookRepository : SportsbookRepository
        {
            private readonly Guid? _failingClientId;
            private readonly SportsbookContext _context;

            public InMemoryGroupsSportsbookRepository(Guid? failingClientId, SportsbookContext context, IConfiguration configuration,
                IPlatformRepository platformRepository, IBackofficeActivityLogRepository activityLogRepository,
                IRabbitMqRepository rabbitMqRepository, IMemCache memCache)
                : base(context, configuration, platformRepository, activityLogRepository, rabbitMqRepository, memCache)
            {
                _failingClientId = failingClientId;
                _context = context;
            }

            protected override async Task InsertClientMarketTemplateGroups(Guid clientId, int sportId, int fixtureStatusId, List<ClientMarketTemplateGroup> groups)
            {
                if (clientId == _failingClientId)
                    throw new InvalidOperationException("database unavailable");

                var nextId = _context.ClientMarketTemplateGroup.AsNoTracking().Select(g => g.Id).AsEnumerable().DefaultIfEmpty(0).Max() + 1;

                foreach (var group in groups)
                {
                    _context.ClientMarketTemplateGroup.Add(new ClientMarketTemplateGroup
                    {
                        ClientId = clientId,
                        SportId = sportId,
                        FixtureStatusId = fixtureStatusId,
                        Id = nextId++,
                        Name = group.Name,
                        SortOrder = group.SortOrder,
                        IsActive = group.IsActive,
                        MarketTemplates = group.MarketTemplates
                    });
                }

                await _context.SaveChangesAsync();
            }
        }

        private static int _groupId;

        private static void SeedOrder(SportsbookContext context, Guid clientId, int sportId, params string[] marketTemplateIds)
        {
            for (var i = 0; i < marketTemplateIds.Length; i++)
                context.ClientMarketTemplateOrder.Add(new ClientMarketTemplateOrder { ClientId = clientId, SportId = sportId, MarketTemplateId = marketTemplateIds[i], SortOrder = i + 1 });
            context.SaveChanges();
        }

        private static void SeedGroup(SportsbookContext context, Guid clientId, int sportId, int fixtureStatusId, string name, int sortOrder, string marketTemplates)
        {
            context.ClientMarketTemplateGroup.Add(new ClientMarketTemplateGroup
            {
                ClientId = clientId,
                SportId = sportId,
                FixtureStatusId = fixtureStatusId,
                Id = System.Threading.Interlocked.Increment(ref _groupId) + 1000,
                Name = name,
                SortOrder = sortOrder,
                IsActive = true,
                MarketTemplates = marketTemplates
            });
            context.SaveChanges();
        }

        // Source: client A, Football. Order m3, m1, m2; Pre-match groups "Main" and "Goals".
        private static void SeedSource(SportsbookContext context)
        {
            SeedOrder(context, ClientAId, Football, "m3", "m1", "m2");
            SeedGroup(context, ClientAId, Football, PreMatch, "Main", 1, "m3,m1");
            SeedGroup(context, ClientAId, Football, PreMatch, "Goals", 2, "m2");
        }

        private static CopyClientMarketSortOrderRequest Request(int toSport, int toScope, params Guid[] targets) => new CopyClientMarketSortOrderRequest
        {
            CopyFromClientId = ClientAId,
            CopyFromSportId = Football,
            CopyFromFixtureStatusId = PreMatch,
            CopyToClientIds = targets.ToList(),
            CopyToSportId = toSport,
            CopyToFixtureStatusId = toScope,
            UserId = "user-1",
            UserName = "Tester"
        };

        private static List<string> OrderOf(SportsbookContext context, Guid clientId, int sportId) =>
            context.ClientMarketTemplateOrder.AsNoTracking()
                .Where(o => o.ClientId == clientId && o.SportId == sportId)
                .OrderBy(o => o.SortOrder)
                .Select(o => o.MarketTemplateId)
                .ToList();

        private static List<string> GroupsOf(SportsbookContext context, Guid clientId, int sportId, int fixtureStatusId) =>
            context.ClientMarketTemplateGroup.AsNoTracking()
                .Where(g => g.ClientId == clientId && g.SportId == sportId && g.FixtureStatusId == fixtureStatusId)
                .OrderBy(g => g.SortOrder)
                .Select(g => $"{g.Name}:{g.MarketTemplates}")
                .ToList();

        private static OnDeleteCacheMessage PayloadOf(MessageRequest message) =>
            JsonSerializer.Deserialize<OnDeleteCacheMessage>(message.Payload);

        // ---------------------------------------------------------------
        // Multi-target copy
        // ---------------------------------------------------------------

        [Fact]
        public async Task Copy_ToSeveralClients_ReplacesEachTargetsOrderAndGroups()
        {
            using var context = BuildContext();
            SeedSource(context);
            SeedOrder(context, ClientBId, Football, "m9");
            SeedGroup(context, ClientBId, Football, PreMatch, "Old", 1, "m9");
            var sut = BuildSut(context);

            var response = await sut.Repository.CopyClientMarketTemplatesOrder(Request(Football, PreMatch, ClientBId, ClientCId));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(new[] { ClientBId, ClientCId }, response.Result.Targets.Select(t => t.ClientId));
            Assert.All(response.Result.Targets, t => Assert.Equal(StatusCode.Success, t.StatusCode));

            foreach (var target in new[] { ClientBId, ClientCId })
            {
                Assert.Equal(new[] { "m3", "m1", "m2" }, OrderOf(context, target, Football));
                Assert.Equal(new[] { "Main:m3,m1", "Goals:m2" }, GroupsOf(context, target, Football, PreMatch));
            }

            // The source is untouched.
            Assert.Equal(new[] { "m3", "m1", "m2" }, OrderOf(context, ClientAId, Football));
            Assert.Equal(new[] { "Main:m3,m1", "Goals:m2" }, GroupsOf(context, ClientAId, Football, PreMatch));
        }

        [Fact]
        public async Task Copy_LeavesOtherSportsAndOtherScopesGroupsOfTheTargetUnchanged()
        {
            using var context = BuildContext();
            SeedSource(context);
            SeedOrder(context, ClientBId, Basketball, "b1", "b2");
            SeedGroup(context, ClientBId, Basketball, PreMatch, "Basket", 1, "b1");
            SeedGroup(context, ClientBId, Football, Live, "LiveGroup", 1, "m7");
            var sut = BuildSut(context);

            await sut.Repository.CopyClientMarketTemplatesOrder(Request(Football, PreMatch, ClientBId));

            Assert.Equal(new[] { "b1", "b2" }, OrderOf(context, ClientBId, Basketball));
            Assert.Equal(new[] { "Basket:b1" }, GroupsOf(context, ClientBId, Basketball, PreMatch));
            Assert.Equal(new[] { "LiveGroup:m7" }, GroupsOf(context, ClientBId, Football, Live));
        }

        [Fact]
        public async Task Copy_ToTheSourceClientForAnotherScope_IsAllowed()
        {
            using var context = BuildContext();
            SeedSource(context);
            var sut = BuildSut(context);

            var response = await sut.Repository.CopyClientMarketTemplatesOrder(Request(Football, Live, ClientAId, ClientBId));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(new[] { "Main:m3,m1", "Goals:m2" }, GroupsOf(context, ClientAId, Football, Live));
            Assert.Equal(new[] { "Main:m3,m1", "Goals:m2" }, GroupsOf(context, ClientAId, Football, PreMatch));
            Assert.Equal(new[] { "m3", "m1", "m2" }, OrderOf(context, ClientAId, Football));
        }

        [Fact]
        public async Task Copy_PublishesOneMarketSortOrderChangePerTarget_WithTheTargetsCode()
        {
            using var context = BuildContext();
            SeedSource(context);
            var sut = BuildSut(context);

            await sut.Repository.CopyClientMarketTemplatesOrder(Request(Football, PreMatch, ClientBId, ClientCId));

            Assert.Equal(2, sut.Published.Count);
            Assert.All(sut.Published, m =>
            {
                Assert.Equal(RoutingKey, m.RoutingKey);
                Assert.Equal("OnMarketSortOrderChange", m.MessageType);
                Assert.Equal(CacheType.MarketSortOrder, PayloadOf(m).CacheType);
            });
            Assert.Equal(new[] { (ClientBId, "QTE-B"), (ClientCId, "QTE-C") },
                sut.Published.Select(m => (PayloadOf(m).ClientId, PayloadOf(m).ClientCode)));
        }

        [Fact]
        public async Task Copy_WritesOneActivityLogPerTarget_WithPreviousAndCopiedOrder()
        {
            using var context = BuildContext();
            SeedSource(context);
            SeedOrder(context, ClientBId, Football, "m9");
            var sut = BuildSut(context);

            await sut.Repository.CopyClientMarketTemplatesOrder(Request(Football, PreMatch, ClientBId, ClientCId));

            Assert.Equal(new[] { "QTE-B", "QTE-C" }, sut.AuditLogs.Select(l => l.Payload));
            Assert.All(sut.AuditLogs, l =>
            {
                Assert.Equal("OE.Administration.MarketSortOrder", l.Page);
                Assert.Equal("Copy", l.Activity);
                Assert.Equal("user-1", l.UserId);
                Assert.Equal("Tester", l.UserName);
                Assert.Contains("\"SourceClientCode\":\"QTE-A\"", l.To);
                Assert.Contains("\"Main\"", l.To);
            });
            Assert.Contains("m9", sut.AuditLogs[0].From);
        }

        [Fact]
        public async Task Copy_WhenTheAuditLogFails_StillSucceeds()
        {
            using var context = BuildContext();
            SeedSource(context);
            var sut = BuildSut(context, auditFailure: new InvalidOperationException("log down"));

            var response = await sut.Repository.CopyClientMarketTemplatesOrder(Request(Football, PreMatch, ClientBId));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(new[] { "m3", "m1", "m2" }, OrderOf(context, ClientBId, Football));
        }

        [Fact]
        public async Task Copy_WhenOneTargetFails_CopiesTheOthersAndNamesTheFailedOne()
        {
            using var context = BuildContext();
            SeedSource(context);
            var sut = BuildSut(context, failingClientId: ClientBId);

            var response = await sut.Repository.CopyClientMarketTemplatesOrder(Request(Football, PreMatch, ClientBId, ClientCId));

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal("Market sort order copied to 1 of 2 clients", response.Message);

            var failed = Assert.Single(response.Result.Targets, t => t.StatusCode != StatusCode.Success);
            Assert.Equal(ClientBId, failed.ClientId);
            Assert.Equal("database unavailable", failed.Message);

            Assert.Equal(new[] { "Main:m3,m1", "Goals:m2" }, GroupsOf(context, ClientCId, Football, PreMatch));
            Assert.Equal(ClientCId, PayloadOf(Assert.Single(sut.Published)).ClientId);
            Assert.Equal("QTE-C", Assert.Single(sut.AuditLogs).Payload);
        }

        [Fact]
        public async Task Copy_WhenThePublishFails_ReportsTheTargetAsCopiedWithAWarning()
        {
            using var context = BuildContext();
            SeedSource(context);
            var sut = BuildSut(context, publishFailure: new InvalidOperationException("broker down"));

            var response = await sut.Repository.CopyClientMarketTemplatesOrder(Request(Football, PreMatch, ClientBId));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            var target = Assert.Single(response.Result.Targets);
            Assert.Equal(StatusCode.Success, target.StatusCode);
            Assert.Contains("cache was not refreshed", target.Message);
        }

        [Fact]
        public async Task Copy_WithOnlyTheLegacySingleTarget_CopiesToThatClient()
        {
            using var context = BuildContext();
            SeedSource(context);
            var sut = BuildSut(context);
            var request = Request(Football, PreMatch);
            request.CopyToClientIds = null;
            request.CopyToClientId = ClientBId;

            var response = await sut.Repository.CopyClientMarketTemplatesOrder(request);

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(new[] { "m3", "m1", "m2" }, OrderOf(context, ClientBId, Football));
        }

        // ---------------------------------------------------------------
        // Rejected before anything is changed
        // ---------------------------------------------------------------

        public static IEnumerable<object[]> InvalidRequests()
        {
            yield return new object[] { Request(Football, PreMatch), "No target client selected" };
            yield return new object[] { Request(Football, PreMatch, ClientAId, ClientBId), "You're copying to the same client, sport, and scope. Change at least one before copying." };
            yield return new object[] { Request(Football, PreMatch, ClientBId, UnknownId), "No client found" };
            yield return new object[] { Request(0, PreMatch, ClientBId), "Invalid sport" };
            yield return new object[] { Request(Football, -1, ClientBId), "Invalid scope" };
        }

        [Theory]
        [MemberData(nameof(InvalidRequests))]
        public async Task Copy_InvalidRequest_IsRejectedAndChangesNothing(CopyClientMarketSortOrderRequest request, string message)
        {
            using var context = BuildContext();
            SeedSource(context);
            SeedOrder(context, ClientBId, Football, "m9");
            var sut = BuildSut(context);

            var response = await sut.Repository.CopyClientMarketTemplatesOrder(request);

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal(message, response.Message);
            Assert.Empty(response.Result.Targets);
            Assert.Equal(new[] { "m9" }, OrderOf(context, ClientBId, Football));
            Assert.Empty(sut.Published);
            Assert.Empty(sut.AuditLogs);
        }

        [Fact]
        public async Task Copy_FromAnEmptySource_IsRejectedAndKeepsTheTargets()
        {
            using var context = BuildContext();
            SeedOrder(context, ClientBId, Football, "m9");
            SeedGroup(context, ClientBId, Football, PreMatch, "Old", 1, "m9");
            var sut = BuildSut(context);

            var response = await sut.Repository.CopyClientMarketTemplatesOrder(Request(Football, PreMatch, ClientBId));

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal("Source client has no market sort order to copy", response.Message);
            Assert.Equal(new[] { "m9" }, OrderOf(context, ClientBId, Football));
            Assert.Equal(new[] { "Old:m9" }, GroupsOf(context, ClientBId, Football, PreMatch));
            Assert.Empty(sut.Published);
        }
    }
}
