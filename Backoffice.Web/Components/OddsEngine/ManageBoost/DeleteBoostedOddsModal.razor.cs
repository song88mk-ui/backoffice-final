using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class DeleteBoostedOddsModal : IDisposable
    {
        #region Properties
        [Inject] IModalService ModalService { get; set; }
        [Inject] ITradingService TradingService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        private PriceBoostConfigModel PriceBoost { get; set; }
        public bool IsPreLoader { get; set; }
        public const string InlineCss = "";
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = false;
            PriceBoost = ModalParameters.TryGet<PriceBoostConfigModel>("PriceBoostConfigModel");
        }
        public async Task OnCancelModalClick()
        {
            ModalService.Cancel();
        }
        public async Task OnDeleteModalClick()
        {
            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            //var priceBoostConfig = AddPriceBoostFilterModel.PriceBoostConfigList.Where(x => x.ClientCode == AddPriceBoostFilterModel.ClientCode &&
            //x.SportId == AddPriceBoostFilterModel.SportId &&
            //x.LeagueId == AddPriceBoostFilterModel.LeagueId &&
            //x.SegmentId == AddPriceBoostFilterModel.SegmentId).FirstOrDefault();

            //priceBoostConfig.FixtureTypeId = AddPriceBoostFilterModel.ShowMatchTypeMenu ? (int)FixtureType.Match : (int)FixtureType.Outright;
            //priceBoostConfig.ProviderFixtureId = AddPriceBoostFilterModel.ProviderFixtureId;
            //priceBoostConfig.MarketTemplateId = AddPriceBoostFilterModel.MarketTemplateId;
            //priceBoostConfig.SelectionTemplateId = AddPriceBoostFilterModel.SelectionTemplateId;
            //priceBoostConfig.SelectionSpecifier = AddPriceBoostFilterModel.SelectionSpecifier;
            //priceBoostConfig.UserId = UserModel.User.Id;
            //priceBoostConfig.UserName = UserModel.User.UserName;
            //priceBoostConfig.TimeStamp = DateTime.Now;
            //priceBoostConfig.BoostedOddsStr = "0";
            //priceBoostConfig.BoostedOdds = 0;

            //AddPriceBoostFilterModel.PriceBoostConfigList = new List<PriceBoostConfigModel>();
            //AddPriceBoostFilterModel.PriceBoostConfigList.Add(priceBoostConfig);

            PriceBoost.BoostedOdds = 0;
            PriceBoost.BoostedOddsStr = "0";
            PriceBoost.TimeStamp = DateTime.UtcNow;
            PriceBoost.UserId = UserModel.Id;
            PriceBoost.UserName = UserModel.UserName;

            var result = await TradingService.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel>() { PriceBoost });
            if (result.StatusCode == StatusCode.Success)
            {

                var payload = new OnPriceBoostMessage
                {
                    ClientCode = PriceBoost.ClientCode,
                    SportId = PriceBoost.SportId,
                    SegmentId = PriceBoost.SegmentId,
                    LeagueId = PriceBoost.LeagueId,
                    FixtureTypeId = PriceBoost.FixtureTypeId,
                    ProviderFixtureId = PriceBoost.ProviderFixtureId,
                    MarketTemplateId = PriceBoost.MarketTemplateId,
                    SelectionTemplateId = PriceBoost.SelectionTemplateId,
                    SelectionSpecifier = PriceBoost.SelectionSpecifier,
                    BoostedOdds = 0,
                    IsBoosted = result.Result.IsBoosted,
                    ClientBoosted = result.Result.ClientBoosted,
                    ClientMarketBoosted = result.Result.ClientMarketBoosted,
                };

                await TradingService.Publish(new MessageRequest()
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    MessageType = "OnPriceBoost",
                    RoutingKey = $"r.uof.onpriceboost",
                    Payload = JsonSerializer.Serialize(payload)
                });
            }

                await InvokeAsync(StateHasChanged);

                ModalService.Cancel();
        }
        public void Dispose()
        {
            
        }
        #endregion

        #region Utilities
        public string DisplayMessage()
        {
            return $"{PriceBoost.ClientCode} > {PriceBoost.SportName} > {PriceBoost.SegmentName} > {PriceBoost.LeagueName} > {PriceBoost.FixtureName} > {PriceBoost.MarketName} > {PriceBoost.SelectionName}";
        }
        #endregion
    }
}
