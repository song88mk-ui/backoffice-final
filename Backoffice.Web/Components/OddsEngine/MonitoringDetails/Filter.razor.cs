using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MonitoringDetails
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        [Parameter] public EventCallback<string> OnMarketChangedCallback { get; set; }
        [Parameter] public int SportId { get; set; }

        public List<SelectListItemModel<string>> MarketTemplates { get; set; }
        public string SelectedMarketTemplateId { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            MarketTemplates = new List<SelectListItemModel<string>>();

            var mainMarketTemplateResponse = await BackOfficeService.GetTopMarkets(SportId);

            if (mainMarketTemplateResponse.StatusCode == StatusCode.Success)
            {
                MarketTemplates = mainMarketTemplateResponse.Result.Select(s => new SelectListItemModel<string>
                {
                    Value = s.MainMarketTemplateId,
                    Name = s.Name
                }).ToList();
            }

            if (MarketTemplates.Any())
            {
                SelectedMarketTemplateId = MarketTemplates.FirstOrDefault().Value;
            }

            await OnMarketChangedCallback.InvokeAsync(SelectedMarketTemplateId);
        }

        public async void OnMarketChangedEvent(ChangeEventArgs e)
        {
            SelectedMarketTemplateId = e.Value.ToString();

            await OnMarketChangedCallback.InvokeAsync(SelectedMarketTemplateId);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
