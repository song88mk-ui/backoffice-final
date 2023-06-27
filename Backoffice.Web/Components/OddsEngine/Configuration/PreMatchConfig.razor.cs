using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class PreMatchConfig : IDisposable
    {
        #region Properties
        [Parameter] public PrematchSettingsTemplateModel PrematchSettings { get; set; } 
        [Parameter] public EventCallback<PrematchSettingsTemplateModel> PrematchSettingsChanged { get; set; }
        #endregion
        #region Methods
        public void Dispose()
        {
            
        }
        #endregion
    }
}
