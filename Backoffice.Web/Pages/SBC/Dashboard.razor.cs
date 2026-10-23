using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Utilities;
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
        [Inject] public IMemCache MemCache { get; set; }
        [Inject] public IJSRuntime JS { get; set; }

        #endregion

        #region Fields

        private bool _isPreLoader = true;
        private bool _hasAccess = false;
        private bool _metabaseInitialized = false;
        private string _currentLocale;
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
            var locale = MetabaseService.ToLocale(_translationState.ActiveLanguageCode);

            if (!_metabaseInitialized)
            {
                _metabaseInitialized = true;
                _currentLocale = locale;
                await JS.InvokeVoidAsync("initMetabaseDashboard",
                    "/sso/metabase",
                    MetabaseService.SiteUrl,
                    locale
                );
            }
            else if (locale != _currentLocale)
            {
                _currentLocale = locale;
                await JS.InvokeVoidAsync("updateMetabaseLocale", locale);
            }
        }

        private async Task<List<string>> GetClientCodeList()
        {
            var allClientCodes = new List<string>();
            var page = 1;
            const int pageSize = 25;

            while (true)
            {
                var request = new GetClientListRequest
                {
                    Search = "",
                    Page = page,
                    PageSize = pageSize,
                    SortBy = (ClientSorting)0
                };

                var response = await BackofficeService.GetClientList(request);

                if (response.StatusCode != StatusCode.Success ||
                    response.Result?.Clients == null ||
                    !response.Result.Clients.Any())
                    break;

                allClientCodes.AddRange(
                    response.Result.Clients.Select(c => c.ClientCode)
                );
                if (response.Result.Clients.Count < pageSize)
                    break;

                page++;
            }

            return allClientCodes;
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

            MemCache.AddOrUpdateCache($"metabase-clients:{_userState.Model.Id}", _clientCodes, 12);
            _hasAccess = true;
            _isPreLoader = false;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
