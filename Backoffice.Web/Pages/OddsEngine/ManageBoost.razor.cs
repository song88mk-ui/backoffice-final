using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
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
        [Inject] private IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter(Name = "UserModel")] UserModel UserModel { get; set; }        
        public MetaData MetaData { get; set; }
        public PriceboostFilterModel PriceBoostFilterModel { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }        
        public List<PriceBoostConfigModel> PriceBoostList { get; set; }
        public LanguagesModel Languages { get; set; }
        private PriceBoostConfigModel TempConfig { get; set; }
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Manage Boost";
            IconClass = string.Empty;

            PriceBoostFilterModel = new PriceboostFilterModel();

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 10000,
                TotalPages = 100
            };
            
            PriceBoostFilterModel.Clients = await LoadClients();
            PriceBoostFilterModel.Sports = await LoadSports();
            PriceBoostFilterModel.Segments = await LoadSegments(PriceBoostFilterModel.SportId);
            PriceBoostFilterModel.Leagues = await LoadLeagues(PriceBoostFilterModel.SportId, PriceBoostFilterModel.SegmentId);
            await GetContents();
            await InvokeAsync(StateHasChanged);
        }        

        public async Task OnEdit(PriceBoostConfigModel priceboost)
        {            
            Languages = new LanguagesModel();

            Languages.Languages = Languages.Languages.Where(x => !priceboost.BoostedSelectionTranslations
            .Select(y => y.Language.LanguageCode).ToList().Contains(x.LanguageCode)).ToList();

            string obj = JsonSerializer.Serialize(priceboost);

            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 90rem;top:50%;transform:translateY(-50%)");
            parameters.Add("PriceBoostConfigModel", JsonSerializer.Deserialize<PriceBoostConfigModel>(obj));
            parameters.Add("Languages", Languages);            

            var options = new ModalOptions();            
            ModalService.Open<EditBoostedOddsModal>($"Edit Boosted Odds", parameters, options);
        }

        public async Task OnViewLog(PriceBoostConfigModel priceboost)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("PriceBoostConfigModel", priceboost);

            var options = new ModalOptions();
            ModalService.Open<ViewLogsBoostedModal>($"View Logs", parameters, options);
        }

        public async Task OnDelete(PriceBoostConfigModel priceboost)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle", "max-width: 80rem;top:50%;transform:translateY(-50%)");
            parameters.Add("PriceBoostConfigModel", priceboost);

            var options = new ModalOptions();
            ModalService.Open<DeleteBoostedOddsModal>($"Delete Boosted Odds", parameters, options);
        }

        public async Task GetContents()
        {
            var priceBoostList = (await TradingService.GetPriceBoostConfig(new PriceBoostConfigRequest 
            {
                ClientCode = !string.IsNullOrEmpty(PriceBoostFilterModel.ClientCode) ? PriceBoostFilterModel.ClientCode : "",
                SportId = PriceBoostFilterModel.SportId > 0 ? PriceBoostFilterModel.SportId.ToString() : "",
                SegmentId = PriceBoostFilterModel.SegmentId > 0 ? PriceBoostFilterModel.SegmentId.ToString() : "",
                LeagueId = PriceBoostFilterModel.LeagueId > 0 ? PriceBoostFilterModel.LeagueId.ToString() : "",
                ProviderFixtureId = "",
                MarketTemplateId = "",
                SelectionTemplateId = "",
                SelectionSpecifier = "",
                MarketSpecifier = ""
            })).Result;

            if (priceBoostList != null && priceBoostList.Any())
            {
                priceBoostList = priceBoostList.Where(x => UserModel.ClientCodes.Contains(x.ClientCode)).ToList();
            }

            PriceBoostList = priceBoostList;
        }

        public async Task OnClientChangedEvent(string clientCode)
        {
            await GetContents();
        }

        public async Task OnSportChangedEvent(int sportId)
        {
            PriceBoostFilterModel.SegmentId = 0;
            PriceBoostFilterModel.LeagueId = 0;

            PriceBoostFilterModel.Segments = await LoadSegments(sportId);
            PriceBoostFilterModel.Leagues = await LoadLeagues(sportId, PriceBoostFilterModel.SegmentId);

            await GetContents();
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnSegmentChangedEvent(Tuple<int,int> param)
        {
            PriceBoostFilterModel.LeagueId = 0;

            PriceBoostFilterModel.Leagues = await LoadLeagues(param.Item1, param.Item2);

            await GetContents();
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnLeagueChangedEvent()
        {
            await GetContents();
            await InvokeAsync(StateHasChanged);
        }

        public async Task OnModalSave()
        {
            PriceBoostList = new List<PriceBoostConfigModel>();
            PriceBoostFilterModel.ClientCode = string.Empty;
            PriceBoostFilterModel.SportId = 0;
            PriceBoostFilterModel.SegmentId = 0;
            PriceBoostFilterModel.LeagueId = 0;

            await GetContents();
        }

        public async Task OnModalBack(object addPriceBoostFilterModel)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("AddPriceBoostFilterModel", (AddPriceBoostFilterModel)addPriceBoostFilterModel);

            var options = new ModalOptions();
            ModalService.Open<AddBoostedOddsModal>($"Add Boosted Odds", parameters, options);
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utilities
        private async Task<List<SelectListItemModel<string>>> LoadClients()
        {
            ClientModel clients = new ClientModel();
            clients = (await BackOfficeService.GetClients()).Result;
            if (clients != null)
            {

                if (UserModel.ClientCodes.Any())
                {
                    clients.Clients = clients.Clients.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();
                }
            }

            return clients.Clients.Select(x => new SelectListItemModel<string>()
            {
                Name = x.ClientName,
                Value = x.ClientCode
            }).ToList();
        }

        private async Task<List<SelectListItemModel<int>>> LoadSports()
        {
            var result = (await TradingService.GetSportLookup()).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var sports = result.SportLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SportId,
                Name = s.SportName
            }).ToList();

            return sports;
        }

        private async Task<List<SelectListItemModel<int>>> LoadSegments(int sportId)
        {
            var result = (await TradingService.GetSegmentLookup(sportId)).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var segments = result.SegmentLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.SegmentId,
                Name = s.SegmentName
            }).ToList();

            return segments;
        }

        private async Task<List<SelectListItemModel<int>>> LoadLeagues(int sportId, int segmentId)
        {
            var result = (await TradingService.GetLeagueLookup(sportId, segmentId)).Result;

            if (result == null)
                return new List<SelectListItemModel<int>>();

            var leagues = result.LeagueLookup.Select(s => new SelectListItemModel<int>()
            {
                Value = s.LeagueId,
                Name = s.LeagueName
            }).ToList();

            return leagues;
        }
        #endregion
    }
}
