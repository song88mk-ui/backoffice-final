using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using ExcelDataReader;
using Serilog;
using System.Data;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backoffice.Web.Services
{
    public class ExcelService : IExcelService, IDisposable
    {
        #region Properties

        private ITradingService _tradingService;
        private IConfiguration _configuration;

        #endregion

        #region Ctor
        public ExcelService(ITradingService tradingService, IConfiguration configuration)
        {
            _tradingService = tradingService;
            _configuration = configuration;
        }
        #endregion

        #region Methods
        
        public async Task<BaseResponse<OnBulkUpdateConfigMessage>> ConvertExceltoBulkUpdateConfigMessage(MemoryStream stream, string fileName, string userId, string clientCodes, IMessageBroker messageBroker)
        {
            try
            {
                // read excel file
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                // load file into a stream

                using (var reader = ExcelReaderFactory.CreateOpenXmlReader(stream))
                {

                    if (reader != null)
                    {
                        // fill dataset
                        var content = reader.AsDataSet();


                        var leagueDT = content.Tables["Leagues"];
                        var marketDT = content.Tables["Markets"];

                        #region Remove all blank columns

                        foreach (var column in leagueDT.Columns.Cast<DataColumn>().ToArray())
                        {
                            if (leagueDT.AsEnumerable().All(c => c.IsNull(column)))
                                leagueDT.Columns.Remove(column);
                        }

                        foreach (var column in marketDT.Columns.Cast<DataColumn>().ToArray())
                        {
                            if (marketDT.AsEnumerable().All(c => c.IsNull(column)))
                                marketDT.Columns.Remove(column);
                        }

                        #endregion

                        #region Validation

                        // validate if columns matches for league sheet headers
                        var leagueColumns = new string[] { "Sport ID", "Sport Name", "Segment ID", "Segment Name", "League ID", "League Name", "Is League Active?" };
                        var leagueHeaders = (from DataRow dr in leagueDT.Rows select dr).FirstOrDefault().ItemArray.Cast<string>().Take(leagueColumns.Count()).ToArray();

                        if (!leagueColumns.SequenceEqual(leagueHeaders))
                        {
                            SendNotification(new SlackMessage { Text = $"Timestamp: {DateTime.UtcNow}\n File: {fileName}\n Status: Error! League sheet header are incorrect. Excel file should follow the same format the excel when it was exported." }, _configuration["SlackWebHook"]);

                            return new BaseResponse<OnBulkUpdateConfigMessage>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Error! League sheet header are incorrect. Excel file should follow the same format the excel when it was exported."
                            };
                        }

                        var marketColumns = new string[] { "Sport ID", "Sport Name", "Segment ID", "Segment Name", "League ID", "League Name", "Market Template ID", "Market Template Name", "Prematch Margin", "Live Margin", "Is Market Active?" };
                        var marketHeaders = (from DataRow dr in marketDT.Rows select dr).FirstOrDefault().ItemArray.Cast<string>().Take(marketColumns.Count()).ToArray();

                        if (!marketColumns.SequenceEqual(marketHeaders))
                        {
                            SendNotification(new SlackMessage { Text = $"Timestamp: {DateTime.UtcNow}\n File: {fileName}\n Status: Error! Markets-Prematch sheet header are incorrect. Excel file should follow the same format the excel when it was exported." }, _configuration["SlackWebHook"]);

                            return new BaseResponse<OnBulkUpdateConfigMessage>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Error! Markets-Prematch sheet header are incorrect. Excel file should follow the same format the excel when it was exported."
                            };
                        }

                        // validate if there are any blank values in all rows
                        if (DataTableHasNull(leagueDT) || DataTableHasNull(marketDT))
                        {
                            SendNotification(new SlackMessage { Text = $"Timestamp: {DateTime.UtcNow}\n Filename: {fileName}\n Status: Error! Detected empty or blank cell values in the Excel file. Please check the file before importing." }, _configuration["SlackWebHook"]);

                            return new BaseResponse<OnBulkUpdateConfigMessage>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Error! Detected empty or blank cell values in the Excel file. Please check the file before importing."
                            };
                        }

                        // validate if there are markets with no prematch margins
                        var hasEmptyPrematchMargins = (from DataRow dr in marketDT.Rows select dr).Skip(1)
                            .Any(c => string.IsNullOrEmpty(c[8].ToString()));

                        if (hasEmptyPrematchMargins)
                        {
                            SendNotification(new SlackMessage { Text = $"Timestamp: {DateTime.UtcNow}\n Filename: {fileName}\n Status: Error! Detected prematch markets without margins, please add margins to all markets to proceed." }, _configuration["SlackWebHook"]);

                            return new BaseResponse<OnBulkUpdateConfigMessage>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Error! Detected prematch markets without margins, please add margins to all markets to proceed."
                            };
                        }

                        // validate if there are markets with no live margins
                        var hasEmptyLiveMargins = (from DataRow dr in marketDT.Rows select dr).Skip(1)
                            .Any(c => string.IsNullOrEmpty(c[9].ToString()));

                        if (hasEmptyLiveMargins)
                        {
                            SendNotification(new SlackMessage { Text = $"Timestamp: {DateTime.UtcNow}\n Filename: {fileName}\n Status: Error! Detected live markets without margins, please add margins to all markets to proceed." }, _configuration["SlackWebHook"]);

                            return new BaseResponse<OnBulkUpdateConfigMessage>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Error! Detected live markets without margins, please add margins to all markets to proceed."
                            };
                        }

                        // validate if there are prematch margins lower than 100
                        var hasPrematchMarginsLowerThan100 = (from DataRow dr in marketDT.Rows select dr).Skip(1)
                                .Where(c => !string.IsNullOrEmpty(c[8].ToString()))
                                .Select(dr => new
                                {
                                    Prematch = int.Parse(dr[8].ToString()),
                                })
                                .Any(c => c.Prematch <= 100);

                        if (hasPrematchMarginsLowerThan100)
                        {
                            SendNotification(new SlackMessage { Text = $"Timestamp: {DateTime.UtcNow}\n Filename: {fileName}\n Status: Error! Detected prematch markets with margins lower than 100, margins should be above or equal to 100." }, _configuration["SlackWebHook"]);

                            return new BaseResponse<OnBulkUpdateConfigMessage>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Error! Detected prematch markets with margins lower than 100, margins should be above or equal to 100."
                            };
                        }

                        // validate if there are live margins lower than 100
                        var hasLiveMarginsLowerThan100 = (from DataRow dr in marketDT.Rows select dr).Skip(1)
                                .Where(c => !string.IsNullOrEmpty(c[9].ToString()))
                                .Select(dr => new
                                {
                                    Live = int.Parse(dr[9].ToString()),
                                })
                                .Any(c => c.Live <= 100);

                        if (hasLiveMarginsLowerThan100)
                        {
                            SendNotification(new SlackMessage { Text = $"Timestamp: {DateTime.UtcNow}\n Filename: {fileName}\n Status: Error! Detected live markets with margins lower than 100, margins should be above or equal to 100." }, _configuration["SlackWebHook"]);

                            return new BaseResponse<OnBulkUpdateConfigMessage>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Error! Detected live markets with margins lower than 100, margins should be above or equal to 100."
                            };
                        }

                        #endregion

                        var clients = (await _tradingService.GetClientList()).Result;

                        if (!string.IsNullOrEmpty(clientCodes))
                        {
                            var addtlClients = clientCodes.Split(',').ToList();
                            if (addtlClients.Any())
                                clients = clients.Where(c => addtlClients.Contains(c.ClientCode)).ToList();
                        }

                        foreach (var client in clients)
                        {

                            #region Market
                            var rawClientMarkets = (from DataRow dr in marketDT.Rows select dr).Skip(1)
                                .Where(c => !string.IsNullOrEmpty(c[8].ToString()) && !string.IsNullOrEmpty(c[9].ToString()))
                                .Select(dr => new ClientMarketConfigModel
                                {
                                    ClientCode = client.ClientCode,
                                    SportId = int.Parse(dr[0].ToString()),
                                    SportName = dr[1].ToString(),
                                    SegmentId = int.Parse(dr[2].ToString()),
                                    SegmentName = dr[3].ToString(),
                                    LeagueId = int.Parse(dr[4].ToString()),
                                    LeagueName = dr[5].ToString(),
                                    MarketTemplateId = dr[6].ToString(),
                                    MarketTemplateName = dr[7].ToString(),
                                    FixtureTypeId = 0,
                                    ClientId = client.ClientId,
                                    IsActive = bool.Parse(dr[10].ToString()),
                                    Prematch = @"[{""T"":-41472000,""M"":" + dr[8].ToString() + "}]",
                                    Live = @"[{""T"":0,""M"":" + dr[9].ToString() + "}]",
                                    Username = "OEMarketActivation"
                                }).ToList();
                            #endregion
                            #region League

                            var rawClientLeagues = (from DataRow dr in leagueDT.Rows select dr).Skip(1)
                                .Where(c => !string.IsNullOrEmpty(c[6].ToString()))
                                .Select(dr => new ClientLeagueConfig
                                {
                                    ClientCode = client.ClientCode,
                                    SportId = int.Parse(dr[0].ToString()),
                                    SegmentId = int.Parse(dr[2].ToString()),
                                    LeagueId = int.Parse(dr[4].ToString()),
                                    ClientId = client.ClientId,
                                    IsActive = bool.Parse(dr[6].ToString()),
                                }).ToList();
                            #endregion

                            var hasRecords = true;
                            var skip = 0;
                            var take = 5000;

                            while (hasRecords)
                            {
                                var filteredClientMarkets = rawClientMarkets.Skip(skip).Take(take).ToList();
                                var filteredClientLeagues = rawClientLeagues.Skip(skip).Take(take).ToList();

                                if (!filteredClientMarkets.Any() && !filteredClientLeagues.Any())
                                {
                                    hasRecords = false;
                                }
                                else
                                {
                                    skip += take;

                                    //publish only on last client
                                    var isPublish = client.ClientCode == clients.LastOrDefault().ClientCode;
                                    var isLastMessage = isPublish && !rawClientMarkets.Skip(skip).Take(take).Any();

                                    var message = new OnBulkUpdateConfigMessage()
                                    {
                                        UserId = userId,
                                        FileName = fileName,
                                        ClientMarketConfigs = filteredClientMarkets,
                                        ClientLeagueConfigs = filteredClientLeagues,
                                        IsPublish = isPublish,
                                        IsLastMessage = isLastMessage
                                    };

                                    messageBroker.Publish(JsonSerializer.Serialize(message), "OnBulkUpdateConfig2", "r.oe.web.config.onbulkupdateconfig2", Guid.NewGuid().ToString());
                                }

                            }
                            //await JSRuntime.InvokeVoidAsync("ShowNotication", ImportButton, $"{fileName} - {client.ClientCode}: Successfully processed..");

                        }
                        reader.Dispose();

                        SendNotification(new SlackMessage { Text = $"Timestamp: {DateTime.UtcNow}\n Filename: {fileName}\n Clients: {string.Join(", ", clients.Select(x => x.ClientCode))}\n Status: {StatusCode.Success.ToString().Replace("_", "")}" }, _configuration["SlackWebHook"]);
                    }
                }

                return new BaseResponse<OnBulkUpdateConfigMessage>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString().Replace("_", "")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());

                SendNotification(new SlackMessage { Text = $"Timestamp: {DateTime.UtcNow}\n Filename: {fileName}\n Status: Error! Please contact the administrator. \n Reason: {ex.Message}" }, _configuration["SlackWebHook"]);

                return new BaseResponse<OnBulkUpdateConfigMessage>
                {
                    StatusCode = StatusCode.Error,
                    Message = "Error! Please contact the administrator."
                };
            }
        }

        public void Dispose()
        {
            Console.WriteLine("- {0} was disposed!", this.GetType().Name);
        }

        #endregion

        #region Utilities

        private bool DataTableHasNull(DataTable table)
        {
            foreach (DataColumn column in table.Columns)
            {
                if (table.Rows.OfType<DataRow>().Any(c => c.IsNull(column) || string.IsNullOrEmpty(c[column].ToString())))
                    return true;
            }

            return false;
        }

        private void SendNotification(SlackMessage message, string url)
        {
            using (WebClient webClient = new WebClient())
            {
                webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                byte[] request = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                byte[] response = webClient.UploadData(url, "POST", request);
            }
        }

        public sealed class SlackMessage
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        #endregion
    }
}
