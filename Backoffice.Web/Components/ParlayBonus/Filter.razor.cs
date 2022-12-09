using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.ParlayBonus
{
    public partial class Filter : IDisposable
    {
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Parameter] public ClientParlayBonusResponse ClientParlayBonusResponse { get; set; }
        [Parameter] public EventCallback<long> OnClientChangedCallback { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        public List<SelectListItemModel<ClientResponse>> Clients { get; set; }
        public string ClientId { get; set; }       
        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Clients = new List<SelectListItemModel<ClientResponse>>();

            ClientId = string.Empty;

            var clients = (await BackOfficeService.GetClients()).Result;
            if (clients != null)
            {

                if (UserModel.ClientCodes.Any())
                {
                    clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                }

                Clients = clients.Clients.Select(s => new SelectListItemModel<ClientResponse>
                {
                    Value = s,
                    Name = s.ClientName
                }).ToList();

            }

            if (!Clients.Any())
                Clients.Add(new SelectListItemModel<ClientResponse>
                {
                    Value = new ClientResponse(),
                    Name = "No Clients Available"
                });
            else
                Clients.Insert(0, new SelectListItemModel<ClientResponse> { Value = new ClientResponse(), Name = "Choose client..." });

            var selectedClients = Clients.Where(c => !string.IsNullOrEmpty(c.Value.ClientCode)).Select(s => s.Value).ToList();
        }

        public void Dispose()
        {
            
        }

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            await OnClientChangedCallback.InvokeAsync(long.Parse(e.Value.ToString()));
        }
        #endregion
    }
}
