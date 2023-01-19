using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class TimelineAxisItem : IDisposable
    {
        #region Properties
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Parameter] public List<KeyTimeModel> Settings { get; set; }
        [Parameter] public EventCallback<List<KeyTimeModel>> SettingsChanged { get; set; }        

        private Dictionary<string,ElementReference> ElementReferenceDict { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            ElementReferenceDict = new Dictionary<string, ElementReference>();
        }

        public async Task OnAddKeyClickEvent(KeyTimeModel settings)
        {

        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
