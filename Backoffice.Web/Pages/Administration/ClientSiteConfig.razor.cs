using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Administration.ClientSiteConfig;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Pages.Administration
{
    public partial class ClientSiteConfig
    {
        private string _title = "Client Site Config";
        private PageHeaderModel _pageHeaderModel;

        private bool _isPreloader = true;
        private bool _isProcessing;
        private bool _hasChange;
        private bool _canEdit;

        // webprops-api's clientId is the client's Platform GUID (PlatformClientResponse.ClientId),
        // not the SBC ClientCode - confirmed by tracing Platform.Parent.Web's Custom:SB:ClientId
        // (a per-deployment GUID) through to CdnConfigurationProvider's GET /config/{clientId}.
        private List<PlatformClientResponse> _clients = new();
        private string _selectedClientGuid;
        private ClientFeatureFlagsResponse _clientFeatureFlags;
        private bool _isNewConfig;
        private string _newGameTrackerProductName = string.Empty;

        // Pre-edit snapshot of the loaded config, used as the activity log's From value.
        private string _loadedConfigJson;

        // Valid theme names per layout, from platform-web's wwwroot/scss/{layout}/theme
        // folders plus themes present in live webprops configs (EL2 "red" is the base
        // theme without its own SCSS folder). Keep in sync with platform-web.
        private static readonly Dictionary<string, List<string>> ValidLayoutThemes = new()
        {
            ["AL"] = new() { "red", "red-light", "gray", "blue", "orange-light", "orange-dark" },
            ["EL2"] = new() { "red", "white" }
        };

        // Adding a new feature flag = one entry here (plus the property on
        // ClientFeatureFlags/UpdateClientFeatureFlagsRequest and the save mapping below).
        private static readonly List<FeatureFlagDefinition> FeatureFlagDefinitions = new()
        {
            new FeatureFlagDefinition("ff-betting-rules", "Betting Rules", "Shows the Betting Rules button in the site header.", f => f.BettingRules, (f, v) => f.BettingRules = v),
            new FeatureFlagDefinition("ff-dark-mode", "Dark Mode", "Enables the dark mode theme toggle for the client's site.", f => f.DarkMode, (f, v) => f.DarkMode = v),
            new FeatureFlagDefinition("ff-language-options", "Language Options", "Enables the language selector, scoped to English and Chinese (ZH) only.", f => f.LanguageOptions, (f, v) => f.LanguageOptions = v),
            new FeatureFlagDefinition("ff-og-auth", "OG Auth", "Switches the site into Oriental Gaming (OG) mode: page title, bet ID formatting, and header/auth behavior for OG-integrated operators.", f => f.OGAuth, (f, v) => f.OGAuth = v),
            // Full-before-partial dependency (GM-790): Partial Cashout can only be enabled while
            // Full Cashout is on (it stays independently disableable), and turning Full Cashout
            // off also turns Partial Cashout off. WebPropsRepository enforces the same rule on save.
            new FeatureFlagDefinition("ff-full-cashout", "Full Cashout", "Enables the full cash-out button on eligible open bets.", f => f.FullCashout, (f, v) => { f.FullCashout = v; if (!v) f.PartialCashout = false; }),
            new FeatureFlagDefinition("ff-partial-cashout", "Partial Cashout", "Enables the partial cash-out slider on eligible open bets.", f => f.PartialCashout, (f, v) => f.PartialCashout = v, disabledWhen: f => !f.FullCashout, disabledReason: "Requires Full Cashout to be enabled first."),
            new FeatureFlagDefinition("ff-bet-recommendation", "Bet Recommendation", "Shows the bet recommendation widget on the client site's main odds table.", f => f.BetRecommendation, (f, v) => f.BetRecommendation = v),
            new FeatureFlagDefinition("ff-euro-layout", "Euro Layout", "Switches the client's site to the Euro (EL2) layout variant instead of the default Asian layout.", f => f.IsEuroLayout, (f, v) => f.IsEuroLayout = v)
        };

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;
            await PageState.AuthenticateAccessEvent.InvokeAsync();

            _pageHeaderModel = new()
            {
                Title = "Client Site Config",
                IconClass = "ico ico-feature-flags"
            };

            _canEdit = _userState.Model.Claims?.Any(x =>
                x.Type == AdminPermission.Admin_ClientSiteConfig.ToString().Replace("_", ".")
                && x.Value == $"{(int)Access.Edit}") ?? false;

            await LoadClients();

            _isPreloader = false;
        }

        private async Task LoadClients()
        {
            var response = await _backofficeService.GetPlatformClients();

            if (response.StatusCode == StatusCode.Success && response.Result?.Clients != null)
                _clients = response.Result.Clients.OrderBy(c => c.ClientName).ToList();
        }

        private async Task HandleClientSelected(ChangeEventArgs e)
        {
            _selectedClientGuid = e.Value?.ToString();
            _clientFeatureFlags = null;
            _isNewConfig = false;
            _hasChange = false;

            if (string.IsNullOrEmpty(_selectedClientGuid))
                return;

            _isPreloader = true;
            StateHasChanged();

            var response = await _backofficeService.GetClientFeatureFlags(_selectedClientGuid);

            if (response.StatusCode == StatusCode.Success && response.Result != null)
            {
                _clientFeatureFlags = response.Result;
                _loadedConfigJson = JsonSerializer.Serialize(_clientFeatureFlags);
            }
            else if (response.StatusCode == StatusCode.NotFound)
            {
                _clientFeatureFlags = new ClientFeatureFlagsResponse() { ClientId = _selectedClientGuid };
                _isNewConfig = true;
                _loadedConfigJson = JsonSerializer.Serialize(_clientFeatureFlags);
            }
            else
            {
                await _alertToastService.SetToast("Error loading feature flags for this client");
            }

            _isPreloader = false;
            StateHasChanged();
        }

        private void HandleFlagToggled(FeatureFlagDefinition flag, bool value)
        {
            flag.Set(_clientFeatureFlags.Features, value);
            _hasChange = true;
        }

        private void HandleViewLogsClick()
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg modal-dialog-centered");
            parameters.Add("DialogStyle", "max-width:1200px");
            parameters.Add("Clients", _clients);

            _modalService.Open<ClientSiteConfigLogsModal>("Client Site Config Logs", parameters, new ModalOptions());
        }

        private void HandleAddGameTrackerProductName()
        {
            var name = _newGameTrackerProductName?.Trim();
            if (string.IsNullOrEmpty(name) || _clientFeatureFlags.Features.GameTrackerProductNames.Contains(name))
                return;

            _clientFeatureFlags.Features.GameTrackerProductNames.Add(name);
            _newGameTrackerProductName = string.Empty;
            _hasChange = true;
        }

        private void HandleRemoveGameTrackerProductName(string name)
        {
            _clientFeatureFlags.Features.GameTrackerProductNames.Remove(name);
            _hasChange = true;
        }

        private bool IsThemeSelected(string layoutKey, string theme) =>
            _clientFeatureFlags.Layout.TryGetValue(layoutKey, out var layoutTheme)
            && layoutTheme.Themes.Contains(theme);

        // Layouts rendered in the Themes section: every layout stored in the client's config
        // plus the known platform-web layouts, so themes are configurable even before the
        // client has any layout data (new/empty configs preload AL and EL2 unchecked).
        private List<string> GetDisplayLayoutKeys()
        {
            var keys = _clientFeatureFlags.Layout.Keys.ToList();
            keys.AddRange(ValidLayoutThemes.Keys.Where(k => !keys.Contains(k, StringComparer.OrdinalIgnoreCase)));
            return keys;
        }

        private void HandleThemeToggled(string layoutKey, string theme, bool isChecked)
        {
            if (!_clientFeatureFlags.Layout.TryGetValue(layoutKey, out var layoutTheme))
            {
                layoutTheme = new ClientLayoutTheme();
                _clientFeatureFlags.Layout[layoutKey] = layoutTheme;
            }

            if (isChecked && !layoutTheme.Themes.Contains(theme))
                layoutTheme.Themes.Add(theme);
            else if (!isChecked)
                layoutTheme.Themes.Remove(theme);

            _hasChange = true;
        }

        private async Task HandleSaveClick()
        {
            if (_clientFeatureFlags == null)
                return;

            _isProcessing = true;

            await _alertToastService.SetLoadingToast("Saving Feature Flags");

            var response = await _backofficeService.UpdateClientFeatureFlags(new UpdateClientFeatureFlagsRequest()
            {
                ClientId = _selectedClientGuid,
                BettingRules = _clientFeatureFlags.Features.BettingRules,
                DarkMode = _clientFeatureFlags.Features.DarkMode,
                LanguageOptions = _clientFeatureFlags.Features.LanguageOptions,
                OGAuth = _clientFeatureFlags.Features.OGAuth,
                FullCashout = _clientFeatureFlags.Features.FullCashout,
                PartialCashout = _clientFeatureFlags.Features.PartialCashout,
                BetRecommendation = _clientFeatureFlags.Features.BetRecommendation,
                IsEuroLayout = _clientFeatureFlags.Features.IsEuroLayout,
                GameTrackerProductNames = _clientFeatureFlags.Features.GameTrackerProductNames,
                LayoutThemes = _clientFeatureFlags.Layout.ToDictionary(l => l.Key, l => l.Value.Themes)
            });

            _isProcessing = false;

            if (response.StatusCode == StatusCode.Success)
            {
                var wasNewConfig = _isNewConfig;
                _clientFeatureFlags = response.Result;
                _isNewConfig = false;
                _hasChange = false;

                var savedConfigJson = JsonSerializer.Serialize(response.Result);
                await _backofficeService.AddActivityLog(new ActivityLogModel()
                {
                    Page = AdminPermission.Admin_ClientSiteConfig.ToString().Replace("_", "."),
                    Activity = (wasNewConfig ? Activity.Create : Activity.Update).ToString(),
                    From = _loadedConfigJson,
                    To = savedConfigJson,
                    Payload = $"{_userState.Model.Name}.{_selectedClientGuid}",
                    UserId = _userState.Model.Id,
                    UserName = _userState.Model.Name
                });
                _loadedConfigJson = savedConfigJson;

                await _alertToastService.SetToast("Feature Flags Saved");
            }
            else
                await _alertToastService.SetToast("Error Saving Feature Flags");
        }

        private async Task HandleCancelClick() => await HandleClientSelected(new ChangeEventArgs() { Value = _selectedClientGuid });
    }

    public class FeatureFlagDefinition
    {
        public string Id { get; }
        public string Label { get; }
        public string Description { get; }
        public Func<ClientFeatureFlags, bool> Get { get; }
        public Action<ClientFeatureFlags, bool> Set { get; }
        // When this returns true for the current flags, the toggle renders disabled and
        // DisabledReason is shown under the description (dependency between flags).
        public Func<ClientFeatureFlags, bool> DisabledWhen { get; }
        public string DisabledReason { get; }

        public FeatureFlagDefinition(string id, string label, string description, Func<ClientFeatureFlags, bool> get, Action<ClientFeatureFlags, bool> set, Func<ClientFeatureFlags, bool> disabledWhen = null, string disabledReason = null)
        {
            Id = id;
            Label = label;
            Description = description;
            Get = get;
            Set = set;
            DisabledWhen = disabledWhen;
            DisabledReason = disabledReason;
        }
    }
}

