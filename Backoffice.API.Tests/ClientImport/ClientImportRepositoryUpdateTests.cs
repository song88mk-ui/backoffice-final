using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
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
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.ClientImport
{
    public class ClientImportRepositoryUpdateTests
    {
        private sealed class Harness
        {
            public ClientImportRepository Sut { get; init; }
            public Mock<IBackofficeRepository> BackofficeRepo { get; init; }
            public Mock<IBackofficeActivityLogRepository> ActivityLog { get; init; }
            public BackofficeContext Context { get; init; }

            public ClientListResponse.Client CapturedClient() =>
                BackofficeRepo.Invocations
                    .Where(i => i.Method.Name == nameof(IBackofficeRepository.AddOrUpdateClient))
                    .Select(i => (ClientListResponse.Client)i.Arguments[0])
                    .Single();

            public void VerifyClientNotUpdated() =>
                BackofficeRepo.Verify(r => r.AddOrUpdateClient(It.IsAny<ClientListResponse.Client>()), Times.Never);
        }

        private static Harness BuildHarness(
            string dbName,
            ClientInformation existingClient,
            Dictionary<int, List<ClientListResponse.BetSetting>> betSettingsByClientId = null,
            BaseResponse<List<PlatformClientResponse>> platformResponse = null)
        {
            var ctx = new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(dbName).Options);
            ctx.ClientInformation.Add(existingClient);
            ctx.SaveChanges();

            var sportsbookCtx = new SportsbookContext(new DbContextOptionsBuilder<SportsbookContext>().UseInMemoryDatabase(dbName).Options);

            var backofficeRepo = new Mock<IBackofficeRepository>();
            backofficeRepo.Setup(r => r.GetPlatformClients())
                .ReturnsAsync(platformResponse ?? new BaseResponse<List<PlatformClientResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<PlatformClientResponse>
                    {
                        new PlatformClientResponse { ClientId = ClientIdFor(existingClient.Brand), ClientIdRef = existingClient.ClientId, ClientCode = existingClient.Brand, LimitId = "2535", Endpoint = "https://operator.example.com" }
                    }
                });
            backofficeRepo.Setup(r => r.GetBetSettingsBulk(It.IsAny<List<int>>()))
                .ReturnsAsync(new BaseResponse<Dictionary<int, List<ClientListResponse.BetSetting>>>
                {
                    StatusCode = StatusCode.Success,
                    Result = betSettingsByClientId ?? new Dictionary<int, List<ClientListResponse.BetSetting>>()
                });
            // The real AddOrUpdateClient sets PlatformClientId only when its Platform call succeeds.
            backofficeRepo.Setup(r => r.AddOrUpdateClient(It.IsAny<ClientListResponse.Client>()))
                .Callback<ClientListResponse.Client>(c => c.PlatformClientId = ClientIdFor(c.ClientCode))
                .ReturnsAsync(new BaseResponse<bool> { StatusCode = StatusCode.Success, Result = true });

            var activityLog = new Mock<IBackofficeActivityLogRepository>();
            activityLog.Setup(a => a.ProcessActivityLog(It.IsAny<OnActivityLogMessage>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            activityLog.Setup(a => a.ProcessActivityLog(It.IsAny<OnActivityLogListMessage>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var sut = new ClientImportRepository(ctx, sportsbookCtx, backofficeRepo.Object, activityLog.Object, new ConfigurationBuilder().Build(), new Mock<IRabbitMqRepository>().Object);

            return new Harness { Sut = sut, BackofficeRepo = backofficeRepo, ActivityLog = activityLog, Context = ctx };
        }

        private static ClientInformation MakeClient(int clientId, string brand) => new ClientInformation
        {
            ClientId = clientId,
            Brand = brand,
            Name = $"{brand} Sports",
            Website = $"https://{brand.ToLowerInvariant()}.example.com",
            Region = "HK",
            Email = $"ops@{brand.ToLowerInvariant()}.example.com",
            TelephoneNumber = "+852 5555 0101",
            TaxConfig = "{\"StakeTax\":0.05,\"WinningsTax\":0.1,\"ExcludeStakeSportTypes\":\"1,2\",\"ExcludeWinningsSportTypes\":\"3\"}",
            MonthlyFee = 500m,
            SportsbookShare = 0.1m,
            JackpotShare = 0.08m,
            StatusId = (int)ClientStatus.Normal,
            IsDeleted = false
        };

        // A fixed Platform Client ID per test client, so rows can reference it like a real export would.
        private static Guid ClientIdFor(string brand) => brand.ToUpperInvariant() switch
        {
            "NWS" => Guid.Parse("6989B427-4234-4AC9-4791-08DEFEC92A2C"),
            "QTE" => Guid.Parse("11111111-2222-3333-4444-555555555555"),
            "VRT" => Guid.Parse("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE"),
            _ => throw new ArgumentException($"No test Client ID for {brand}")
        };

        private static ClientUpdateRowRequest MakeRow(string clientCode) => new ClientUpdateRowRequest
        {
            RowNumber = 2,
            ClientId = ClientIdFor(clientCode).ToString(),
            ClientCode = clientCode,
            ClientName = string.Empty,
            Website = string.Empty,
            Region = string.Empty,
            Email = string.Empty,
            LimitId = string.Empty,
            Endpoint = string.Empty,
            CurrencyCode = string.Empty,
            CurrencyAction = string.Empty
        };

        private static ClientUpdateRowRequest MakeCurrencyRow(string clientCode, string currency, decimal soccerMin, decimal soccerMax, decimal soccerGame)
        {
            var row = MakeRow(clientCode);
            row.CurrencyCode = currency;
            row.SoccerMinPerTicket = soccerMin;
            row.SoccerMaxPerTicket = soccerMax;
            row.SoccerMaxPerGame = soccerGame;
            row.AllSportsMinPerTicket = 1;
            row.AllSportsMaxPerTicket = 5000;
            row.AllSportsMaxPerGame = 10000;
            row.ParlayMinPerTicket = 1;
            row.ParlayMaxPerTicket = 2000;
            row.ParlayMaxPayoutPerTicket = 50000;
            row.ParlayMaxWinningsBonus = 0.02m;
            return row;
        }

        private static CommitClientUpdateRequest MakeRequest(params ClientUpdateRowRequest[] rows) => new CommitClientUpdateRequest
        {
            BatchId = Guid.NewGuid(),
            Environment = "Staging",
            Rows = rows.ToList()
        };

        [Fact]
        public async Task BlankOptionalFields_PreserveExistingClientValues()
        {
            var h = BuildHarness(nameof(BlankOptionalFields_PreserveExistingClientValues), MakeClient(1, "NWS"));

            // ClientName is the one real change, so the row isn't a no-op and reaches AddOrUpdateClient.
            var row = MakeRow("NWS");
            row.ClientName = "NWS Sports Updated";

            var response = await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            Assert.Equal(1, response.Result.SucceededCount);
            var captured = h.CapturedClient();
            Assert.Equal("NWS Sports Updated", captured.Name);
            Assert.Equal("https://nws.example.com", captured.Website);
            Assert.Equal("HK", captured.Region);
            Assert.Equal("ops@nws.example.com", captured.Email);
            Assert.Equal(500m, captured.MonthlyFee);
            Assert.Equal(0.1m, captured.RevenueShare.SportsbookShare);
            Assert.Equal(0.08m, captured.RevenueShare.JackpotShare);
            Assert.Equal("2535", captured.LimitId);
            Assert.Equal("https://operator.example.com", captured.Endpoint);
        }

        [Fact]
        public async Task Update_PreservesTaxConfigAndTelephoneNumber()
        {
            var h = BuildHarness(nameof(Update_PreservesTaxConfigAndTelephoneNumber), MakeClient(1, "NWS"));

            var row = MakeRow("NWS");
            row.MonthlyFee = 750m;

            await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            var captured = h.CapturedClient();
            Assert.Equal("+852 5555 0101", captured.TelephoneNumber);
            // Stored and sent as fractions — no percent scaling on this path.
            Assert.Equal(0.05m, captured.TaxConfig.StakeTax);
            Assert.Equal(0.1m, captured.TaxConfig.WinningsTax);
            Assert.Equal("1,2", captured.TaxConfig.ExcludeStakeSportTypes);
            Assert.Equal("3", captured.TaxConfig.ExcludeWinningsSportTypes);
        }

        [Fact]
        public async Task EditingExistingCurrency_PreservesCricketPriceboostBetBuilder()
        {
            var existingBetSetting = new ClientListResponse.BetSetting
            {
                CurrencyCode = "USD",
                BetSettingSoccer = new ClientListResponse.BetSettingSoccer { MinPerTicket = 1, MaxPerTicket = 5000, MaxPerGame = 10000 },
                BetSettingAllSports = new ClientListResponse.BetSettingAllSports { MinPerTicket = 1, MaxPerTicket = 5000, MaxPerGame = 10000 },
                BetSettingParlay = new ClientListResponse.BetSettingParlay { MinPerTicket = 1, MaxPerTicket = 2000, MaxPayoutPerTicket = 50000, MaxWinningsBonus = 0.02m },
                BetSettingCricket = new ClientListResponse.BetSettingCricket { MinPerTicket = 99, MaxPerTicket = 9999, MaxPerGame = 99999 },
                BetSettingPriceboost = new ClientListResponse.BetSettingPriceboost { MaxStake = 777 },
                BetSettingBetBuilder = new ClientListResponse.BetSettingBetBuilder { MinStake = 55, MaxStake = 666 }
            };
            var h = BuildHarness(nameof(EditingExistingCurrency_PreservesCricketPriceboostBetBuilder), MakeClient(2, "QTE"),
                new Dictionary<int, List<ClientListResponse.BetSetting>> { [2] = new List<ClientListResponse.BetSetting> { existingBetSetting } });

            await h.Sut.CommitUpdateBatch(MakeRequest(MakeCurrencyRow("QTE", "USD", 2, 6000, 12000)), "user-1", "Tester");

            var usd = h.CapturedClient().BetSettings.Single(b => b.CurrencyCode == "USD");
            Assert.Equal(2, usd.BetSettingSoccer.MinPerTicket);
            Assert.Equal(6000, usd.BetSettingSoccer.MaxPerTicket);
            Assert.Equal(99, usd.BetSettingCricket.MinPerTicket);
            Assert.Equal(9999, usd.BetSettingCricket.MaxPerTicket);
            Assert.Equal(99999, usd.BetSettingCricket.MaxPerGame);
            Assert.Equal(777, usd.BetSettingPriceboost.MaxStake);
            Assert.Equal(55, usd.BetSettingBetBuilder.MinStake);
            Assert.Equal(666, usd.BetSettingBetBuilder.MaxStake);
        }

        [Fact]
        public async Task AddingBrandNewCurrency_DefaultsCricketPriceboostBetBuilderFromSoccer()
        {
            var h = BuildHarness(nameof(AddingBrandNewCurrency_DefaultsCricketPriceboostBetBuilderFromSoccer), MakeClient(3, "VRT"));

            await h.Sut.CommitUpdateBatch(MakeRequest(MakeCurrencyRow("VRT", "EUR", 3, 7000, 14000)), "user-1", "Tester");

            var eur = h.CapturedClient().BetSettings.Single(b => b.CurrencyCode == "EUR");
            Assert.Equal(3, eur.BetSettingCricket.MinPerTicket);
            Assert.Equal(7000, eur.BetSettingCricket.MaxPerTicket);
            Assert.Equal(7000, eur.BetSettingPriceboost.MaxStake);
            Assert.Equal(3, eur.BetSettingBetBuilder.MinStake);
            Assert.Equal(7000, eur.BetSettingBetBuilder.MaxStake);
        }

        [Fact]
        public async Task LowercaseClientCode_CommitsLikeValidationSaysItWill()
        {
            var h = BuildHarness(nameof(LowercaseClientCode_CommitsLikeValidationSaysItWill), MakeClient(1, "NWS"));

            var row = MakeRow("nws");
            row.MonthlyFee = 750m;

            var response = await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            Assert.Equal(1, response.Result.SucceededCount);
            Assert.Equal(750m, h.CapturedClient().MonthlyFee);
        }

        [Fact]
        public async Task CurrencyRowWithBlankBetColumn_FailsRowWithoutUpdating()
        {
            var h = BuildHarness(nameof(CurrencyRowWithBlankBetColumn_FailsRowWithoutUpdating), MakeClient(1, "NWS"));

            var row = MakeCurrencyRow("NWS", "USD", 1, 5000, 10000);
            row.ParlayMaxPayoutPerTicket = null;

            var response = await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            Assert.Equal(1, response.Result.FailedCount);
            Assert.Contains("ParlayMaxPayoutPerTicket", response.Result.Rows.Single().Message);
            h.VerifyClientNotUpdated();
        }

        [Fact]
        public async Task CommitRevalidates_UnknownCurrencyActionIsRejected()
        {
            var h = BuildHarness(nameof(CommitRevalidates_UnknownCurrencyActionIsRejected), MakeClient(1, "NWS"));

            var row = MakeCurrencyRow("NWS", "USD", 1, 5000, 10000);
            row.CurrencyAction = "Delete";

            var response = await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            Assert.Equal(1, response.Result.FailedCount);
            Assert.Contains("CurrencyAction", response.Result.Rows.Single().Message);
            h.VerifyClientNotUpdated();
        }

        [Fact]
        public async Task PlatformListUnavailable_AbortsBatchBeforeTouchingAnything()
        {
            var h = BuildHarness(nameof(PlatformListUnavailable_AbortsBatchBeforeTouchingAnything), MakeClient(1, "NWS"),
                platformResponse: new BaseResponse<List<PlatformClientResponse>> { StatusCode = StatusCode.Error, Message = "timeout", Result = null });

            var row = MakeRow("NWS");
            row.MonthlyFee = 750m;

            var response = await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Empty(h.Context.ClientImportBatch);
            h.VerifyClientNotUpdated();
        }

        [Fact]
        public async Task ClientMissingOnPlatform_FailsRowInsteadOfBlankingLimitIdAndEndpoint()
        {
            var h = BuildHarness(nameof(ClientMissingOnPlatform_FailsRowInsteadOfBlankingLimitIdAndEndpoint), MakeClient(1, "NWS"),
                platformResponse: new BaseResponse<List<PlatformClientResponse>> { StatusCode = StatusCode.Success, Result = new List<PlatformClientResponse>() });

            var row = MakeRow("NWS");
            row.MonthlyFee = 750m;

            var response = await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            Assert.Equal(1, response.Result.FailedCount);
            Assert.Contains("was not found", response.Result.Rows.Single().Message);
            h.VerifyClientNotUpdated();
        }

        [Theory]
        [InlineData("", "Client ID is required")]
        [InlineData("not-a-guid", "is not a valid Client ID")]
        [InlineData("00000000-0000-0000-0000-000000000999", "was not found")]
        public async Task BadClientId_FailsRowWithoutUpdating(string clientId, string expectedMessage)
        {
            var h = BuildHarness(nameof(BadClientId_FailsRowWithoutUpdating) + clientId, MakeClient(1, "NWS"));

            var row = MakeRow("NWS");
            row.ClientId = clientId;
            row.MonthlyFee = 750m;

            var response = await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            Assert.Equal(1, response.Result.FailedCount);
            Assert.Contains(expectedMessage, response.Result.Rows.Single().Message);
            h.VerifyClientNotUpdated();
        }

        [Fact]
        public async Task ClientIdOfAnotherClient_IsRejectedEvenIfThatClientExists()
        {
            // NWS exists on Platform, but the row says it's QTE — someone copied the wrong ID.
            var h = BuildHarness(nameof(ClientIdOfAnotherClient_IsRejectedEvenIfThatClientExists), MakeClient(1, "NWS"));

            var row = MakeRow("NWS");
            row.ClientCode = "QTE";
            row.MonthlyFee = 750m;

            var response = await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            Assert.Equal(1, response.Result.FailedCount);
            Assert.Contains("belongs to client \"NWS\", not \"QTE\"", response.Result.Rows.Single().Message);
            h.VerifyClientNotUpdated();
        }

        [Fact]
        public async Task LowercaseClientId_StillMatches()
        {
            var h = BuildHarness(nameof(LowercaseClientId_StillMatches), MakeClient(1, "NWS"));

            var row = MakeRow("NWS");
            row.ClientId = row.ClientId.ToLowerInvariant();
            row.MonthlyFee = 750m;

            var response = await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            Assert.Equal(1, response.Result.SucceededCount);
            Assert.Equal(750m, h.CapturedClient().MonthlyFee);
        }

        [Fact]
        public async Task ClientIdIsMatchedByClientIdRef_NotJustClientCode()
        {
            // Platform's ClientIdRef is the real link to Backoffice; the Platform code can differ in older data.
            var client = MakeClient(1, "NWS");
            var h = BuildHarness(nameof(ClientIdIsMatchedByClientIdRef_NotJustClientCode), client,
                platformResponse: new BaseResponse<List<PlatformClientResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<PlatformClientResponse>
                    {
                        new PlatformClientResponse { ClientId = ClientIdFor("NWS"), ClientIdRef = 1, ClientCode = "NWS-LEGACY", LimitId = "2535", Endpoint = "https://operator.example.com" }
                    }
                });

            var row = MakeRow("NWS");
            row.MonthlyFee = 750m;

            var response = await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            Assert.Equal(1, response.Result.SucceededCount);
            Assert.Equal(1, h.CapturedClient().ClientId);
        }

        [Fact]
        public async Task BetSettingsReadFails_AbortsBatchInsteadOfWipingCurrencies()
        {
            var h = BuildHarness(nameof(BetSettingsReadFails_AbortsBatchInsteadOfWipingCurrencies), MakeClient(1, "NWS"));
            h.BackofficeRepo.Setup(r => r.GetBetSettingsBulk(It.IsAny<List<int>>()))
                .ReturnsAsync(new BaseResponse<Dictionary<int, List<ClientListResponse.BetSetting>>> { StatusCode = StatusCode.Error, Message = "bad JSON", Result = null });

            // A Website-only change: before the fix this sent an empty bet-settings list, which deletes every currency.
            var row = MakeRow("NWS");
            row.Website = "https://new.example.com";

            var response = await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Contains("bet settings", response.Message);
            Assert.Empty(h.Context.ClientImportBatch);
            h.VerifyClientNotUpdated();
        }

        [Fact]
        public async Task ReadThrowsBeforeBatchStarts_LeavesNoRunningBatchLockingTheEnvironment()
        {
            var h = BuildHarness(nameof(ReadThrowsBeforeBatchStarts_LeavesNoRunningBatchLockingTheEnvironment), MakeClient(1, "NWS"));
            h.BackofficeRepo.Setup(r => r.GetBetSettingsBulk(It.IsAny<List<int>>()))
                .ThrowsAsync(new TimeoutException("Backoffice DB timeout"));

            var row = MakeRow("NWS");
            row.MonthlyFee = 750m;

            await Assert.ThrowsAsync<TimeoutException>(() => h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester"));

            Assert.DoesNotContain(h.Context.ClientImportBatch, b => b.Status == ClientImportBatchStatus.Running);
            Assert.False((await h.Sut.IsImportRunning("Staging")).Result);
        }

        [Fact]
        public async Task PlatformUpdateFails_RowIsFailedButBackofficeChangesAreStillAudited()
        {
            var h = BuildHarness(nameof(PlatformUpdateFails_RowIsFailedButBackofficeChangesAreStillAudited), MakeClient(1, "NWS"));
            // AddOrUpdateClient's real behaviour on a Platform failure: Success, but PlatformClientId never set.
            h.BackofficeRepo.Setup(r => r.AddOrUpdateClient(It.IsAny<ClientListResponse.Client>()))
                .ReturnsAsync(new BaseResponse<bool> { StatusCode = StatusCode.Success, Result = true });

            var row = MakeRow("NWS");
            row.MonthlyFee = 750m;

            var response = await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            var result = response.Result.Rows.Single();
            Assert.Equal(1, response.Result.FailedCount);
            Assert.Equal(ClientImportRowStatus.Failed, result.Status);
            Assert.Contains("Platform update failed", result.Message);
            Assert.Contains(result.Changes, c => c.FieldName == "MonthlyFee");
            h.ActivityLog.Verify(a => a.ProcessActivityLog(It.Is<OnActivityLogListMessage>(m => m.OnActivityLogMessages.Any(x => x.Payload.EndsWith("|MonthlyFee"))), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Export_BetSettingsReadFails_ReturnsErrorNotAnExportWithoutCurrencies()
        {
            var h = BuildHarness(nameof(Export_BetSettingsReadFails_ReturnsErrorNotAnExportWithoutCurrencies), MakeClient(1, "NWS"));
            h.BackofficeRepo.Setup(r => r.GetBetSettingsBulk(It.IsAny<List<int>>()))
                .ReturnsAsync(new BaseResponse<Dictionary<int, List<ClientListResponse.BetSetting>>> { StatusCode = StatusCode.Error, Message = "bad JSON", Result = null });

            var response = await h.Sut.ExportClients();

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Null(response.Result);
        }

        [Fact]
        public async Task Validate_ReportsClientIdProblemsPerClient()
        {
            var h = BuildHarness(nameof(Validate_ReportsClientIdProblemsPerClient), MakeClient(1, "NWS"));

            var good = MakeRow("NWS");
            var unknown = MakeRow("NWS");
            unknown.RowNumber = 3;
            unknown.ClientId = "00000000-0000-0000-0000-000000000999";

            var response = await h.Sut.ValidateUpdateBatch(new ValidateClientUpdateRequest { Rows = new List<ClientUpdateRowRequest> { good, unknown } });

            Assert.Equal(1, response.Result.ReadyCount);
            Assert.Equal(1, response.Result.ErrorCount);
            Assert.Contains(response.Result.Rows, r => r.Status == "Error" && r.Message.Contains("was not found"));
        }

        [Fact]
        public async Task AuditLogWriteFailure_StillReportsUpdatedClientAsSucceeded()
        {
            var h = BuildHarness(nameof(AuditLogWriteFailure_StillReportsUpdatedClientAsSucceeded), MakeClient(1, "NWS"));
            h.ActivityLog.Setup(a => a.ProcessActivityLog(It.IsAny<OnActivityLogListMessage>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("ActivityLog2 unavailable"));

            var row = MakeRow("NWS");
            row.MonthlyFee = 750m;

            var response = await h.Sut.CommitUpdateBatch(MakeRequest(row), "user-1", "Tester");

            Assert.Equal(1, response.Result.SucceededCount);
            Assert.Contains("audit log write failed", h.Context.ClientImportBatchRow.Single().StepsCompleted);
        }
    }
}
