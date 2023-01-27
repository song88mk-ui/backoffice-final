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

        #endregion

        #region Methods

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
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
