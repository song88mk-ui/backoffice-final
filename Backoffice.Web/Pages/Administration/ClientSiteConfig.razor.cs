using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

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

        // Valid theme names per layout, from platform-web's wwwroot/scss/{layout}/theme
        // folders plus themes present in live webprops configs (EL2 "red" is the base
        // theme without its own SCSS folder). Keep in sync with platform-web.
        private static readonly Dictionary<string, List<string>> ValidLayoutThemes = new()
        {
            ["AL"] = new() { "red", "red-light", "gray", "blue", "orange-light", "orange-dark" },
            ["EL2"] = new() { "red", "white" }
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
            }
            else if (response.StatusCode == StatusCode.NotFound)
            {
                _clientFeatureFlags = new ClientFeatureFlagsResponse() { ClientCode = _selectedClientGuid };
                _isNewConfig = true;
            }
            else
            {
                await _alertToastService.SetToast("Error loading feature flags for this client");
            }

            _isPreloader = false;
            StateHasChanged();
        }

        private void HandleFlagChanged() => _hasChange = true;

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

        private void HandleThemeToggled(string layoutKey, string theme, bool isChecked)
        {
            if (!_clientFeatureFlags.Layout.TryGetValue(layoutKey, out var layoutTheme))
                return;

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
                ClientCode = _selectedClientGuid,
                BettingRules = _clientFeatureFlags.Features.BettingRules,
                DarkMode = _clientFeatureFlags.Features.DarkMode,
                LanguageOptions = _clientFeatureFlags.Features.LanguageOptions,
                OGAuth = _clientFeatureFlags.Features.OGAuth,
                PartialCashout = _clientFeatureFlags.Features.PartialCashout,
                BetRecommendation = _clientFeatureFlags.Features.BetRecommendation,
                IsEuroLayout = _clientFeatureFlags.Features.IsEuroLayout,
                GameTrackerProductNames = _clientFeatureFlags.Features.GameTrackerProductNames,
                LayoutThemes = _clientFeatureFlags.Layout.ToDictionary(l => l.Key, l => l.Value.Themes)
            });

            _isProcessing = false;

            if (response.StatusCode == StatusCode.Success)
            {
                _clientFeatureFlags = response.Result;
                _isNewConfig = false;
                _hasChange = false;
                await _alertToastService.SetToast("Feature Flags Saved");
            }
            else
                await _alertToastService.SetToast("Error Saving Feature Flags");
        }

        private async Task HandleCancelClick() => await HandleClientSelected(new ChangeEventArgs() { Value = _selectedClientGuid });
    }
}
