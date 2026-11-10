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
    // BO-160: Sport Settings copy to several clients.
    public class ClientSportSettingsCopyRepositoryTests
    {
        private const string RoutingKey = "r.oe.cachesync.onsportsortorderchange";
        private const int Football = 1;
        private const int Basketball = 2;
        private const int Tennis = 5;

        private static readonly Guid ClientAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ClientBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid ClientCId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid ClientDId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

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

        private static Sut BuildSut(SportsbookContext context, Guid? failingClientId = null, bool publishFails = false, Exception auditFailure = null)
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
                        new PlatformClientResponse
                        {
                            ClientId = ClientAId,
                            ClientCode = "QTE-A",
                            ClientName = "Client A"
                        },
                        new PlatformClientResponse
                        {
                            ClientId = ClientBId,
                            ClientCode = "QTE-B",
                            ClientName = "Client B"
                        },
                        new PlatformClientResponse
                        {
                            ClientId = ClientCId,
                            ClientCode = "QTE-C",
                            ClientName = "Client C"
                        },
                        new PlatformClientResponse
                        {
                            ClientId = ClientDId,
                            ClientCode = "QTE-D",
                            ClientName = "Client D"
                        },
                    }
                });

            // Publish never throws: a failure comes back as StatusCode.Error.
            var rabbitMq = new Mock<IRabbitMqRepository>();
            rabbitMq
                .Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                .Callback<MessageRequest, string>((request, _) => sut.Published.Add(request))
                .ReturnsAsync(new BaseResponse<MessageResponse>
                {
                    StatusCode = publishFails ? StatusCode.Error : StatusCode.Success,
                    Message = publishFails ? "Error (Service Unavailable)" : "Success"
                });

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

            sut.Repository = new FailingClientSportsbookRepository(
                failingClientId, context, configuration, platform.Object, activityLog.Object, rabbitMq.Object, new Mock<IMemCache>().Object);

            return sut;
        }

        private sealed class FailingClientSportsbookRepository : SportsbookRepository
        {
            private readonly Guid? _failingClientId;

            public FailingClientSportsbookRepository(Guid? failingClientId, SportsbookContext context, IConfiguration configuration,
                IPlatformRepository platformRepository, IBackofficeActivityLogRepository activityLogRepository,
                IRabbitMqRepository rabbitMqRepository, IMemCache memCache)
                : base(context, configuration, platformRepository, activityLogRepository, rabbitMqRepository, memCache)
            {
                _failingClientId = failingClientId;
            }

            protected override Task<SportSettingsRows> ReplaceClientSportSettings(Guid clientId, List<SportsSortOrder> sports)
            {
                if (clientId == _failingClientId)
                    throw new InvalidOperationException("database unavailable");

                return base.ReplaceClientSportSettings(clientId, sports);
            }
        }

        // Client A (the source on screen): Basketball first, then Football; Football has two segments, the first with two leagues.
        private static List<SportsSortOrder> OnScreenSports() => new List<SportsSortOrder>
        {
            new SportsSortOrder
            {
                SportId = Basketball,
                SportName = "Basketball",
                SortOrder = 1,
                IsActive = true,
                SegmentsSortOrder = new List<SportsSegmentSortOrder>()
            },
            new SportsSortOrder
            {
                SportId = Football,
                SportName = "Football",
                SortOrder = 2,
                IsActive = true,
                SegmentsSortOrder = new List<SportsSegmentSortOrder>
                {
                    new SportsSegmentSortOrder
                    {
                        SportId = Football,
                        SportName = "Football",
                        SegmentId = 20,
                        SegmentName = "Europe",
                        SortOrder = 1,
                        LeagueSortOrder = new List<SportSegmentLeagueSortOrder>
                        {
                            League(Football, 20, 202, "Premier League", 1),
                            League(Football, 20, 201, "La Liga", 2)
                        }
                    },
                    new SportsSegmentSortOrder
                    {
                        SportId = Football,
                        SportName = "Football",
                        SegmentId = 10,
                        SegmentName = "Asia",
                        SortOrder = 2,
                        LeagueSortOrder = new List<SportSegmentLeagueSortOrder>()
                    }
                }
            }
        };

        private static SportSegmentLeagueSortOrder League(int sportId, int segmentId, int leagueId, string name, int sortOrder) => new SportSegmentLeagueSortOrder
        {
            SportId = sportId,
            SportName = sportId == Football ? "Football" : "Other",
            SegmentId = segmentId,
            SegmentName = segmentId == 20 ? "Europe" : "Asia",
            LeagueId = leagueId,
            LeagueName = name,
            SortOrder = sortOrder,
            IsActive = true
        };

        // Gives a client Football then Tennis, Football's segments Asia then Europe, and Europe's leagues La Liga then Premier League.
        private static void SeedOldSettings(SportsbookContext context, Guid clientId)
        {
            context.ClientSportOrder.AddRange(
                new ClientSportOrder
                {
                    ClientId = clientId,
                    SportId = Football,
                    SportName = "Football",
                    SortOrder = 1,
                    IsActive = true
                },
                new ClientSportOrder
                {
                    ClientId = clientId,
                    SportId = Tennis,
                    SportName = "Tennis",
                    SortOrder = 2,
                    IsActive = true
                });

            context.ClientSegmentOrder.AddRange(
                new ClientSegmentOrder
                {
                    ClientId = clientId,
                    SportId = Football,
                    SportName = "Football",
                    SegmentId = 10,
                    SegmentName = "Asia",
                    SortOrder = 1
                },
                new ClientSegmentOrder
                {
                    ClientId = clientId,
                    SportId = Football,
                    SportName = "Football",
                    SegmentId = 20,
                    SegmentName = "Europe",
                    SortOrder = 2
                });

            context.ClientLeagueOrder.AddRange(
                new ClientLeagueOrder
                {
                    ClientId = clientId,
                    SportId = Football,
                    SportName = "Football",
                    SegmentId = 20,
                    SegmentName = "Europe",
                    LeagueId = 201,
                    LeagueName = "La Liga",
                    SortOrder = 1
                },
                new ClientLeagueOrder
                {
                    ClientId = clientId,
                    SportId = Football,
                    SportName = "Football",
                    SegmentId = 20,
                    SegmentName = "Europe",
                    LeagueId = 202,
                    LeagueName = "Premier League",
                    SortOrder = 2
                });

            context.SaveChanges();
        }

        private static CopyClientSportSettingsRequest Request(params string[] targets) => new CopyClientSportSettingsRequest
        {
            FromClientCode = "QTE-A",
            ToClientCodes = targets.ToList(),
            SportsSortOrder = OnScreenSports(),
            UserId = "user-1",
            UserName = "Tester"
        };

        private static List<int> SportOrder(SportsbookContext context, Guid clientId) =>
            context.ClientSportOrder.AsNoTracking().Where(x => x.ClientId == clientId).OrderBy(x => x.SortOrder).Select(x => x.SportId).ToList();

        private static List<int> SegmentOrder(SportsbookContext context, Guid clientId, int sportId) =>
            context.ClientSegmentOrder.AsNoTracking().Where(x => x.ClientId == clientId && x.SportId == sportId).OrderBy(x => x.SortOrder).Select(x => x.SegmentId).ToList();

        private static List<int> LeagueOrder(SportsbookContext context, Guid clientId, int sportId, int segmentId) =>
            context.ClientLeagueOrder.AsNoTracking().Where(x => x.ClientId == clientId && x.SportId == sportId && x.SegmentId == segmentId).OrderBy(x => x.SortOrder).Select(x => x.LeagueId).ToList();

        private static void AssertHasOnScreenSettings(SportsbookContext context, Guid clientId)
        {
            Assert.Equal(new[] { Basketball, Football }, SportOrder(context, clientId));
            Assert.Equal(new[] { 20, 10 }, SegmentOrder(context, clientId, Football));
            Assert.Equal(new[] { 202, 201 }, LeagueOrder(context, clientId, Football, 20));
            Assert.Empty(LeagueOrder(context, clientId, Football, 10));
        }

        private static void AssertHasOldSettings(SportsbookContext context, Guid clientId)
        {
            Assert.Equal(new[] { Football, Tennis }, SportOrder(context, clientId));
            Assert.Equal(new[] { 10, 20 }, SegmentOrder(context, clientId, Football));
            Assert.Equal(new[] { 201, 202 }, LeagueOrder(context, clientId, Football, 20));
        }

        // ---------------------------------------------------------------
        // Copy
        // ---------------------------------------------------------------

        [Fact]
        public async Task Copy_ReplacesEveryTargetsSportsSegmentsAndLeagues_AndLeavesOtherClientsAlone()
        {
            using var context = BuildContext();
            SeedOldSettings(context, ClientBId);
            SeedOldSettings(context, ClientCId);
            SeedOldSettings(context, ClientDId);
            var sut = BuildSut(context);

            var response = await sut.Repository.CopyClientSportSettings(Request("QTE-B", "QTE-C"));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(new[] { "QTE-B", "QTE-C" }, response.Result.Targets.Select(t => t.ClientCode));
            Assert.All(response.Result.Targets, t =>
            {
                Assert.Equal(StatusCode.Success, t.StatusCode);
                Assert.Equal("Copied", t.Message);
            });
            Assert.Equal(new[] { ClientBId, ClientCId }, response.Result.Targets.Select(t => t.ClientId));

            AssertHasOnScreenSettings(context, ClientBId);
            AssertHasOnScreenSettings(context, ClientCId);
            AssertHasOldSettings(context, ClientDId);
        }

        [Fact]
        public async Task Copy_ToAClientWithNoSettings_CreatesThem()
        {
            using var context = BuildContext();
            var sut = BuildSut(context);

            var response = await sut.Repository.CopyClientSportSettings(Request("QTE-B"));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            AssertHasOnScreenSettings(context, ClientBId);
        }

        [Fact]
        public async Task Copy_MatchesTargetCodesWithoutCase_AndIgnoresDuplicates()
        {
            using var context = BuildContext();
            var sut = BuildSut(context);

            var response = await sut.Repository.CopyClientSportSettings(Request("qte-b", "QTE-B", " "));

            var target = Assert.Single(response.Result.Targets);
            Assert.Equal("QTE-B", target.ClientCode);
            AssertHasOnScreenSettings(context, ClientBId);
        }

        [Fact]
        public async Task Copy_PublishesOneSportSortOrderChange_ForAllTargets()
        {
            using var context = BuildContext();
            var sut = BuildSut(context);

            await sut.Repository.CopyClientSportSettings(Request("QTE-B", "QTE-C", "QTE-D"));

            var message = Assert.Single(sut.Published);
            Assert.Equal("OnSportSortOrderChange", message.MessageType);
            Assert.Equal(RoutingKey, message.RoutingKey);

            var payload = JsonSerializer.Deserialize<OnDeleteCacheMessage>(message.Payload);
            Assert.Equal(CacheType.SportSortOrder, payload.CacheType);
            Assert.Equal(Guid.Empty, payload.ClientId);
        }

        [Fact]
        public async Task Copy_WritesOneActivityLogPerTarget_InTheShapeTheLogsDialogReads()
        {
            using var context = BuildContext();
            SeedOldSettings(context, ClientBId);
            var sut = BuildSut(context);

            await sut.Repository.CopyClientSportSettings(Request("QTE-B", "QTE-C"));

            Assert.Equal(new[] { "QTE-B", "QTE-C" }, sut.AuditLogs.Select(l => l.Payload));
            Assert.All(sut.AuditLogs, log =>
            {
                Assert.Equal("SBC.OE.SportsSettings", log.Page);
                Assert.Equal("Copy", log.Activity);
                Assert.Equal("user-1", log.UserId);
                Assert.Equal("Tester", log.UserName);
            });

            var log = sut.AuditLogs[0];
            using var from = JsonDocument.Parse(log.From);
            using var to = JsonDocument.Parse(log.To);

            Assert.Equal(new[] { "Football", "Tennis" }, Names(from.RootElement.GetProperty("Sports"), "SportName"));
            Assert.Equal(new[] { "Basketball", "Football" }, Names(to.RootElement.GetProperty("Sports"), "SportName"));
            Assert.Equal("QTE-A", to.RootElement.GetProperty("CopiedFromClientCode").GetString());

            var fromSegments = Assert.Single(from.RootElement.GetProperty("Segments").EnumerateArray());
            Assert.Equal("Football", fromSegments.GetProperty("SportName").GetString());
            Assert.Equal(new[] { "Asia", "Europe" }, Names(fromSegments.GetProperty("Segments"), "SegmentName"));

            var toSegments = Assert.Single(to.RootElement.GetProperty("Segments").EnumerateArray());
            Assert.Equal(new[] { "Europe", "Asia" }, Names(toSegments.GetProperty("Segments"), "SegmentName"));

            var toLeagues = Assert.Single(Assert.Single(to.RootElement.GetProperty("Leagues").EnumerateArray()).GetProperty("Segments").EnumerateArray());
            Assert.Equal("Europe", toLeagues.GetProperty("SegmentName").GetString());
            Assert.Equal(new[] { "Premier League", "La Liga" }, Names(toLeagues.GetProperty("Leagues"), "LeagueName"));
        }

        [Fact]
        public async Task Copy_ActivityLog_LeavesOutSegmentsAndLeaguesWhoseOrderDidNotChange()
        {
            using var context = BuildContext();
            var sut = BuildSut(context);

            // Client B already has the on-screen settings.
            await sut.Repository.CopyClientSportSettings(Request("QTE-B"));
            sut.AuditLogs.Clear();

            await sut.Repository.CopyClientSportSettings(Request("QTE-B"));

            using var to = JsonDocument.Parse(Assert.Single(sut.AuditLogs).To);
            Assert.Equal(new[] { "Basketball", "Football" }, Names(to.RootElement.GetProperty("Sports"), "SportName"));
            Assert.Empty(to.RootElement.GetProperty("Segments").EnumerateArray());
            Assert.Empty(to.RootElement.GetProperty("Leagues").EnumerateArray());
        }

        [Fact]
        public async Task Copy_WhenOneTargetFails_CopiesTheOthers_AndSaysWhichFailed()
        {
            using var context = BuildContext();
            SeedOldSettings(context, ClientBId);
            SeedOldSettings(context, ClientCId);
            var sut = BuildSut(context, failingClientId: ClientBId);

            var response = await sut.Repository.CopyClientSportSettings(Request("QTE-B", "QTE-C"));

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal("Sport settings copied to 1 of 2 clients", response.Message);

            var failed = response.Result.Targets.Single(t => t.ClientCode == "QTE-B");
            Assert.Equal(StatusCode.Error, failed.StatusCode);
            Assert.Equal("database unavailable", failed.Message);
            Assert.Equal(StatusCode.Success, response.Result.Targets.Single(t => t.ClientCode == "QTE-C").StatusCode);

            AssertHasOldSettings(context, ClientBId);
            AssertHasOnScreenSettings(context, ClientCId);

            Assert.Equal(new[] { "QTE-C" }, sut.AuditLogs.Select(l => l.Payload));
            Assert.Single(sut.Published);
        }

        [Fact]
        public async Task Copy_WhenEveryTargetFails_PublishesNothing()
        {
            using var context = BuildContext();
            var sut = BuildSut(context, failingClientId: ClientBId);

            var response = await sut.Repository.CopyClientSportSettings(Request("QTE-B"));

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Empty(sut.Published);
            Assert.Empty(sut.AuditLogs);
        }

        [Fact]
        public async Task Copy_WhenTheCacheMessageFails_StillCopiesAndLogs_AndWarns()
        {
            using var context = BuildContext();
            var sut = BuildSut(context, publishFails: true);

            var response = await sut.Repository.CopyClientSportSettings(Request("QTE-B"));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Contains("cache was not refreshed", Assert.Single(response.Result.Targets).Message);
            AssertHasOnScreenSettings(context, ClientBId);
            Assert.Single(sut.AuditLogs);
        }

        [Fact]
        public async Task Copy_WhenTheActivityLogFails_StillCopies_AndWarns()
        {
            using var context = BuildContext();
            var sut = BuildSut(context, auditFailure: new InvalidOperationException("log db down"));

            var response = await sut.Repository.CopyClientSportSettings(Request("QTE-B"));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Contains("activity log could not be written", Assert.Single(response.Result.Targets).Message);
            AssertHasOnScreenSettings(context, ClientBId);
        }

        // ---------------------------------------------------------------
        // Refused requests change nothing
        // ---------------------------------------------------------------

        public static IEnumerable<object[]> RefusedRequests()
        {
            yield return new object[] { "no target", (Action<CopyClientSportSettingsRequest>)(r => r.ToClientCodes = new List<string>()), "No target client selected" };
            yield return new object[] { "no source", (Action<CopyClientSportSettingsRequest>)(r => r.FromClientCode = ""), "Invalid source client" };
            yield return new object[] { "source as target", (Action<CopyClientSportSettingsRequest>)(r => r.ToClientCodes.Add("qte-a")), "cannot be copied to itself" };
            yield return new object[] { "empty source", (Action<CopyClientSportSettingsRequest>)(r => r.SportsSortOrder = new List<SportsSortOrder>()), "no sport settings to copy" };
            yield return new object[] { "unknown client", (Action<CopyClientSportSettingsRequest>)(r => r.ToClientCodes.Add("QTE-X")), "No client found: QTE-X" };
            yield return new object[] { "duplicate sport", (Action<CopyClientSportSettingsRequest>)(r => r.SportsSortOrder.Add(new SportsSortOrder
            {
                SportId = Football,
                SportName = "Football",
                SortOrder = 3,
                IsActive = true
            })), "Duplicate client sport found" };
            yield return new object[] { "duplicate segment", (Action<CopyClientSportSettingsRequest>)(r => r.SportsSortOrder[1].SegmentsSortOrder.Add(new SportsSegmentSortOrder
            {
                SportId = Football,
                SportName = "Football",
                SegmentId = 10,
                SegmentName = "Asia",
                SortOrder = 3
            })), "Duplicate client segment found" };
            yield return new object[] { "duplicate league", (Action<CopyClientSportSettingsRequest>)(r => r.SportsSortOrder[1].SegmentsSortOrder[0].LeagueSortOrder.Add(League(Football, 20, 201, "La Liga", 3))), "Duplicate client league found" };
            yield return new object[] { "sport without sort order", (Action<CopyClientSportSettingsRequest>)(r => r.SportsSortOrder[0].SortOrder = 0), "All fields cannot be empty" };
        }

        [Theory]
        [MemberData(nameof(RefusedRequests))]
        public async Task Copy_RefusesAnInvalidRequest_AndChangesNothing(string because, Action<CopyClientSportSettingsRequest> breakIt, string expected)
        {
            using var context = BuildContext();
            SeedOldSettings(context, ClientBId);
            var sut = BuildSut(context);

            var request = Request("QTE-B");
            breakIt(request);

            var response = await sut.Repository.CopyClientSportSettings(request);

            Assert.True(response.StatusCode == StatusCode.Error, because);
            Assert.Contains(expected, response.Message);
            Assert.Empty(response.Result.Targets);
            AssertHasOldSettings(context, ClientBId);
            Assert.Empty(sut.Published);
            Assert.Empty(sut.AuditLogs);
        }

        private static List<string> Names(JsonElement array, string property) =>
            array.EnumerateArray().Select(e => e.GetProperty(property).GetString()).ToList();
    }
}
