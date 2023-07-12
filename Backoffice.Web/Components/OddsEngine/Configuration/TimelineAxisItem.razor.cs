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
        [Parameter] public List<KeyTimeModel> Settings { get; set; }
        [Parameter] public PrematchSettingsTemplateModel PrematchSettings { get; set; }
        //[Parameter] public EventCallback<List<KeyTimeModel>> SettingsChanged { get; set; }
        public Type ContentComponent { get; set; }
        public ModalParameters Parameters { get; set; }
        private Dictionary<string,ElementReference> ElementReferenceDict { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            ElementReferenceDict = new Dictionary<string, ElementReference>();
            ContentComponent = typeof(ModalConfig);
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
        private void LoadModalParam(KeyTimeModel settings)
        {
            string Title = "Add Config";

            if (settings.Key != null && PrematchSettings.Settings.Where(a => a.Key == settings.Key && a.Time == settings.Time).Count() > 0)
            { Title = "Edit Config"; }

            Parameters = new ModalParameters();
            Parameters.Add("IsPrematch", true);
            Parameters.Add("Setting", settings);
            Parameters.Add("Settings", PrematchSettings);
            Parameters.Add("Class", "configmatch");
            Parameters.Add("MaxWidth", 400);
            Parameters.Add("Title", Title);

            ContentComponent = typeof(ModalConfig);

        }

        #endregion
    }
}
