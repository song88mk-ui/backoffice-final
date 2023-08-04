using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;


namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ModalConfig : ComponentBase, IDisposable
    {
        #region Properties

        [Inject] public IModalService Modal { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IConfiguration Configuration { get; set; }
        [CascadingParameter] public ModalParameters Parameters { get; set; }
        [CascadingParameter] public BlazoredToolTip BlazoredModal { get; set; }
        [Parameter] public bool ShowModal { get; set; }

        public bool IsPrematch { get; set; }
        public KeyTimeModel Setting { get; set; }
        public KeyTimeModel NewSetting { get; set; }
        //public List<KeyTimeModel> Settings { get; set; }
        public LiveSettingsTemplateModel SettingsLive { get; set; }
        public PrematchSettingsTemplateModel SettingsPrematch { get; set; }
        protected string ValidationClass { get; set; } = "";
        #endregion

        #region Fields
        protected EditContext _editContext;
        protected bool _isInvalidForm = true;
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsPrematch = Parameters.Get<bool>("IsPrematch");
            Setting = Parameters.Get<KeyTimeModel>("Setting");

            if (IsPrematch == true)
            {
                SettingsPrematch = Parameters.Get<PrematchSettingsTemplateModel>("Settings");
            }
            else
            {
                SettingsLive = Parameters.Get<LiveSettingsTemplateModel>("Settings");
            }

            NewSetting = new KeyTimeModel()
            {
                Key = Setting.Key,
                Time = Setting.Time,
                SortOrder = Setting.SortOrder,
                TimeDisplay = Setting.TimeDisplay
            };

            _editContext = new EditContext(NewSetting);

            _editContext.OnFieldChanged += (x, y) =>
            {
                _isInvalidForm = !(_editContext.Validate() && NewSetting.Key != null && NewSetting.Key > 100 && NewSetting.Key <= 999);
                ValidationClass = _isInvalidForm ? "is-invalid" : "";
                StateHasChanged();
            };

        }

        protected override void OnParametersSet()
        {
            Setting = Parameters.Get<KeyTimeModel>("Setting");
            if (IsPrematch == true)
            {
                SettingsPrematch = Parameters.Get<PrematchSettingsTemplateModel>("Settings");
            }
            else
            {
                SettingsLive = Parameters.Get<LiveSettingsTemplateModel>("Settings");
            }
            NewSetting = new KeyTimeModel()
            {
                Key = Setting.Key,
                Time = Setting.Time,
                SortOrder = Setting.SortOrder,
                TimeDisplay = Setting.TimeDisplay
            };
        }

        public async void Dispose()
        {
        }

        public void CloseModal()
        {
            Modal.Cancel();
        }

        public async void UpdateSetting()
        {
            if (NewSetting.Key == null || NewSetting.Key <= 100 || NewSetting.Key > 999)
            {
                _isInvalidForm = true;
                ValidationClass = _isInvalidForm ? "is-invalid" : "";
                StateHasChanged();
                return;
            }

            Setting.Key = null;
            if (IsPrematch == true)
            {
                var setting = SettingsPrematch.Settings.FirstOrDefault(s => s.Time == NewSetting.Time);
                if (setting != null)
                {
                    setting.Key = NewSetting.Key;
                }
                SettingsPrematch.LastKey = GetLastKey(true);
            }
            else
            {
                var setting = SettingsLive.Settings.FirstOrDefault(s => s.Time == NewSetting.Time);
                if (setting != null)
                {
                    setting.Key = NewSetting.Key;
                }

                SettingsLive.LastKey = GetLastKey(false);
            }
            
            StateHasChanged();

            Modal.Close(ModalResult.Ok("Form Submitted"));
        }

        public async void DeleteKey()
        {
            if (Setting.Key != null)
            {
                Setting.Key = null;
                if (IsPrematch == true)
                {
                    SettingsPrematch.LastKey = GetLastKey(true);
                }
                else
                {
                    SettingsLive.LastKey = GetLastKey(false);
                }
                StateHasChanged();
                Modal.Close(ModalResult.Ok("Form Submitted"));
            }
        }

        #endregion

        //#region Utilies
        private string GetLastKey(bool isPrematch)
        {
            if (isPrematch)
            {
                var lastKey = SettingsPrematch.Settings.Where(s => s.Key > 101).LastOrDefault()?.Key;
                return (lastKey ?? 101).ToString();
            }
            else
            {
                var lastKey = SettingsLive.Settings.Where(s => s.Key > 101).LastOrDefault()?.Key;
                return (lastKey ?? 101).ToString();
            }

        }
        //#endregion
    }
}
