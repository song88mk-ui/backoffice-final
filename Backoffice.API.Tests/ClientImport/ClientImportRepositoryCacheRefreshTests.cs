using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using OE.Shared.Data.Sportsbook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.ClientImport
{
    // The Create import copies sport settings and market templates with raw SQL, which Sportsbook.API never
    // hears about: it caches those whole tables for 24h, so a new client got an empty sports list and the
    // sportsbook crashed (Index.razor.cs SportsTreeList.FirstOrDefault()) until the pods restarted. A created
    // client must now send the same cache-clear messages the Sport Settings and Market Groups screens send.
    public class ClientImportRepositoryCacheRefreshTests
    {
        private static readonly Guid NewClientId = Guid.Parse("0C942F91-272A-49D9-96D7-13C401BAA206");
        private static readonly Guid QteId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        // The three raw-SQL steps can't run on the EF InMemory provider; everything around them is real.
        private sealed class SqlFreeClientImportRepository : ClientImportRepository
        {
            public SqlFreeClientImportRepository(BackofficeContext ctx, SportsbookContext sb, IBackofficeRepository repo,
                IBackofficeActivityLogRepository log, IRabbitMqRepository rabbit)
                : base(ctx, sb, repo, log, new ConfigurationBuilder().Build(), rabbit) { }

            protected override Task CopySelectorMarketTemplate(Guid newClientId, Guid referenceClientId) => Task.CompletedTask;
            protected override Task<int> CopySportSettings(Guid newClientId, Guid referenceClientId) => Task.FromResult(19);
            protected override Task RegisterTradingClient(Guid clientId, string clientName, string clientCode) => Task.CompletedTask;
        }

        private static (ClientImportRepository Sut, List<MessageRequest> Published) Build(
            string dbName,
            bool platformRegisters = true,
            StatusCode publishStatus = StatusCode.Success)
        {
            var ctx = new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(dbName).Options);
            var sportsbookCtx = new SportsbookContext(new DbContextOptionsBuilder<SportsbookContext>().UseInMemoryDatabase(dbName).Options);

            var repo = new Mock<IBackofficeRepository>();
            repo.Setup(r => r.GetPlatformClients())
                .ReturnsAsync(new BaseResponse<List<PlatformClientResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<PlatformClientResponse> { new PlatformClientResponse { ClientId = QteId, ClientCode = "QTE", LimitId = "10939" } }
                });
            // The real AddOrUpdateClient sets PlatformClientId only when its Platform call succeeds.
            repo.Setup(r => r.AddOrUpdateClient(It.IsAny<ClientListResponse.Client>()))
                .Callback<ClientListResponse.Client>(c => { if (platformRegisters) c.PlatformClientId = NewClientId; })
                .ReturnsAsync(new BaseResponse<bool> { StatusCode = StatusCode.Success, Result = true });

            var activityLog = new Mock<IBackofficeActivityLogRepository>();
            activityLog.Setup(a => a.ProcessActivityLog(It.IsAny<OnActivityLogMessage>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var published = new List<MessageRequest>();
            var rabbit = new Mock<IRabbitMqRepository>();
            rabbit.Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                .Callback<MessageRequest, string>((m, _) => published.Add(m))
                .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = publishStatus, Message = publishStatus.ToString() });

            return (new SqlFreeClientImportRepository(ctx, sportsbookCtx, repo.Object, activityLog.Object, rabbit.Object), published);
        }

        private static CommitClientImportRequest OneClient() => new CommitClientImportRequest
        {
            Environment = "Staging",
            Rows = new List<ClientImportRowRequest>
            {
                new ClientImportRowRequest
                {
                    RowNumber = 2, ClientCode = "SOL", ClientName = "Solstice Sports", Website = "https://solsticesports.com",
                    LimitId = "10939", Endpoint = "https://operator.example.com", ReferenceClientCode = "QTE", CurrencyCode = "USD"
                }
            }
        };

        [Fact]
        public async Task CreatedClient_ClearsTheSportAndMarketCachesForThatClient()
        {
            var (sut, published) = Build(nameof(CreatedClient_ClearsTheSportAndMarketCachesForThatClient));

            var row = Assert.Single((await sut.CommitBatch(OneClient(), "u1", "tester")).Result.Rows);

            Assert.Equal(ClientImportRowStatus.Succeeded, row.Status);
            Assert.Equal(new[] { "r.oe.cachesync.onsportsortorderchange", "r.oe.cachesync.onmarketsortorderchange" }, published.Select(m => m.RoutingKey));
            Assert.Equal(new[] { "OnSportSortOrderChange", "OnMarketSortOrderChange" }, published.Select(m => m.MessageType));

            var payloads = published.Select(m => JsonSerializer.Deserialize<OnDeleteCacheMessage>(m.Payload)).ToList();
            Assert.Equal(new[] { CacheType.SportSortOrder, CacheType.MarketSortOrder }, payloads.Select(p => p.CacheType));
            Assert.All(payloads, p =>
            {
                Assert.Equal(NewClientId, p.ClientId);
                // CacheSync builds a routing key from it; null would crash the consumer.
                Assert.Equal("SOL", p.ClientCode);
            });
        }

        [Fact]
        public async Task CreatedClient_RecordsTheCacheStepLast()
        {
            var dbName = nameof(CreatedClient_RecordsTheCacheStepLast);
            var (sut, _) = Build(dbName);

            await sut.CommitBatch(OneClient(), "u1", "tester");

            var ctx = new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(dbName).Options);
            Assert.EndsWith("Trading DB registration -> Sportsbook caches cleared", (await ctx.ClientImportBatchRow.SingleAsync()).StepsCompleted);
        }

        [Fact]
        public async Task PublishFailure_KeepsTheClientAndSaysHowToRefresh()
        {
            var dbName = nameof(PublishFailure_KeepsTheClientAndSaysHowToRefresh);
            var (sut, published) = Build(dbName, publishStatus: StatusCode.Error);

            var row = Assert.Single((await sut.CommitBatch(OneClient(), "u1", "tester")).Result.Rows);

            // The client is fully created; a missed cache refresh must not turn it into a failure.
            Assert.Equal(ClientImportRowStatus.Succeeded, row.Status);
            Assert.Equal(2, published.Count);
            var ctx = new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(dbName).Options);
            Assert.Contains("Sportsbook caches NOT cleared", (await ctx.ClientImportBatchRow.SingleAsync()).StepsCompleted);
        }

        [Fact]
        public async Task FailedClient_SendsNoCacheMessages()
        {
            var (sut, published) = Build(nameof(FailedClient_SendsNoCacheMessages), platformRegisters: false);

            var row = Assert.Single((await sut.CommitBatch(OneClient(), "u1", "tester")).Result.Rows);

            Assert.Equal(ClientImportRowStatus.Failed, row.Status);
            Assert.Empty(published);
        }
    }
}
