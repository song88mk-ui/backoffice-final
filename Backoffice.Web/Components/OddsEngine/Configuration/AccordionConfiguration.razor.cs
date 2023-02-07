using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class AccordionConfiguration : IDisposable
    {
        [Parameter] public MarketLookupNavigationModel Market { get; set; }
        [Parameter] public List<MarketLookupNavigationModel> MarketLookupNavigation { get; set; }
        [Parameter] public EventCallback<List<MarketLookupNavigationModel>> MarketLookupNavigationChanged { get; set; }
        [Parameter] public EventCallback<int> FilterConfigStatusChanged { get; set; }
        [Parameter] public EventCallback<int> ScopeChanged { get; set; }
        [Parameter] public EventCallback<MarketLookupNavigationModel> OnMarketChangedCallback { get; set; }
        [Parameter] public int FilterConfigStatus { get; set; }
        [Parameter] public int Scope { get; set; }
        [Parameter] public int MarketIndex { get; set; }


        #region Methods                                
        protected override async Task OnParametersSetAsync()
        {
        }

        public async Task OnMarketActiveChangedEvent(ChangeEventArgs args)
        {

            Market.MarketLookup.IsActive = (bool)args.Value;

            await OnMarketChangedCallback.InvokeAsync(Market);
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
