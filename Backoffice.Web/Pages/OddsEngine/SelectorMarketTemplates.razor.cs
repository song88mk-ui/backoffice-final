using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class SelectorMarketTemplates
    {
        #region Fields

        private bool _isPreLoader;
        private bool _isProcessing;
        private bool _canEdit;
        private bool _showCopyModal;
        private string _selectedClientCode;
        private string _copyFromClientCode;
        private PageHeaderModel _pageHeaderModel;
        private List<BreadcrumbModel> _breadcrumbs;
        private AlertModel _alertModel;
        private List<PlatformClientResponseModel> _clients;
        private List<SportModel> _sports;
        private List<SelectorMarketTemplateLookupModel> _templateLookup;
        private List<ClientSelectorMarketTemplateModel> _templates;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _pageHeaderModel = new()
            {
                Title = "Selector Market Templates",
                ShowLanguageDropDown = false
            };

            _alertModel = new();
            _breadcrumbs = new List<BreadcrumbModel>
            {
                new BreadcrumbModel
                {
                    Name = "Market Settings",
                    Url = string.Empty,
                    IsCurrentPage = false
                },
                new BreadcrumbModel
                {
                    Name = "Selector Market Templates",
                    Url = string.Empty,
                    IsCurrentPage = true
                }
            };

            _clients = new List<PlatformClientResponseModel>();
            _sports = new List<SportModel>();
            _templateLookup = new List<SelectorMarketTemplateLookupModel>();
            _templates = null;
            _isPreLoader = false;
            _isProcessing = false;
            _showCopyModal = false;

            _canEdit = _userState.Model.Claims?.Any(x =>
                x.Type == $"{SportsbookClientPermission.SBC_OE_MarketSetting_SelectorTemplates.ToString().Replace("_", ".")}"
                && x.Value == $"{(int)Access.Edit}") ?? false;

            if (_userState.Model.TradingClients != null && _userState.Model.TradingClients.Any())
            {
                _clients = _userState.Model.TradingClients.Select(c => new PlatformClientResponseModel
                {
                    ClientId = c.ClientId,
                    ClientCode = c.ClientCode,
                    ClientName = c.ClientName
                }).OrderBy(c => c.ClientName).ToList();
            }

            var sportsResponse = await _backofficeService.GetSportLookup(_translationState.ActiveLanguageCode.ToString());
            if (sportsResponse?.Result?.SportLookup != null)
            {
                _sports = sportsResponse.Result.SportLookup.Select(s => new SportModel
                {
                    SportId = s.SportId,
                    SportName = s.SportName
                }).ToList();
            }

            var lookupResponse = await _backofficeService.GetSelectorMarketTemplateLookup();
            if (lookupResponse?.StatusCode == StatusCode.Success && lookupResponse.Result != null)
            {
                _templateLookup = lookupResponse.Result;
            }

            await PageState.AuthenticateAccessEvent.InvokeAsync();
        }

        public async Task OnClientChangedEvent(ChangeEventArgs e)
        {
            _selectedClientCode = e.Value?.ToString();
            _alertModel = new();
            _templates = null;

            if (string.IsNullOrEmpty(_selectedClientCode))
                return;

            await LoadTemplates();
        }

        public void OnAddRowClickEvent()
        {
            var nextSortOrder = _templates.Any() ? _templates.Max(t => t.AsiaSortOrder) + 1 : 1;

            _templates.Add(new ClientSelectorMarketTemplateModel
            {
                SportId = 0,
                MarketTemplateId = string.Empty,
                MarketTemplateCode = string.Empty,
                AsiaSortOrder = nextSortOrder,
                EuroSortOrder = nextSortOrder,
                Identifier = "FT"
            });
        }

        public void OnDeleteRowClickEvent(ClientSelectorMarketTemplateModel row)
        {
            _templates.Remove(row);
        }

        public void OnRowSportChangedEvent(ClientSelectorMarketTemplateModel row, ChangeEventArgs e)
        {
            row.SportId = Convert.ToInt32(e.Value);
            row.MarketTemplateId = string.Empty;
            row.MarketTemplateCode = string.Empty;
        }

        public void OnRowTemplateChangedEvent(ClientSelectorMarketTemplateModel row, ChangeEventArgs e)
        {
            row.MarketTemplateId = e.Value?.ToString() ?? string.Empty;
            row.MarketTemplateCode = _templateLookup.FirstOrDefault(t => t.SportId == row.SportId && t.MarketTemplateId == row.MarketTemplateId)?.MarketTemplateCode ?? string.Empty;
        }

        public async Task OnSaveClickEvent()
        {
            _alertModel = new();

            if (_templates.Any(t => t.SportId == 0 || string.IsNullOrEmpty(t.MarketTemplateId)))
            {
                _alertModel = new()
                {
                    Status = AlertStatus.Error,
                    Message = _translationState.GetValue("Every row needs a sport and a market template before saving.")
                };
                return;
            }

            var duplicates = _templates.GroupBy(t => new { t.SportId, t.MarketTemplateId }).Where(g => g.Count() > 1).ToList();
            if (duplicates.Any())
            {
                _alertModel = new()
                {
                    Status = AlertStatus.Error,
                    Message = _translationState.GetValue("The same market template is listed more than once for one sport. Remove the duplicates before saving.")
                };
                return;
            }

            var client = _clients.FirstOrDefault(c => c.ClientCode == _selectedClientCode);
            if (client == null)
                return;

            _isProcessing = true;

            var request = new ClientSelectorMarketTemplateRequest
            {
                ClientId = client.ClientId,
                ClientCode = client.ClientCode,
                UserId = _userState.Model.Id,
                Templates = _templates.Select(t => new ClientSelectorMarketTemplateRequest.ClientSelectorMarketTemplateModel
                {
                    SportId = t.SportId,
                    MarketTemplateId = t.MarketTemplateId,
                    MarketTemplateCode = t.MarketTemplateCode,
                    AsiaSortOrder = t.AsiaSortOrder,
                    EuroSortOrder = t.EuroSortOrder,
                    Identifier = t.Identifier
                }).ToList()
            };

            var response = await _backofficeService.AddOrUpdateClientSelectorMarketTemplates(request);

            _isProcessing = false;

            if (response.StatusCode == StatusCode.Success)
            {
                _alertModel = new()
                {
                    Status = AlertStatus.Success,
                    Message = _translationState.GetValue("Selector market templates saved successfully.")
                };

                await LoadTemplates();
            }
            else
            {
                _alertModel = new()
                {
                    Status = AlertStatus.Error,
                    Message = response.Message
                };
            }
        }

        public void OnOpenCopyModalClickEvent()
        {
            _copyFromClientCode = string.Empty;
            _showCopyModal = true;
        }

        public void OnCloseCopyModalClickEvent()
        {
            _showCopyModal = false;
        }

        public void OnCopyFromClientChangedEvent(ChangeEventArgs e)
        {
            _copyFromClientCode = e.Value?.ToString();
        }

        public async Task OnCopyConfirmClickEvent()
        {
            var fromClient = _clients.FirstOrDefault(c => c.ClientCode == _copyFromClientCode);
            var toClient = _clients.FirstOrDefault(c => c.ClientCode == _selectedClientCode);

            if (fromClient == null || toClient == null)
                return;

            _isProcessing = true;

            var request = new CopyClientSelectorMarketTemplateRequest
            {
                CopyFromClientId = fromClient.ClientId,
                CopyToClientId = toClient.ClientId,
                CopyToClientCode = toClient.ClientCode,
                UserId = _userState.Model.Id
            };

            var response = await _backofficeService.CopyClientSelectorMarketTemplates(request);

            _isProcessing = false;
            _showCopyModal = false;

            if (response.StatusCode == StatusCode.Success)
            {
                _alertModel = new()
                {
                    Status = AlertStatus.Success,
                    Message = _translationState.GetValue("Selector market templates copied successfully.")
                };

                await LoadTemplates();
            }
            else
            {
                _alertModel = new()
                {
                    Status = AlertStatus.Error,
                    Message = response.Message
                };
            }
        }

        #endregion

        #region Utilities

        private async Task LoadTemplates()
        {
            var client = _clients.FirstOrDefault(c => c.ClientCode == _selectedClientCode);
            if (client == null)
                return;

            _isPreLoader = true;

            var response = await _backofficeService.GetClientSelectorMarketTemplates(client.ClientId);

            if (response.StatusCode == StatusCode.Success)
            {
                _templates = (response.Result ?? new List<ClientSelectorMarketTemplateModel>())
                    .OrderBy(t => t.SportId).ThenBy(t => t.AsiaSortOrder).ToList();
            }
            else
            {
                _templates = null;
                _alertModel = new()
                {
                    Status = AlertStatus.Error,
                    Message = response.Message
                };
            }

            _isPreLoader = false;
        }

        #endregion

        #region Nested Class

        public class PlatformClientResponseModel
        {
            public Guid ClientId { get; set; }
            public string ClientCode { get; set; }
            public string ClientName { get; set; }
        }

        #endregion
    }
}
