using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Transactions;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Response;
using System.Linq;
using OfficeOpenXml;
using LicenseContext = OfficeOpenXml.LicenseContext;
using OfficeOpenXml.Style;
using System.Text.Json;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Backoffice.Shared.Utilities;
using System.Text;
using static Backoffice.Shared.Models.ManualBetDetailsModel;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Query;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;

namespace Backoffice.Utility.Repository
{
    public class BackofficeRepository : IBackofficeRepository
    {
        private readonly IConfiguration _configuration;
        private BackofficeContext _backofficeContext;
        private HubConnection _hubConnection;
        private readonly ILogger<Worker> _logger;
        public BackofficeRepository(IConfiguration configuration, ILogger<Worker> logger)
        {
            _configuration = configuration;
            var optionsBuilder = new DbContextOptionsBuilder<BackofficeContext>().UseSqlServer(_configuration["ConnectionStrings:Backoffice"]);
            _backofficeContext = new BackofficeContext(optionsBuilder.Options);

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(configuration["Custom:Hub"]))
                .Build();
            Subscribe();

            _logger = logger;
        }
        public async Task ProcessActivityLog(OnActivityLogMessage message)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<BackofficeContext>().UseSqlServer(_configuration["ConnectionStrings:Backoffice"]);

                using (var context = new BackofficeContext(optionsBuilder.Options))
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        var timeStamp = message.TimeStamp;
                        if (message.TimeStamp == DateTime.MinValue)
                        {
                            timeStamp = DateTime.UtcNow;
                        }

                        // insert new activity logs record
                        var activityLog = new ActivityLog
                        {
                            Page = message.Page,
                            Payload = message.Payload,
                            From = message.From,
                            To = message.To,
                            Activity = message.Activity,
                            UserId = message.UserId,
                            UserName = message.UserName,
                            TimeStamp = timeStamp
                        };

                        context.ActivityLog.Add(activityLog);

                        await context.SaveChangesAsync();

                        scope.Complete();

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : " + ex.Message);
            }
        }
        public async Task ProcessActivityLog(OnActivityLogListMessage message)
        {
            try
            {
                var activityLogs = new List<ActivityLog>();

                foreach (var item in message.OnActivityLogMessages)
                {
                    var timeStamp = item.TimeStamp;
                    if (item.TimeStamp == DateTime.MinValue)
                    {
                        timeStamp = DateTime.UtcNow;
                    }
                    activityLogs.Add(new ActivityLog()
                    {
                        Page = item.Page,
                        Payload = item.Payload,
                        From = item.From,
                        To = item.To,
                        Activity = item.Activity,
                        UserId = item.UserId,
                        UserName = item.UserName,
                        TimeStamp = timeStamp
                    });
                };

                var optionsBuilder = new DbContextOptionsBuilder<BackofficeContext>().UseSqlServer(_configuration["ConnectionStrings:Backoffice"]);

                using (var context = new BackofficeContext(optionsBuilder.Options))
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        context.ActivityLog.AddRange(activityLogs);
                        await context.SaveChangesAsync();

                        scope.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : " + ex.Message);
            }
        }
        public async Task ProcessOnGetMessageSettlementHistory(OnGetMessageSettlementHistoryMessage request, IMessageBroker messageBroker, string connectionId)
        {
            // If you use EPPlus in a noncommercial context
            // according to the Polyform Noncommercial license:
            var title = "Message Settlement History";
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var excel = new ExcelPackage();
            try
            {
                var result = GetActivityLogs(new ActivityLogRequest()
                {
                    Page = AdminPermission.Admin_Settlements_MessageSettlement.ToString().Replace("_", "."),
                    Payload = BuildPayload(request.SportId.ToString(), request.SegmentId.ToString(), request.LeagueId.ToString(), request.FixtureId),
                    DateFrom = request.FromDate.ToUniversalTime(),
                    DateTo = request.ToDate.ToUniversalTime(),
                    MetaData = null
                }).Result;

                if (result == null)
                    return;


                #region Initialize Spreadsheet

                var workSheet = excel.Workbook.Worksheets.Add($"{title}");
                InitManualMessageSettlementHistoryWorksheet(workSheet);

                var row = 2;
                int count = 1;
                #endregion

                #region Generate Data

                foreach (var log in result.ActivityLogs)
                {
                    MessageSettlementTo toData = JsonSerializer.Deserialize<MessageSettlementTo>(log.To);
                    workSheet.Cells[row, 1].Value = count;
                    workSheet.Cells[row, 2].Value = $"{toData.BetDate.ToLocalTime().ToString("MM/dd/yyyy hh:mm:ss tt")}";
                    workSheet.Cells[row, 3].Value = toData.BetId.ToString();
                    workSheet.Cells[row, 4].Value = toData.BetSelectionId.ToString();
                    workSheet.Cells[row, 5].Value = toData.SportName;
                    workSheet.Cells[row, 6].Value = toData.FixtureName;
                    workSheet.Cells[row, 7].Value = log.UserName;
                    workSheet.Cells[row, 8].Value = $"{log.TimeStamp.ToLocalTime().ToString("MM/dd/yyyy hh:mm:ss tt")}";
                    workSheet.Cells[row, 9].Value = toData.Reason;
                    row++;
                    count++;
                }
                #endregion

                #region Upload File to Azure

                var fileName = $"{title.Trim()}_{DateTime.UtcNow:MMddyyyyHHmm}.xlsx";
                var downloadUrl = UploadToAzure(excel, fileName);

                if (!string.IsNullOrEmpty(downloadUrl))
                {

                    var notificationsModel = new NotificationsDetailsModel
                    {
                        Id = Guid.NewGuid(),
                        Page = "Administration",
                        ClientCode = string.Empty,
                        SportsId = 0,
                        Details = $"Your excel file is now ready: <a onmouseout=\"this.style.color = '#404E67'\" onmouseover=\"this.style.color = 'blue'\" href=\"{downloadUrl}\">{fileName}</a>",
                        PageRedirect = "/admin/message-settlement",
                        UserId = Guid.Parse(request.UserId),
                        ClaimType = AdminPermission.Admin_Settlements_MessageSettlement.ToString().Replace("_", "."),
                        Type = (int)NotificationType.General
                    };

                    await _hubConnection.SendAsync("SendMessageToUser", "Notification", request.ConnectionId, JsonSerializer.Serialize(notificationsModel));
                    //messageBroker.Publish(JsonSerializer.Serialize(notificationsModel), "OnNewNotification", "r.oe.bm.engine.notifications", Guid.NewGuid().ToString());
                }

                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : " + ex.Message);
            }
            finally
            {
                //Close Excel package
                excel.Dispose();
            }
        }
        public async Task ProcessOnGetMarketSettlementHistory(OnGetMarketSettlementHistoryMessage request, IMessageBroker messageBroker)
        {
            // If you use EPPlus in a noncommercial context
            // according to the Polyform Noncommercial license:
            var title = "Market Settlement History";
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var excel = new ExcelPackage();
            try
            {
                var result = GetActivityLogs(new ActivityLogRequest()
                {
                    Page = AdminPermission.Admin_Settlements_MarketSettlement.ToString().Replace("_", "."),
                    Payload = BuildPayload(request.SportId, request.SegmentId, request.LeagueId, request.FixtureId),
                    DateFrom = request.FromDate.ToUniversalTime(),
                    DateTo = request.ToDate.ToUniversalTime(),
                    MetaData = null
                }).Result;

                if (result == null)
                    return;


                #region Initialize Spreadsheet

                var workSheet = excel.Workbook.Worksheets.Add($"{title}");
                InitManualMessageSettlementHistoryWorksheet(workSheet);

                var row = 2;
                int count = 1;
                #endregion

                #region Generate Data

                foreach (var log in result.ActivityLogs)
                {

                    var manual = JsonSerializer.Deserialize<ManualMarketSettlementRequest>(log.To);
                    if (manual != null)
                    {
                        var eventDetails = $"{manual.LeagueName}\n{manual.FixtureName}\n{manual.FixtureStartDate.ToLocalTime().ToString("yyyy/MM/dd hh:mm:ss tt")}\n{manual.MarketName}";
                        var dateSettled = $"{log.TimeStamp.ToLocalTime().ToString("MM/dd/yyyy hh:mm:ss tt")}";

                        workSheet.Cells[row, 1].Value = count;
                        workSheet.Cells[row, 2].Value = manual.FixtureName;
                        workSheet.Cells[row, 3].Value = manual.MarketName;
                        workSheet.Cells[row, 4].Value = manual.SportName;
                        workSheet.Cells[row, 5].Value = eventDetails;
                        workSheet.Cells[row, 6].Value = manual.SelectionName;
                        workSheet.Cells[row, 7].Value = manual.FixtureStatusName;
                        workSheet.Cells[row, 8].Value = manual.ManualSettledBy;
                        workSheet.Cells[row, 9].Value = dateSettled;
                        workSheet.Cells[row, 10].Value = manual.Reason;
                        row++;
                        count++;
                    }
                }
                #endregion

                #region Upload File to Azure
                var fileName = $"{title.Trim()}_{DateTime.UtcNow:MMddyyyyHHmm}.xlsx";
                var downloadUrl = UploadToAzure(excel, fileName);
                if (!string.IsNullOrEmpty(downloadUrl))
                {
                    var notificationsModel = new NotificationsDetailsModel
                    {
                        Id = Guid.NewGuid(),
                        Page = "Administration",
                        ClientCode = string.Empty,
                        SportsId = 0,
                        Details = $"Your excel file is now ready: <a onmouseout=\"this.style.color = '#404E67'\" onmouseover=\"this.style.color = 'blue'\" href=\"{downloadUrl}\">{fileName}</a>",
                        PageRedirect = "/admin/market-settlement",
                        UserId = Guid.Parse(request.UserId),
                        ClaimType = AdminPermission.Admin_Settlements_ManualBetSettlement.ToString().Replace("_", "."),
                        Type = (int)NotificationType.General
                    };

                    await _hubConnection.SendAsync("SendMessageToUser", "Notification", request.ConnectionId, JsonSerializer.Serialize(notificationsModel));
                    //messageBroker.Publish(JsonSerializer.Serialize(notificationsModel), "OnNewNotification", "r.oe.bm.engine.notifications", Guid.NewGuid().ToString());
                }
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : " + ex.Message);
            }
            finally
            {
                //Close Excel package
                excel.Dispose();
            }
        }
        public async Task ProcessOnGetManualBetSettlementHistory(OnGetManualBetSettlementHistoryMessage request, IMessageBroker messageBroker)
        {
            var title = "Manual Bet Settlement History";
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var excel = new ExcelPackage();
            try
            {
                var result = GetManualBetSettlementActivityLogs(new ManualBetSettlementActivityLogRequest()
                {
                    ManualBetSettlementCallbackModel = request.ManualBetSettlementCallbackModel
                }).Result;

                if (result == null)
                {
                    return;
                }


                #region Initialize Spreadsheet
                var worksheet = excel.Workbook.Worksheets.Add($"{title}");
                InitManualBetSettlementHistoryWorksheet(worksheet);
                #endregion

                #region Generate Data
                for (int i = 0; i < result.ActivityLogs.Count; i++)
                {
                    var log = result.ActivityLogs[i];

                    int cellIndex = i + 2;

                    ManualBetDetails betDetails = JsonSerializer.Deserialize<ManualBetDetails>(log.To);
                    worksheet.Cells[cellIndex, 1].Value = i + 1;
                    worksheet.Cells[cellIndex, 2].Value = betDetails.SettlementDate.ToString("MM/dd/yyyy hh:mm:ss tt").ToUpper();
                    worksheet.Cells[cellIndex, 3].Value = betDetails.BetId;
                    worksheet.Cells[cellIndex, 4].Value = betDetails.Selections.FirstOrDefault().SportName;
                    worksheet.Cells[cellIndex, 5].Value = betDetails.BetDate.ToString("MM/dd/yyyy hh:mm:ss tt").ToUpper();
                    worksheet.Cells[cellIndex, 6].Value = BuildEventDetails(betDetails.Selections.FirstOrDefault(), betDetails.OddsTypeName);
                    worksheet.Cells[cellIndex, 7].Value = betDetails.BetStatusName;
                    worksheet.Cells[cellIndex, 8].Value = GetNewBetStatusName(betDetails);
                    worksheet.Cells[cellIndex, 9].Value = betDetails.UserId;
                    worksheet.Cells[cellIndex, 10].Value = log.UserName;
                    worksheet.Cells[cellIndex, 11].Value = !string.IsNullOrEmpty(betDetails.NewReason) ? betDetails.NewReason : betDetails.Reason;
                }
                #endregion

                #region Upload File to Azure

                var fileName = $"{title}_{DateTime.UtcNow:MMddyyyyHHmm}.xlsx";
                var downloadUrl = UploadToAzure(excel, fileName);

                if (!string.IsNullOrEmpty(downloadUrl))
                {

                    var notificationsModel = new NotificationsDetailsModel
                    {
                        Id = Guid.NewGuid(),
                        Page = "Administration",
                        ClientCode = string.Empty,
                        SportsId = 0,
                        Details = $"Your excel file is now ready: <a onmouseout=\"this.style.color = '#404E67'\" onmouseover=\"this.style.color = 'blue'\" href=\"{downloadUrl}\">{fileName}</a>",
                        PageRedirect = "/admin/manual-bet-settlement",
                        UserId = Guid.Parse(result.ActivityLogs.FirstOrDefault().UserId),
                        ClaimType = AdminPermission.Admin_Settlements_ManualBetSettlement.ToString().Replace("_", "."),
                        Type = (int)NotificationType.General
                    };

                    await _hubConnection.SendAsync("SendMessageToUser", "Notification", request.ConnectionId, JsonSerializer.Serialize(notificationsModel));
                    //messageBroker.Publish(JsonSerializer.Serialize(notificationsModel), "OnNewNotification", "r.oe.bm.engine.notifications", Guid.NewGuid().ToString());
                }
                #endregion

            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : " + ex.Message);
            }
            finally
            {
                excel.Dispose();
            }
        }
        public async Task ProcessOnGetCancelBetHistory(OnGetCancelBetHistoryMessage request, IMessageBroker messageBroker)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var excel = new ExcelPackage();
            try
            {
                var response = (await GetActivityLogs(new ActivityLogRequest()
                {
                    Page = request.Page,
                    Payload = request.Payload,
                    DateFrom = request.DateFrom,
                    DateTo = request.DateTo,
                    MetaData = null
                }));

                if (response == null)
                    return;

                var cancelBets = new List<BetDetails>(); ;

                foreach (var item in response.ActivityLogs)
                {
                    var betDetails = JsonSerializer.Deserialize<BetDetails>(item.From);
                    if (betDetails != null)
                    {
                        betDetails.CancelledBy = item.UserName;

                        var betSelectionDetails = JsonSerializer.Deserialize<BetSelectionDetails>(item.To);
                        betDetails.Selections = new List<BetSelectionDetails>();

                        if (betSelectionDetails != null)
                            betDetails.Selections.Add(betSelectionDetails);

                        cancelBets.Add(betDetails);
                    }
                }


                #region Initialize Spreadsheet
                var worksheet = excel.Workbook.Worksheets.Add("Cancel Bet History");
                InitCancelBetHistoryWorksheet(worksheet);
                #endregion

                #region Generate Data
                for (int i = 0; i < cancelBets.Count; i++)
                {
                    var log = cancelBets[i];

                    int cellIndex = (i + 1) + 1;

                    worksheet.Cells[cellIndex, 1].Value = (cellIndex - 1);
                    worksheet.Cells[cellIndex, 2].Value = log.SettlementDate.ToString("MM/dd/yyyy hh:mm:ss tt").ToUpper();
                    worksheet.Cells[cellIndex, 3].Value = log.BetId;
                    worksheet.Cells[cellIndex, 4].Value = log.BetDate.ToString("MM/dd/yyyy hh:mm:ss tt").ToUpper();
                    worksheet.Cells[cellIndex, 5].Value = BuildEventDetails(log.Selections.FirstOrDefault(), log.OddsTypeName);
                    worksheet.Cells[cellIndex, 6].Value = log.UserId;
                    worksheet.Cells[cellIndex, 7].Value = log.ClientName;
                    worksheet.Cells[cellIndex, 8].Value = log.CancelledBy;
                    worksheet.Cells[cellIndex, 9].Value = log.Reason;
                }
                #endregion

                #region Upload File to Azure

                var fileName = $"CancelBetHistory_{DateTime.UtcNow:MMddyyyyHHmm}.xlsx";
                var downloadUrl = UploadToAzure(excel, fileName);

                if (!string.IsNullOrEmpty(downloadUrl))
                {

                    var notificationsModel = new NotificationsDetailsModel
                    {
                        Id = Guid.NewGuid(),
                        Page = AdminPermission.Admin_CancelBets.ToString().Replace("_", "."),
                        ClientCode = string.Empty,
                        SportsId = 0,
                        Details = $"Your excel file is now ready: <a onmouseout=\"this.style.color = '#404E67'\" onmouseover=\"this.style.color = 'blue'\" href=\"{downloadUrl}\">{fileName}</a>",
                        PageRedirect = "/admin/cancel-bet-history",
                        UserId = Guid.Parse(response.ActivityLogs.FirstOrDefault().UserId),
                        ClaimType = AdminPermission.Admin_CancelBets.ToString().Replace("_", "."),
                        Type = (int)NotificationType.General
                    };

                    await _hubConnection.SendAsync("SendMessageToUser", "Notification", request.ConnectionId, JsonSerializer.Serialize(notificationsModel));
                    //messageBroker.Publish(JsonSerializer.Serialize(notificationsModel), "OnNewNotification", "r.oe.bm.engine.notifications", Guid.NewGuid().ToString());
                }
                #endregion

            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : " + ex.Message);
            }
            finally
            {
                excel.Dispose();
            }
        }
        public async Task ProcessOnGetOeConfigTemplate(OnGetOeConfigTemplateMessage request, IMessageBroker messageBroker)
        {
            try
            {
                var assignedLeagues = _backofficeContext.ConfigTemplateAssigned.Where(x => x.ConfigTemplateId.ToString() == request.ConfigTemplateId).ToList()
                    .GroupBy(x => x.SportId).Select(x => new KeyValuePair<int, List<MyTemplateExportPayloadModel>>(x.Key, x.GroupBy(y => y.ClientCode).Select(y => new MyTemplateExportPayloadModel()
                    {
                        ClientCode = y.Key,
                        LeagueIds = y.Select(league => league.LeagueId).ToList()
                    }).ToList()));

                //var templates = _backofficeContext.ConfigTemplates.Where(x => assignedLeagues.Any(y => y.ConfigTemplateId == x.Id));
                var clientCodes = JwtParser.ParseClaimsFromJwt(request.AccessToken).Where(x => x.Type.ToUpper() == "CLIENTCODES").FirstOrDefault()?.Value.ToString();

                var payloads = new Dictionary<int, List<MyTemplateExportPayloadModel>>();

                foreach (var itemLeague in assignedLeagues)
                {
                    payloads.Add(itemLeague.Key, itemLeague.Value);
                }

                var payload = new OnConfigMyTemplateExportMessage()
                {
                    FileName = request.Filename,
                    UserId = request.UserId,
                    ConnectionId = request.ConnectionId,
                    SportPayloads = payloads
                };

                messageBroker.Publish(JsonSerializer.Serialize(payload), "OnGetMarginSpreadsheet3", $"r.oe.web.config.ongetmarginspreadsheet3", Guid.NewGuid().ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : " + ex.Message);
            }
        }

        #region Utilities
        private async Task Subscribe()
        {
            try
            {
                _hubConnection.Closed += async (error) =>
                {
                    while (_hubConnection.State == HubConnectionState.Disconnected)
                    {
                        await Task.Delay(5000);
                        await _hubConnection.StartAsync().ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                            {
                                _logger.LogError($"Error opening connection: {_configuration["Custom:Hub"]}");
                            }
                            else
                            {
                                _logger.LogInformation($"Connected to ({_configuration["Custom:Hub"]})");
                            }
                        });
                    }
                };

                await _hubConnection.StartAsync().ContinueWith(async task =>
                {
                    if (task.IsFaulted)
                    {
                        while (_hubConnection.State == HubConnectionState.Disconnected)
                        {
                            await Task.Delay(5000);
                            await _hubConnection.StartAsync().ContinueWith(task =>
                            {
                                if (task.IsFaulted)
                                {
                                    _logger.LogError($"Error opening connection: {_configuration["Custom:Hub"]}");
                                }
                                else
                                {
                                    _logger.LogInformation($"Connected to ({_configuration["Custom:Hub"]})");
                                }
                            });
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Connected to ({_configuration["Custom:Hub"]})");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        private async Task<ActivityLogResponse> GetActivityLogs(ActivityLogRequest request)
        {
            try
            {
                var metaData = request.MetaData;
                var activityLogs = new List<ActivityLogModel>();

                if (request.DateFrom == DateTime.MinValue)
                    request.DateFrom = DateTime.UtcNow.AddDays(-1).Date;
                else
                    request.DateFrom = request.DateFrom.ToUniversalTime();
                if (request.DateTo == DateTime.MinValue)
                    request.DateTo = DateTime.UtcNow.Date.AddTicks(-1);
                else
                    request.DateTo = request.DateTo.ToUniversalTime();

                if (request.TimeStamp == DateTime.MinValue)
                {
                    request.TimeStamp = DateTime.UtcNow;
                }

                var fromUTC = request.DateFrom.Date.ToString("yyyy-MM-dd HH:mm:ss");
                var toUTC = request.DateTo.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss");
                activityLogs = (await _backofficeContext.ReadActivityLog
                        .FromSqlRaw($"GetActivityLogs '{request.Page}', '{request.Payload}', '{fromUTC}', '{toUTC}'")
                    .ToListAsync())
                    .Select(s => new ActivityLogModel
                    {
                        Id = s.Id,
                        Page = s.Page,
                        Payload = s.Payload,
                        Activity = s.Activity,
                        From = s.From,
                        To = s.To,
                        UserId = s.UserId,
                        UserName = s.UserName,
                        TimeStamp = s.TimeStamp,
                    }).ToList();


                if (activityLogs.Count == 0)
                    return null;

                if (metaData == null)
                {
                    metaData = new MetaData()
                    {
                        CurrentPage = 0,
                        PageSize = activityLogs.Count(),
                    };
                }

                metaData.TotalCount = activityLogs.Count();
                metaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(metaData.TotalCount) / Convert.ToDecimal(metaData.PageSize)));

                var skip = (metaData.CurrentPage - 1) * metaData.PageSize;

                if (metaData.CurrentPage > 0)
                    activityLogs = activityLogs.Skip(skip).Take(metaData.PageSize).ToList();

                return new ActivityLogResponse()
                {
                    ActivityLogs = activityLogs,
                    MetaData = metaData
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<ActivityLogResponse> GetManualBetSettlementActivityLogs(ManualBetSettlementActivityLogRequest request)
        {
            var metaData = request.MetaData;
            if (metaData == null)
                metaData = new MetaData()
                {
                    CurrentPage = 0,
                    PageSize = 10,
                };
            if (request.ManualBetSettlementCallbackModel.DateFrom == DateTime.MinValue)
                request.ManualBetSettlementCallbackModel.DateFrom = DateTime.UtcNow.AddDays(-1).Date;
            if (request.ManualBetSettlementCallbackModel.DateTo == DateTime.MinValue)
                request.ManualBetSettlementCallbackModel.DateTo = DateTime.UtcNow.Date;

            request.ManualBetSettlementCallbackModel.DateFrom = request.ManualBetSettlementCallbackModel.DateFrom.Date.Add(new TimeSpan(0, 0, 0));
            request.ManualBetSettlementCallbackModel.DateTo = request.ManualBetSettlementCallbackModel.DateTo.Date.Add(new TimeSpan(23, 59, 59));

            var activityLogs = (await _backofficeContext.ReadActivityLog
                    .FromSqlRaw($"GetActivityLogs '{AdminPermission.Admin_Settlements_ManualBetSettlement.ToString().Replace('_', '.')}', '', '{request.ManualBetSettlementCallbackModel.DateFrom.ToString("yyyyMMdd HH:mm:ss")}', '{request.ManualBetSettlementCallbackModel.DateTo.ToString("yyyyMMdd HH:mm:ss")}'")
                .ToListAsync())
                .Select(s => new ActivityLogModel
                {
                    Id = s.Id,
                    Page = s.Page,
                    Payload = s.Payload,
                    Activity = s.Activity,
                    From = s.From,
                    To = s.To,
                    UserId = s.UserId,
                    UserName = s.UserName,
                    TimeStamp = s.TimeStamp,
                }).OrderByDescending(x => x.TimeStamp).ToList();

            if (request.ManualBetSettlementCallbackModel != null)
            {
                activityLogs = FilterManualSettlementLogs(activityLogs, request.ManualBetSettlementCallbackModel);
            }

            metaData.TotalCount = activityLogs.Count();
            metaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(metaData.TotalCount) / Convert.ToDecimal(metaData.PageSize)));

            var skip = (metaData.CurrentPage - 1) * metaData.PageSize;

            if (metaData.CurrentPage > 0)
                activityLogs = activityLogs.Skip(skip).Take(metaData.PageSize).ToList();

            return new ActivityLogResponse()
            {
                ActivityLogs = activityLogs,
                MetaData = metaData
            };
        }

        private string BuildEventDetails(BetSelectionDetails betSelectionDetails, string oddsFormat)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append($"{betSelectionDetails.LeagueName}\n");
            builder.Append($"{betSelectionDetails.FixtureName}\n");
            builder.Append($"{betSelectionDetails.FixtureStartDate.ToString("MM/dd/yyyy hh:mm:ss tt").ToUpper()}\n");
            builder.Append($"{betSelectionDetails.SelectionName} \n");
            builder.Append($"{betSelectionDetails.MarketName} @ {betSelectionDetails.Odds} ({oddsFormat})\n");
            builder.Append($"{betSelectionDetails.BetScore}, Final Score: {betSelectionDetails.SettlementScore}\n");

            return builder.ToString();
        }

        private string BuildPayload(string SportId, string SegmentId, string LeagueId, string FixtureId)
        {
            return $"{(!string.IsNullOrEmpty(SportId) && SportId != "0" ? SportId : "%")}." +
                $"{(!string.IsNullOrEmpty(SegmentId) && SegmentId != "0" ? SegmentId : "%")}." +
                $"{(!string.IsNullOrEmpty(LeagueId) && LeagueId != "0" ? LeagueId : "%")}." +
                $"{(!string.IsNullOrEmpty(FixtureId) && FixtureId != "0" ? FixtureId : "%")}";
        }
        private void InitMarketSettlementHistoryWorksheet(ExcelWorksheet worksheet)
        {
            // setting the properties
            // of the work sheet 
            worksheet.DefaultRowHeight = 12;

            // Setting the properties
            // of the first row
            worksheet.Row(1).Height = 20;
            worksheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Row(1).Style.Font.Bold = true;

            // Header of the Excel sheet
            worksheet.Cells[1, 1].Value = "#";
            worksheet.Cells[1, 2].Value = "Fixture";
            worksheet.Cells[1, 3].Value = "Market Name";
            worksheet.Cells[1, 4].Value = "Sport";
            worksheet.Cells[1, 5].Value = "Event Details";
            worksheet.Cells[1, 6].Value = "Selection";
            worksheet.Cells[1, 7].Value = "Status";
            worksheet.Cells[1, 8].Value = "Settled By";
            worksheet.Cells[1, 9].Value = "Date Settled";
            worksheet.Cells[1, 10].Value = "Reason";
        }
        private void InitManualMessageSettlementHistoryWorksheet(ExcelWorksheet worksheet)
        {
            // setting the properties
            // of the work sheet 
            worksheet.DefaultRowHeight = 12;

            // Setting the properties
            // of the first row
            worksheet.Row(1).Height = 20;
            worksheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Row(1).Style.Font.Bold = true;

            // Header of the Excel sheet
            worksheet.Cells[1, 1].Value = "#";
            worksheet.Cells[1, 2].Value = "Bet Date";
            worksheet.Cells[1, 3].Value = "Bet Id";
            worksheet.Cells[1, 4].Value = "Bet Selection Id";
            worksheet.Cells[1, 5].Value = "Sport";
            worksheet.Cells[1, 6].Value = "Messages Settled";
            worksheet.Cells[1, 7].Value = "Settled By";
            worksheet.Cells[1, 8].Value = "Date Settled";
            worksheet.Cells[1, 9].Value = "Reason";
        }

        private void InitManualBetSettlementHistoryWorksheet(ExcelWorksheet worksheet)
        {
            worksheet.DefaultRowHeight = 12;

            worksheet.Row(1).Height = 20;
            worksheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Row(1).Style.Font.Bold = true;

            // Header of the Excel sheet
            worksheet.Cells[1, 1].Value = "#";
            worksheet.Cells[1, 2].Value = "Date Settled";
            worksheet.Cells[1, 3].Value = "Reference ID";
            worksheet.Cells[1, 4].Value = "Sport";
            worksheet.Cells[1, 5].Value = "Bet Date";
            worksheet.Cells[1, 6].Value = "Event Details";
            worksheet.Cells[1, 7].Value = "From Status";
            worksheet.Cells[1, 8].Value = "To Status";
            worksheet.Cells[1, 9].Value = "Member Name";
            worksheet.Cells[1, 10].Value = "Settled By";
            worksheet.Cells[1, 11].Value = "Reason";
        }

        private void InitCancelBetHistoryWorksheet(ExcelWorksheet worksheet)
        {
            worksheet.DefaultRowHeight = 12;

            worksheet.Row(1).Height = 20;
            worksheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Row(1).Style.Font.Bold = true;

            // Header of the Excel sheet
            worksheet.Cells[1, 1].Value = "No.";
            worksheet.Cells[1, 2].Value = "Date Cancelled";
            worksheet.Cells[1, 3].Value = "Reference ID";
            worksheet.Cells[1, 4].Value = "Bet Date";
            worksheet.Cells[1, 5].Value = "Selection Cancelled";
            worksheet.Cells[1, 6].Value = "Member Name";
            worksheet.Cells[1, 7].Value = "Partner/SB Client Name";
            worksheet.Cells[1, 8].Value = "Cancelled By";
            worksheet.Cells[1, 9].Value = "Reason";
        }

        private string UploadToAzure(ExcelPackage excel, string fileName)
        {
            var storageAccount = CloudStorageAccount.Parse(_configuration["Custom:AzureStorage"]);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("reports");

            var blockBlob = container.GetBlockBlobReference(fileName);

            using (var exportData = new MemoryStream())
            {
                excel.SaveAs(exportData);
                byte[] bytes = exportData.ToArray();
                blockBlob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);
            }

            return $"https://prodstoragecdn.blob.core.windows.net/reports/{fileName}";
        }

        private List<ActivityLogModel> FilterManualSettlementLogs(List<ActivityLogModel> activityLogs, ManualBetSettlementCallbackModel callbackModel)
        {
            string[] clientIds = null;
            string[] sportIds = null;
            string[] currencyCodes = null;
            string[] fromStatusesIds = null;
            string[] toStatusesIds = null;

            if (!string.IsNullOrEmpty(callbackModel.MemberName)) //
            {
                activityLogs = activityLogs
                .Where(x => x.To.Contains($"\"UserId\":\"{callbackModel.MemberName}\"", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(callbackModel.SettledBy))
            {
                activityLogs = activityLogs
                .Where(x => x.UserName.ToLower() == callbackModel.SettledBy.ToLower()).ToList();
            }

            if (!string.IsNullOrEmpty(callbackModel.BetId))
            {
                activityLogs = activityLogs
                .Where(x => x.To.Contains($"BetId\":\"{callbackModel.BetId}\",", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (callbackModel.Clients != null && callbackModel.Clients.Any())
            {
                clientIds = string.Join(",", callbackModel.Clients.Select(client => client.ClientId)).Split(',');

                activityLogs = activityLogs
                .Where(x => clientIds.Contains(JsonSerializer.Deserialize<ManualBetDetails>(x.To).ClientIdRef.ToString())).ToList();
            }

            if (callbackModel.Sports != null && callbackModel.Sports.Any())
            {
                sportIds = string.Join(",", callbackModel.Sports.Select(sport => sport.SportId)).Split(',');

                activityLogs = activityLogs
                .Where(x => sportIds.Contains(JsonSerializer.Deserialize<ManualBetDetails>(x.To).Selections.FirstOrDefault().SportId.ToString())).ToList();
            }

            if (callbackModel.Currencies != null && callbackModel.Currencies.Any())
            {
                currencyCodes = string.Join(",", callbackModel.Currencies.Select(currency => currency.CurrencyCode)).Split(',');

                activityLogs = activityLogs
                .Where(x => currencyCodes.Contains(JsonSerializer.Deserialize<ManualBetDetails>(x.To).CurrencyCode)).ToList();
            }

            if (callbackModel.FromBetStatuses.Any())
            {
                fromStatusesIds = string.Join(",", callbackModel.FromBetStatuses.Select(status => status.BetStatusId)).Split(',');

                activityLogs = activityLogs
                .Where(x => fromStatusesIds.Contains(JsonSerializer.Deserialize<ManualBetDetails>(x.To).BetStatusId.ToString())).ToList();
            }

            if (callbackModel.ToBetStatuses != null && callbackModel.ToBetStatuses.Any())
            {
                toStatusesIds = string.Join(",", callbackModel.ToBetStatuses.Select(status => status.BetStatusId)).Split(',');

                activityLogs = activityLogs
                .Where(x => toStatusesIds.Contains(JsonSerializer.Deserialize<ManualBetDetails>(x.To).NewBetStatusId.ToString())).ToList();
            }

            return activityLogs;
        }

        private string GetNewBetStatusName(ManualBetDetails betDetails)
        {
            if (string.IsNullOrEmpty(betDetails.NewBetStatusName))
            {
                return ((BetStatus)int.Parse(betDetails.NewBetStatusId)).ToString();
            }
            else
            {
                return betDetails.NewBetStatusName;
            }
        }
        #endregion
    }
}