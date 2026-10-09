using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
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

        // ---------------------------------------------------------------
        // GetPagedActivityLogs (plain EF read, newest first)
        // ---------------------------------------------------------------

        private static readonly DateTime Day1 = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);

        private static async Task<BackofficeActivityLogRepository> SeededRepository(string dbName)
        {
            var ctx = BuildInMemoryContext(dbName);
            var sut = new BackofficeActivityLogRepository(ctx);

            await sut.ProcessActivityLog(new OnActivityLogListMessage
            {
                OnActivityLogMessages = new List<OnActivityLogMessage>
                {
                    MakeMessage("SBC.OE.LeagueSorting", "QTE-A", "a-old", Day1.AddDays(-40)),
                    MakeMessage("SBC.OE.LeagueSorting", "QTE-A", "a-1", Day1),
                    MakeMessage("SBC.OE.LeagueSorting", "QTE-B", "b-1", Day1.AddHours(1)),
                    MakeMessage("SBC.OE.LeagueSorting", "QTE-A", "a-2", Day1.AddHours(2)),
                    MakeMessage("SBC.OE.SportsSettings", "QTE-A", "other-page", Day1.AddHours(3)),
                }
            });

            return sut;
        }

        [Fact]
        public async Task GetPagedActivityLogs_ReturnsOnlyThatPageAndDateRange_NewestFirst()
        {
            var sut = await SeededRepository(nameof(GetPagedActivityLogs_ReturnsOnlyThatPageAndDateRange_NewestFirst));

            var response = await sut.GetPagedActivityLogs("SBC.OE.LeagueSorting", null, Day1.AddDays(-30), new MetaData { CurrentPage = 1, PageSize = 10 });

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(new[] { "a-2", "b-1", "a-1" }, response.Result.ActivityLogs.Select(l => l.To));
            Assert.Equal(3, response.Result.MetaData.TotalCount);
            Assert.Equal(1, response.Result.MetaData.TotalPages);
        }

        [Fact]
        public async Task GetPagedActivityLogs_FiltersByPayload()
        {
            var sut = await SeededRepository(nameof(GetPagedActivityLogs_FiltersByPayload));

            var response = await sut.GetPagedActivityLogs("SBC.OE.LeagueSorting", "QTE-B", Day1.AddDays(-30), new MetaData { CurrentPage = 1, PageSize = 10 });

            Assert.Equal(new[] { "b-1" }, response.Result.ActivityLogs.Select(l => l.To));
        }

        [Fact]
        public async Task GetPagedActivityLogs_Pages_AndReportsTheTotals()
        {
            var sut = await SeededRepository(nameof(GetPagedActivityLogs_Pages_AndReportsTheTotals));

            var second = await sut.GetPagedActivityLogs("SBC.OE.LeagueSorting", null, Day1.AddDays(-30), new MetaData { CurrentPage = 2, PageSize = 2 });

            Assert.Equal(new[] { "a-1" }, second.Result.ActivityLogs.Select(l => l.To));
            Assert.Equal((2, 2, 3, 2), (second.Result.MetaData.CurrentPage, second.Result.MetaData.PageSize, second.Result.MetaData.TotalCount, second.Result.MetaData.TotalPages));
        }

        [Fact]
        public async Task GetPagedActivityLogs_WithoutMetaData_ReturnsTheFirstPageOfTen()
        {
            var sut = await SeededRepository(nameof(GetPagedActivityLogs_WithoutMetaData_ReturnsTheFirstPageOfTen));

            var response = await sut.GetPagedActivityLogs("SBC.OE.LeagueSorting", null, Day1.AddDays(-30), null);

            Assert.Equal(3, response.Result.ActivityLogs.Count);
            Assert.Equal((1, 10), (response.Result.MetaData.CurrentPage, response.Result.MetaData.PageSize));
        }
    }
}
