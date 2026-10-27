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
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.ClientImport
{
    // GM-1001: the Create import accepts an optional Client ID. It must be rejected when it already
    // belongs to another client (naming that client's LimitId when it differs from the file's), and
    // a Platform that ignores it must not leave a half-configured client under the wrong ID.
    public class ClientImportRepositoryCreateClientIdTests
    {
        private static readonly Guid QteId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid OktId = Guid.Parse("B439428D-BCB8-46E0-E03C-08DF292152D3");
        private static readonly Guid ChosenId = Guid.Parse("6989B427-4234-4AC9-4791-08DEFEC92A2C");

        private static (ClientImportRepository Sut, Mock<IBackofficeRepository> Repo) Build(
            string dbName,
            Func<ClientListResponse.Client, Guid?> platformAssigns = null)
        {
            var ctx = new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(dbName).Options);
            var sportsbookCtx = new SportsbookContext(new DbContextOptionsBuilder<SportsbookContext>().UseInMemoryDatabase(dbName).Options);

            var repo = new Mock<IBackofficeRepository>();
            repo.Setup(r => r.GetClientList(It.IsAny<GetClientListRequest>(), It.IsAny<List<string>>()))
                .ReturnsAsync(new BaseResponse<ClientListResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ClientListResponse
                    {
                        Clients = new List<ClientListResponse.Client>
                        {
                            new ClientListResponse.Client { ClientId = 1, ClientCode = "QTE", Name = "QTE Sports", PlatformClientId = QteId, LimitId = "2535" },
                            new ClientListResponse.Client { ClientId = 2, ClientCode = "OKT", Name = "Oakton Sports", PlatformClientId = OktId, LimitId = "10939" }
                        }
                    }
                });
            repo.Setup(r => r.GetPlatformClients())
                .ReturnsAsync(new BaseResponse<List<PlatformClientResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<PlatformClientResponse>
                    {
                        new PlatformClientResponse { ClientId = QteId, ClientCode = "QTE", LimitId = "2535" },
                        new PlatformClientResponse { ClientId = OktId, ClientCode = "OKT", LimitId = "10939" }
                    }
                });
            // The real AddOrUpdateClient sets PlatformClientId only when its Platform call succeeds.
            repo.Setup(r => r.AddOrUpdateClient(It.IsAny<ClientListResponse.Client>()))
                .Callback<ClientListResponse.Client>(c => c.PlatformClientId = (platformAssigns ?? (x => x.RequestedPlatformClientId ?? Guid.NewGuid()))(c))
                .ReturnsAsync(new BaseResponse<bool> { StatusCode = StatusCode.Success, Result = true });

            var activityLog = new Mock<IBackofficeActivityLogRepository>();
            activityLog.Setup(a => a.ProcessActivityLog(It.IsAny<OnActivityLogMessage>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            return (new ClientImportRepository(ctx, sportsbookCtx, repo.Object, activityLog.Object, new ConfigurationBuilder().Build(), new Mock<IRabbitMqRepository>().Object), repo);
        }

        private static ClientImportRowRequest Row(string code, string clientId, string limitId = "10939", int rowNumber = 2, string currency = "USD") => new ClientImportRowRequest
        {
            RowNumber = rowNumber,
            ClientId = clientId,
            ClientCode = code,
            ClientName = $"{code} Sports",
            Website = $"https://{code.ToLowerInvariant()}.example.com",
            LimitId = limitId,
            Endpoint = "https://operator.example.com",
            ReferenceClientCode = "QTE",
            CurrencyCode = currency
        };

        private static async Task<ClientImportRowResult> Validate(ClientImportRepository sut, params ClientImportRowRequest[] rows) =>
            (await sut.ValidateBatch(new ValidateClientImportRequest { Rows = rows.ToList() })).Result.Rows.First();

        [Theory]
        [InlineData("")]
        [InlineData("6989B427-4234-4AC9-4791-08DEFEC92A2C")]
        public async Task Validate_NewOrBlankClientId_IsReady(string clientId)
        {
            var (sut, _) = Build(nameof(Validate_NewOrBlankClientId_IsReady) + clientId);

            var result = await Validate(sut, Row("NWS", clientId));

            Assert.Equal("Ready", result.Status);
        }

        [Fact]
        public async Task Validate_ExistingClientIdWithOtherLimitId_NamesTheClientAndItsLimitId()
        {
            var (sut, _) = Build(nameof(Validate_ExistingClientIdWithOtherLimitId_NamesTheClientAndItsLimitId));

            var result = await Validate(sut, Row("NWS", OktId.ToString(), limitId: "2535"));

            Assert.Equal("Error", result.Status);
            Assert.Contains("already belongs to client \"OKT\" with LimitId 10939, not 2535", result.Message);
        }

        [Fact]
        public async Task Validate_ExistingClientIdWithSameLimitId_IsRejected()
        {
            var (sut, _) = Build(nameof(Validate_ExistingClientIdWithSameLimitId_IsRejected));

            var result = await Validate(sut, Row("NWS", OktId.ToString(), limitId: "10939"));

            Assert.Equal("Error", result.Status);
            Assert.Contains($"Client ID {OktId} already exists (client \"OKT\")", result.Message);
        }

        [Fact]
        public async Task Validate_DifferentClientIdsForOneClient_IsRejected()
        {
            var (sut, _) = Build(nameof(Validate_DifferentClientIdsForOneClient_IsRejected));

            var result = await Validate(sut, Row("NWS", ChosenId.ToString(), rowNumber: 2), Row("NWS", Guid.NewGuid().ToString(), rowNumber: 3, currency: "EUR"));

            Assert.Equal("Error", result.Status);
            Assert.Contains("different Client IDs for the same client", result.Message);
        }

        [Fact]
        public async Task Validate_SameClientIdForTwoClientsInTheFile_RejectsTheSecond()
        {
            var (sut, _) = Build(nameof(Validate_SameClientIdForTwoClientsInTheFile_RejectsTheSecond));

            var response = (await sut.ValidateBatch(new ValidateClientImportRequest
            {
                Rows = new List<ClientImportRowRequest> { Row("NWS", ChosenId.ToString(), rowNumber: 2), Row("ZZZ", ChosenId.ToString(), rowNumber: 3) }
            })).Result;

            Assert.Equal("Ready", response.Rows.Single(r => r.ClientCode == "NWS").Status);
            var second = response.Rows.Single(r => r.ClientCode == "ZZZ");
            Assert.Equal("Error", second.Status);
            Assert.Contains("also used by Client Code \"NWS\"", second.Message);
        }

        [Fact]
        public async Task Commit_SendsTheChosenClientIdToPlatform()
        {
            var (sut, repo) = Build(nameof(Commit_SendsTheChosenClientIdToPlatform));

            await sut.CommitBatch(new CommitClientImportRequest { Environment = "Dev", Rows = new List<ClientImportRowRequest> { Row("NWS", ChosenId.ToString()) } }, "u1", "tester");

            var sent = (ClientListResponse.Client)repo.Invocations.Single(i => i.Method.Name == nameof(IBackofficeRepository.AddOrUpdateClient)).Arguments[0];
            Assert.Equal(ChosenId, sent.RequestedPlatformClientId);
        }

        [Fact]
        public async Task Commit_ClientIdTakenSinceValidation_FailsWithoutCreating()
        {
            var (sut, repo) = Build(nameof(Commit_ClientIdTakenSinceValidation_FailsWithoutCreating));

            var response = (await sut.CommitBatch(new CommitClientImportRequest { Environment = "Dev", Rows = new List<ClientImportRowRequest> { Row("NWS", OktId.ToString(), limitId: "2535") } }, "u1", "tester")).Result;

            var row = Assert.Single(response.Rows);
            Assert.Equal(ClientImportRowStatus.Failed, row.Status);
            repo.Verify(r => r.AddOrUpdateClient(It.IsAny<ClientListResponse.Client>()), Times.Never);
        }

        [Fact]
        public async Task Commit_PlatformIgnoresTheChosenClientId_FailsBeforeCopyingSettings()
        {
            var generated = Guid.NewGuid();
            var (sut, _) = Build(nameof(Commit_PlatformIgnoresTheChosenClientId_FailsBeforeCopyingSettings), _ => generated);

            var response = (await sut.CommitBatch(new CommitClientImportRequest { Environment = "Dev", Rows = new List<ClientImportRowRequest> { Row("NWS", ChosenId.ToString()) } }, "u1", "tester")).Result;

            var row = Assert.Single(response.Rows);
            Assert.Equal(ClientImportRowStatus.Failed, row.Status);
            Assert.Contains($"registered the client as {generated} instead of the requested Client ID {ChosenId}", row.Message);
        }
    }
}
