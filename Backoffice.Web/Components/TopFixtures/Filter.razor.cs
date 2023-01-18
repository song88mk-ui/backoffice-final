using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopFixtures
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public EventCallback<TopFixtureCallbackModel> OnInitCallback { get; set; }
        [Parameter] public EventCallback<string> OnSearchCallback { get; set; }

        public TopFixtureCallbackModel CallbackModel { get; set; }
        public SearchFormModel SearchForm { get; set; }
        public List<SelectListItemModel<string>> Clients { get; set; }
        public string SelectedClientId { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            CallbackModel = new TopFixtureCallbackModel();
            Clients = new List<SelectListItemModel<string>>();

            SearchForm = new SearchFormModel()
            {
                SearchText = string.Empty
            };

            var clients = (await BackOfficeService.GetClients()).Result;

            if (clients != null && clients.Clients.Any())
            {
                if (UserModel.ClientCodes.Any())
                    clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                Clients = clients.Clients.Select(s => new SelectListItemModel<string>
                {
                    Value = s.ClientId.ToString(),
                    Name = s.ClientName
                }).ToList();

                Clients.Insert(0, new SelectListItemModel<string>
                {
                    Value = "all",
                    Name = "All Clients"
                });
            }
            else
            {
                Clients.Add(new SelectListItemModel<string>
                {
                    Value = "",
                    Name = "No Clients Available"
                });
            }

            SelectedClientId = Clients.FirstOrDefault().Value;

            CallbackModel.SearchText = string.Empty;

            if (SelectedClientId == "all")
            {
                CallbackModel.Clients = Clients.Where(c => c.Value != "all")
                    .Select(s => new TopFixtureCallbackModel.Client
                    {
                        ClientId = long.Parse(s.Value),
                        ClientName = s.Name
                    }).ToList();
            }

            await OnInitCallback.InvokeAsync(CallbackModel);
        }

        public async void OnClientChangedEvent(ChangeEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Value.ToString()))
                return;

            SelectedClientId = e.Value.ToString();

            if (SelectedClientId == "all")
            {
                CallbackModel.Clients = Clients.Where(c => c.Value != "all")
                    .Select(s => new TopFixtureCallbackModel.Client
                    {
                        ClientId = long.Parse(s.Value),
                        ClientName = s.Name
                    }).ToList();
            }
            else
            {
                CallbackModel.Clients = Clients.Where(c => c.Value != SelectedClientId)
                    .Select(s => new TopFixtureCallbackModel.Client
                    {
                        ClientId = long.Parse(s.Value),
                        ClientName = s.Name
                    }).ToList();
            }

            await OnInitCallback.InvokeAsync(CallbackModel);
        }

        public async Task OnInputSearchEvent()
        {
            await OnSearchCallback.InvokeAsync(SearchForm.SearchText);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
