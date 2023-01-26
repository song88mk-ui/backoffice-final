using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.MonitoringDetails
{
    public partial class MarketFilter : IDisposable
    {
        #region Properties

        [Parameter] public List<MarketNavigationModel2> MarketTemplates { get; set; }
        [Parameter] public EventCallback<string> OnMarketTemplateClickCallback { get; set; }
        //[Parameter] public EventCallback<List<MarketNavigationModel>> OnMarketTemplateClicked { get; set; }

        #endregion

        #region Methods

        public async Task OnInitMarketTemplates(List<MarketNavigationModel> marketTemplates, int SelectedMarketTemplateGroupId, SearchFormModel searchForm)
        {
            //MarketTemplates = new List<MarketNavigationModel>();

            //var result = marketTemplates.Where(m => m.MarketTemplateGroupId == SelectedMarketTemplateGroupId).Distinct().OrderBy(m => m.MarketName).ToList();

            //if (!string.IsNullOrEmpty(searchForm.SearchText) && searchForm.SearchText.Length > 1)
            //    result = result.Where(c => c.MarketName.ToUpper().Contains(searchForm.SearchText.ToUpper())).ToList();

            //MarketTemplates = result;

            //await InvokeAsync(StateHasChanged);

            //await OnMarketTemplateClicked.InvokeAsync(MarketTemplates);
        }

        public async Task OnMarketTemplateClickEvent(MarketNavigationModel2 model)
        {
            if (model.IsSelected)
                return;

            MarketTemplates.ForEach(s =>
            {
                s.IsSelected = false;
            });

            model.IsSelected = true;

            await OnMarketTemplateClickCallback.InvokeAsync(model.MarketTemplateId);

            //var marketTemplate = MarketTemplates.FirstOrDefault(c => c.MarketTemplateId == marketTemplateId && c.MarketTemplateGroupId == marketTemplateGroupId && c.MarketName == marketName);

            //if (marketTemplate != null && !marketTemplate.IsSelected)
            //{
            //    // set all selected property to false
            //    MarketTemplates.Select(s =>
            //    {
            //        s.IsSelected = false;
            //        return s;
            //    }).ToList();

            //    marketTemplate.IsSelected = true;
            //}

            //await OnMarketTemplateClicked.InvokeAsync(MarketTemplates);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
