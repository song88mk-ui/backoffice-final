using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class AccordionTemplateConfiguration : IDisposable
    {
        [Parameter] public MarketLookupNavigationModel Market { get; set; }
        //[Parameter] public EventCallback<MarketLookupNavigationModel> OnMarketChangedCallback { get; set; }
        [Parameter] public List<string> FilterConfigStatus { get; set; }
        [Parameter] public int Scope { get; set; }
        

        #region Methods                                        

        public async Task OnMarketActiveChangedEvent(ChangeEventArgs args)
        {

            Market.MarketLookup.IsActive = (bool)args.Value;

            //await OnMarketChangedCallback.InvokeAsync(Market);
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
