using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.OutgoingEngineMessages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.ActivityLog
{
    public class BackofficeActivityLogRepositoryTests
    {
        private static BackofficeContext BuildInMemoryContext(string dbName)
        {
            var opts = new DbContextOptionsBuilder<BackofficeContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new BackofficeContext(opts);
        }

        private static OnActivityLogMessage MakeMessage(string page, string payload, string to, DateTime timeStamp)
        {
            return new OnActivityLogMessage
            {
                Page = page,
                Payload = payload,
                Activity = "ManualSettlement",
                From = "-",
                To = to,
                UserId = "tester",
                UserName = "Test User",
                TimeStamp = timeStamp
            };
        }

        [Fact]
        public async Task ProcessActivityLogList_WritesAllRowsToActivityLogTable()
        {
            var ctx = BuildInMemoryContext(nameof(ProcessActivityLogList_WritesAllRowsToActivityLogTable));
            var sut = new BackofficeActivityLogRepository(ctx);

            var message = new OnActivityLogListMessage
            {
                OnActivityLogMessages = new List<OnActivityLogMessage>
                {
                    MakeMessage("Admin.Settlements.MarketSettlement", "10.1.1.1", "{\"MarketName\":\"Match Winner\"}", new DateTime(2026, 6, 24, 10, 0, 0, DateTimeKind.Utc)),
                    MakeMessage("Admin.Settlements.MarketSettlement", "10.1.1.2", "{\"MarketName\":\"Total Goals\"}", new DateTime(2026, 6, 24, 11, 0, 0, DateTimeKind.Utc)),
                }
            };

            await sut.ProcessActivityLog(message);

            // Use a fresh context against the same in-memory store to assert what was persisted.
            using var verify = BuildInMemoryContext(nameof(ProcessActivityLogList_WritesAllRowsToActivityLogTable));
            var rows = verify.ActivityLog.ToList();

            Assert.Equal(2, rows.Count);
            Assert.All(rows, r => Assert.Equal("Admin.Settlements.MarketSettlement", r.Page));
            Assert.Contains(rows, r => r.To.Contains("Match Winner"));
            Assert.Contains(rows, r => r.To.Contains("Total Goals"));
            Assert.All(rows, r => Assert.Equal("tester", r.UserId));
        }

        [Fact]
        public async Task ProcessActivityLogList_DefaultsTimeStampWhenMissing()
        {
            var ctx = BuildInMemoryContext(nameof(ProcessActivityLogList_DefaultsTimeStampWhenMissing));
            var sut = new BackofficeActivityLogRepository(ctx);

            var before = DateTime.UtcNow;
            var message = new OnActivityLogListMessage
            {
                OnActivityLogMessages = new List<OnActivityLogMessage>
                {
                    MakeMessage("Admin.Settlements.MarketSettlement", "10.1.1.1", "{}", default),
                }
            };

            await sut.ProcessActivityLog(message);

            using var verify = BuildInMemoryContext(nameof(ProcessActivityLogList_DefaultsTimeStampWhenMissing));
            var row = verify.ActivityLog.Single();

            Assert.NotEqual(default, row.TimeStamp);
            Assert.True(row.TimeStamp >= before, "TimeStamp should be defaulted to UtcNow when not supplied");
        }

        [Fact]
        public async Task ProcessActivityLogSingle_WritesOneRow()
        {
            var ctx = BuildInMemoryContext(nameof(ProcessActivityLogSingle_WritesOneRow));
            var sut = new BackofficeActivityLogRepository(ctx);

            await sut.ProcessActivityLog(
                MakeMessage("Admin.Settlements.MarketSettlement", "10.1.1.1", "{\"MarketName\":\"Match Winner\"}", new DateTime(2026, 6, 24, 10, 0, 0, DateTimeKind.Utc)));

            using var verify = BuildInMemoryContext(nameof(ProcessActivityLogSingle_WritesOneRow));
            var rows = verify.ActivityLog.ToList();

            Assert.Single(rows);
            Assert.Equal("Admin.Settlements.MarketSettlement", rows[0].Page);
            Assert.Contains("Match Winner", rows[0].To);
        }
    }
}
