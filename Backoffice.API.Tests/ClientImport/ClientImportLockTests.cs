using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.OutgoingEngineMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using OE.Shared.Data.Sportsbook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.ClientImport
{
    // BO-133: the per-environment import lock — batches left Running by a killed process, and the
    // environment coming from the server rather than the request.
    public class ClientImportLockTests
    {
        private const string StagingConnectionString = "Server=stg-song88-mssql-server;Database=Backoffice";

        private sealed class Harness
        {
            public ClientImportRepository Sut { get; init; }
            public BackofficeContext Context { get; init; }
            public List<OnActivityLogMessage> AuditLogs { get; init; }
        }

        private static Harness BuildHarness(string dbName, string connectionString = StagingConnectionString, int? staleRunningMinutes = null)
        {
            var ctx = new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(dbName).Options);
            var sportsbookCtx = new SportsbookContext(new DbContextOptionsBuilder<SportsbookContext>().UseInMemoryDatabase(dbName).Options);

            var settings = new Dictionary<string, string> { ["ConnectionStrings:Backoffice"] = connectionString };
            if (staleRunningMinutes.HasValue)
                settings["ClientImport:StaleRunningMinutes"] = staleRunningMinutes.Value.ToString();

            var auditLogs = new List<OnActivityLogMessage>();
            var activityLog = new Mock<IBackofficeActivityLogRepository>();
            activityLog.Setup(a => a.ProcessActivityLog(It.IsAny<OnActivityLogMessage>(), It.IsAny<string>()))
                .Callback<OnActivityLogMessage, string>((m, _) => auditLogs.Add(m))
                .Returns(Task.CompletedTask);

            return new Harness
            {
                Context = ctx,
                AuditLogs = auditLogs,
                Sut = new ClientImportRepository(ctx, sportsbookCtx, new Mock<IBackofficeRepository>().Object, activityLog.Object,
                    new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), new Mock<IRabbitMqRepository>().Object)
            };
        }

        private static ClientImportBatch SeedRunningBatch(BackofficeContext ctx, string environment, TimeSpan age, params string[] rowStatuses)
        {
            var batch = new ClientImportBatch
            {
                BatchId = Guid.NewGuid(),
                Environment = environment,
                OperationType = ClientImportOperationType.Create,
                Status = ClientImportBatchStatus.Running,
                StartedByUserName = "Tester",
                StartedAt = DateTime.UtcNow - age,
                TotalRows = rowStatuses.Length
            };
            ctx.ClientImportBatch.Add(batch);
            for (var i = 0; i < rowStatuses.Length; i++)
            {
                ctx.ClientImportBatchRow.Add(new ClientImportBatchRow { BatchId = batch.BatchId, RowNumber = i + 1, ClientCode = $"C{i}", ClientName = $"Client {i}", Status = rowStatuses[i] });
            }
            ctx.SaveChanges();
            return batch;
        }

        // No rows: the batch opens and closes without touching clients, which is all the lock tests need.
        private static CommitClientImportRequest EmptyRequest(string environment = "Staging") =>
            new CommitClientImportRequest { Environment = environment, Rows = new List<ClientImportRowRequest>() };

        [Fact]
        public async Task StaleRunningBatch_NoLongerBlocks_AndIsMarkedInterruptedWithRowCounts()
        {
            var h = BuildHarness(nameof(StaleRunningBatch_NoLongerBlocks_AndIsMarkedInterruptedWithRowCounts));
            var stale = SeedRunningBatch(h.Context, "Staging", TimeSpan.FromHours(3),
                ClientImportRowStatus.Succeeded, ClientImportRowStatus.Failed, ClientImportRowStatus.Pending);

            Assert.False((await h.Sut.IsImportRunning("Staging")).Result);

            var response = await h.Sut.CommitBatch(EmptyRequest(), "u1", "Tester");

            Assert.Equal(StatusCode.Success, response.StatusCode);
            var interrupted = h.Context.ClientImportBatch.Single(b => b.BatchId == stale.BatchId);
            Assert.Equal(ClientImportBatchStatus.Interrupted, interrupted.Status);
            Assert.NotNull(interrupted.CompletedAt);
            Assert.Equal(1, interrupted.SucceededRows);
            Assert.Equal(1, interrupted.FailedRows);
            Assert.Equal(3, interrupted.TotalRows);
        }

        [Fact]
        public async Task StaleLimit_ComesFromConfiguration()
        {
            var h = BuildHarness(nameof(StaleLimit_ComesFromConfiguration), staleRunningMinutes: 5);
            SeedRunningBatch(h.Context, "Staging", TimeSpan.FromMinutes(10));

            var response = await h.Sut.CommitBatch(EmptyRequest(), "u1", "Tester");

            Assert.Equal(StatusCode.Success, response.StatusCode);
        }

        [Fact]
        public async Task RecentRunningBatch_StillBlocks()
        {
            var h = BuildHarness(nameof(RecentRunningBatch_StillBlocks));
            var running = SeedRunningBatch(h.Context, "Staging", TimeSpan.FromMinutes(1), ClientImportRowStatus.Pending);

            var create = await h.Sut.CommitBatch(EmptyRequest(), "u1", "Tester");
            var update = await h.Sut.CommitUpdateBatch(new CommitClientUpdateRequest { Environment = "Staging", Rows = new List<ClientUpdateRowRequest>() }, "u1", "Tester");

            Assert.Equal(StatusCode.Error, create.StatusCode);
            Assert.Contains("already running for Staging", create.Message);
            Assert.Equal(StatusCode.Error, update.StatusCode);
            Assert.Contains("already running for Staging", update.Message);
            Assert.Equal(ClientImportBatchStatus.Running, h.Context.ClientImportBatch.Single(b => b.BatchId == running.BatchId).Status);
            Assert.Single(h.Context.ClientImportBatch);
        }

        [Fact]
        public async Task Startup_InterruptsThisEnvironmentsRunningBatches_Only()
        {
            var h = BuildHarness(nameof(Startup_InterruptsThisEnvironmentsRunningBatches_Only));
            var staging = SeedRunningBatch(h.Context, "Staging", TimeSpan.FromMinutes(1), ClientImportRowStatus.Succeeded, ClientImportRowStatus.Pending);
            var production = SeedRunningBatch(h.Context, "Production", TimeSpan.FromMinutes(1));

            var result = await h.Sut.InterruptAbandonedBatches();

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(1, result.Result);
            var interrupted = h.Context.ClientImportBatch.Single(b => b.BatchId == staging.BatchId);
            Assert.Equal(ClientImportBatchStatus.Interrupted, interrupted.Status);
            Assert.Equal(1, interrupted.SucceededRows);
            Assert.Equal(0, interrupted.FailedRows);
            Assert.Equal(ClientImportBatchStatus.Running, h.Context.ClientImportBatch.Single(b => b.BatchId == production.BatchId).Status);
            Assert.False((await h.Sut.IsImportRunning("Staging")).Result);
        }

        [Fact]
        public async Task Startup_WithUnknownDatabaseHost_ChangesNothing()
        {
            var h = BuildHarness(nameof(Startup_WithUnknownDatabaseHost_ChangesNothing), "Server=some-other-host;Database=Backoffice");
            SeedRunningBatch(h.Context, "Staging", TimeSpan.FromMinutes(1));

            var result = await h.Sut.InterruptAbandonedBatches();

            Assert.Equal(StatusCode.Error, result.StatusCode);
            Assert.All(h.Context.ClientImportBatch, b => Assert.Equal(ClientImportBatchStatus.Running, b.Status));
        }

        [Fact]
        public async Task WrongEnvironmentInRequest_IsRefusedAndNoBatchIsCreated()
        {
            var h = BuildHarness(nameof(WrongEnvironmentInRequest_IsRefusedAndNoBatchIsCreated));
            // A Production request on a Staging server must not dodge Staging's running import.
            SeedRunningBatch(h.Context, "Staging", TimeSpan.FromMinutes(1));

            var create = await h.Sut.CommitBatch(EmptyRequest("Production"), "u1", "Tester");
            var update = await h.Sut.CommitUpdateBatch(new CommitClientUpdateRequest { Environment = "Production", Rows = new List<ClientUpdateRowRequest>() }, "u1", "Tester");

            Assert.Equal(StatusCode.Error, create.StatusCode);
            Assert.Contains("runs in Staging", create.Message);
            Assert.Equal(StatusCode.Error, update.StatusCode);
            Assert.Contains("runs in Staging", update.Message);
            Assert.Single(h.Context.ClientImportBatch);
            Assert.Empty(h.AuditLogs);
        }

        [Fact]
        public async Task RequestWithoutEnvironment_UsesTheServersEnvironment()
        {
            var h = BuildHarness(nameof(RequestWithoutEnvironment_UsesTheServersEnvironment));

            var response = await h.Sut.CommitBatch(EmptyRequest(null), "u1", "Tester");

            Assert.Equal(StatusCode.Success, response.StatusCode);
            var batch = Assert.Single(h.Context.ClientImportBatch);
            Assert.Equal("Staging", batch.Environment);
            Assert.Equal(ClientImportBatchStatus.Completed, batch.Status);
            Assert.Equal("Staging", Assert.Single(h.AuditLogs).From);
        }

        [Fact]
        public async Task UnknownDatabaseHost_RefusesCommit()
        {
            var h = BuildHarness(nameof(UnknownDatabaseHost_RefusesCommit), "Server=some-other-host;Database=Backoffice");

            var response = await h.Sut.CommitBatch(EmptyRequest(), "u1", "Tester");

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Contains("unrecognised database host", response.Message);
            Assert.Empty(h.Context.ClientImportBatch);
        }
    }
}
