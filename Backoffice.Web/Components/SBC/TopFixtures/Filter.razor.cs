using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SBC.TopFixtures
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter(Name = "UserClientCodes")] public List<string>? UserClientCodes { get; set; }

        [Parameter] public EventCallback<TopFixtureCallbackModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback<TopFixtureCallbackModel> OnFilterCallback { get; set; }

        public TopFixtureCallbackModel CallbackModel { get; set; }
        public SearchFormModel SearchForm { get; set; }
        public List<SelectListItemWithCheckboxModel<string>> Clients { get; set; }

        public int SelectedClientCount { get; set; }
        public string SelectedClientName { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Clients = new List<SelectListItemWithCheckboxModel<string>>();

            SearchForm = new SearchFormModel()
            {
                SearchText = string.Empty
            };

            var clients = (await BackOfficeService.GetClients()).Result;

            if (clients != null && clients.Clients.Any())
            {
                    clients.Clients = UserClientCodes != null && UserClientCodes.Any() ? clients.Clients.Where(c => UserClientCodes.Contains(c.ClientCode.ToUpper())).ToList() : new List<Client>();

                Clients = clients.Clients.OrderBy(x => x.ClientName).Select(s => new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = s.ClientId.ToString(),
                    Name = s.ClientName
                }).ToList();

                Clients.Insert(0, new SelectListItemWithCheckboxModel<string>
                {
                    IsChecked = true,
                    Value = "all",
                    Name = "All Clients"
                });

                var selectedClients = Clients.Where(c => c.Value != "all" && c.IsChecked).Select(s => s.Value).ToList();

                SelectedClientCount = selectedClients.Count;
                SelectedClientName = SelectedClientCount > 0 ? string.Empty : Clients.FirstOrDefault(c => c.Value != "all" && c.IsChecked).Name;
            }

            CallbackModel = new TopFixtureCallbackModel
            {
                SearchText = string.Empty,
                Clients = Clients.Where(c => c.Value != "all" && c.IsChecked)
                    .Select(s => new TopFixtureCallbackModel.Client
                    {
                        ClientId = long.Parse(s.Value),
                        ClientName = s.Name
                    }).ToList()
            };

            await OnInitCallback.InvokeAsync(CallbackModel);
        }

        public async Task OnClientCheckboxClickEvent(Dictionary<string, string> items)
        {
            SelectedClientName = string.Empty;
            SelectedClientCount = items.Count;

            if (SelectedClientCount == 1)
            {
                SelectedClientName = items.FirstOrDefault().Value;
            }

            CallbackModel.Clients = items.Select(s => new TopFixtureCallbackModel.Client
            {
                ClientId = long.Parse(s.Key),
                ClientName = s.Value
            }).ToList();

            await OnFilterCallback.InvokeAsync(CallbackModel);
        }

        public async Task OnInputSearchEvent()
        {
            CallbackModel.SearchText = SearchForm.SearchText;

            await OnFilterCallback.InvokeAsync(CallbackModel);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
