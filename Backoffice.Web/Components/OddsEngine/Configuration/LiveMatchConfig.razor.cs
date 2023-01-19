using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class LiveMatchConfig : IDisposable
    {
        #region Properties        
        [Parameter] public LiveSettingsModel LiveSettings { get; set; }
        [Parameter] public EventCallback<LiveSettingsModel> LiveSettingsChanged { get; set; }
        
        #endregion

        #region Methods
        public void Dispose()
        {

        }
        #endregion
    }
}
