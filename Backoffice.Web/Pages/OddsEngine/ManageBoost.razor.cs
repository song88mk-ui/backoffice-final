using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.OddsEngine.ManageBoost;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class ManageBoost : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }

        [Parameter] public string Region { get; set; } = "hk";

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        #endregion

        #region Fields
        private string _region;
        private PageHeaderModel _pageHeaderModel;
        public LanguagesModel _languagesModel;
        private AlertModel _alertModel;
        private string _search;
        private string _clientCode;
        private int _sportId;
        private int _segmentId;
        private int _leagueId;
        private List<PriceBoostConfigModel> _priceBoostList;
        private List<BreadcrumbModel> _breadcrumbs;
        private Filter _filterReference;
        private bool _isPreLoader;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageState.Page = this;

            _isPreLoader = true;
            _alertModel = new();
            _pageHeaderModel = new()
            {
                Title = "Manage Boost",
                ShowLanguageDropDown = false
            };
            _breadcrumbs = new();
            await PageState.AuthenticateAccessEvent.InvokeAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (_region != Region.ToUpper())
            {
                _region = Region.ToUpper();
                TradingService.Region = _region;
                _breadcrumbs = new()
                {
                    new()
                    {
                        Name = "Price Boost",
                        Url = string.Empty,
                        IsCurrentPage = false
                    },
                    new()
                    {
                        Name = "Manage Boost",
                        Url = $"{_region}/odds-engine/manage-boost",
                        IsCurrentPage = true
                    }
                };
            }
        }
        public void OnEditClickEvent(PriceBoostConfigModel priceboost)
        {
            _languagesModel = new LanguagesModel();

            _languagesModel.Languages = _languagesModel.Languages.Where(c => !priceboost.BoostedSelectionTranslations
            .Select(s => s.Language.LanguageCode).ToList().Contains(c.LanguageCode)).ToList();

            var parameters = new ModalParameters();

            var boostedSelectionTranslations = new List<BoostedSelectionTranslation>();
            foreach (var translation in priceboost.BoostedSelectionTranslations)
            {
                boostedSelectionTranslations.Add(translation);
            }

            var newPriceBoost = new PriceBoostConfigModel
            {
                BoostedOdds = priceboost.BoostedOdds,
                BoostedOddsStr = priceboost.BoostedOddsStr,
                BoostedSelectionName = priceboost.SelectionName,
                BoostedSelectionTranslations = boostedSelectionTranslations,
                BROdds = priceboost.BROdds,
                ClientCode = priceboost.ClientCode,
                ClientId = priceboost.ClientId,
                ClientName = priceboost.ClientName,
                ClientOdds = priceboost.ClientOdds,
                CurrentMinute = priceboost.CurrentMinute,
                CurrentPeriod = priceboost.CurrentPeriod,
                FixtureName = priceboost.FixtureName,
                FixtureStartDate = priceboost.FixtureStartDate,
                FixtureStatusId = priceboost.FixtureStatusId,
                FixtureTypeId = priceboost.FixtureTypeId,
                IconFixtureClass = priceboost.IconFixtureClass,
                LeagueId = priceboost.LeagueId,
                LeagueName = priceboost.LeagueName,
                MarketName = priceboost.MarketName,
                MarketSpecifier = priceboost.MarketSpecifier,
                MarketTemplateId = priceboost.MarketTemplateId,
                ProviderFixtureId = priceboost.ProviderFixtureId,
                Score = priceboost.Score,
                SegmentId = priceboost.SegmentId,
                SegmentName = priceboost.SegmentName,
                SelectionName = priceboost.SelectionName,
                SelectionSpecifier = priceboost.SelectionSpecifier,
                SelectionTemplateId = priceboost.SelectionTemplateId,
                SportId = priceboost.SportId,
                SportName = priceboost.SportName,
                TimeStamp = priceboost.TimeStamp,
                UserId = priceboost.UserId,
                UserName = priceboost.UserName,
            };


            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 90rem;top:50%;transform:translateY(-50%)");
            parameters.Add("PriceBoostConfigModel", newPriceBoost);
            parameters.Add("PriceBoostConfigModelSerialized", JsonSerializer.Serialize(priceboost));
            parameters.Add("Languages", _languagesModel);
            parameters.Add("Region", _region);

            var options = new ModalOptions();

            ModalService.Open<EditBoostedOddsModal>($"Edit Boosted Odds", parameters, options);
        }

        public void OnViewLogClickEvent(PriceBoostConfigModel priceboost)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("PriceBoostConfigModel", priceboost);
            parameters.Add("Region", _region);

            var options = new ModalOptions();

            ModalService.Open<ViewLogsBoostedModal>($"View Logs", parameters, options);
        }

        public void OnDeleteClickEvent(PriceBoostConfigModel priceboost)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem;top:50%;transform:translateY(-50%)");
            parameters.Add("PriceBoostConfigModel", priceboost);
            parameters.Add("PriceBoostConfigModelSerialized", JsonSerializer.Serialize(priceboost));
            parameters.Add("Region", _region);

            var options = new ModalOptions();

            ModalService.Open<DeleteBoostedOddsModal>($"Delete Boosted Odds", parameters, options);
        }

        public async Task OnInitCallback()
        {
            await GetContents();
        }

        public async Task OnClientChangedEvent(string clientCode)
        {
            _clientCode = clientCode;

            await GetContents();
        }

        public async Task OnSportChangedEvent(int sportId)
        {
            _sportId = sportId;
            _segmentId = 0;
            _leagueId = 0;

            await GetContents();
        }

        public async Task OnSegmentChangedEvent(int segmentId)
        {
            _segmentId = segmentId;
            _leagueId = 0;

            await GetContents();
        }

        public async Task OnLeagueChangedEvent(int leagueId)
        {
            _leagueId = leagueId;

            await GetContents();
        }

        public async Task OnInputSearchEvent(string search)
        {
            _search = search;

            await GetContents();
        }

        public async Task OnSaveCallback(object message)
        {
            _clientCode = string.Empty;
            _sportId = 0;
            _segmentId = 0;
            _leagueId = 0;
            _search = string.Empty;

            _filterReference.SelectedClientCode = _clientCode;
            _filterReference.SelectedSportId = _sportId;
            _filterReference.SelectedSegmentId = _segmentId;
            _filterReference.SelectedLeagueId = _leagueId;
            _filterReference.SearchForm.SearchText = _search;

            _alertModel = new()
            {
                Message = $"Price Boost has been {(string)message}!",
                Status = AlertStatus.Success,
            };

            await GetContents();
        }

        public void OnCancelCallback(object addPriceBoostFilterModel)
        {
            var parameters = new ModalParameters();

            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("AddPriceBoostFilterModel", (AddPriceBoostFilterModel)addPriceBoostFilterModel);
            parameters.Add("Region", _region);

            var options = new ModalOptions();

            ModalService.Open<AddBoostedOddsModal>($"Add Boosted Odds", parameters, options);
        }
        public void OpacityChanged(decimal opacity)
        {
            if (_alertModel != null)
            {
                _alertModel.Opacity = opacity;
                _alertModel.Message = string.Empty;
            }
        }

        public void Dispose()
        {
            _priceBoostList = new();
            _breadcrumbs = new();
            _filterReference = new();
            _languagesModel = new();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private async Task GetContents()
        {
            _isPreLoader = true;
            await InvokeAsync(StateHasChanged);

            var priceBoostConfigResponse = (await TradingService.GetPriceBoostConfig(new PriceBoostConfigRequest
            {
                ClientCode = !string.IsNullOrEmpty(_clientCode) ? _clientCode : string.Empty,
                SportId = _sportId > 0 ? _sportId.ToString() : string.Empty,
                SegmentId = _segmentId > 0 ? _segmentId.ToString() : string.Empty,
                LeagueId = _leagueId > 0 ? _leagueId.ToString() : string.Empty,
                ProviderFixtureId = string.Empty,
                MarketTemplateId = string.Empty,
                SelectionTemplateId = string.Empty,
                SelectionSpecifier = string.Empty,
                MarketSpecifier = string.Empty
            }));

            if (priceBoostConfigResponse.StatusCode == StatusCode.Success)
            {
                if (priceBoostConfigResponse.Result == null || !priceBoostConfigResponse.Result.Any())
                {
                    _priceBoostList = new List<PriceBoostConfigModel>();
                }
                else
                {
                    var query = priceBoostConfigResponse.Result.AsQueryable().Where(c => UserModel.ClientCodes.Contains(c.ClientCode));

                    if (!string.IsNullOrEmpty(_search))
                    {
                        var search = _search.ToLower();

                        query = query.Where(c => c.ClientName.ToLower().Contains(_search) || c.SportName.ToLower().Contains(_search)
                        || c.SegmentName.ToLower().Contains(_search) || c.LeagueName.ToLower().Contains(_search)
                        || c.SelectionName.ToLower().Contains(_search));
                    }

                    _priceBoostList = query.ToList();
                }
            }

            _isPreLoader = false;
        }

        #endregion
    }
}
