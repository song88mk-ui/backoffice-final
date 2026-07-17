using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.Administration
{
    public partial class FeatureFlags
    {
        private string _title = "Feature Flags";
        private PageHeaderModel _pageHeaderModel;

        private bool _isPreloader = true;
        private bool _isProcessing;
        private bool _hasChange;

        // webprops-api's clientId is the client's Platform GUID (PlatformClientResponse.ClientId),
        // not the SBC ClientCode - confirmed by tracing Platform.Parent.Web's Custom:SB:ClientId
        // (a per-deployment GUID) through to CdnConfigurationProvider's GET /config/{clientId}.
        private List<PlatformClientResponse> _clients = new();
        private string _selectedClientGuid;
        private ClientFeatureFlagsResponse _clientFeatureFlags;
        private bool _isNewConfig;

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;
            await PageState.AuthenticateAccessEvent.InvokeAsync();

            _pageHeaderModel = new()
            {
                Title = "Feature Flags",
                IconClass = "ico ico-feature-flags"
            };

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
                BetRecommendation = _clientFeatureFlags.Features.BetRecommendation
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
