using Backoffice.Config.Service.Repository;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace Backoffice.Config.Service.Tests.Repository
{
    // BO-138: the singleton repository kept one BackofficeContext, and DeleteConfigTemplate disposed it, so every later
    // message failed with "Cannot access a disposed context instance". Each call now gets its own context.
    public class BackofficeRepositoryTests
    {
        private const long DeletedTemplateId = 10;
        private const long OtherTemplateId = 20;
        private const long SecondDeletedTemplateId = 30;

        private sealed class InMemoryContextFactory : IDbContextFactory<BackofficeContext>
        {
            private readonly DbContextOptions<BackofficeContext> _options;

            public InMemoryContextFactory(string databaseName) =>
                _options = new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(databaseName).Options;

            public BackofficeContext CreateDbContext() => new BackofficeContext(_options);
        }

        private static (BackofficeRepository Sut, InMemoryContextFactory Factory) Build()
        {
            var factory = new InMemoryContextFactory(Guid.NewGuid().ToString());

            using (var context = factory.CreateDbContext())
            {
                context.ConfigTemplateAssigned.AddRange(
                    Assigned(1, prematch: DeletedTemplateId, live: OtherTemplateId),   // keeps its live template
                    Assigned(2, prematch: -1, live: DeletedTemplateId),                // nothing left, removed
                    Assigned(3, prematch: OtherTemplateId, live: SecondDeletedTemplateId),
                    Assigned(4, prematch: SecondDeletedTemplateId, live: -1));
                context.ConfigTemplateMargin.Add(new ConfigTemplateMargin
                {
                    ConfigTemplateId = OtherTemplateId,
                    SportId = 1,
                    FixtureTypeId = 1,
                    FixtureStatusId = (int)Scope.Prematch,
                    MarketTemplateId = "1",
                    IsActive = true,
                    Config = "[]"
                });
                context.SaveChanges();
            }

            return (new BackofficeRepository(factory, NullLogger<BackofficeRepository>.Instance), factory);
        }

        private static ConfigTemplateAssigned Assigned(long id, long prematch, long live) => new()
        {
            Id = id,
            ClientId = Guid.NewGuid(),
            ClientCode = $"C{id}",
            ClientName = $"Client {id}",
            SportId = 1,
            SegmentId = 10,
            LeagueId = (int)id,
            PrematchConfigTemplateId = prematch,
            LiveConfigTemplateId = live
        };

        [Fact]
        public void DeleteConfigTemplate_twice_then_UpdateClientMargin_all_succeed()
        {
            var (sut, factory) = Build();
            var broker = new Mock<IMessageBroker>();

            sut.DeleteConfigTemplate(new ConfigTemplate { Id = DeletedTemplateId });
            sut.DeleteConfigTemplate(new ConfigTemplate { Id = SecondDeletedTemplateId });
            sut.UpdateClientMargin(OtherTemplateId, broker.Object);

            using var context = factory.CreateDbContext();
            var rows = context.ConfigTemplateAssigned.OrderBy(r => r.Id).ToList();

            Assert.Equal(new long[] { 1, 3 }, rows.Select(r => r.Id));
            Assert.Equal(-1, rows[0].PrematchConfigTemplateId);
            Assert.Equal(OtherTemplateId, rows[0].LiveConfigTemplateId);
            Assert.Equal(OtherTemplateId, rows[1].PrematchConfigTemplateId);
            Assert.Equal(-1, rows[1].LiveConfigTemplateId);

            broker.Verify(b => b.Publish(It.IsAny<string>(), "OnUpdateClientLeagueConfig", "r.oe.web.config.OnUpdateClientLeagueConfig", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void DeleteConfigTemplate_of_a_template_assigned_as_both_scopes_removes_the_row()
        {
            var (sut, factory) = Build();
            using (var context = factory.CreateDbContext())
            {
                context.ConfigTemplateAssigned.Add(Assigned(5, prematch: DeletedTemplateId, live: DeletedTemplateId));
                context.SaveChanges();
            }

            sut.DeleteConfigTemplate(new ConfigTemplate { Id = DeletedTemplateId });

            using var check = factory.CreateDbContext();
            Assert.Null(check.ConfigTemplateAssigned.Find(5L));
        }

        [Fact]
        public void UpdateClientMargin_failure_is_thrown_to_the_worker_and_the_next_call_still_works()
        {
            var (sut, _) = Build();
            var broker = new Mock<IMessageBroker>();
            broker.SetupSequence(b => b.Publish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("broker down"))
                .Pass();

            Assert.Throws<InvalidOperationException>(() => sut.UpdateClientMargin(OtherTemplateId, broker.Object));

            sut.DeleteConfigTemplate(new ConfigTemplate { Id = DeletedTemplateId });
            sut.UpdateClientMargin(OtherTemplateId, broker.Object);
        }
    }
}
