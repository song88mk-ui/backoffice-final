using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Globalization;

namespace Backoffice.Web.Components.OddsEngine.BoostedSettings
{
    public partial class ViewLogsBoostedSettingModalBody : IDisposable
    {

        #region Properties
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        protected SearchFormModel SearchForm { get; set; }
        public MetaData MetaData { get; set; } = new MetaData();
        protected DateTime StartDate { get; set; }
        protected DateTime EndDate { get; set; }
        protected List<ActivityLogModel> ActivityLogs { get; set; } = new List<ActivityLogModel>();
        private int PreviousPage { get; set; }
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

            StartDate = DateTime.UtcNow.AddDays(-1).Date;
            EndDate = DateTime.UtcNow.Date;

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
            s.To.ToLower().Contains(SearchForm.SearchText.ToLower()) || s.Username.ToLower().Contains(SearchForm.SearchText.ToLower())
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
            await InvokeAsync(StateHasChanged);

            ActivityLogs = new List<ActivityLogModel>();
            var response = (await BackOfficeService.GetActivityLogs(new ActivityLogRequest
            {
                Page = "OE_PriceBoost_Settings".ToString().Replace("_", "."),
                Payload = $"{ClientCode}",
                MetaData = MetaData,
                DateFrom = Convert.ToDateTime(StartDate),
                DateTo = Convert.ToDateTime(EndDate)
            })).Result;

            if (response != null)
            {
                var tempActivityLogs = response.ActivityLogs.Where(s => s.Activity.ToLower().Contains(SearchForm.SearchText.ToLower()) || s.From.ToLower().Contains(SearchForm.SearchText.ToLower()) ||
                                       s.To.ToLower().Contains(SearchForm.SearchText.ToLower()) || s.Username.ToLower().Contains(SearchForm.SearchText.ToLower())).ToList();

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
                        Username = activityLog.Username,
                        TimeStamp = activityLog.TimeStamp
                    });
                }

                MetaData = response.MetaData;
            }

            IsPreLoader = false;
        }
        public async Task OnRangeSelect(DateRange range)
        {
            StartDate = Convert.ToDateTime(range.Start.ToString("yyyy-MM-dd 00:00:00"));
            EndDate = Convert.ToDateTime(range.End.ToString("yyyy-MM-dd 23:59:59"));

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
