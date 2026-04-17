using System.Text.Json;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class Dashboard : IDisposable
    {
        #region Properties
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public MetabaseService MetabaseService { get; set; }

        #endregion

        #region Fields

        private string _iFrameSrc;
        private bool _isPreLoader = true;
        
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

            await LoadEmbedUrl();
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

        private async Task LoadEmbedUrl()
        {
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

            _iFrameSrc = MetabaseService.GetEmbedUrl(_clientCodes);
            _isPreLoader = false;
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
