using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Monitoring
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [Parameter] public EventCallback<MonitoringCallbackModel> OnInitCallback { get; set; }
        [Parameter] public int SportId { get; set; }
        public SearchFormModel SearchForm { get; set; }
        public List<MainMarketTemplateResponse> TopMarketTemplates { get; set; }
        public ClientModel Clients { get; set; }
        public string MarketTemplateId { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                TopMarketTemplates = new List<MainMarketTemplateResponse>();
                Clients = new ClientModel();

                SearchForm = new SearchFormModel();

                TopMarketTemplates = (await BackOfficeService.GetTopMarkets(SportId)).Result;

                var clients = (await BackOfficeService.GetClients()).Result;
                if (clients != null)
                {

                    if (UserModel.ClientCodes.Any())
                    {
                        clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                    }

                    Clients = clients;
                }

                if (TopMarketTemplates != null && TopMarketTemplates.Any())
                    MarketTemplateId = TopMarketTemplates.FirstOrDefault().MainMarketTemplateId;

                await OnInitCallback.InvokeAsync(new MonitoringCallbackModel
                {
                    SearchText = string.Empty,
                    TopMarketTemplates = TopMarketTemplates,
                    SelectedMarketTemplateId = MarketTemplateId,
                    Clients = Clients
                });
            }
            catch (Exception)
            {
            }
        }

        public async void OnMarketChangedEvent(ChangeEventArgs e)
        {
            MarketTemplateId = e.Value.ToString();

            await OnInitCallback.InvokeAsync(new MonitoringCallbackModel
            {
                SelectedMarketTemplateId = MarketTemplateId,
                Clients = Clients,
                TopMarketTemplates = TopMarketTemplates,
                SearchText = SearchForm.SearchText
            });
        }

        public async Task OnInputSearchEvent()
        {
            await OnInitCallback.InvokeAsync(new MonitoringCallbackModel { SearchText = SearchForm.SearchText, TopMarketTemplates = TopMarketTemplates, Clients = Clients });
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
