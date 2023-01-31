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

namespace Backoffice.Utility.Repository
{
    public class BackofficeRepository : IBackofficeRepository
    {
        private readonly IConfiguration _configuration;
        private BackofficeContext _backofficeContext;
        public BackofficeRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            var optionsBuilder = new DbContextOptionsBuilder<BackofficeContext>().UseSqlServer(_configuration["ConnectionStrings:Backoffice"]);
            _backofficeContext = new BackofficeContext(optionsBuilder.Options);
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
        public async Task ProcessOnGetManualMessageSettlementHistory(OnGetManualMarketSettlementHistoryMessage request, IMessageBroker messageBroker)
        {
            // If you use EPPlus in a noncommercial context
            // according to the Polyform Noncommercial license:
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var excel = new ExcelPackage();
            try
            {
                var result = GetActivityLogs(new ActivityLogRequest()
                {
                    Page = Page.OE_Administration_MessageSettlement.ToString().Replace("_", "."),
                    Payload = BuildPayload(request.SportId, request.SegmentId, request.LeagueId, request.FixtureId),
                    DateFrom = request.FromDate.ToUniversalTime(),
                    DateTo = request.ToDate.ToUniversalTime(),
                    MetaData = null
                }).Result;

                if (result == null)
                    return;


                #region Initialize Spreadsheet

                var workSheet = excel.Workbook.Worksheets.Add($"Message Settlement History");
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

                var fileName = $"MessageSettlementHistory_{DateTime.UtcNow:MMddyyyyHHmm}.xlsx";
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

                    messageBroker.Publish(JsonSerializer.Serialize(notificationsModel), "OnNewNotification", "r.oe.bm.engine.notifications", Guid.NewGuid().ToString());
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

        #region Utilities
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
        private string BuildPayload(string SportId, string SegmentId, string LeagueId, string FixtureId)
        {
            return $"{(!string.IsNullOrEmpty(SportId) || SportId != "0" ? SportId : "%")}." +
                $"{(!string.IsNullOrEmpty(SegmentId) || SegmentId != "0" ? SegmentId : "%")}." +
                $"{(!string.IsNullOrEmpty(LeagueId) || LeagueId != "0" ? LeagueId : "%")}." +
                $"{(!string.IsNullOrEmpty(FixtureId) || FixtureId != "0" ? FixtureId : "%")}";
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
        #endregion
    }
}