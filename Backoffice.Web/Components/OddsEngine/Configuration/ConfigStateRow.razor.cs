using Backoffice.Shared.Enums;
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
        [Parameter] public ConfigState? State { get; set; }
        [Parameter] public ConfigClientViewModel Config { get; set; }

        [Parameter] public EventCallback<ConfigClientViewModel> ItemSelectionChangedEvent { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }

        private bool _IsEdit = false;

        #region Methods
        public async Task OnEditClickedEvent()
        {
            try
            {
                _IsEdit = !_IsEdit;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
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
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }
        public async Task OnCheckboxClicked(ChangeEventArgs e)
        {
            Config.IsChecked = (bool)e.Value;
            await ItemSelectionChangedEvent.InvokeAsync(Config);
        }

        public async Task OnEditTemplateClickEvent(long configTemplateId)
        {
            NavigationManager.NavigateTo($"/hk/odds-engine/config-templates/edit/{configTemplateId}/true");
        }
        #endregion
    }
}
