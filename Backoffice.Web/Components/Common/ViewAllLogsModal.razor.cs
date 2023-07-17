using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.SBC;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using Activity = Backoffice.Shared.Enums.Activity;
using PriceBoostLogModel = Backoffice.Web.Models.PriceBoostLogModel;

namespace Backoffice.Web.Components.Common
{
    public partial class ViewAllLogsModal : IDisposable
    {
        #region Properties
        [Inject] IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserClientCodes")] public List<string>? UserClientCodes { get; set; }

        protected bool IsPreLoader { get; set; } = false;
        public SportsbookClientPermission CurrentPage { get; set; }
        public MetaData MetaData { get; set; } = new MetaData();
        public List<string> UserClients { get; set; }
        public PriceBoostLogFilterModel PriceBoostLogFilterModel { get; set; } = new PriceBoostLogFilterModel();
        public List<PriceBoostActivityLog> PriceBoostActivityLogs { get; set; }
        public List<ActivityLogModel> BoostedSettingsActivityLogs { get; set; }
        public bool WithAdministrationPagesAccess { get; set; }
        private string _region = string.Empty;
        #endregion

        protected override async Task OnInitializedAsync()
        {
            TradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();
            IsPreLoader = true;
            CurrentPage = ModalParameters.TryGet<SportsbookClientPermission>("SelectedPage");
            UserClients = UserClientCodes != null && UserClientCodes.Any() ? UserClientCodes : new List<string>();
            WithAdministrationPagesAccess = false;
            BoostedSettingsActivityLogs = new List<ActivityLogModel>();

            PriceBoostLogFilterModel = new PriceBoostLogFilterModel()
            {
                Clients = new List<KeyValuePair<string, string>>(),
                Tab = CurrentPage,
                ClientCode = "ALL",
                Sports = null,
                SportId = 0,
                Segments = null,
                SegmentId = 0,
                Leagues = null,
                LeagueId = 0,
                DateFrom = DateTime.Now.AddDays(-7),
                DateTo = DateTime.Now,
                SearchText = string.Empty
            };

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 20,
                TotalCount = 0,
                TotalPages = 0,
            };

            await GetContent();
        }
        #region Methods
        public async Task GetContent()
        {
            string payload = BuildPayload();

            if (!string.IsNullOrEmpty(PriceBoostLogFilterModel?.SearchText))
            {
                MetaData.PageSize = int.MaxValue;
            }

            var response = (await BackOfficeService.GetPriceboostActivityLogs(new PriceboostActivityLogRequest
            {
                Page = CurrentPage.ToString().Replace("_", "."),
                Payload = payload,
                MetaData = MetaData,
                DateFrom = PriceBoostLogFilterModel.DateFrom.Date.Add(new TimeSpan(0, 0, 0)),
                DateTo = PriceBoostLogFilterModel.DateTo.Date.Add(new TimeSpan(23, 59, 59)),
                UserClients = UserClients
            }, _region)).Result;

            PriceBoostActivityLogs = new List<PriceBoostActivityLog>();
            BoostedSettingsActivityLogs = new List<ActivityLogModel>();

            if (response != null && response.ActivityLogs != null && response.ActivityLogs.Any())
            {
                if (CurrentPage == SportsbookClientPermission.SBC_OE_PriceBoost_Manage)
                {
                    if (response.ActivityLogs != null && response.ActivityLogs.Any())
                    {
                        PriceBoostActivityLogs = response.ActivityLogs
                        .Select(x => new PriceBoostActivityLog()
                        {
                            Id = x.Id,
                            Page = x.Page,
                            Payload = x.Payload,
                            Activity = x.Activity,
                            From = !string.IsNullOrEmpty(x.From) && x.From != "-" ? JsonConvert.DeserializeObject<PriceBoostLogModel>(x.From) : new PriceBoostLogModel(),
                            To = !string.IsNullOrEmpty(x.To) && x.To != "-" ? JsonConvert.DeserializeObject<PriceBoostLogModel>(x.To) : new PriceBoostLogModel(),
                            UserId = x.UserId,
                            Username = WithAdministrationPagesAccess ? "Administrator" : x.UserName,
                            TimeStamp = x.TimeStamp
                        }).ToList();

                        //build log summary
                        foreach (var log in PriceBoostActivityLogs)
                        {
                            BuildSummary(log);
                        }
                    }
                }
                else
                {
                    foreach (var activityLog in response.ActivityLogs)
                    {
                        if (UserClients.Contains(activityLog.Payload))
                        {
                            var activity = "Increased Cap %";

                            if (activityLog.From != "-")
                            {
                                var cahngedFromCap = JsonConvert.DeserializeObject<BoostedSettingsModel>(activityLog.From).Cap;
                                var cahngedToCap = JsonConvert.DeserializeObject<BoostedSettingsModel>(activityLog.To).Cap;
                                if (cahngedFromCap > cahngedToCap)
                                    activity = "Decrease Cap %";
                            }

                            BoostedSettingsActivityLogs.Add(new ActivityLogModel()
                            {
                                Id = activityLog.Id,
                                Page = activityLog.Page,
                                Payload = activityLog.Payload,
                                Activity = activity,
                                From = activityLog.From,
                                To = activityLog.To,
                                UserId = activityLog.UserId,
                                UserName = WithAdministrationPagesAccess ? "Administrator" : activityLog.UserName,
                                TimeStamp = activityLog.TimeStamp
                            });
                        }
                    }
                }

                MetaData = response.MetaData;
            }

            IsPreLoader = false;
        }
        private async Task<List<KeyValuePair<string, string>>> GetClients()
        {
            var response = await TradingService.GetClientList();

            List<KeyValuePair<string, string>> Clients = new List<KeyValuePair<string, string>>();

            if (response != null)
            {
                foreach (var client in response.Result)
                {
                    if (UserClients.Contains(client.ClientCode, StringComparer.OrdinalIgnoreCase))
                    {
                        KeyValuePair<string, string> Client = new KeyValuePair<string, string>(client.ClientCode, client.ClientName);

                        Clients.Add(Client);
                    }
                }
            }

            return Clients;
        }

        private string BuildPayload()
        {
            if (CurrentPage == SportsbookClientPermission.SBC_OE_PriceBoost_Manage)
            {
                return $"%{(PriceBoostLogFilterModel.ClientCode.ToUpper() != "ALL" ? PriceBoostLogFilterModel.ClientCode : "")}%." +
                $"%{(PriceBoostLogFilterModel.SportId != 0 ? PriceBoostLogFilterModel.SportId : "")}%." +
                $"%{(PriceBoostLogFilterModel.SegmentId != 0 ? PriceBoostLogFilterModel.SegmentId : "")}%." +
                $"%{(PriceBoostLogFilterModel.LeagueId != 0 ? PriceBoostLogFilterModel.LeagueId : "")}%";
            }
            else
            {
                return $"{(PriceBoostLogFilterModel?.ClientCode != "ALL" ? PriceBoostLogFilterModel?.ClientCode : "%")}";
            }
        }
        private void BuildSummary(PriceBoostActivityLog log)
        {
            StringBuilder remarks = new StringBuilder();

            if (log.Activity == Activity.Create.ToString())
            {
                remarks.Append($"Boosted Odds {log.To.Odds.ToString("0.00")} ");
                log.From.HtmlDetail = $"-";
                log.To.HtmlDetail = $"<b>Boosted Odds</b><br>{log.To.Odds.ToString("0.00")}";
                log.To.Summary = remarks.ToString();
            }
            else if (log.Activity == Activity.Update.ToString())
            {
                StringBuilder fromHtmlDetailBuilder = new StringBuilder();
                StringBuilder toHtmlDetailBuilder = new StringBuilder();


                //odds
                if (log.From.Odds != log.To.Odds)
                {
                    remarks.Append($"Boosted Odds {log.From.Odds.ToString("0.00")} {log.To.Odds.ToString("0.00")} ");
                    fromHtmlDetailBuilder.Append($"<b>Boosted Odds</b><br>{log.From.Odds.ToString("0.00")}<br><br>");
                    toHtmlDetailBuilder.Append($"<b>Boosted Odds</b><br>{log.To.Odds.ToString("0.00")}<br><br>");
                }

                //selection
                if (!string.Equals(log.To.SelectionName, log.From.SelectionName, StringComparison.OrdinalIgnoreCase))
                {
                    remarks.Append($"Selection {log.From.SelectionName} {log.To.SelectionName} ");
                    fromHtmlDetailBuilder.Append($"<b>Selection</b><br>{log.From.SelectionName} <br><br>");
                    toHtmlDetailBuilder.Append($"<b>Selection</b><br>{log.To.SelectionName} <br><br>");
                }

                //translation
                if (!string.Equals(JsonConvert.SerializeObject(log.From.Translations), JsonConvert.SerializeObject(log.To.Translations), StringComparison.OrdinalIgnoreCase))
                {
                    //from translation
                    if (log.From.Translations.Any())
                    {
                        fromHtmlDetailBuilder.Append($"<b>Translation</b><br>");
                        remarks.Append("Language Code: Language Name: Boosted Selection: ");
                        foreach (var translation in log.From.Translations)
                        {
                            fromHtmlDetailBuilder.Append($"Language Code: {translation.Language.LanguageCode}<br>Language Name: {translation.Language.LanguageName}<br>Boosted Selection: {translation.BoostedSelectionName}");
                            remarks.Append($"{translation.Language.LanguageCode} {translation.Language.LanguageName} {translation.BoostedSelectionName} ");

                            if (log.From.Translations.IndexOf(translation) != log.From.Translations.Count - 1)
                            {
                                fromHtmlDetailBuilder.Append("<br><br>");
                            }
                        }
                        fromHtmlDetailBuilder.Append("<br>");
                    }

                    //to translation
                    if (log.To.Translations.Any())
                    {
                        toHtmlDetailBuilder.Append($"<b>Translation</b><br>");
                        remarks.Append("Language Code: Language Name: Boosted Selection: ");
                        foreach (var translation in log.To.Translations)
                        {
                            toHtmlDetailBuilder.Append($"Language Code: {translation.Language.LanguageCode}<br>Language Name: {translation.Language.LanguageName}<br>Boosted Selection: {translation.BoostedSelectionName}");
                            remarks.Append($"{translation.Language.LanguageCode} {translation.Language.LanguageName} {translation.BoostedSelectionName} ");

                            if (log.To.Translations.IndexOf(translation) != log.To.Translations.Count - 1)
                            {
                                toHtmlDetailBuilder.Append("<br><br>");
                            }
                        }
                        toHtmlDetailBuilder.Append("<br>");
                    }
                }

                log.From.HtmlDetail = fromHtmlDetailBuilder.ToString();
                log.From.Summary = remarks.ToString();

                log.To.HtmlDetail = toHtmlDetailBuilder.ToString();
                log.To.Summary = remarks.ToString();
            }
            else if (log.Activity == Activity.Delete.ToString())
            {
                remarks.Append($"Boosted Odds {log.From.Odds.ToString("0.00")}");
                log.From.HtmlDetail = $"<b>Boosted Odds</b><br>{log.From.Odds.ToString("0.00")}";
                log.To.HtmlDetail = $"-";
                log.From.Summary = remarks.ToString();
            }
        }

        public async Task OnFilterChanged(PriceBoostLogFilterModel item)
        {
            PriceBoostLogFilterModel = item;
            CurrentPage = item.Tab;

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 20,
                TotalCount = 0,
                TotalPages = 0,
            };
            await GetContent();
        }
        public async Task OnCancelModalClick()
        {
            ModalService.Cancel();
        }
        public void Dispose()
        {
            Task.WhenAll();
        }
        #endregion
        #region Utilities
        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;
            await GetContent();
        }

        public string GetSportName(PriceBoostActivityLog activityLog)
        {
            var splitPayload = activityLog.Payload.Split(".");

            return activityLog.Activity == Activity.Create.ToString() || activityLog.Activity == Activity.Update.ToString() ?
                activityLog.To.SportName : activityLog.From.SportName;
        }

        public string GetDetails(PriceBoostActivityLog activityLog)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("<div>");

            if (activityLog.Activity == Activity.Create.ToString() || activityLog.Activity == Activity.Update.ToString())
            {
                stringBuilder.Append($"<strong><b>{activityLog.To.SelectionName}</b></strong><br>");
                stringBuilder.Append($"{activityLog.To.MarketName}<br>");
                stringBuilder.Append($"<span>{activityLog.To.SegmentName}</span><br>");
                stringBuilder.Append($"<span>{activityLog.To.LeagueName}</span><br>");
                stringBuilder.Append($"{activityLog.To.FixtureName}<br>");
                stringBuilder.Append($"<span>{activityLog.TimeStamp.ToLocalTime().ToString("dd/MM/yy")}</span><br>");
                stringBuilder.Append($"<span>{activityLog.TimeStamp.ToLocalTime().ToString("HH:mm")}</span>");
            }
            else
            {
                stringBuilder.Append($"<strong><b>{activityLog.From.SelectionName}</b></strong><br>");
                stringBuilder.Append($"{activityLog.From.MarketName}<br>");
                stringBuilder.Append($"<span>{activityLog.From.SegmentName}</span><br>");
                stringBuilder.Append($"<span>{activityLog.From.LeagueName}</span><br>");
                stringBuilder.Append($"{activityLog.From.FixtureName}<br>");
                stringBuilder.Append($"<span>{activityLog.TimeStamp.ToLocalTime().ToString("dd/MM/yy")}</span><br>");
                stringBuilder.Append($"<span>{activityLog.TimeStamp.ToLocalTime().ToString("HH:mm")}</span>");
            }

            stringBuilder.Append("</div>");

            return stringBuilder.ToString();
        }

        public string GetClientName(string payload)
        {
            var payloadSplit = payload.Split(".");

            return PriceBoostLogFilterModel.Clients
                .Where(x => x.Key == payloadSplit[0]).FirstOrDefault()
                .Value;
        }

        private async Task<SportsLookupResponse> GetSports()
        {
            return (await TradingService.GetSportLookup()).Result;
        }

        private async Task<SegmentLookupResponse> GetSegments(int sportId)
        {
            return sportId != 0 ? (await TradingService.GetSegmentLookup(sportId)).Result : null;
        }

        private async Task<LeagueLookupResponse> GetLeagues(int sportId, int segmentId)
        {
            return sportId != 0 && segmentId != 0 ? (await TradingService.GetLeagueLookup(sportId, segmentId)).Result : null;
        }

        private string EvaluateBoostedSettingsActivity(string changeData)
        {
            if (!(changeData == null || changeData == string.Empty))
            {
                if (changeData != "-")
                {
                    var viewData = JsonConvert.DeserializeObject<BoostedSettingsModel>(changeData);

                    return $"<b><strong>% Cap of Current Odds from Boosted Odds</strong></b><br><p>{viewData.Cap.ToString("P2", CultureInfo.InvariantCulture)}</p>";
                }
            }
            return "-";
        }
        #endregion
    }
}
