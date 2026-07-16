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

        private List<Client> _clients = new();
        private string _selectedClientCode;
        private ClientFeatureFlagsResponse _clientFeatureFlags;

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
            var response = await _backofficeService.GetClients();

            if (response.StatusCode == StatusCode.Success && response.Result?.Clients != null)
                _clients = response.Result.Clients.OrderBy(c => c.ClientName).ToList();
        }

        private async Task HandleClientSelected(ChangeEventArgs e)
        {
            _selectedClientCode = e.Value?.ToString();
            _clientFeatureFlags = null;
            _hasChange = false;

            if (string.IsNullOrEmpty(_selectedClientCode))
                return;

            _isPreloader = true;
            StateHasChanged();

            var response = await _backofficeService.GetClientFeatureFlags(_selectedClientCode);

            _clientFeatureFlags = response.StatusCode == StatusCode.Success && response.Result != null
                ? response.Result
                : new ClientFeatureFlagsResponse() { ClientCode = _selectedClientCode };

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
                ClientCode = _selectedClientCode,
                BettingRules = _clientFeatureFlags.Features.BettingRules,
                DarkMode = _clientFeatureFlags.Features.DarkMode,
                LanguageOptions = _clientFeatureFlags.Features.LanguageOptions,
                OGAuth = _clientFeatureFlags.Features.OGAuth,
                PartialCashout = _clientFeatureFlags.Features.PartialCashout
            });

            _isProcessing = false;

            if (response.StatusCode == StatusCode.Success)
            {
                _clientFeatureFlags = response.Result;
                _hasChange = false;
                await _alertToastService.SetToast("Feature Flags Saved");
            }
            else
                await _alertToastService.SetToast("Error Saving Feature Flags");
        }

        private async Task HandleCancelClick() => await HandleClientSelected(new ChangeEventArgs() { Value = _selectedClientCode });
    }
}
