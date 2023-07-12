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

            Setting.Key = NewSetting.Key;
            Setting.Time = NewSetting.Time;
            if (IsPrematch == true)
            {
                SettingsPrematch.LastKey = GetLastKey(true);
            }
            else
            {
                SettingsLive.LastKey = GetLastKey(false);
            }
                //if (IsPrematch == true)
                //{
                //    //var currentSetting = SettingsPrematch.Settings.Where(s => s.Time == Setting.Time).FirstOrDefault();
                //    //currentSetting.Key = Setting.Key;
                //    SettingsPrematch.LastKey = GetLastKey(true);
                //    //    ConfigSettingsModel data = new ConfigSettingsModel();
                //    //    data.ClientId = new Guid(SettingsPrematch.ClientId);
                //    //    data.SportId = SettingsPrematch.SportId;
                //    //    data.SegmentId = SettingsPrematch.SegmentId;
                //    //    data.LeagueId = SettingsPrematch.LeagueId;
                //    //    data.MarketTemplateId = int.Parse(SettingsPrematch.MarketTemplateId);


                //    //    //Check for existing keys
                //    //    var existingKeys = SettingsPrematch.Settings.Where(a => a.Key != null).ToList();

                //    //    if (existingKeys != null)
                //    //    {
                //    //        data.PrematchSettings.Settings = existingKeys.Select(a=> new KeyTimeClientConfigModel { Key= (int)a.Key, Time = a.Time, SortOrder = a.SortOrder }).ToList();
                //    //        var dataExist = data.PrematchSettings.Settings.Where(a => a.Time == Setting.Time).FirstOrDefault();
                //    //        if (dataExist == null)
                //    //        {
                //    //            data.PrematchSettings.Settings.Add(new KeyTimeClientConfigModel { Key = (int)Setting.Key, Time = Setting.Time , SortOrder = Setting.SortOrder});
                //    //        }
                //    //        else
                //    //        {
                //    //            foreach (var row in data.PrematchSettings.Settings)
                //    //            {
                //    //                if (row.Time == Setting.Time)
                //    //                {
                //    //                    row.Key = (int)Setting.Key;
                //    //                    break;
                //    //                }
                //    //            }
                //    //        }
                //    //    }
                //    //    else
                //    //    {
                //    //        data.PrematchSettings.Settings = new List<KeyTimeClientConfigModel>();
                //    //    }

                //    //await TradingService.AddOrUpdateConfig(data);
                //    //if (IsConnected) await HubConnection.SendAsync("OnConfigChange", data);
                //}
                //else
                //{
                //    var currentSetting = SettingsLive.Settings.Where(s => s.Time == Setting.Time).FirstOrDefault();
                //    currentSetting.Key = Setting.Key;
                //    SettingsLive.LastKey = GetLastKey(false);
                //    //    ConfigSettingsModel data = new ConfigSettingsModel();
                //    //    data.ClientId = new Guid(SettingsLive.ClientId);
                //    //    data.SportId = SettingsLive.SportId;
                //    //    data.SegmentId = SettingsLive.SegmentId;
                //    //    data.LeagueId = SettingsLive.LeagueId;
                //    //    data.MarketTemplateId = int.Parse(SettingsLive.MarketTemplateId);


                //    //    //Check for existing keys
                //    //    var existingKeys = SettingsLive.Settings.Where(a => a.Key != null).ToList();

                //    //    if (existingKeys != null)
                //    //    {
                //    //        data.LivematchSettings.Settings = existingKeys.Select(a => new KeyTimeClientConfigModel { Key = (int)a.Key, Time = a.Time, SortOrder = a.SortOrder }).ToList();

                //    //        var dataExist = data.LivematchSettings.Settings.Where(a => a.Time == Setting.Time).FirstOrDefault();
                //    //        if (dataExist == null)
                //    //        {
                //    //            data.LivematchSettings.Settings.Add(new KeyTimeClientConfigModel { Key = (int)Setting.Key, Time = Setting.Time, SortOrder=Setting.SortOrder });
                //    //        }
                //    //        else
                //    //        {
                //    //            foreach (var row in data.LivematchSettings.Settings)
                //    //            {
                //    //                if (row.Time == Setting.Time)
                //    //                {
                //    //                    row.Key = (int)Setting.Key;
                //    //                    break;
                //    //                }
                //    //            }
                //    //        }
                //    //    }
                //    //    else
                //    //    {
                //    //        data.LivematchSettings.Settings = new List<KeyTimeClientConfigModel>();
                //    //    }

                //    //    //await TradingService.AddOrUpdateConfig(data);
                //    //    //if (IsConnected) await HubConnection.SendAsync("OnConfigChange", data);
                //}
                StateHasChanged();

            Modal.Close(ModalResult.Ok("Form Submitted"));
        }

        public async void DeleteKey()
        {
            if (Setting.Key != null)
            {
                //////ConfigSettingsModel data = new ConfigSettingsModel();
                ////if (IsPrematch == true)
                ////{
                ////    //var setting = SettingsPrematch.Settings.Where(a => a.Time == Setting.Time && a.Key == Setting.Key && a.SortOrder == Setting.SortOrder).FirstOrDefault();
                ////    //setting.Key = null;
                ////    Setting.Key = null;
                ////    ////Check for existing keys
                ////    //var existingKeys = SettingsPrematch.Settings.Where(a => a.Time == Setting.Time).Select(a => new { Key = a.Key, Time = a.Time, SortOrder = a.SortOrder }).FirstOrDefault();
                ////    //if (existingKeys != null)
                ////    //{

                ////    //    //data.ClientId = new Guid(SettingsPrematch.ClientId);
                ////    //    //data.SportId = SettingsPrematch.SportId;
                ////    //    //data.SegmentId = SettingsPrematch.SegmentId;
                ////    //    //data.LeagueId = SettingsPrematch.LeagueId;
                ////    //    //data.MarketTemplateId = int.Parse(SettingsPrematch.MarketTemplateId);
                ////    //    //data.PrematchSettings.Settings = SettingsPrematch.Settings.Where(a => a.Key != null).Select(a => new KeyTimeClientConfigModel { Key = (int)a.Key, Time = a.Time, SortOrder = a.SortOrder }).ToList();

                ////    //    ////Remove key
                ////    //    //data.PrematchSettings.Settings.RemoveAll(a=>a.Time == existingKeys.Time && a.Key==existingKeys.Key && a.SortOrder == existingKeys.SortOrder);
                ////    //    //await TradingService.AddOrUpdateConfig(data);
                ////    //}
                ////}
                ////else
                ////{
                ////    //var setting = SettingsLive.Settings.Where(a => a.Time == Setting.Time && a.Key == Setting.Key && a.SortOrder == Setting.SortOrder).FirstOrDefault();
                ////    //setting.Key = null;
                ////    Setting.Key = null;

                ////    ////Check for existing keys
                ////    //var existingKeys = SettingsLive.Settings.Where(a => a.Key == Setting.Key && a.Time == Setting.Time).Select(a => new KeyTimeClientConfigModel { Key = (int)a.Key, Time = a.Time, SortOrder = a.SortOrder }).FirstOrDefault();
                ////    //if (existingKeys != null)
                ////    //{

                ////    //    //data.ClientId = new Guid(SettingsLive.ClientId);
                ////    //    //data.SportId = SettingsLive.SportId;
                ////    //    //data.SegmentId = SettingsLive.SegmentId;
                ////    //    //data.LeagueId = SettingsLive.LeagueId;
                ////    //    //data.MarketTemplateId = int.Parse(SettingsLive.MarketTemplateId);
                ////    //    //data.LivematchSettings.Settings = SettingsLive.Settings.Where(a => a.Key != null).Select(a => new KeyTimeClientConfigModel { Key = (int)a.Key, Time = a.Time, SortOrder = a.SortOrder }).ToList();

                ////    //    ////Remove key
                ////    //    //data.LivematchSettings.Settings.RemoveAll(a => a.Time == existingKeys.Time && a.Key == existingKeys.Key && a.SortOrder == existingKeys.SortOrder);
                ////    //    Setting.Key = null;
                ////    //    //await TradingService.AddOrUpdateConfig(data);
                ////    //}
                ////}
                //if (IsConnected) await HubConnection.SendAsync("onconfigchange", data);

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
