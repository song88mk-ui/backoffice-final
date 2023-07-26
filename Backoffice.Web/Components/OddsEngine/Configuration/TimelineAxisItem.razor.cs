using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class TimelineAxisItem : IDisposable
    {
        #region Properties
        [Inject] public IModalService Modal { get; set; }
        [Parameter] public PrematchSettingsTemplateModel PrematchSettings { get; set; }
        [Parameter] public EventCallback OnMarginChangeCallback { get; set; }
        public Type ContentComponent { get; set; }
        public ModalParameters Parameters { get; set; }
        private Dictionary<string,ElementReference> ElementReferenceDict { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            ElementReferenceDict = new Dictionary<string, ElementReference>();
            ContentComponent = typeof(ModalConfig);
            Modal.OnClose += Modal_OnClose;
        }

        public async Task OnAddKeyClickEvent(KeyTimeModel settings)
        {
            settings.IsTooltipVisible = !settings.IsTooltipVisible;

            if (settings.IsTooltipVisible)
            {
                LoadModalParam(settings);

                await InvokeAsync(StateHasChanged);
            }
            else
            {
                CloseModal();
            }
        }

        public void CloseModal()
        {
            Modal.Cancel();
        }
        
        public void Dispose()
        {
            
        }
        #endregion

        #region Utilities
        private async void Modal_OnClose(ModalResult obj)
        {
            if (!obj.Cancelled)
            {
                await OnMarginChangeCallback.InvokeAsync();
            }
        }

        private void LoadModalParam(KeyTimeModel setting)
        {
            string Title = "Add Config";

            if (setting.Key != null && PrematchSettings.Settings.Where(a => a.Key == setting.Key && a.Time == setting.Time).Count() > 0)
            { Title = "Edit Config"; }

            Parameters = new ModalParameters();
            Parameters.Add("IsPrematch", true);
            Parameters.Add("Setting", setting);
            Parameters.Add("Settings", PrematchSettings);
            Parameters.Add("Class", "configmatch");
            Parameters.Add("MaxWidth", 400);
            Parameters.Add("Title", Title);

            ContentComponent = typeof(ModalConfig);

        }

        #endregion
    }
}
