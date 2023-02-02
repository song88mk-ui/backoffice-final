using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Text;

namespace Backoffice.Web.Components.ManageBoost
{
    public partial class ViewLogsBoostedModal : IDisposable
    {
        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        public List<PriceBoostActivityLog> ActivityLogs { get; set; }
        public PriceBoostConfigModel PriceBoost { get; set; } 
        public bool IsPreLoader { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = true;

            PriceBoost = ModalParameters.TryGet<PriceBoostConfigModel>("PriceBoostConfigModel");

            ActivityLogs = new List<PriceBoostActivityLog>();

            var payload = $"{PriceBoost.ClientCode}.{PriceBoost.SportId}.{PriceBoost.SegmentId}.{PriceBoost.LeagueId}";

            var result = (await BackOfficeService.GetActivityLogs(new ActivityLogRequest()
            {
                Page = Page.OE_PriceBoost_Manage.ToString().Replace("_", "."),
                Payload = payload,                
                MetaData = new MetaData() { PageSize = 10 }
            })).Result;

            if (result != null && result.ActivityLogs != null && result.ActivityLogs.Any())
            {
                ActivityLogs = result.ActivityLogs.Select(x => new PriceBoostActivityLog()
                {
                    Id = x.Id,
                    Page = x.Page,
                    Payload = x.Payload,
                    Activity = x.Activity,
                    From = x.From != "-" ? JsonConvert.DeserializeObject<Models.PriceBoostLogModel>(x.From) : new Models.PriceBoostLogModel(),
                    To = x.To != "-" ? JsonConvert.DeserializeObject<Models.PriceBoostLogModel>(x.To) : new Models.PriceBoostLogModel(),
                    UserId = x.UserId,
                    Username = x.UserName,
                    TimeStamp = x.TimeStamp
                }).Where(x => (x.From.MarketSpecifier == PriceBoost.MarketSpecifier || x.To.MarketSpecifier == PriceBoost.MarketSpecifier) &&
                (x.From.SelectionTemplateId == PriceBoost.SelectionTemplateId || x.To.SelectionTemplateId == PriceBoost.SelectionTemplateId))
                .ToList();
            }

            IsPreLoader = false;
        }
        public async Task OnCancelModalClick()
        {
            ModalService.Cancel();
        }

        public string GetLogDetails(PriceBoostActivityLog log, bool isFrom)
        {
            if (log.Activity == Activity.Create.ToString())
            {
                if (isFrom)
                {
                    return "-";
                }
                else
                {
                    var toDeserialized = log.To;
                    return $"<b>Boosted Odds</b></br> {toDeserialized.Odds.ToString("0.00")}";
                }
            }
            else if (log.Activity == Activity.Update.ToString())
            {
                var fromDeserialized = log.From;
                var toDeserialized = log.To;

                StringBuilder stringBuilder = new StringBuilder();

                if (isFrom)
                {
                    if (fromDeserialized.Odds != toDeserialized.Odds)
                    {
                        stringBuilder.Append($"<b>Boosted Odds</b> </br> {fromDeserialized.Odds.ToString("0.00")}");
                    }

                    if (fromDeserialized.Translations.Count != toDeserialized.Translations.Count ||
                        JsonConvert.SerializeObject(fromDeserialized.Translations) != JsonConvert.SerializeObject(toDeserialized.Translations))
                    {
                        stringBuilder.Append("<b>Translations:</b> </br>");
                        foreach (var translation in fromDeserialized.Translations)
                        {                            
                            stringBuilder.Append($"{translation.Language.LanguageName}<br>");
                            stringBuilder.Append($"{translation.Language.LanguageCode}<br>");
                            stringBuilder.Append($"{translation.BoostedSelectionName}<br>");
                        }
                    }
                }
                else
                {
                    if (fromDeserialized.Odds != toDeserialized.Odds)
                    {
                        stringBuilder.Append($"<b>Boosted Odds</b> </br> {toDeserialized.Odds.ToString("0.00")}");
                    }

                    if (fromDeserialized.Translations.Count != toDeserialized.Translations.Count || 
                        JsonConvert.SerializeObject(fromDeserialized.Translations) != JsonConvert.SerializeObject(toDeserialized.Translations))
                    {
                        stringBuilder.Append("<b>Translations:</b> </br>");
                        foreach (var translation in toDeserialized.Translations)
                        {                            
                            stringBuilder.Append($"{translation.Language.LanguageName}<br>");
                            stringBuilder.Append($"{translation.Language.LanguageCode}<br>");
                            stringBuilder.Append($"{translation.BoostedSelectionName}<br>");
                        }
                    }
                }

                return stringBuilder.ToString();
            }
            else //delete
            {
                return "-";
            }
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
