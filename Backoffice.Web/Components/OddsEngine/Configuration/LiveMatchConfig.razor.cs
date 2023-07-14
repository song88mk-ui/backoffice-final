using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class LiveMatchConfig : IDisposable
    {
        #region Properties        
        [Parameter] public LiveSettingsTemplateModel LiveSettings { get; set; }
        [Parameter] public EventCallback<bool> OnMarginChangeCallback { get; set; }
        
        #endregion

        #region Methods
        public void Dispose()
        {

        }
        #endregion
    }
}
