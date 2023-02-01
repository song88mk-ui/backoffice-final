using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.ManageBoost
{
    public partial class DeleteBoostedOddsModal : IDisposable
    {
        #region Properties

        [Inject] IModalService ModalService { get; set; }
        [Inject] ITradingService TradingService { get; set; }

        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }

        public PriceBoostConfigModel PriceBoost { get; set; }
        public bool IsPreLoader { get; set; }

        #endregion

        #region Fields

        public const string InlineCss = "top:50%";

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            PriceBoost = ModalParameters.TryGet<PriceBoostConfigModel>("PriceBoostConfigModel");
        }

        public async Task OnCancelModalClick()
        {
            ModalService.Cancel();
        }

        public async Task OnDeleteModalClick()
        {
            IsPreLoader = true;

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

            IsPreLoader = false;

            await Modal.OnSaveClickCallback.InvokeAsync("deleted");

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
