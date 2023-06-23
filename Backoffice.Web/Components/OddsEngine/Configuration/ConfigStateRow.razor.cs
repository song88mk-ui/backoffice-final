using Backoffice.Shared.Models;
using Backoffice.Web.Components.OddsEngine.BoostedSettings;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using System.Collections.Generic;
using System.Drawing;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ConfigStateRow
    {
        [Parameter] public int Index { get; set; }
        [Parameter] public ConfigClientModel Config { get; set; }
        private bool _IsEdit = false;

        private List<ConfigTemplateModel> _template;
        protected override Task OnInitializedAsync()
        {
            _template  = new List<ConfigTemplateModel>();

            return base.OnInitializedAsync();
        }

        #region Methods
        public async Task OnEditClickedEvent()
        {
            try
            {
                _IsEdit = !_IsEdit;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }
        public async Task OnSaveClickedEvent()
        {
            try
            {

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }
        #endregion
    }
}
