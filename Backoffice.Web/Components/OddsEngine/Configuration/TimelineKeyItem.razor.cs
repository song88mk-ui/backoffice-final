using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class TimelineKeyItem : IDisposable
    {
        #region Properties
        [Parameter] public List<KeyTimeModel> Settings { get; set; }
        //[Parameter] public EventCallback<List<KeyTimeModel>> SettingsChanged { get; set; }
        #endregion
        #region Methods
        public void Dispose()
        {
            
        }
        #endregion
    }
}
