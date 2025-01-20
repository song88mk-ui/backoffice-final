using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.ComponentModel;

namespace Backoffice.Web.Components.SBC.TopFixtures
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }

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

            if (_userState.Model.ClientCodes != null && _userState.Model.ClientCodes.Any())
            {
                var clients = _userState.Model.SportsbookClients;
                
                Clients = clients.OrderBy(x => x.ClientName).Select(s => new SelectListItemWithCheckboxModel<string>
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

            _userState.PropertyChanged += UserStatePropertyChanged;
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

        #region Utilities
        private async void UserStatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_userState.Model.ClientCodes != null && _userState.Model.ClientCodes.Any())
            {
                var clients = _userState.Model.SportsbookClients;

                Clients = clients.OrderBy(x => x.ClientName).Select(s => new SelectListItemWithCheckboxModel<string>
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
        }
        #endregion
    }
}
