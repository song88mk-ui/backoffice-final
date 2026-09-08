using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Newtonsoft.Json;
using System.Globalization;

namespace Backoffice.Web.Components.OddsEngine.BoostedSettings
{
    public partial class ViewLogsBoostedSettingModalBody : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IBackofficeService BackofficeService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        protected SearchFormModel SearchForm { get; set; }
        public MetaData MetaData { get; set; } = new MetaData();
        protected List<ActivityLogModel> ActivityLogs { get; set; } = new List<ActivityLogModel>();

        protected DateTime StartDate { get; set; }
        protected DateTime EndDate { get; set; }
        protected string ClientCode { get; set; }
        protected string ClientName { get; set; }
        protected bool IsPreLoader { get; set; } = false;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            ActivityLogs = new List<ActivityLogModel>();
            MetaData = new MetaData()
            {
                PageSize = 10,
                CurrentPage = 1,
                TotalPages = 0,
                TotalCount = 0
            };
            SearchForm = new SearchFormModel();

            StartDate = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(-1);
            EndDate = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(1).AddTicks(-1);

            var client = ModalParameters?.TryGet<BoostedSettingsModel>("Client");

            ClientCode = client.ClientCode;
            ClientName = client.ClientName;

            await GetContent();
        }

        protected async Task SelectedPage(int page)
        {
            MetaData.CurrentPage = page;
            await GetContent();
        }

        public async Task OnSubmitClickEvent()
        {
            await GetContent();

            ActivityLogs = ActivityLogs.Where(s => s.Activity.ToLower().Contains(SearchForm.SearchText.ToLower()) || s.From.ToLower().Contains(SearchForm.SearchText.ToLower()) ||
            s.To.ToLower().Contains(SearchForm.SearchText.ToLower()) || s.UserName.ToLower().Contains(SearchForm.SearchText.ToLower())
            ).ToList();

            MetaData.TotalCount = ActivityLogs.Count();
            MetaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(MetaData.TotalCount) / Convert.ToDecimal(MetaData.PageSize)));
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task GetContent()
        {
            IsPreLoader = true;

            ActivityLogs = new List<ActivityLogModel>();

            var response = await BackofficeService.GetActivityLogs(new ActivityLogRequest
            {
                Page = SportsbookClientPermission.SBC_OE_PriceBoost_Settings.ToString().Replace("_", "."),
                Payload = $"{ClientCode}",
                MetaData = MetaData,
                DateFrom = StartDate,
                DateTo = EndDate
            });

            if (response.Result != null)
            {
                var tempActivityLogs = response.Result.ActivityLogs.Where(s => s.Activity.ToLower().Contains(SearchForm.SearchText.ToLower()) || s.From.ToLower().Contains(SearchForm.SearchText.ToLower()) ||
                                       s.To.ToLower().Contains(SearchForm.SearchText.ToLower()) || s.UserName.ToLower().Contains(SearchForm.SearchText.ToLower())).ToList();

                foreach (var activityLog in tempActivityLogs)
                {
                    var activity = "Increased Cap %";

                    if (activityLog.From != "-")
                    {
                        var cahngedFromCap = JsonConvert.DeserializeObject<BoostedSettingsModel>(activityLog.From).Cap;
                        var cahngedToCap = JsonConvert.DeserializeObject<BoostedSettingsModel>(activityLog.To).Cap;
                        if (cahngedFromCap > cahngedToCap)
                            activity = "Decrease Cap %";
                    }

                    ActivityLogs.Add(new ActivityLogModel()
                    {
                        Id = activityLog.Id,
                        Page = activityLog.Page,
                        Payload = activityLog.Payload,
                        Activity = activity,
                        From = activityLog.From,
                        To = activityLog.To,
                        UserId = activityLog.UserId,
                        UserName = activityLog.UserName,
                        TimeStamp = activityLog.TimeStamp
                    });
                }

                MetaData = response.Result.MetaData;
            }

            IsPreLoader = false;
        }

        public async Task OnRangeSelect(DateRange range)
        {
            var clientDateRange = DateTimeHelper.ConvertToClientTimeZone(range, _userState.TimeZoneOffset);
            StartDate = clientDateRange.Start.DateTime;
            EndDate = clientDateRange.End.DateTime;

            await GetContent();
        }

        private string EvaluateActivity(string changeData)
        {
            if (!(changeData == null || changeData == string.Empty))
            {
                if (changeData != "-")
                {
                    var viewData = JsonConvert.DeserializeObject<BoostedSettingsModel>(changeData);

                    return $"<b><strong>% Cap of Current Odds from</b><br><b>Boosted Odds</strong></b><br><p>{viewData.Cap.ToString("P2", CultureInfo.InvariantCulture)}</p>";
                }
            }
            return "-";
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
