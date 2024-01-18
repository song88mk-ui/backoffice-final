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
        [Inject] IBackofficeService BackOfficeService { get; set; }

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
            TradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();
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

                Backoffice.Shared.Models.PriceBoostLogModel priceBoostLogModelFrom = new Backoffice.Shared.Models.PriceBoostLogModel()
                {
                    ClientName = PriceBoost.ClientName,
                    ClientCode = PriceBoost.ClientCode,
                    SelectionName = PriceBoost.BoostedSelectionName,
                    MarketName = PriceBoost.MarketName,
                    SportName = PriceBoost.SportName,
                    SegmentName = PriceBoost.SegmentName,
                    LeagueName = PriceBoost.LeagueName,
                    Odds = PriceBoost.BoostedOdds,
                    FixtureName = PriceBoost.FixtureName,
                    UserName = PriceBoost.UserName,
                    SelectionTemplateId = PriceBoost.SelectionTemplateId,
                    MarketSpecifier = PriceBoost.MarketSpecifier,
                    Translations = PriceBoost.BoostedSelectionTranslations
                };

                var deleteLogResponse = await BackOfficeService.AddActivityLog(new ActivityLogModel()
                {
                    Page = SportsbookClientPermission.SBC_OE_PriceBoost_Manage.ToString().Replace('_', '.'),
                    Payload = $"{PriceBoost.ClientCode}.{PriceBoost.SportId}.{PriceBoost.SegmentId}.{PriceBoost.LeagueId}",
                    Activity = Activity.Delete.ToString(),
                    From = JsonSerializer.Serialize(priceBoostLogModelFrom),
                    To = "-",
                    UserId = UserModel.Id,
                    UserName = UserModel.UserName,
                    TimeStamp = DateTime.UtcNow
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
