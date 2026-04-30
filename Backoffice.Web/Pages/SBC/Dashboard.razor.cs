using System.Text.Json;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.SBC
{
    public partial class Dashboard : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public MetabaseService MetabaseService { get; set; }
        [Inject] public IJSRuntime JS { get; set; }

        #endregion

        #region Fields

        private bool _isPreLoader = true;
        private bool _hasAccess = false;
        private bool _metabaseInitialized = false;
        private string _ssoToken;
        private string _parametersJson;
        private List<string> _clientCodes { get; set; } = new List<string>();

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            PageHeaderModel = new PageHeaderModel()
            {
                Title = "Dashboard",
                IconClass = "ico ico-dashboard"
            };

            await PageState.AuthenticateAccessEvent.InvokeAsync();
            await LoadEmbedData();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!string.IsNullOrEmpty(_ssoToken) && !_metabaseInitialized)
            {
                _metabaseInitialized = true;
                await JS.InvokeVoidAsync("initMetabaseDashboard",
                    MetabaseService.SiteUrl,
                    _ssoToken
                );
            }
        }

        private async Task<List<string>> GetClientCodeList()
        {
            var request = new GetClientListRequest
            {
                Search = "",
                Page = 1,
                PageSize = 25,
                SortBy = (ClientSorting)0
            };
            var response = await BackofficeService.GetClientList(request);
            return response.Result.Clients.Select(c => c.ClientCode).ToList();
        }

        private async Task LoadEmbedData()
        {
            await _userState.SportsbookClientsReadyAsync;

            var adminClientCodes = await GetClientCodeList();
            var sportsbookClientCodes = _userState.Model.SportsbookClients.Select(s => s.ClientCode).ToList();

            if ((_userState.Model.RoleName.ToLower() == "super admin" || _userState.Model.RoleName.ToLower() == "admin") && adminClientCodes != null && adminClientCodes.Any())
            {
                _clientCodes = adminClientCodes;
            }
            else if (sportsbookClientCodes != null && sportsbookClientCodes.Any())
            {
                _clientCodes = sportsbookClientCodes;
            }
            else
            {
                _isPreLoader = false;
                return;
            }

            _ssoToken = MetabaseService.GetSsoToken();
            _parametersJson = JsonSerializer.Serialize(new { client = _clientCodes });
            _hasAccess = true;
            _isPreLoader = false;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
