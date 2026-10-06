using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
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
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        private bool _isPreLoader;
        public SportsbookClientPermission CurrentPage { get; set; }
        public MetaData _metaData { get; set; } = new MetaData();
        public List<string> _userClients { get; set; }
        public PriceBoostLogFilterModel _priceBoostLogFilterModel { get; set; } = new PriceBoostLogFilterModel();
        public List<PriceBoostActivityLog> _priceBoostActivityLogs { get; set; }
        public List<ActivityLogModel> _boostedSettingsActivityLogs { get; set; }
        public bool WithAdministrationPagesAccess { get; set; }
        private string _region = string.Empty;
        private bool _init = true;
        #endregion

        protected override async Task OnInitializedAsync()
        {
            _isPreLoader = true;
            _userClients = _userState.Model.SportsbookClients.Any() ? _userState.Model.SportsbookClients.Select(s => s.ClientCode).ToList() : new List<string>();
            WithAdministrationPagesAccess = false;
            _boostedSettingsActivityLogs = new List<ActivityLogModel>();

            _priceBoostLogFilterModel = new PriceBoostLogFilterModel()
            {
                Clients = new List<KeyValuePair<string, string>>(),
                Tab = CurrentPage,
                ClientCode = "ALL",
                SportId = 0,
                SegmentId = 0,
                LeagueId = 0,
                DateFrom = DateTimeHelper.Now(_userState.TimeZoneOffset).AddDays(-7),
                DateTo = DateTimeHelper.Now(_userState.TimeZoneOffset),
                SearchText = string.Empty
            };

            _metaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 20,
                TotalCount = 0,
                TotalPages = 0,
            };

        }
        protected override async Task OnParametersSetAsync()
        {
            _tradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();
            CurrentPage = ModalParameters.TryGet<SportsbookClientPermission>("SelectedPage");

            if (_init)
            {
                await GetContent();
            }
        }
        private bool ValidateParameter() => _init;
        #region Methods
        public async Task GetContent()
        {
            string payload = BuildPayload();

            if (!string.IsNullOrEmpty(_priceBoostLogFilterModel?.SearchText))
            {
                _metaData.PageSize = int.MaxValue;
            }

            var response = await _backofficeService.GetPriceboostActivityLogs(new PriceboostActivityLogRequest
            {
                Page = CurrentPage.ToString().Replace("_", "."),
                Payload = payload,
                MetaData = _metaData,
                DateFrom = _priceBoostLogFilterModel.DateFrom.Date.AddDays(-1),
                DateTo = _priceBoostLogFilterModel.DateTo.Date.AddDays(1).AddTicks(-1),
                UserClients = _userClients
            }, _region);

            _priceBoostActivityLogs = new List<PriceBoostActivityLog>();
            _boostedSettingsActivityLogs = new List<ActivityLogModel>();

            var activityLogs = response.Result;
            if (activityLogs != null && activityLogs.ActivityLogs != null && activityLogs.ActivityLogs.Any())
            {
                switch (CurrentPage)
                {
                    case SportsbookClientPermission.SBC_OE_PriceBoost_Manage:
                        if (activityLogs.ActivityLogs != null && activityLogs.ActivityLogs.Any())
                        {
                            _priceBoostActivityLogs = activityLogs.ActivityLogs
                            .Select(x => new PriceBoostActivityLog()
                            {
                                Id = x.Id,
                                Page = x.Page,
                                Payload = x.Payload,
                                Activity = x.Activity,
                                From = !string.IsNullOrEmpty(x.From) && x.From != "-" ? JsonConvert.DeserializeObject<PriceBoostLogModel>(x.From) : new(),
                                To = !string.IsNullOrEmpty(x.To) && x.To != "-" ? JsonConvert.DeserializeObject<PriceBoostLogModel>(x.To) : new(),
                                UserId = x.UserId,
                                Username = WithAdministrationPagesAccess ? "Administrator" : x.UserName,
                                TimeStamp = x.TimeStamp
                            }).ToList();

                            //build log summary
                            foreach (var log in _priceBoostActivityLogs)
                            {
                                BuildSummary(log);
                            }
                        }
                        break;
                    case SportsbookClientPermission.SBC_OE_PriceBoost_Settings:
                        foreach (var activityLog in activityLogs.ActivityLogs.Where(c => _userClients.Contains(c.Payload)))
                        {
                            var activity = "Increased Cap %";

                            if (activityLog.From != "-")
                            {
                                var cahngedFromCap = JsonConvert.DeserializeObject<BoostedSettingsModel>(activityLog.From).Cap;
                                var cahngedToCap = JsonConvert.DeserializeObject<BoostedSettingsModel>(activityLog.To).Cap;
                                if (cahngedFromCap > cahngedToCap)
                                    activity = "Decrease Cap %";
                            }

                            _boostedSettingsActivityLogs.Add(new ActivityLogModel()
                            {
                                Id = activityLog.Id,
                                Page = activityLog.Page,
                                Payload = GetClientName(activityLog.Payload),
                                Activity = activity,
                                From = EvaluateBoostedSettingsActivity(activityLog.From),
                                To = EvaluateBoostedSettingsActivity(activityLog.To),
                                UserId = activityLog.UserId,
                                UserName = WithAdministrationPagesAccess ? "Administrator" : activityLog.UserName,
                                TimeStamp = activityLog.TimeStamp
                            });
                        }
                        break;
                }

                _metaData = activityLogs.MetaData;
            }

            _isPreLoader = false;
        }
        private async Task<List<KeyValuePair<string, string>>> GetClients()
        {
            var response = await _tradingService.GetClientList();

            List<KeyValuePair<string, string>> Clients = new List<KeyValuePair<string, string>>();

            if (response != null)
            {
                foreach (var client in response.Result)
                {
                    if (_userClients.Contains(client.ClientCode, StringComparer.OrdinalIgnoreCase))
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
                return $"%{(_priceBoostLogFilterModel.ClientCode.ToUpper() != "ALL" ? _priceBoostLogFilterModel.ClientCode : "")}%." +
                $"%{(_priceBoostLogFilterModel.SportId != 0 ? _priceBoostLogFilterModel.SportId : "")}%." +
                $"%{(_priceBoostLogFilterModel.SegmentId != 0 ? _priceBoostLogFilterModel.SegmentId : "")}%." +
                $"%{(_priceBoostLogFilterModel.LeagueId != 0 ? _priceBoostLogFilterModel.LeagueId : "")}%";
            }
            else
            {
                return $"{(_priceBoostLogFilterModel?.ClientCode != "ALL" ? _priceBoostLogFilterModel?.ClientCode : "%")}";
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
            _priceBoostLogFilterModel = item;
            CurrentPage = item.Tab;

            _metaData = new MetaData()
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
            _modalService.Cancel();
        }
        public void Dispose()
        {
            Task.WhenAll();
        }
        #endregion
        #region Utilities
        private int GetRowNumber(int index)
        {
            return (index + 1) + ((_metaData.CurrentPage * _metaData.PageSize) - _metaData.PageSize);
        }
        public async Task SelectPage(int page)
        {
            _metaData.CurrentPage = page;
            await GetContent();
        }
        public string GetSportName(PriceBoostActivityLog activityLog)
        {
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
                stringBuilder.Append($"<span>{activityLog.TimeStamp.ToLocalTime().ToString("dd/MM/yy", CultureInfo.InvariantCulture)}</span><br>");
                stringBuilder.Append($"<span>{activityLog.TimeStamp.ToLocalTime().ToString("HH:mm")}</span>");
            }
            else
            {
                stringBuilder.Append($"<strong><b>{activityLog.From.SelectionName}</b></strong><br>");
                stringBuilder.Append($"{activityLog.From.MarketName}<br>");
                stringBuilder.Append($"<span>{activityLog.From.SegmentName}</span><br>");
                stringBuilder.Append($"<span>{activityLog.From.LeagueName}</span><br>");
                stringBuilder.Append($"{activityLog.From.FixtureName}<br>");
                stringBuilder.Append($"<span>{activityLog.TimeStamp.ToLocalTime().ToString("dd/MM/yy", CultureInfo.InvariantCulture)}</span><br>");
                stringBuilder.Append($"<span>{activityLog.TimeStamp.ToLocalTime().ToString("HH:mm")}</span>");
            }

            stringBuilder.Append("</div>");

            return stringBuilder.ToString();
        }

        public string GetClientName(string payload)
        {
            var payloadSplit = payload.Split(".");

            return _priceBoostLogFilterModel.Clients
                .Where(x => x.Key == payloadSplit[0]).FirstOrDefault()
                .Value;
        }

        private async Task<SportsLookupResponse> GetSports()
        {
            return (await _backofficeService.GetSportLookup(_translationState.ActiveLanguageCode.ToString().ToUpper())).Result;
        }

        private async Task<SegmentLookupResponse> GetSegments(int sportId)
        {
            return sportId != 0 ? (await _backofficeService.GetSegmentLookup(sportId)).Result : null;
        }

        private async Task<LeagueLookupResponse> GetLeagues(int sportId, int segmentId)
        {
            return sportId != 0 && segmentId != 0 ? (await _backofficeService.GetLeagueLookup(sportId, segmentId)).Result : null;
        }

        private string EvaluateBoostedSettingsActivity(string changeData)
        {
            if (!(changeData == null || changeData == string.Empty))
            {
                if (changeData != "-")
                {
                    var viewData = JsonConvert.DeserializeObject<BoostedSettingsModel>(changeData);

                    return $"<b><strong>% {_translationState.GetValue("Cap of Current Odds from Boosted Odds")}</strong></b><br><p>{viewData.Cap.ToString("P2", CultureInfo.InvariantCulture)}</p>";
                }
            }
            return "-";
        }
        #endregion
    }
}
