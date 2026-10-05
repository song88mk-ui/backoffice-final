using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using Serilog;
using System.ComponentModel;

namespace Backoffice.Web.Components.OddsEngine.MarketGroups
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Parameter] public EventCallback OnAddCallback { get; set; }
        [Parameter] public EventCallback OnUpdateCallback { get; set; }
        [Parameter] public EventCallback OnSaveUpdateCallback { get; set; }
        [Parameter] public EventCallback OnCopyCallback { get; set; }
        [Parameter] public EventCallback OnViewLogsCallback { get; set; }
        [Parameter] public MarketGroupModel MarketGroupModel { get; set; }
        [Parameter] public EventCallback OnInitCallback { get; set; }
        [Parameter] public EventCallback OnCancelCallback { get; set; }
        [Parameter] public EventCallback<MarketGroupModel> MarketGroupModelChanged { get; set; }
        [Parameter] public string Region { get; set; } = "HK";

        public List<SelectListItemModel<string>> Clients { get; set; }
        public string SelectedClientCode { get; set; }
        public List<SelectListItemModel<int>> Sports { get; set; }
        public int SelectedSportId { get; set; }
        public List<SelectListItemModel<int>> FixtureStatuses { get; set; }
        public int SelectedFixtureStatusId { get; set; }
        private string _clientReference;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Clients = new List<SelectListItemModel<string>>();
            Sports = new List<SelectListItemModel<int>>();
            FixtureStatuses = new List<SelectListItemModel<int>>();
            MarketGroupModel = new MarketGroupModel();

            try
            {
                MarketGroupModel.IsUpdate = false;
                _tradingService.Region = Region.ToUpper();

                if (_userState.Model.TradingClients != null && _userState.Model.TradingClients.Any())
                {
                    Clients = _userState.Model.TradingClients.Select(s => new SelectListItemModel<string>
                    {
                        Value = s.ClientCode,
                        Name = s.ClientName
                    }).ToList();

                }
                else
                {
                    Clients.Add(new SelectListItemModel<string>
                    {
                        Value = "",
                        Name = _translationState.GetValue("No Clients Available")
                    });
                }

                _clientReference = string.Join(',', Clients.Where(c => c.Value != "").Select(c => c.Value.Trim()).OrderBy(c => c).ToList());

                _userState.PropertyChanged += UserStatePropertyChanged;

                SelectedClientCode = Clients.FirstOrDefault().Value;

                var sports = (await _backofficeService.GetSportLookup(_translationState.ActiveLanguageCode.ToString())).Result;

                if (sports != null)
                {
                    Sports = sports.SportLookup.Select(s => new SelectListItemModel<int>
                    {
                        Value = s.SportId,
                        Name = s.SportName
                    }).ToList();
                }
                else
                {
                    Sports.Add(new SelectListItemModel<int>
                    {
                        Value = 0,
                        Name = "No Sports Available"
                    });
                }

                SelectedSportId = Sports.Any() ? Sports.FirstOrDefault().Value : 0;

                FixtureStatuses.Add(new SelectListItemModel<int>
                {
                    Value = (int)FixtureStatus.Prematch,
                    Name = FixtureStatus.Prematch.ToString()
                });

                FixtureStatuses.Add(new SelectListItemModel<int>
                {
                    Value = (int)FixtureStatus.Live,
                    Name = FixtureStatus.Live.ToString()
                });

                SelectedFixtureStatusId = FixtureStatuses.FirstOrDefault().Value;

                await PrepareMarketGroupModel();
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                if (ex.StackTrace != null)
                    Log.Error(ex.StackTrace.ToString());
            }
        }

        public async Task OnUpdateClickEvent()
        {
            MarketGroupModel.IsUpdate = true;

            await OnUpdateCallback.InvokeAsync();
        }

        public async Task OnSaveUpdateClickEvent()
        {
            MarketGroupModel.IsUpdate = true;

            await OnSaveUpdateCallback.InvokeAsync();
        }

        public async Task OnCancelClickEvent()
        {
            MarketGroupModel.IsUpdate = false;

            await OnCancelCallback.InvokeAsync();
        }

        public async Task OnAddMarketGroupClickEvent()
        {
            await OnAddCallback.InvokeAsync();
        }

        public async Task OnCopyMarketGroupClickEvent()
        {
            await OnCopyCallback.InvokeAsync();
        }

        public async Task OnViewLogsClickEvent()
        {
            await OnViewLogsCallback.InvokeAsync();
        }

        public async void OnClientChangedEvent(ChangeEventArgs e)
        {
            SelectedClientCode = e.Value.ToString();

            await PrepareMarketGroupModel();
        }
        public async void OnSportChangedEvent(ChangeEventArgs e)
        {
            SelectedSportId = int.Parse(e.Value.ToString());

            await PrepareMarketGroupModel();
        }

        public async void OnFixtureStatusChangedEvent(ChangeEventArgs e)
        {
            SelectedFixtureStatusId = int.Parse(e.Value.ToString());

            await PrepareMarketGroupModel();
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities

        private async void UserStatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var tempClientReference = string.Join(',', _userState.Model.TradingClients.Select(c => c.ClientCode.Trim()).OrderBy(c => c).ToList());
            if (_clientReference == tempClientReference)
                return;

            Clients = new List<SelectListItemModel<string>>();

            if (_userState.Model.TradingClients != null && _userState.Model.TradingClients.Any())
            {
                Clients = _userState.Model.TradingClients.Select(s => new SelectListItemModel<string>
                {
                    Value = s.ClientCode,
                    Name = s.ClientName
                }).ToList();
            }
            else
            {
                Clients.Add(new SelectListItemModel<string>
                {
                    Value = "",
                    Name = _translationState.GetValue("No Clients Available")
                });
            }

            _clientReference = string.Join(',', Clients.Where(c => c.Value != "").Select(c => c.Value.Trim()).OrderBy(c => c).ToList());
            SelectedClientCode = Clients.FirstOrDefault().Value;
            await PrepareMarketGroupModel();
        }
        private async Task PrepareMarketGroupModel()
        {
            if (!string.IsNullOrEmpty(SelectedClientCode) && SelectedSportId != 0)
            {
                var clientId = _userState.Model.TradingClients.FirstOrDefault(c => c.ClientCode == SelectedClientCode).ClientId;

                var marketTemplateResponse = (await _backofficeService.GetClientMarketTemplateGroups(clientId, SelectedSportId, SelectedFixtureStatusId)).Result;

                if (marketTemplateResponse != null)
                {
                    MarketGroupModel.ClientCode = SelectedClientCode;
                    MarketGroupModel.SportId = marketTemplateResponse.SportId;
                    MarketGroupModel.ClientId = marketTemplateResponse.ClientId;
                    MarketGroupModel.FixtureStatusId = marketTemplateResponse.FixtureStatusId;
                    await MarketGroupModelChanged.InvokeAsync(MarketGroupModel);
                }
            }

            await MarketGroupModelChanged.InvokeAsync(MarketGroupModel);
            await OnInitCallback.InvokeAsync();
        }

        #endregion
    }
}
