using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class AccordionTemplateConfiguration : IDisposable
    {
        [Parameter] public MarketLookupNavigationModel Market { get; set; }
        [Parameter] public EventCallback OnTemplateMarginChangeCallback { get; set; }
        [Parameter] public List<string> FilterConfigStatus { get; set; }
        [Parameter] public int Scope { get; set; }
        

        #region Methods                                        

        public async Task OnMarketActiveChangedEvent(ChangeEventArgs args)
        {
            Market.NewMarketLookup.IsActive = (bool)args.Value;

            await OnTemplateMarginChangeCallback.InvokeAsync();
        }

        public async Task OnMarginChangedEvent()
        {

            await OnTemplateMarginChangeCallback.InvokeAsync();
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
