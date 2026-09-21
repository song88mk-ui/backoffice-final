using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OE.Shared.Data.Sportsbook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using StatusCode = Backoffice.Shared.Enums.StatusCode;

namespace Backoffice.API.Repositories
{
    public class ClientImportRepository : IClientImportRepository
    {
        private readonly BackofficeContext _backofficeContext;
        private readonly SportsbookContext _sportsbookContext;
        private readonly IBackofficeRepository _backofficeRepository;
        private readonly IBackofficeActivityLogRepository _activityLogRepository;
        private readonly IConfiguration _configuration;

        public ClientImportRepository(
            BackofficeContext backofficeContext,
            SportsbookContext sportsbookContext,
            IBackofficeRepository backofficeRepository,
            IBackofficeActivityLogRepository activityLogRepository,
            IConfiguration configuration)
        {
            _backofficeContext = backofficeContext;
            _sportsbookContext = sportsbookContext;
            _backofficeRepository = backofficeRepository;
            _activityLogRepository = activityLogRepository;
            _configuration = configuration;
        }

        private static readonly (string Match, string Environment)[] KnownDatabaseHosts = new[]
        {
            ("stg-song88-mssql-server", "Staging"),
            ("prd-song88-mssql-server", "Production"),
            ("k8s-dev-database", "Dev")
        };

        public BaseResponse<string> GetEnvironment()
        {
            var connectionString = _configuration.GetConnectionString("Backoffice") ?? string.Empty;

            var match = KnownDatabaseHosts.FirstOrDefault(h => connectionString.Contains(h.Match, StringComparison.OrdinalIgnoreCase));

            if (match.Environment == null)
            {
                return new BaseResponse<string>
                {
                    StatusCode = StatusCode.Error,
                    Message = "Could not determine which environment this is from the Backoffice connection string. Import is blocked until this is fixed."
                };
            }

            return new BaseResponse<string>
            {
                StatusCode = StatusCode.Success,
                Result = match.Environment
            };
        }

        public async Task<BaseResponse<bool>> IsImportRunning(string environment)
        {
            var isRunning = await _backofficeContext.ClientImportBatch
                .AsNoTracking()
                .AnyAsync(b => b.Environment == environment && b.Status == ClientImportBatchStatus.Running);

            return new BaseResponse<bool>
            {
                StatusCode = StatusCode.Success,
                Result = isRunning
            };
        }

        public async Task<BaseResponse<ValidateClientImportResponse>> ValidateBatch(ValidateClientImportRequest request)
        {
            var response = new ValidateClientImportResponse();

            var groups = GroupRows(request.Rows);

            var existingClients = (await _backofficeRepository.GetClientList(new GetClientListRequest
            {
                Page = 1,
                PageSize = int.MaxValue,
                SortBy = ClientSorting.SortByBusinessNameAsc
            })).Result?.Clients ?? new List<Backoffice.Shared.Models.Response.ClientListResponse.Client>();

            var existingCodes = existingClients.Select(c => c.ClientCode?.ToUpperInvariant()).ToHashSet();
            var existingNames = existingClients.Select(c => c.Name?.ToUpperInvariant()).ToHashSet();

            var namesSeenInFile = new Dictionary<string, string>();

            response.TotalClients = groups.Count;

            foreach (var group in groups)
            {
                var rowNumbers = string.Join(", ", group.Value.Select(r => r.RowNumber));
                var first = group.Value.First();
                var code = (first.ClientCode ?? string.Empty).Trim();
                var name = (first.ClientName ?? string.Empty).Trim();

                var result = new ClientImportRowResult
                {
                    RowNumber = first.RowNumber,
                    ClientCode = code,
                    ClientName = name,
                    Status = "Ready"
                };

                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                {
                    result.Status = "Error";
                    result.Message = "Client Code and Client Name are required.";
                }
                else if (group.Value.Select(r => (r.ClientName ?? string.Empty).Trim()).Distinct().Count() > 1)
                {
                    result.Status = "Error";
                    result.Message = $"Rows {rowNumbers} use Client Code \"{code}\" with different Client Names — this looks like two different clients sharing a code.";
                }
                else if (string.IsNullOrWhiteSpace(first.Website) || !first.Website.StartsWith("https://"))
                {
                    result.Status = "Error";
                    result.Message = "Website is required and must start with https://";
                }
                else if (group.Value.Any(r => string.IsNullOrWhiteSpace(r.CurrencyCode) || !Enum.TryParse<Currency>(r.CurrencyCode, true, out _)))
                {
                    var badCurrency = group.Value.First(r => string.IsNullOrWhiteSpace(r.CurrencyCode) || !Enum.TryParse<Currency>(r.CurrencyCode, true, out _));
                    result.Status = "Error";
                    result.Message = string.IsNullOrWhiteSpace(badCurrency.CurrencyCode)
                        ? $"Row {badCurrency.RowNumber}: CurrencyCode is required."
                        : $"Row {badCurrency.RowNumber}: \"{badCurrency.CurrencyCode}\" is not a recognized currency code.";
                }
                else if (existingCodes.Contains(code.ToUpperInvariant()))
                {
                    result.Status = "Error";
                    result.Message = $"Client Code \"{code}\" already exists.";
                }
                else if (existingNames.Contains(name.ToUpperInvariant()))
                {
                    result.Status = "Error";
                    result.Message = $"Client Name \"{name}\" already exists.";
                }
                else if (namesSeenInFile.TryGetValue(name.ToUpperInvariant(), out var otherCode))
                {
                    result.Status = "Error";
                    result.Message = $"Client Name \"{name}\" is also used by Client Code \"{otherCode}\" earlier in this file — check with whoever else is importing that these aren't meant to be the same client.";
                }
                else if (string.IsNullOrWhiteSpace(first.ReferenceClientCode))
                {
                    result.Status = "Error";
                    result.Message = "A reference client to clone the Selector Market Template setup from is required.";
                }
                else if (!existingCodes.Contains(first.ReferenceClientCode.Trim().ToUpperInvariant()))
                {
                    result.Status = "Error";
                    result.Message = $"Reference client \"{first.ReferenceClientCode}\" does not exist.";
                }

                if (result.Status == "Ready" && !string.IsNullOrWhiteSpace(name))
                {
                    namesSeenInFile[name.ToUpperInvariant()] = code;
                }

                response.Rows.Add(result);
            }

            response.ReadyCount = response.Rows.Count(r => r.Status == "Ready");
            response.ErrorCount = response.Rows.Count(r => r.Status == "Error");

            return new BaseResponse<ValidateClientImportResponse>
            {
                StatusCode = StatusCode.Success,
                Result = response
            };
        }

        public async Task<BaseResponse<ClientImportProgressResponse>> GetProgress(Guid batchId)
        {
            var batch = await _backofficeContext.ClientImportBatch.AsNoTracking().FirstOrDefaultAsync(b => b.BatchId == batchId);

            if (batch == null)
            {
                return new BaseResponse<ClientImportProgressResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = "Batch not found."
                };
            }

            var rows = await _backofficeContext.ClientImportBatchRow.AsNoTracking().Where(r => r.BatchId == batchId).ToListAsync();

            return new BaseResponse<ClientImportProgressResponse>
            {
                StatusCode = StatusCode.Success,
                Result = new ClientImportProgressResponse
                {
                    TotalClients = batch.TotalRows,
                    ProcessedClients = rows.Count(r => r.Status != ClientImportRowStatus.Pending),
                    SucceededClients = rows.Count(r => r.Status == ClientImportRowStatus.Succeeded),
                    FailedClients = rows.Count(r => r.Status == ClientImportRowStatus.Failed),
                    IsComplete = batch.Status != ClientImportBatchStatus.Running
                }
            };
        }

        public async Task<BaseResponse<CommitClientImportResponse>> CommitBatch(CommitClientImportRequest request, string userId, string userName)
        {
            var lockResult = await IsImportRunning(request.Environment);
            if (lockResult.Result)
            {
                return new BaseResponse<CommitClientImportResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"An import is already running for {request.Environment}. Wait for it to finish before starting another."
                };
            }

            var groups = GroupRows(request.Rows);

            var batch = new ClientImportBatch
            {
                BatchId = request.BatchId != Guid.Empty ? request.BatchId : Guid.NewGuid(),
                Environment = request.Environment,
                Status = ClientImportBatchStatus.Running,
                StartedByUserId = userId,
                StartedByUserName = userName,
                StartedAt = DateTime.UtcNow,
                TotalRows = groups.Count
            };

            _backofficeContext.ClientImportBatch.Add(batch);
            await _backofficeContext.SaveChangesAsync();

            var response = new CommitClientImportResponse
            {
                BatchId = batch.BatchId,
                TotalClients = groups.Count
            };

            try
            {
                foreach (var group in groups)
                {
                    var result = await CommitClient(batch.BatchId, group.Value);
                    response.Rows.Add(result);
                }
            }
            finally
            {
                response.SucceededCount = response.Rows.Count(r => r.Status == ClientImportRowStatus.Succeeded);
                response.FailedCount = response.Rows.Count(r => r.Status == ClientImportRowStatus.Failed);

                batch.Status = response.FailedCount == 0 ? ClientImportBatchStatus.Completed : ClientImportBatchStatus.Failed;
                batch.CompletedAt = DateTime.UtcNow;
                batch.SucceededRows = response.SucceededCount;
                batch.FailedRows = response.FailedCount;
                await _backofficeContext.SaveChangesAsync();

                await _activityLogRepository.ProcessActivityLog(new OnActivityLogMessage
                {
                    Page = "Clients - Import",
                    Activity = "Batch client import",
                    Payload = JsonSerializer.Serialize(new { batch.BatchId, request.Environment, response.TotalClients, response.SucceededCount, response.FailedCount }),
                    UserId = userId,
                    UserName = userName,
                    TimeStamp = DateTime.UtcNow
                });
            }

            return new BaseResponse<CommitClientImportResponse>
            {
                StatusCode = StatusCode.Success,
                Result = response
            };
        }

        private async Task<ClientImportRowResult> CommitClient(Guid batchId, List<ClientImportRowRequest> rows)
        {
            var first = rows.First();
            var code = first.ClientCode.Trim();
            var name = first.ClientName.Trim();

            var row = new ClientImportBatchRow
            {
                BatchId = batchId,
                RowNumber = first.RowNumber,
                ClientCode = code,
                ClientName = name,
                Status = ClientImportRowStatus.Pending
            };
            _backofficeContext.ClientImportBatchRow.Add(row);
            await _backofficeContext.SaveChangesAsync();

            var stepsCompleted = new List<string>();

            try
            {
                var client = ToClientListResponseClient(rows);
                var createResult = await _backofficeRepository.AddOrUpdateClient(client);

                if (createResult.StatusCode != Backoffice.Shared.Enums.StatusCode.Success)
                {
                    return await FailRow(row, createResult.Message ?? "Failed to create client record.");
                }
                stepsCompleted.Add("Backoffice client record created");

                if (client.PlatformClientId == null)
                {
                    return await FailRow(row, "Client record was created in Backoffice, but Platform registration did not complete. Needs manual follow-up.");
                }
                var newPlatformClientId = client.PlatformClientId.Value;
                stepsCompleted.Add("Platform registration confirmed");

                var platformClients = (await _backofficeRepository.GetPlatformClients()).Result ?? new List<PlatformClientResponse>();
                var referenceClient = platformClients.FirstOrDefault(c => string.Equals(c.ClientCode, first.ReferenceClientCode.Trim(), StringComparison.OrdinalIgnoreCase));
                if (referenceClient == null)
                {
                    return await FailRow(row, $"Reference client \"{first.ReferenceClientCode}\" was not found in Platform.", stepsCompleted);
                }

                await CopySelectorMarketTemplate(newPlatformClientId, referenceClient.ClientId);
                stepsCompleted.Add("Selector Market Template copied");

                await RegisterTradingClient(newPlatformClientId, name, code);
                stepsCompleted.Add("Trading DB registration");

                row.Status = ClientImportRowStatus.Succeeded;
                row.StepsCompleted = string.Join(" -> ", stepsCompleted);
                row.ResultClientId = newPlatformClientId;
                await _backofficeContext.SaveChangesAsync();

                return new ClientImportRowResult
                {
                    RowNumber = first.RowNumber,
                    ClientCode = code,
                    ClientName = name,
                    Status = ClientImportRowStatus.Succeeded,
                    ResultClientId = newPlatformClientId
                };
            }
            catch (Exception ex)
            {
                return await FailRow(row, $"Unexpected error: {ex.Message}", stepsCompleted);
            }
        }

        private async Task<ClientImportRowResult> FailRow(ClientImportBatchRow row, string message, List<string> stepsCompleted = null)
        {
            row.Status = ClientImportRowStatus.Failed;
            row.ErrorMessage = message;
            row.StepsCompleted = stepsCompleted != null && stepsCompleted.Any() ? string.Join(" -> ", stepsCompleted) : null;
            await _backofficeContext.SaveChangesAsync();

            return new ClientImportRowResult
            {
                RowNumber = row.RowNumber,
                ClientCode = row.ClientCode,
                ClientName = row.ClientName,
                Status = ClientImportRowStatus.Failed,
                Message = message
            };
        }

        private async Task CopySelectorMarketTemplate(Guid newClientId, Guid referenceClientId)
        {
            await _sportsbookContext.Database.ExecuteSqlRawAsync(
                @"INSERT INTO [Sportsbook].[dbo].[ClientSelectorMarketTemplate2]
                    (ClientId, SportId, MarketTemplateId, MarketTemplateCode, AsiaSortOrder, EuroSortOrder, Identifier)
                  SELECT {0}, t.SportId, t.MarketTemplateId, t.MarketTemplateCode, t.AsiaSortOrder, t.EuroSortOrder, t.Identifier
                  FROM [Sportsbook].[dbo].[ClientSelectorMarketTemplate2] t
                  WHERE t.ClientId = {1}",
                newClientId, referenceClientId);
        }

        private async Task RegisterTradingClient(Guid clientId, string clientName, string clientCode)
        {
            var connectionString = _configuration.GetConnectionString("Trading");

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(
                "INSERT INTO [Client] (ClientId, ClientName, ClientCode) VALUES (@ClientId, @ClientName, @ClientCode)",
                connection);

            command.Parameters.AddWithValue("@ClientId", clientId);
            command.Parameters.AddWithValue("@ClientName", clientName);
            command.Parameters.AddWithValue("@ClientCode", clientCode);

            await command.ExecuteNonQueryAsync();
        }

        private static Dictionary<string, List<ClientImportRowRequest>> GroupRows(List<ClientImportRowRequest> rows)
        {
            return rows
                .GroupBy(r => (r.ClientCode ?? string.Empty).Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.RowNumber).ToList());
        }

        private static Backoffice.Shared.Models.Response.ClientListResponse.Client ToClientListResponseClient(List<ClientImportRowRequest> rows)
        {
            var first = rows.First();

            return new Backoffice.Shared.Models.Response.ClientListResponse.Client
            {
                ClientId = 0,
                ClientCode = first.ClientCode.Trim(),
                Name = first.ClientName.Trim(),
                Website = first.Website.Trim(),
                StatusId = (int)Backoffice.Shared.Enums.ClientStatus.Normal,
                Region = first.Region,
                Email = first.Email,
                IsEmailNotification = false,
                MonthlyFee = first.MonthlyFee,
                IsTierRevenue = false,
                RevenueShare = new Backoffice.Shared.Models.Response.ClientListResponse.RevenueShare
                {
                    SportsbookShare = first.RevenueShareSportsbook,
                    JackpotShare = first.RevenueShareJackpot
                },
                TaxConfig = new Backoffice.Shared.Models.Response.ClientListResponse.TaxConfig(),
                BetSettings = rows.Select(r => new Backoffice.Shared.Models.Response.ClientListResponse.BetSetting
                {
                    CurrencyCode = r.CurrencyCode,
                    BetSettingSoccer = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingSoccer
                    {
                        MinPerTicket = r.SoccerMinPerTicket,
                        MaxPerTicket = r.SoccerMaxPerTicket,
                        MaxPerGame = r.SoccerMaxPerGame
                    },
                    BetSettingAllSports = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingAllSports
                    {
                        MinPerTicket = r.AllSportsMinPerTicket,
                        MaxPerTicket = r.AllSportsMaxPerTicket,
                        MaxPerGame = r.AllSportsMaxPerGame
                    },
                    BetSettingParlay = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingParlay
                    {
                        MinPerTicket = r.ParlayMinPerTicket,
                        MaxPerTicket = r.ParlayMaxPerTicket,
                        MaxPayoutPerTicket = r.ParlayMaxPayoutPerTicket,
                        MaxWinningsBonus = r.ParlayMaxWinningsBonus
                    },
                    BetSettingCricket = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingCricket
                    {
                        MinPerTicket = r.SoccerMinPerTicket,
                        MaxPerTicket = r.SoccerMaxPerTicket,
                        MaxPerGame = r.SoccerMaxPerGame
                    },
                    BetSettingPriceboost = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingPriceboost
                    {
                        MaxStake = r.SoccerMaxPerTicket
                    },
                    BetSettingBetBuilder = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingBetBuilder
                    {
                        MinStake = r.SoccerMinPerTicket,
                        MaxStake = r.SoccerMaxPerTicket
                    }
                }).ToList()
            };
        }
    }
}
