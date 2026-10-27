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
using Serilog;
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
        private readonly IRabbitMqRepository _rabbitMqRepository;

        public ClientImportRepository(
            BackofficeContext backofficeContext,
            SportsbookContext sportsbookContext,
            IBackofficeRepository backofficeRepository,
            IBackofficeActivityLogRepository activityLogRepository,
            IConfiguration configuration,
            IRabbitMqRepository rabbitMqRepository)
        {
            _backofficeContext = backofficeContext;
            _sportsbookContext = sportsbookContext;
            _backofficeRepository = backofficeRepository;
            _activityLogRepository = activityLogRepository;
            _configuration = configuration;
            _rabbitMqRepository = rabbitMqRepository;
        }

        private static readonly (string Match, string Environment)[] KnownDatabaseHosts = new[]
        {
            ("stg-song88-mssql-server", "Staging"),
            ("prd-song88-mssql-server", "Production"),
            // Dev's SQL Server runs in-cluster (Helm release "mssql", chart mssqlserver-2022): pods reach it by
            // the service name, outside clients by the k8s-dev-database load balancer. Same database.
            ("mssql-mssqlserver-2022", "Dev"),
            ("k8s-dev-database", "Dev")
        };

        // Server name only — never the credentials — so it's safe to show in the UI.
        private static string GetDatabaseHost(string connectionString)
        {
            try
            {
                var dataSource = new SqlConnectionStringBuilder(connectionString).DataSource ?? string.Empty;
                return string.IsNullOrWhiteSpace(dataSource) ? "(none)" : dataSource.Split(',')[0].Trim();
            }
            catch (ArgumentException)
            {
                return "(unreadable connection string)";
            }
        }

        public BaseResponse<string> GetEnvironment()
        {
            var connectionString = _configuration.GetConnectionString("Backoffice") ?? string.Empty;

            var match = KnownDatabaseHosts.FirstOrDefault(h => connectionString.Contains(h.Match, StringComparison.OrdinalIgnoreCase));

            if (match.Environment == null)
            {
                return new BaseResponse<string>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"Could not determine which environment this is: unrecognised database host \"{GetDatabaseHost(connectionString)}\". Import is blocked until this host is added to the known list."
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
            var clientIdsSeenInFile = new Dictionary<Guid, string>();

            // Only needed when the file chooses Client IDs; blank = Platform generates one.
            var platformClients = groups.Values.Any(g => g.Any(r => !string.IsNullOrWhiteSpace(r.ClientId)))
                ? (await _backofficeRepository.GetPlatformClients()).Result ?? new List<PlatformClientResponse>()
                : new List<PlatformClientResponse>();

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
                Guid? requestedClientId = null;

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
                else if (CheckRequestedClientId(group.Value, existingClients, platformClients, out requestedClientId) is string clientIdError)
                {
                    result.Status = "Error";
                    result.Message = clientIdError;
                }
                else if (requestedClientId.HasValue && clientIdsSeenInFile.TryGetValue(requestedClientId.Value, out var codeWithSameId))
                {
                    result.Status = "Error";
                    result.Message = $"Client ID {requestedClientId} is also used by Client Code \"{codeWithSameId}\" earlier in this file. Each client needs its own Client ID.";
                }

                if (result.Status == "Ready" && !string.IsNullOrWhiteSpace(name))
                {
                    namesSeenInFile[name.ToUpperInvariant()] = code;
                    if (requestedClientId.HasValue)
                        clientIdsSeenInFile[requestedClientId.Value] = code;
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
                OperationType = ClientImportOperationType.Create,
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

                batch.Status = DetermineBatchStatus(response.SucceededCount, response.FailedCount);
                batch.CompletedAt = DateTime.UtcNow;
                batch.SucceededRows = response.SucceededCount;
                batch.FailedRows = response.FailedCount;
                await _backofficeContext.SaveChangesAsync();

                await _activityLogRepository.ProcessActivityLog(new OnActivityLogMessage
                {
                    Page = "Clients - Import",
                    Activity = Activity.Create.ToString(),
                    Payload = batch.BatchId.ToString(),
                    From = request.Environment,
                    To = $"{response.SucceededCount}/{response.TotalClients} succeeded, {response.FailedCount} failed",
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
                Guid? requestedClientId = null;
                if (rows.Any(r => !string.IsNullOrWhiteSpace(r.ClientId)))
                {
                    // Re-check at commit time: the ID may have been taken since the file was validated.
                    var existingClients = (await _backofficeRepository.GetClientList(new GetClientListRequest
                    {
                        Page = 1,
                        PageSize = int.MaxValue,
                        SortBy = ClientSorting.SortByBusinessNameAsc
                    })).Result?.Clients ?? new List<Backoffice.Shared.Models.Response.ClientListResponse.Client>();
                    var currentPlatformClients = (await _backofficeRepository.GetPlatformClients()).Result ?? new List<PlatformClientResponse>();

                    var clientIdError = CheckRequestedClientId(rows, existingClients, currentPlatformClients, out requestedClientId);
                    if (clientIdError != null)
                    {
                        return await FailRow(row, clientIdError);
                    }
                }

                var client = ToClientListResponseClient(rows);
                client.RequestedPlatformClientId = requestedClientId;
                var createResult = await _backofficeRepository.AddOrUpdateClient(client);

                if (createResult.StatusCode != Backoffice.Shared.Enums.StatusCode.Success)
                {
                    return await FailRow(row, createResult.Message ?? "Failed to create client record.");
                }
                stepsCompleted.Add("Backoffice client record created");

                if (client.PlatformClientId == null)
                {
                    return await FailRow(row, requestedClientId.HasValue
                        ? $"Client record was created in Backoffice, but Platform did not register it with Client ID {requestedClientId} (it may already be in use). Needs manual follow-up."
                        : "Client record was created in Backoffice, but Platform registration did not complete. Needs manual follow-up.", stepsCompleted);
                }
                var newPlatformClientId = client.PlatformClientId.Value;

                // An older Platform build ignores the requested ID and generates its own: stop before
                // copying settings so nobody configures a client under an ID they didn't ask for.
                if (requestedClientId.HasValue && newPlatformClientId != requestedClientId.Value)
                {
                    return await FailRow(row, $"Platform registered the client as {newPlatformClientId} instead of the requested Client ID {requestedClientId} (Platform.API may need the GM-1001 update). Needs manual follow-up.", stepsCompleted);
                }
                stepsCompleted.Add("Platform registration confirmed");

                var platformClients = (await _backofficeRepository.GetPlatformClients()).Result ?? new List<PlatformClientResponse>();
                var referenceClient = platformClients.FirstOrDefault(c => string.Equals(c.ClientCode, first.ReferenceClientCode.Trim(), StringComparison.OrdinalIgnoreCase));
                if (referenceClient == null)
                {
                    return await FailRow(row, $"Reference client \"{first.ReferenceClientCode}\" was not found in Platform.", stepsCompleted);
                }

                await CopySelectorMarketTemplate(newPlatformClientId, referenceClient.ClientId);
                stepsCompleted.Add("Selector Market Template copied");

                var sportOrderRows = await CopySportSettings(newPlatformClientId, referenceClient.ClientId);
                stepsCompleted.Add(sportOrderRows > 0
                    ? "Sport settings copied (sports, segments, leagues)"
                    : $"Sport settings NOT copied — reference client \"{referenceClient.ClientCode}\" has none; set them up in Sport Settings");

                // No Trading DB step: Trading.dbo.Client is retired (BO-42 reads clients from the Platform list and
                // flyway renames the table to Client_DELETE_AFTER_BO42), so inserting there fails the whole row.

                // The copies above are raw SQL, so nothing told Sportsbook.API, which caches these whole tables
                // for 24h: without this the new client gets an empty sports list and sb crashes until the cache
                // expires or the pods restart. Sent last, so the caches reload with every row already written.
                stepsCompleted.Add(await PublishSportsbookCacheRefresh(newPlatformClientId, code)
                    ? "Sportsbook caches cleared"
                    : "Sportsbook caches NOT cleared — save the client's Sport Settings once to refresh them");

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

        // Same messages the Sport Settings and Market Groups screens send after a save. CacheSync forwards each one
        // to every Sportsbook.API pod: OnSportSortOrderChange clears the sport/segment/league order caches,
        // OnMarketSortOrderChange the selector market template and market template group caches.
        // CacheSync builds a routing key from ClientCode, so it must not be null.
        private async Task<bool> PublishSportsbookCacheRefresh(Guid clientId, string clientCode)
        {
            var messages = new[]
            {
                (CacheType.SportSortOrder, "OnSportSortOrderChange", "r.oe.cachesync.onsportsortorderchange"),
                (CacheType.MarketSortOrder, "OnMarketSortOrderChange", "r.oe.cachesync.onmarketsortorderchange")
            };

            var allSent = true;
            foreach (var (cacheType, messageType, routingKey) in messages)
            {
                var result = await _rabbitMqRepository.Publish(new MessageRequest
                {
                    Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                    {
                        CacheType = cacheType,
                        ClientId = clientId,
                        ClientCode = clientCode,
                        CacheKey = string.Empty
                    }),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = messageType,
                    RoutingKey = routingKey
                });

                if (result?.StatusCode != StatusCode.Success)
                {
                    allSent = false;
                    Log.Warning("Client import: {MessageType} for {ClientCode} ({ClientId}) was not published: {Message}", messageType, clientCode, clientId, result?.Message);
                }
            }

            return allSent;
        }

        private async Task<ClientImportRowResult> FailRow(ClientImportBatchRow row, string message, List<string> stepsCompleted = null)
        {
            row.Status = ClientImportRowStatus.Failed;
            row.ErrorMessage = message != null && message.Length > 1000 ? message.Substring(0, 1000) : message;
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

        // protected virtual: raw SQL the EF InMemory test provider cannot run, so tests override these two steps.
        protected virtual async Task CopySelectorMarketTemplate(Guid newClientId, Guid referenceClientId)
        {
            await _sportsbookContext.Database.ExecuteSqlRawAsync(
                @"INSERT INTO [Sportsbook].[dbo].[ClientSelectorMarketTemplate2]
                    (ClientId, SportId, MarketTemplateId, MarketTemplateCode, AsiaSortOrder, EuroSortOrder, Identifier)
                  SELECT {0}, t.SportId, t.MarketTemplateId, t.MarketTemplateCode, t.AsiaSortOrder, t.EuroSortOrder, t.Identifier
                  FROM [Sportsbook].[dbo].[ClientSelectorMarketTemplate2] t
                  WHERE t.ClientId = {1}",
                newClientId, referenceClientId);
        }

        // The three tables the Sport Settings screen saves as one unit (SportsbookRepository sport-settings save).
        // Without ClientSportOrder a new client's sportsbook has no sports to show.
        // NOT EXISTS keeps a retry from duplicating rows. {0} = new client, {1} = reference client.
        private const string CopyClientSportOrderSql =
            @"INSERT INTO [Sportsbook].[dbo].[ClientSportOrder] (ClientId, SportId, SportName, SortOrder, IsActive)
              SELECT {0}, SportId, SportName, SortOrder, IsActive
              FROM [Sportsbook].[dbo].[ClientSportOrder]
              WHERE ClientId = {1}
                AND NOT EXISTS (SELECT 1 FROM [Sportsbook].[dbo].[ClientSportOrder] WHERE ClientId = {0})";

        private const string CopyClientSegmentOrderSql =
            @"INSERT INTO [Sportsbook].[dbo].[ClientSegmentOrder] (ClientId, SportId, SportName, SegmentId, SegmentName, SortOrder)
              SELECT {0}, SportId, SportName, SegmentId, SegmentName, SortOrder
              FROM [Sportsbook].[dbo].[ClientSegmentOrder]
              WHERE ClientId = {1}
                AND NOT EXISTS (SELECT 1 FROM [Sportsbook].[dbo].[ClientSegmentOrder] WHERE ClientId = {0})";

        private const string CopyClientLeagueOrderSql =
            @"INSERT INTO [Sportsbook].[dbo].[ClientLeagueOrder] (ClientId, SportId, SportName, SegmentId, SegmentName, LeagueId, LeagueName, SortOrder)
              SELECT {0}, SportId, SportName, SegmentId, SegmentName, LeagueId, LeagueName, SortOrder
              FROM [Sportsbook].[dbo].[ClientLeagueOrder]
              WHERE ClientId = {1}
                AND NOT EXISTS (SELECT 1 FROM [Sportsbook].[dbo].[ClientLeagueOrder] WHERE ClientId = {0})";

        // Returns how many ClientSportOrder rows were copied, so the caller can flag a reference client with none.
        protected virtual async Task<int> CopySportSettings(Guid newClientId, Guid referenceClientId)
        {
            var sportOrderRows = await _sportsbookContext.Database.ExecuteSqlRawAsync(CopyClientSportOrderSql, newClientId, referenceClientId);
            await _sportsbookContext.Database.ExecuteSqlRawAsync(CopyClientSegmentOrderSql, newClientId, referenceClientId);
            await _sportsbookContext.Database.ExecuteSqlRawAsync(CopyClientLeagueOrderSql, newClientId, referenceClientId);
            return sportOrderRows;
        }

        // Checks the optional Client ID chosen for a new client. Returns an error message, or null when the
        // client can be created (requestedClientId = null means Platform generates one).
        private static string CheckRequestedClientId(
            List<ClientImportRowRequest> rows,
            List<Backoffice.Shared.Models.Response.ClientListResponse.Client> existingClients,
            List<PlatformClientResponse> platformClients,
            out Guid? requestedClientId)
        {
            requestedClientId = null;

            var given = rows.Where(r => !string.IsNullOrWhiteSpace(r.ClientId)).ToList();
            if (!given.Any())
                return null;

            var invalid = given.FirstOrDefault(r => !Guid.TryParse(r.ClientId.Trim(), out var parsed) || parsed == Guid.Empty);
            if (invalid != null)
                return $"Row {invalid.RowNumber}: \"{invalid.ClientId}\" is not a valid Client ID.";

            var ids = given.Select(r => Guid.Parse(r.ClientId.Trim())).Distinct().ToList();
            if (ids.Count > 1)
                return $"Rows {string.Join(", ", given.Select(r => r.RowNumber))} give different Client IDs for the same client. Use one Client ID (or leave the others blank).";

            var id = ids[0];
            var limitId = (rows.First().LimitId ?? string.Empty).Trim();

            // Platform is the source of truth; Backoffice's own record catches clients Platform's 24h cache hasn't picked up yet.
            var owner = platformClients.Where(c => c.ClientId == id).Select(c => new { c.ClientCode, c.LimitId })
                .Concat(existingClients.Where(c => c.PlatformClientId == id).Select(c => new { c.ClientCode, c.LimitId }))
                .FirstOrDefault();
            if (owner == null)
            {
                requestedClientId = id;
                return null;
            }

            return !string.Equals((owner.LimitId ?? string.Empty).Trim(), limitId, StringComparison.OrdinalIgnoreCase)
                ? $"Client ID {id} already belongs to client \"{owner.ClientCode}\" with LimitId {owner.LimitId}, not {limitId}. Check the Client ID and LimitId."
                : $"Client ID {id} already exists (client \"{owner.ClientCode}\"). Leave Client ID blank to have a new one generated.";
        }

        private static Dictionary<string, List<ClientImportRowRequest>> GroupRows(List<ClientImportRowRequest> rows)
        {
            return rows
                .GroupBy(r => (r.ClientCode ?? string.Empty).Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.RowNumber).ToList());
        }

        #region Export

        public async Task<BaseResponse<List<ClientExportRow>>> ExportClients()
        {
            var clients = await _backofficeContext.ClientInformation.AsNoTracking().Where(c => !c.IsDeleted).ToListAsync();
            var platformClients = (await _backofficeRepository.GetPlatformClients()).Result ?? new List<PlatformClientResponse>();

            // An export without currencies looks like "this client has none" — fail loudly instead.
            var clientIds = clients.Select(c => c.ClientId).ToList();
            var betSettingsResponse = await _backofficeRepository.GetBetSettingsBulk(clientIds);
            if (betSettingsResponse.StatusCode != StatusCode.Success || betSettingsResponse.Result == null)
            {
                return new BaseResponse<List<ClientExportRow>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"Could not load client bet settings for the export ({betSettingsResponse.Message})."
                };
            }
            var betSettingsBulk = betSettingsResponse.Result;

            var rows = new List<ClientExportRow>();

            foreach (var client in clients)
            {
                var platformClient = FindPlatformClient(client, platformClients);
                var betSettings = betSettingsBulk.TryGetValue(client.ClientId, out var clientBetSettings)
                    ? clientBetSettings
                    : new List<Backoffice.Shared.Models.Response.ClientListResponse.BetSetting>();

                if (!betSettings.Any())
                {
                    rows.Add(BuildExportRow(client, platformClient, null));
                    continue;
                }

                foreach (var betSetting in betSettings)
                {
                    rows.Add(BuildExportRow(client, platformClient, betSetting));
                }
            }

            return new BaseResponse<List<ClientExportRow>>
            {
                StatusCode = StatusCode.Success,
                Result = rows
            };
        }

        private static ClientExportRow BuildExportRow(
            ClientInformation client,
            PlatformClientResponse platformClient,
            Backoffice.Shared.Models.Response.ClientListResponse.BetSetting betSetting)
        {
            return new ClientExportRow
            {
                ClientId = platformClient?.ClientId.ToString().ToUpperInvariant(),
                ClientCode = client.Brand,
                ClientName = client.Name,
                Website = client.Website,
                Region = client.Region,
                Email = client.Email,
                MonthlyFee = client.MonthlyFee,
                RevenueShareSportsbook = client.SportsbookShare,
                RevenueShareJackpot = client.JackpotShare,
                LimitId = platformClient?.LimitId,
                Endpoint = platformClient?.Endpoint,
                CurrencyCode = betSetting?.CurrencyCode,
                SoccerMinPerTicket = betSetting?.BetSettingSoccer?.MinPerTicket ?? 0,
                SoccerMaxPerTicket = betSetting?.BetSettingSoccer?.MaxPerTicket ?? 0,
                SoccerMaxPerGame = betSetting?.BetSettingSoccer?.MaxPerGame ?? 0,
                AllSportsMinPerTicket = betSetting?.BetSettingAllSports?.MinPerTicket ?? 0,
                AllSportsMaxPerTicket = betSetting?.BetSettingAllSports?.MaxPerTicket ?? 0,
                AllSportsMaxPerGame = betSetting?.BetSettingAllSports?.MaxPerGame ?? 0,
                ParlayMinPerTicket = betSetting?.BetSettingParlay?.MinPerTicket ?? 0,
                ParlayMaxPerTicket = betSetting?.BetSettingParlay?.MaxPerTicket ?? 0,
                ParlayMaxPayoutPerTicket = betSetting?.BetSettingParlay?.MaxPayoutPerTicket ?? 0,
                ParlayMaxWinningsBonus = betSetting?.BetSettingParlay?.MaxWinningsBonus ?? 0
            };
        }

        // Platform.dbo.Client.ClientIdRef is the Backoffice ClientId — the real link. Code is a fallback for
        // older Platform rows where ClientIdRef wasn't set.
        private static PlatformClientResponse FindPlatformClient(ClientInformation client, List<PlatformClientResponse> platformClients)
        {
            return platformClients.FirstOrDefault(p => p.ClientIdRef == client.ClientId)
                ?? platformClients.FirstOrDefault(p => string.Equals(p.ClientCode, client.Brand, StringComparison.OrdinalIgnoreCase));
        }

        private static ClientInformation FindBackofficeClient(PlatformClientResponse platformClient, List<ClientInformation> existingClients)
        {
            return existingClients.FirstOrDefault(c => c.ClientId == platformClient.ClientIdRef)
                ?? existingClients.FirstOrDefault(c => string.Equals(c.Brand, platformClient.ClientCode, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Update

        // Rows are grouped by Client ID — that's what identifies the client on update.
        private static Dictionary<string, List<ClientUpdateRowRequest>> GroupUpdateRows(List<ClientUpdateRowRequest> rows)
        {
            return rows
                .GroupBy(r => (r.ClientId ?? string.Empty).Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.RowNumber).ToList());
        }

        // Resolves a group's Client ID to its Platform and Backoffice client, or explains why it can't.
        private static (PlatformClientResponse Platform, ClientInformation Client, string Error) ResolveUpdateClient(
            List<ClientUpdateRowRequest> groupRows,
            List<PlatformClientResponse> platformClients,
            List<ClientInformation> existingClients)
        {
            var first = groupRows[0];
            var rawId = (first.ClientId ?? string.Empty).Trim();
            var code = (first.ClientCode ?? string.Empty).Trim();
            var rowNumbers = string.Join(", ", groupRows.Select(r => r.RowNumber));

            if (string.IsNullOrEmpty(rawId))
                return (null, null, $"Row {rowNumbers}: Client ID is required — copy it from Export CSV or the client's Client Information.");

            if (!Guid.TryParse(rawId, out var clientId))
                return (null, null, $"\"{rawId}\" is not a valid Client ID. It should look like 6989B427-4234-4AC9-4791-08DEFEC92A2C.");

            var platformClient = platformClients.FirstOrDefault(p => p.ClientId == clientId);
            if (platformClient == null)
                return (null, null, $"Client ID {rawId} was not found — this tool only updates existing clients.");

            var client = FindBackofficeClient(platformClient, existingClients);
            if (client == null)
                return (platformClient, null, $"Client ID {rawId} ({platformClient.ClientCode}) has no active client in Backoffice.");

            if (string.IsNullOrEmpty(code))
                return (platformClient, client, $"Row {rowNumbers}: Client Code is required.");

            var mismatchedRow = groupRows.FirstOrDefault(r => !string.Equals((r.ClientCode ?? string.Empty).Trim(), client.Brand, StringComparison.OrdinalIgnoreCase));
            if (mismatchedRow != null)
                return (platformClient, client, $"Row {mismatchedRow.RowNumber}: Client ID {rawId} belongs to client \"{client.Brand}\", not \"{(mismatchedRow.ClientCode ?? string.Empty).Trim()}\". Check you copied the right Client ID.");

            return (platformClient, client, null);
        }

        public async Task<BaseResponse<ValidateClientUpdateResponse>> ValidateUpdateBatch(ValidateClientUpdateRequest request)
        {
            var response = new ValidateClientUpdateResponse();
            var groups = GroupUpdateRows(request.Rows);

            // Client IDs live on Platform, so they can't be checked without the Platform client list.
            var platformResponse = await _backofficeRepository.GetPlatformClients();
            if (platformResponse.StatusCode != StatusCode.Success || platformResponse.Result == null)
            {
                return new BaseResponse<ValidateClientUpdateResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"Could not load the Platform client list to check Client IDs ({platformResponse.Message})."
                };
            }

            var existingClients = await _backofficeContext.ClientInformation.AsNoTracking().Where(c => !c.IsDeleted).ToListAsync();

            response.TotalClients = groups.Count;

            foreach (var group in groups)
            {
                var first = group.Value[0];
                var (_, client, resolveError) = ResolveUpdateClient(group.Value, platformResponse.Result, existingClients);
                var error = resolveError ?? GetUpdateGroupError(group.Value, client, existingClients);

                response.Rows.Add(new ClientUpdateRowResult
                {
                    RowNumber = first.RowNumber,
                    ClientId = (first.ClientId ?? string.Empty).Trim(),
                    ClientCode = (first.ClientCode ?? string.Empty).Trim(),
                    ClientName = (first.ClientName ?? string.Empty).Trim(),
                    Status = error == null ? "Ready" : "Error",
                    Message = error
                });
            }

            response.ReadyCount = response.Rows.Count(r => r.Status == "Ready");
            response.ErrorCount = response.Rows.Count(r => r.Status == "Error");

            return new BaseResponse<ValidateClientUpdateResponse>
            {
                StatusCode = StatusCode.Success,
                Result = response
            };
        }

        // Shared by validate and commit — commit must not trust that the caller validated first.
        // Runs after ResolveUpdateClient, so `client` is the client the Client ID points to.
        private static string GetUpdateGroupError(List<ClientUpdateRowRequest> groupRows, ClientInformation client, List<ClientInformation> existingClients)
        {
            var first = groupRows[0];
            var code = client.Brand;
            var name = (first.ClientName ?? string.Empty).Trim();
            var rowNumbers = string.Join(", ", groupRows.Select(r => r.RowNumber));

            if (groupRows.Select(r => (r.ClientName ?? string.Empty).Trim()).Distinct().Count() > 1)
                return $"Rows {rowNumbers} use Client ID {first.ClientId?.Trim()} with different Client Names.";

            if (!string.IsNullOrWhiteSpace(first.Website) && !first.Website.StartsWith("https://"))
                return "Website must start with https://";

            if (!string.IsNullOrWhiteSpace(name) && existingClients.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) && c.ClientId != client.ClientId))
                return $"Client Name \"{name}\" is already used by a different client.";

            var currencyRows = groupRows.Where(r => !string.IsNullOrWhiteSpace(r.CurrencyCode)).ToList();

            var badCurrency = currencyRows.FirstOrDefault(r => !Enum.TryParse<Currency>(r.CurrencyCode, true, out _));
            if (badCurrency != null)
                return $"Row {badCurrency.RowNumber}: \"{badCurrency.CurrencyCode}\" is not a recognized currency code.";

            var badAction = currencyRows.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.CurrencyAction) && !string.Equals(r.CurrencyAction, ClientCurrencyAction.Remove, StringComparison.OrdinalIgnoreCase));
            if (badAction != null)
                return $"Row {badAction.RowNumber}: CurrencyAction must be blank or \"Remove\".";

            var duplicateCurrency = currencyRows.GroupBy(r => r.CurrencyCode.Trim().ToUpperInvariant()).FirstOrDefault(g => g.Count() > 1);
            if (duplicateCurrency != null)
                return $"Currency \"{duplicateCurrency.Key}\" appears more than once for Client Code \"{code}\" in this file.";

            foreach (var currencyRow in currencyRows.Where(r => string.IsNullOrWhiteSpace(r.CurrencyAction)))
            {
                var blankColumns = GetBlankBetColumns(currencyRow);
                if (blankColumns.Count > 0)
                    return $"Row {currencyRow.RowNumber}: every bet-limit column is required when adding or updating a currency. Blank: {string.Join(", ", blankColumns)}.";
            }

            return null;
        }

        private static List<string> GetBlankBetColumns(ClientUpdateRowRequest row)
        {
            var columns = new (string Name, decimal? Value)[]
            {
                (nameof(row.SoccerMinPerTicket), row.SoccerMinPerTicket),
                (nameof(row.SoccerMaxPerTicket), row.SoccerMaxPerTicket),
                (nameof(row.SoccerMaxPerGame), row.SoccerMaxPerGame),
                (nameof(row.AllSportsMinPerTicket), row.AllSportsMinPerTicket),
                (nameof(row.AllSportsMaxPerTicket), row.AllSportsMaxPerTicket),
                (nameof(row.AllSportsMaxPerGame), row.AllSportsMaxPerGame),
                (nameof(row.ParlayMinPerTicket), row.ParlayMinPerTicket),
                (nameof(row.ParlayMaxPerTicket), row.ParlayMaxPerTicket),
                (nameof(row.ParlayMaxPayoutPerTicket), row.ParlayMaxPayoutPerTicket),
                (nameof(row.ParlayMaxWinningsBonus), row.ParlayMaxWinningsBonus)
            };

            return columns.Where(c => !c.Value.HasValue).Select(c => c.Name).ToList();
        }

        public async Task<BaseResponse<CommitClientUpdateResponse>> CommitUpdateBatch(CommitClientUpdateRequest request, string userId, string userName)
        {
            var lockResult = await IsImportRunning(request.Environment);
            if (lockResult.Result)
            {
                return new BaseResponse<CommitClientUpdateResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"An import is already running for {request.Environment}. Wait for it to finish before starting another."
                };
            }

            // Without the Platform list we can't tell what LimitId/Endpoint a client currently has,
            // and would push them to Platform as empty — abort rather than corrupt them.
            var platformResponse = await _backofficeRepository.GetPlatformClients();
            if (platformResponse.StatusCode != StatusCode.Success || platformResponse.Result == null)
            {
                return new BaseResponse<CommitClientUpdateResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"Could not load the Platform client list ({platformResponse.Message}). No clients were updated."
                };
            }
            var platformClients = platformResponse.Result;

            var groups = GroupUpdateRows(request.Rows);

            // All reads happen before the batch is saved as Running: anything that throws after that
            // point but outside the try/finally below would leave the environment locked.
            var allExistingClients = await _backofficeContext.ClientInformation.AsNoTracking()
                .Where(c => !c.IsDeleted)
                .ToListAsync();

            var resolved = groups.ToDictionary(g => g.Key, g => ResolveUpdateClient(g.Value, platformClients, allExistingClients));

            // AddOrUpdateClient replaces ALL of a client's bet settings with the list it's given, so a
            // failed read here would delete every currency the CSV doesn't mention. Abort instead.
            var clientIds = resolved.Values.Where(r => r.Client != null).Select(r => r.Client.ClientId).Distinct().ToList();
            var betSettingsResponse = await _backofficeRepository.GetBetSettingsBulk(clientIds);
            if (betSettingsResponse.StatusCode != StatusCode.Success || betSettingsResponse.Result == null)
            {
                return new BaseResponse<CommitClientUpdateResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"Could not load the clients' current bet settings ({betSettingsResponse.Message}). No clients were updated."
                };
            }
            var betSettingsBulk = betSettingsResponse.Result;

            var batch = new ClientImportBatch
            {
                BatchId = request.BatchId != Guid.Empty ? request.BatchId : Guid.NewGuid(),
                Environment = request.Environment,
                OperationType = ClientImportOperationType.Update,
                Status = ClientImportBatchStatus.Running,
                StartedByUserId = userId,
                StartedByUserName = userName,
                StartedAt = DateTime.UtcNow,
                TotalRows = groups.Count
            };

            _backofficeContext.ClientImportBatch.Add(batch);
            await _backofficeContext.SaveChangesAsync();

            var response = new CommitClientUpdateResponse
            {
                BatchId = batch.BatchId,
                TotalClients = groups.Count
            };

            try
            {
                foreach (var group in groups)
                {
                    var (platformClient, client, resolveError) = resolved[group.Key];
                    var validationError = resolveError ?? GetUpdateGroupError(group.Value, client, allExistingClients);
                    var result = await CommitUpdateClient(batch.BatchId, group.Value, userId, userName, validationError, platformClient, client, betSettingsBulk);
                    result.ClientId = (group.Value[0].ClientId ?? string.Empty).Trim();
                    response.Rows.Add(result);
                }
            }
            finally
            {
                response.SucceededCount = response.Rows.Count(r => r.Status == ClientImportRowStatus.Succeeded);
                response.FailedCount = response.Rows.Count(r => r.Status == ClientImportRowStatus.Failed);

                batch.Status = DetermineBatchStatus(response.SucceededCount, response.FailedCount);
                batch.CompletedAt = DateTime.UtcNow;
                batch.SucceededRows = response.SucceededCount;
                batch.FailedRows = response.FailedCount;
                await _backofficeContext.SaveChangesAsync();

                await _activityLogRepository.ProcessActivityLog(new OnActivityLogMessage
                {
                    Page = "Clients - Import",
                    Activity = Activity.Update.ToString(),
                    Payload = batch.BatchId.ToString(),
                    From = request.Environment,
                    To = $"{response.SucceededCount}/{response.TotalClients} succeeded, {response.FailedCount} failed",
                    UserId = userId,
                    UserName = userName,
                    TimeStamp = DateTime.UtcNow
                });
            }

            return new BaseResponse<CommitClientUpdateResponse>
            {
                StatusCode = StatusCode.Success,
                Result = response
            };
        }

        private static string DetermineBatchStatus(int succeeded, int failed)
        {
            if (failed == 0) return ClientImportBatchStatus.Completed;
            if (succeeded == 0) return ClientImportBatchStatus.Failed;
            return ClientImportBatchStatus.PartiallySuccessful;
        }

        private async Task<ClientUpdateRowResult> CommitUpdateClient(
            Guid batchId,
            List<ClientUpdateRowRequest> rows,
            string userId,
            string userName,
            string validationError,
            PlatformClientResponse platformClient,
            ClientInformation existingClient,
            Dictionary<int, List<Backoffice.Shared.Models.Response.ClientListResponse.BetSetting>> betSettingsByClientId)
        {
            var first = rows[0];
            // Once resolved, the client's own code is authoritative; the row's code was only checked against it.
            var code = existingClient?.Brand ?? (first.ClientCode ?? string.Empty).Trim();

            var row = new ClientImportBatchRow
            {
                BatchId = batchId,
                RowNumber = first.RowNumber,
                ClientCode = code,
                ClientName = (first.ClientName ?? string.Empty).Trim(),
                Status = ClientImportRowStatus.Pending
            };
            _backofficeContext.ClientImportBatchRow.Add(row);
            await _backofficeContext.SaveChangesAsync();

            var changes = new List<ClientFieldChangeResponse>();

            try
            {
                // Covers a missing/unknown Client ID too, so platformClient and existingClient are non-null past here.
                if (validationError != null)
                {
                    return await FailUpdateRow(row, validationError);
                }

                var existingBetSettings = betSettingsByClientId.TryGetValue(existingClient.ClientId, out var clientBetSettings)
                    ? clientBetSettings
                    : new List<Backoffice.Shared.Models.Response.ClientListResponse.BetSetting>();

                var name = string.IsNullOrWhiteSpace(first.ClientName) ? existingClient.Name : first.ClientName.Trim();
                var website = string.IsNullOrWhiteSpace(first.Website) ? existingClient.Website : first.Website.Trim();
                var region = string.IsNullOrWhiteSpace(first.Region) ? existingClient.Region : first.Region.Trim();
                var email = string.IsNullOrWhiteSpace(first.Email) ? existingClient.Email : first.Email.Trim();
                var monthlyFee = first.MonthlyFee ?? existingClient.MonthlyFee;
                var revenueShareSportsbook = first.RevenueShareSportsbook ?? existingClient.SportsbookShare;
                var revenueShareJackpot = first.RevenueShareJackpot ?? existingClient.JackpotShare;
                var limitId = string.IsNullOrWhiteSpace(first.LimitId) ? platformClient?.LimitId : first.LimitId;
                var endpoint = string.IsNullOrWhiteSpace(first.Endpoint) ? platformClient?.Endpoint : first.Endpoint;

                AddIfChanged(changes, "ClientName", existingClient.Name, name);
                AddIfChanged(changes, "Website", existingClient.Website, website);
                AddIfChanged(changes, "Region", existingClient.Region, region);
                AddIfChanged(changes, "Email", existingClient.Email, email);
                AddIfChanged(changes, "MonthlyFee", existingClient.MonthlyFee.ToString(), monthlyFee.ToString());
                AddIfChanged(changes, "RevenueShareSportsbook", existingClient.SportsbookShare.ToString(), revenueShareSportsbook.ToString());
                AddIfChanged(changes, "RevenueShareJackpot", existingClient.JackpotShare.ToString(), revenueShareJackpot.ToString());
                AddIfChanged(changes, "LimitId", platformClient?.LimitId, limitId);
                AddIfChanged(changes, "Endpoint", platformClient?.Endpoint, endpoint);

                var mergedBetSettings = new List<Backoffice.Shared.Models.Response.ClientListResponse.BetSetting>(
                    existingBetSettings.Select(CloneBetSetting));

                foreach (var currencyRow in rows.Where(r => !string.IsNullOrWhiteSpace(r.CurrencyCode)))
                {
                    var currencyCode = currencyRow.CurrencyCode.Trim().ToUpperInvariant();
                    var existingIndex = mergedBetSettings.FindIndex(b => string.Equals(b.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase));

                    if (string.Equals(currencyRow.CurrencyAction, ClientCurrencyAction.Remove, StringComparison.OrdinalIgnoreCase))
                    {
                        if (existingIndex >= 0)
                        {
                            changes.Add(new ClientFieldChangeResponse { FieldName = $"Currency.{currencyCode}", PreviousValue = SummarizeBetSetting(mergedBetSettings[existingIndex]), NewValue = "Removed" });
                            mergedBetSettings.RemoveAt(existingIndex);
                        }
                        continue;
                    }

                    var existingBetSetting = existingIndex >= 0 ? mergedBetSettings[existingIndex] : null;
                    var newBetSetting = BuildBetSettingFromRow(currencyCode, currencyRow, existingBetSetting);
                    var previousSummary = existingIndex >= 0 ? SummarizeBetSetting(mergedBetSettings[existingIndex]) : null;
                    var newSummary = SummarizeBetSetting(newBetSetting);

                    if (previousSummary != newSummary)
                    {
                        changes.Add(new ClientFieldChangeResponse { FieldName = $"Currency.{currencyCode}", PreviousValue = previousSummary ?? "(none)", NewValue = newSummary });
                    }

                    if (existingIndex >= 0)
                        mergedBetSettings[existingIndex] = newBetSetting;
                    else
                        mergedBetSettings.Add(newBetSetting);
                }

                if (!changes.Any())
                {
                    row.Status = ClientImportRowStatus.Succeeded;
                    row.StepsCompleted = "No changes detected";
                    await _backofficeContext.SaveChangesAsync();

                    return new ClientUpdateRowResult
                    {
                        RowNumber = first.RowNumber,
                        ClientCode = code,
                        ClientName = existingClient.Name,
                        Status = ClientImportRowStatus.Succeeded,
                        Message = "No changes detected.",
                        Changes = changes
                    };
                }

                var updatedClient = new Backoffice.Shared.Models.Response.ClientListResponse.Client
                {
                    ClientId = existingClient.ClientId,
                    ClientCode = code,
                    Name = name,
                    Website = website,
                    StatusId = existingClient.StatusId,
                    Region = region,
                    Email = email,
                    TelephoneNumber = existingClient.TelephoneNumber,
                    // Stored as fractions (0.05 = 5%), which is what AddOrUpdateClient expects — no scaling.
                    TaxConfig = string.IsNullOrEmpty(existingClient.TaxConfig)
                        ? new Backoffice.Shared.Models.Response.ClientListResponse.TaxConfig()
                        : JsonSerializer.Deserialize<Backoffice.Shared.Models.Response.ClientListResponse.TaxConfig>(existingClient.TaxConfig),
                    IsEmailNotification = existingClient.IsEmailNotification,
                    MonthlyFee = monthlyFee,
                    LimitId = limitId,
                    Endpoint = endpoint,
                    IsTierRevenue = existingClient.IsTierRevenueShare,
                    RevenueShare = new Backoffice.Shared.Models.Response.ClientListResponse.RevenueShare
                    {
                        SportsbookShare = revenueShareSportsbook,
                        JackpotShare = revenueShareJackpot
                    },
                    TierSportsbookShare = existingClient.RevenueShareSportsbook,
                    TierJackpotShare = existingClient.RevenueShareJackpot,
                    BetSlipMaxSelection = existingClient.BetSlipMaxSelection,
                    BetSettings = mergedBetSettings
                };

                var updateResult = await _backofficeRepository.AddOrUpdateClient(updatedClient);
                if (updateResult.StatusCode != Backoffice.Shared.Enums.StatusCode.Success)
                {
                    return await FailUpdateRow(row, updateResult.Message ?? "Failed to update client record.");
                }

                // AddOrUpdateClient returns Success even when its Platform call fails (it only logs);
                // it sets PlatformClientId only when Platform accepted the update, same check as the create flow.
                var platformUpdated = updatedClient.PlatformClientId != null;
                var platformFailureMessage = "Saved in Backoffice, but the Platform update failed — LimitId/Endpoint/bet limits on Platform may be stale. Re-run this client.";

                // The Backoffice record is already changed at this point, so record the outcome before
                // the audit write — a failed audit write must not change what's reported for the client.
                row.Status = platformUpdated ? ClientImportRowStatus.Succeeded : ClientImportRowStatus.Failed;
                row.StepsCompleted = $"{changes.Count} field(s) changed" + (platformUpdated ? string.Empty : "; Platform update failed");
                row.ErrorMessage = platformUpdated ? null : platformFailureMessage;
                row.ResultClientId = platformClient.ClientId;
                await _backofficeContext.SaveChangesAsync();

                var now = DateTime.UtcNow;
                try
                {
                    await _activityLogRepository.ProcessActivityLog(new OnActivityLogListMessage
                    {
                        OnActivityLogMessages = changes.Select(c => new OnActivityLogMessage
                        {
                            Page = "Clients - Import",
                            Activity = Activity.Update.ToString(),
                            Payload = $"{batchId}|{code}|{c.FieldName}",
                            From = Truncate(c.PreviousValue),
                            To = Truncate(c.NewValue),
                            UserId = userId,
                            UserName = userName,
                            TimeStamp = now
                        }).ToList()
                    });
                }
                catch (Exception logEx)
                {
                    Log.Error(logEx, "Client {ClientCode} was updated in batch {BatchId} but its field-change audit log could not be written", code, batchId);
                    row.StepsCompleted += "; audit log write failed";
                    await _backofficeContext.SaveChangesAsync();
                }

                // Changes are returned either way: on a Platform failure they still happened in Backoffice.
                return new ClientUpdateRowResult
                {
                    RowNumber = first.RowNumber,
                    ClientCode = code,
                    ClientName = name,
                    Status = row.Status,
                    Message = row.ErrorMessage,
                    Changes = changes
                };
            }
            catch (Exception ex)
            {
                return await FailUpdateRow(row, $"Unexpected error: {ex.Message}");
            }
        }

        private async Task<ClientUpdateRowResult> FailUpdateRow(ClientImportBatchRow row, string message)
        {
            row.Status = ClientImportRowStatus.Failed;
            row.ErrorMessage = message != null && message.Length > 1000 ? message.Substring(0, 1000) : message;
            await _backofficeContext.SaveChangesAsync();

            return new ClientUpdateRowResult
            {
                RowNumber = row.RowNumber,
                ClientCode = row.ClientCode,
                ClientName = row.ClientName,
                Status = ClientImportRowStatus.Failed,
                Message = message
            };
        }

        public async Task<BaseResponse<List<ClientImportBatchSummary>>> GetBatchHistory(string operationType)
        {
            var query = _backofficeContext.ClientImportBatch.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(operationType))
                query = query.Where(b => b.OperationType == operationType);

            var batches = await query
                .OrderByDescending(b => b.StartedAt)
                .Take(100)
                .Select(b => new ClientImportBatchSummary
                {
                    BatchId = b.BatchId,
                    Environment = b.Environment,
                    OperationType = b.OperationType,
                    Status = b.Status,
                    StartedByUserName = b.StartedByUserName,
                    StartedAt = b.StartedAt,
                    CompletedAt = b.CompletedAt,
                    TotalRows = b.TotalRows,
                    SucceededRows = b.SucceededRows,
                    FailedRows = b.FailedRows
                })
                .ToListAsync();

            return new BaseResponse<List<ClientImportBatchSummary>>
            {
                StatusCode = StatusCode.Success,
                Result = batches
            };
        }

        public async Task<BaseResponse<List<ClientFieldChangeResponse>>> GetFieldChanges(Guid batchId)
        {
            var payloadPrefix = $"{batchId}|";

            var logs = await _backofficeContext.ActivityLog
                .AsNoTracking()
                .Where(a => a.Page == "Clients - Import" && a.Activity == Activity.Update.ToString() && a.Payload.StartsWith(payloadPrefix))
                .OrderBy(a => a.Id)
                .ToListAsync();

            var changes = new List<ClientFieldChangeResponse>();
            foreach (var log in logs)
            {
                // Payload shape: "{batchId}|{clientCode}|{fieldName}"
                var parts = log.Payload.Split('|', 3);
                if (parts.Length != 3)
                    continue;

                changes.Add(new ClientFieldChangeResponse
                {
                    ClientCode = parts[1],
                    FieldName = parts[2],
                    PreviousValue = log.From,
                    NewValue = log.To,
                    TimeStamp = log.TimeStamp
                });
            }

            return new BaseResponse<List<ClientFieldChangeResponse>>
            {
                StatusCode = StatusCode.Success,
                Result = changes
            };
        }

        private static void AddIfChanged(List<ClientFieldChangeResponse> changes, string field, string previous, string next)
        {
            if (!string.Equals(previous ?? string.Empty, next ?? string.Empty, StringComparison.Ordinal))
            {
                changes.Add(new ClientFieldChangeResponse { FieldName = field, PreviousValue = previous, NewValue = next });
            }
        }

        private static string Truncate(string value) => value != null && value.Length > 500 ? value.Substring(0, 500) : value;

        private static Backoffice.Shared.Models.Response.ClientListResponse.BetSetting CloneBetSetting(Backoffice.Shared.Models.Response.ClientListResponse.BetSetting source)
        {
            return new Backoffice.Shared.Models.Response.ClientListResponse.BetSetting
            {
                CurrencyCode = source.CurrencyCode,
                BetSettingSoccer = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingSoccer { MinPerTicket = source.BetSettingSoccer?.MinPerTicket, MaxPerTicket = source.BetSettingSoccer?.MaxPerTicket, MaxPerGame = source.BetSettingSoccer?.MaxPerGame },
                BetSettingAllSports = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingAllSports { MinPerTicket = source.BetSettingAllSports?.MinPerTicket, MaxPerTicket = source.BetSettingAllSports?.MaxPerTicket, MaxPerGame = source.BetSettingAllSports?.MaxPerGame },
                BetSettingParlay = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingParlay { MinPerTicket = source.BetSettingParlay?.MinPerTicket, MaxPerTicket = source.BetSettingParlay?.MaxPerTicket, MaxPayoutPerTicket = source.BetSettingParlay?.MaxPayoutPerTicket, MaxWinningsBonus = source.BetSettingParlay?.MaxWinningsBonus },
                BetSettingCricket = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingCricket { MinPerTicket = source.BetSettingCricket?.MinPerTicket, MaxPerTicket = source.BetSettingCricket?.MaxPerTicket, MaxPerGame = source.BetSettingCricket?.MaxPerGame },
                BetSettingPriceboost = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingPriceboost { MaxStake = source.BetSettingPriceboost?.MaxStake },
                BetSettingBetBuilder = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingBetBuilder { MinStake = source.BetSettingBetBuilder?.MinStake, MaxStake = source.BetSettingBetBuilder?.MaxStake }
            };
        }

        // The update CSV has no Cricket/Priceboost/BetBuilder columns. When a currency already
        // exists, its real values for those three categories are carried over untouched — only
        // Soccer/AllSports/Parlay are driven by the CSV. They're defaulted from Soccer only when
        // adding a brand-new currency that has no prior values to preserve.
        private static Backoffice.Shared.Models.Response.ClientListResponse.BetSetting BuildBetSettingFromRow(
            string currencyCode,
            ClientUpdateRowRequest row,
            Backoffice.Shared.Models.Response.ClientListResponse.BetSetting existing)
        {
            return new Backoffice.Shared.Models.Response.ClientListResponse.BetSetting
            {
                CurrencyCode = currencyCode,
                BetSettingSoccer = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingSoccer { MinPerTicket = row.SoccerMinPerTicket, MaxPerTicket = row.SoccerMaxPerTicket, MaxPerGame = row.SoccerMaxPerGame },
                BetSettingAllSports = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingAllSports { MinPerTicket = row.AllSportsMinPerTicket, MaxPerTicket = row.AllSportsMaxPerTicket, MaxPerGame = row.AllSportsMaxPerGame },
                BetSettingParlay = new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingParlay { MinPerTicket = row.ParlayMinPerTicket, MaxPerTicket = row.ParlayMaxPerTicket, MaxPayoutPerTicket = row.ParlayMaxPayoutPerTicket, MaxWinningsBonus = row.ParlayMaxWinningsBonus },
                BetSettingCricket = existing?.BetSettingCricket != null
                    ? new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingCricket { MinPerTicket = existing.BetSettingCricket.MinPerTicket, MaxPerTicket = existing.BetSettingCricket.MaxPerTicket, MaxPerGame = existing.BetSettingCricket.MaxPerGame }
                    : new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingCricket { MinPerTicket = row.SoccerMinPerTicket, MaxPerTicket = row.SoccerMaxPerTicket, MaxPerGame = row.SoccerMaxPerGame },
                BetSettingPriceboost = existing?.BetSettingPriceboost != null
                    ? new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingPriceboost { MaxStake = existing.BetSettingPriceboost.MaxStake }
                    : new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingPriceboost { MaxStake = row.SoccerMaxPerTicket },
                BetSettingBetBuilder = existing?.BetSettingBetBuilder != null
                    ? new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingBetBuilder { MinStake = existing.BetSettingBetBuilder.MinStake, MaxStake = existing.BetSettingBetBuilder.MaxStake }
                    : new Backoffice.Shared.Models.Response.ClientListResponse.BetSettingBetBuilder { MinStake = row.SoccerMinPerTicket, MaxStake = row.SoccerMaxPerTicket }
            };
        }

        private static string SummarizeBetSetting(Backoffice.Shared.Models.Response.ClientListResponse.BetSetting betSetting)
        {
            if (betSetting == null) return null;

            return $"Soccer({betSetting.BetSettingSoccer?.MinPerTicket}/{betSetting.BetSettingSoccer?.MaxPerTicket}/{betSetting.BetSettingSoccer?.MaxPerGame})" +
                   $" AllSports({betSetting.BetSettingAllSports?.MinPerTicket}/{betSetting.BetSettingAllSports?.MaxPerTicket}/{betSetting.BetSettingAllSports?.MaxPerGame})" +
                   $" Parlay({betSetting.BetSettingParlay?.MinPerTicket}/{betSetting.BetSettingParlay?.MaxPerTicket}/{betSetting.BetSettingParlay?.MaxPayoutPerTicket}/{betSetting.BetSettingParlay?.MaxWinningsBonus})" +
                   $" Cricket({betSetting.BetSettingCricket?.MinPerTicket}/{betSetting.BetSettingCricket?.MaxPerTicket}/{betSetting.BetSettingCricket?.MaxPerGame})" +
                   $" Priceboost({betSetting.BetSettingPriceboost?.MaxStake})" +
                   $" BetBuilder({betSetting.BetSettingBetBuilder?.MinStake}/{betSetting.BetSettingBetBuilder?.MaxStake})";
        }

        #endregion

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
                LimitId = first.LimitId,
                Endpoint = first.Endpoint,
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
