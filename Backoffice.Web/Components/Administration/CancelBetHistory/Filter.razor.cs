using Backoffice.Shared.Models;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Administration.CancelBetHistory
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Parameter] public EventCallback<CancelBetHistoryCallbackModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback<CancelBetHistoryCallbackModel> OnExportCallback { get; set; }
        [Parameter] public bool IsPreload { get; set; }
        protected CancelBetHistoryCallbackModel FilterModel { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            FilterModel = new CancelBetHistoryCallbackModel
            {
                DateFrom = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(-1),
                DateTo = DateTimeHelper.Now(_userState.TimeZoneOffset).Date.AddDays(1).AddTicks(-1),
            };

            await OnInitCallback.InvokeAsync(FilterModel);
        }
        public async Task OnSearchClickEvent()
        {
            if (IsPreload)
                return;

            await OnInitCallback.InvokeAsync(FilterModel);
        }

        public async Task OnExportToExcelClickEvent()
        {
            if (IsPreload)
                return;

            await OnExportCallback.InvokeAsync(FilterModel);
        }

        public async Task OnDateRangeChangedEvent(DateRange range)
        {
            var clientDateRange = DateTimeHelper.ConvertToClientTimeZone(range, _userState.TimeZoneOffset);
            FilterModel.DateFrom = clientDateRange.Start.DateTime;
            FilterModel.DateTo = clientDateRange.End.DateTime;

            await OnInitCallback.InvokeAsync(FilterModel);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
