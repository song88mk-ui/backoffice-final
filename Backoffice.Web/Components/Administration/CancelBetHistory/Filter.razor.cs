using Backoffice.Shared.Models;
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
        protected CancelBetHistoryCallbackModel FilterModel { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            FilterModel = new CancelBetHistoryCallbackModel
            {
                DateFrom = DateTime.Now.Date,
                DateTo = DateTime.Now,
            };

            await OnInitCallback.InvokeAsync(FilterModel);
        }
        public async Task OnSearchClickEvent()
        {
            await OnInitCallback.InvokeAsync(FilterModel);
        }

        public async Task OnExportToExcelClickEvent()
        {
            await OnExportCallback.InvokeAsync(FilterModel);
        }

        public async Task OnDateRangeChangedEvent(DateRange range)
        {
            FilterModel.DateFrom = DateTime.Parse(range.Start.ToString()).Date.Add(new TimeSpan(0, 0, 0));
            FilterModel.DateTo = DateTime.Parse(range.End.ToString()).Date.Add(new TimeSpan(23, 59, 59));

            await OnInitCallback.InvokeAsync(FilterModel);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
