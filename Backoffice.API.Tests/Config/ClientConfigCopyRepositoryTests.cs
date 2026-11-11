using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
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

namespace Backoffice.API.Tests.Config
{
    // BO-162: Config > Copy Client Settings copies the league template assignments of one client to several clients.
    public class ClientConfigCopyRepositoryTests
    {
        private const string RoutingKey = "r.oe.web.config.oncopyclientsettings2";

        private static readonly Guid ClientAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid ClientBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid ClientCId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid ClientDId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        // Fails the copy of one client after adding a stray row, to prove the failed target's changes are not saved
        // with the next target.
        private sealed class FailingRepository : BackofficeRepository
        {
            private readonly BackofficeContext _context;
            private readonly string _failingClientCode;

            public FailingRepository(BackofficeContext context, TranslationContext translation, IRabbitMqRepository rabbitMq, string failingClientCode)
                : base(context, translation, new Mock<IPlatformRepository>().Object, new Mock<ISportsbookRepository>().Object, new Mock<ITradingRepository>().Object, new Mock<IMemCache>().Object, rabbitMq)
            {
                _context = context;
                _failingClientCode = failingClientCode;
            }

            protected override List<ConfigTemplateAssigned> ReplaceClientConfigTemplateAssigned(List<ConfigTemplateAssigned> fromAssigned, CopyClientConfigTarget target)
            {
                if (target.ClientCode != _failingClientCode)
                    return base.ReplaceClientConfigTemplateAssigned(fromAssigned, target);

                _context.ConfigTemplateAssigned.Add(Assigned("STRAY", Guid.Empty, 9, 9, 9, 9, 9));
                throw new InvalidOperationException("database unavailable");
            }
        }

        private sealed class Sut
        {
            public BackofficeRepository Repository;
            public BackofficeContext Context;
            public List<MessageRequest> Published = new List<MessageRequest>();
        }

        private static Sut Build(string failingClientCode = null)
        {
            var name = Guid.NewGuid().ToString();
            var sut = new Sut
            {
                Context = new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(name).Options)
            };
            var translation = new TranslationContext(new DbContextOptionsBuilder<TranslationContext>().UseInMemoryDatabase(name).Options);

            var rabbitMq = new Mock<IRabbitMqRepository>();
            rabbitMq.Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                .Callback<MessageRequest, string>((m, _) => sut.Published.Add(m))
                .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success });

            sut.Repository = failingClientCode == null
                ? new BackofficeRepository(
                    sut.Context,
                    translation,
                    new Mock<IPlatformRepository>().Object,
                    new Mock<ISportsbookRepository>().Object,
                    new Mock<ITradingRepository>().Object,
                    new Mock<IMemCache>().Object,
                    rabbitMq.Object)
                : new FailingRepository(sut.Context, translation, rabbitMq.Object, failingClientCode);

            return sut;
        }

        private static ConfigTemplateAssigned Assigned(string clientCode, Guid clientId, int sportId, int segmentId, int leagueId, long prematch, long live) => new ConfigTemplateAssigned
        {
            ClientCode = clientCode,
            ClientId = clientId,
            ClientName = $"Name {clientCode}",
            SportId = sportId,
            SportName = $"Sport {sportId}",
            SegmentId = segmentId,
            SegmentName = $"Segment {segmentId}",
            LeagueId = leagueId,
            LeagueName = $"League {leagueId}",
            PrematchConfigTemplateId = prematch,
            LiveConfigTemplateId = live
        };

        // Source A: two leagues. Target B: one shared league with other templates and one league A does not have.
        // Client D is never a target.
        private static void Seed(BackofficeContext context)
        {
            context.ConfigTemplateAssigned.AddRange(
                Assigned("QTE-A", ClientAId, 1, 10, 100, 11, 12),
                Assigned("QTE-A", ClientAId, 1, 10, 101, 13, 14),
                Assigned("QTE-B", ClientBId, 1, 10, 100, 21, 22),
                Assigned("QTE-B", ClientBId, 2, 20, 200, 23, 24),
                Assigned("QTE-D", ClientDId, 1, 10, 100, 31, 32));
            context.SaveChanges();
        }

        private static CopyClientConfigTarget Target(string code, Guid id) => new CopyClientConfigTarget
        {
            ClientCode = code,
            ClientId = id,
            ClientName = $"Client {code}"
        };

        private static CopyClientConfigRequest Request(params CopyClientConfigTarget[] targets) => new CopyClientConfigRequest
        {
            FromClientCode = "QTE-A",
            ToClients = targets.ToList(),
            UserId = "user-1"
        };

        private static List<(int Sport, int Segment, int League, long Prematch, long Live)> Leagues(BackofficeContext context, string clientCode) =>
            context.ConfigTemplateAssigned.AsNoTracking()
                .Where(c => c.ClientCode == clientCode)
                .OrderBy(c => c.LeagueId)
                .AsEnumerable()
                .Select(c => (c.SportId, c.SegmentId, c.LeagueId, c.PrematchConfigTemplateId, c.LiveConfigTemplateId))
                .ToList();

        private static readonly List<(int, int, int, long, long)> SourceLeagues = new List<(int, int, int, long, long)>
        {
            (1, 10, 100, 11, 12),
            (1, 10, 101, 13, 14)
        };

        private static OnCopyClientSettingsMessage Message(MessageRequest request) =>
            JsonSerializer.Deserialize<OnCopyClientSettingsMessage>(request.Payload);

        // ---------------------------------------------------------------

        [Fact]
        public async Task EveryTarget_GetsTheSourceAssignments_AndOtherClientsAreUntouched()
        {
            var sut = Build();
            Seed(sut.Context);

            var response = await sut.Repository.CopyClientConfig(Request(Target("QTE-B", ClientBId), Target("QTE-C", ClientCId)));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.True(response.Result.Result);
            Assert.Equal(new[] { "QTE-B", "QTE-C" }, response.Result.Targets.Select(t => t.ClientCode));
            Assert.All(response.Result.Targets, t => Assert.Equal(StatusCode.Success, t.StatusCode));

            Assert.Equal(SourceLeagues, Leagues(sut.Context, "QTE-B"));
            Assert.Equal(SourceLeagues, Leagues(sut.Context, "QTE-C"));
            Assert.Equal(SourceLeagues, Leagues(sut.Context, "QTE-A"));
            Assert.Equal(new[] { (1, 10, 100, 31L, 32L) }, Leagues(sut.Context, "QTE-D"));

            var copiedToC = sut.Context.ConfigTemplateAssigned.AsNoTracking().Where(c => c.ClientCode == "QTE-C").ToList();
            Assert.All(copiedToC, c =>
            {
                Assert.Equal(ClientCId, c.ClientId);
                Assert.Equal("Client QTE-C", c.ClientName);
            });
        }

        [Fact]
        public async Task LeaguesTheSourceDoesNotHave_AreClearedAndReportedForThatTargetOnly()
        {
            var sut = Build();
            Seed(sut.Context);

            var response = await sut.Repository.CopyClientConfig(Request(Target("QTE-B", ClientBId), Target("QTE-C", ClientCId)));

            var removed = Assert.Single(response.Result.ToBeRemoved);
            Assert.Equal(("QTE-B", 200), (removed.ClientCode, removed.LeagueId));
            Assert.DoesNotContain(Leagues(sut.Context, "QTE-B"), l => l.League == 200);

            Assert.Equal(2, sut.Published.Count);
            Assert.All(sut.Published, m => Assert.Equal(RoutingKey, m.RoutingKey));
            Assert.All(sut.Published, m => Assert.Equal("OnCopyClientSettings2", m.MessageType));

            var toB = Message(sut.Published[0]);
            Assert.Equal(("QTE-A", "QTE-B", "user-1"), (toB.FromClientCode, toB.ToClientCode, toB.UserId));
            var toRemove = Assert.Single(toB.ToRemoveConfig);
            Assert.Equal((ClientBId, 2, 20, 200), (toRemove.ClientId, toRemove.SportId, toRemove.SegmentId, toRemove.LeagueId));

            var toC = Message(sut.Published[1]);
            Assert.Equal("QTE-C", toC.ToClientCode);
            Assert.Empty(toC.ToRemoveConfig);
        }

        [Fact]
        public async Task TheInjectedContext_IsStillUsableAfterACopy()
        {
            var sut = Build();
            Seed(sut.Context);

            await sut.Repository.CopyClientConfig(Request(Target("QTE-B", ClientBId)));
            var second = await sut.Repository.CopyClientConfig(Request(Target("QTE-C", ClientCId)));

            Assert.Equal(StatusCode.Success, second.StatusCode);
            Assert.Equal(SourceLeagues, Leagues(sut.Context, "QTE-C"));
        }

        [Fact]
        public async Task ASourceWithNoAssignments_IsRefused_AndNoTargetIsCleared()
        {
            var sut = Build();
            Seed(sut.Context);

            var response = await sut.Repository.CopyClientConfig(new CopyClientConfigRequest
            {
                FromClientCode = "QTE-C",
                ToClients = new List<CopyClientConfigTarget> { Target("QTE-B", ClientBId) }
            });

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Contains("no template assignments", response.Message);
            Assert.Equal(2, Leagues(sut.Context, "QTE-B").Count);
            Assert.Empty(sut.Published);
        }

        [Fact]
        public async Task NoTarget_IsRefused()
        {
            var sut = Build();
            Seed(sut.Context);

            var response = await sut.Repository.CopyClientConfig(Request());

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Empty(sut.Published);
        }

        [Fact]
        public async Task TheSourceListedAsATarget_IsSkipped_AndTheOthersAreCopied()
        {
            var sut = Build();
            Seed(sut.Context);

            var response = await sut.Repository.CopyClientConfig(Request(Target("QTE-A", ClientAId), Target("QTE-C", ClientCId)));

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal(StatusCode.Error, response.Result.Targets.Single(t => t.ClientCode == "QTE-A").StatusCode);
            Assert.Equal(StatusCode.Success, response.Result.Targets.Single(t => t.ClientCode == "QTE-C").StatusCode);
            Assert.Equal(SourceLeagues, Leagues(sut.Context, "QTE-A"));
            Assert.Equal(SourceLeagues, Leagues(sut.Context, "QTE-C"));
            Assert.Single(sut.Published);
        }

        [Fact]
        public async Task ATargetListedTwice_IsCopiedOnce()
        {
            var sut = Build();
            Seed(sut.Context);

            var response = await sut.Repository.CopyClientConfig(Request(Target("QTE-C", ClientCId), Target("qte-c", ClientCId)));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Single(response.Result.Targets);
            Assert.Equal(SourceLeagues, Leagues(sut.Context, "QTE-C"));
        }

        [Fact]
        public async Task TheOldSingleTargetRequest_StillWorks()
        {
            var sut = Build();
            Seed(sut.Context);

            var response = await sut.Repository.CopyClientConfig(new CopyClientConfigRequest
            {
                FromClientCode = "QTE-A",
                ToClientCode = "QTE-C",
                ToClientId = ClientCId,
                ToClientName = "Client QTE-C"
            });

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(SourceLeagues, Leagues(sut.Context, "QTE-C"));
        }

        [Fact]
        public async Task OneFailingTarget_LeavesTheOthersCopied_AndSaysWhichFailed()
        {
            var sut = Build(failingClientCode: "QTE-B");
            Seed(sut.Context);

            var response = await sut.Repository.CopyClientConfig(Request(Target("QTE-B", ClientBId), Target("QTE-C", ClientCId)));

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.False(response.Result.Result);

            var failed = response.Result.Targets.Single(t => t.ClientCode == "QTE-B");
            Assert.Equal(StatusCode.Error, failed.StatusCode);
            Assert.Equal("database unavailable", failed.Message);
            Assert.Equal(StatusCode.Success, response.Result.Targets.Single(t => t.ClientCode == "QTE-C").StatusCode);

            // B keeps what it had, C is copied, and the failed target's pending row was not saved with C.
            Assert.Equal(new[] { (1, 10, 100, 21L, 22L), (2, 20, 200, 23L, 24L) }, Leagues(sut.Context, "QTE-B"));
            Assert.Equal(SourceLeagues, Leagues(sut.Context, "QTE-C"));
            Assert.Empty(Leagues(sut.Context, "STRAY"));

            var message = Assert.Single(sut.Published);
            Assert.Equal("QTE-C", Message(message).ToClientCode);
        }
    }
}
