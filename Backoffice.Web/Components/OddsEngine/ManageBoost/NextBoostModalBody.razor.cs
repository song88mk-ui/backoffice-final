using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Text.Json;
using System.Transactions;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class NextBoostModalBody : IDisposable
    {
        #region Properties

        [Inject] ITradingService TradingService { get; set; }
        [Inject] IModalService ModalService { get; set; }
        [Inject] IBackofficeService BackofficeService { get; set; }

        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] ModalParameters ModalParameters { get; set; }

        public AddPriceBoostFilterModel AddPriceBoostFilterModel { get; set; }

        public bool IsPreLoader { get; set; }
        public string Placeholder { get; set; }
        public bool IsSaveDisabled { get; set; }

        #endregion

        #region Fields

        public const string PreloaderInlineCss = "transform: unset !important;position: absolute;display: flex;justify-content: center;align-items: center;z-index: 1000;top: 0;left: 0;right: 0;bottom: 0;background-color: rgba(0, 0, 0, 0.3);";

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            TradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();
            AddPriceBoostFilterModel = ModalParameters.TryGet<AddPriceBoostFilterModel>("AddPriceBoostFilterModel");
            IsSaveDisabled = true;
        }

        public async Task OnConfigBoostedInputChangedEvent(KeyboardEventArgs args)
        {
            IsSaveDisabled = true;
            if (AddPriceBoostFilterModel.PriceBoostConfigList.Any(c => !string.IsNullOrEmpty(c.BoostedOddsStr)))
            {
                IsSaveDisabled = AddPriceBoostFilterModel.PriceBoostConfigList.Where(x => !string.IsNullOrEmpty(x.BoostedOddsStr)).Any(x => double.TryParse(x.BoostedOddsStr, out double result) && result <= 1);
            }

            await InvokeAsync(StateHasChanged);
        }

        public async Task OnCancelModalClick()
        {
            ModalService.Cancel();
        }

        public async Task OnSaveModalClick()
        {
            if (!AddPriceBoostFilterModel.PriceBoostConfigList.Any(c => !string.IsNullOrEmpty(c.BoostedOddsStr)))
            {
                return;
            }

            if (AddPriceBoostFilterModel.SportId == 0 || AddPriceBoostFilterModel.SegmentId == 0 || AddPriceBoostFilterModel.LeagueId == 0 ||
            string.IsNullOrEmpty(AddPriceBoostFilterModel.ProviderFixtureId) || string.IsNullOrEmpty(AddPriceBoostFilterModel.MarketTemplateId) || string.IsNullOrEmpty($"{AddPriceBoostFilterModel.SelectionTemplateId}{AddPriceBoostFilterModel.SelectionSpecifier}"))
            {
                return;
            }

            //validate if odds less than 1
            if (AddPriceBoostFilterModel.PriceBoostConfigList != null && AddPriceBoostFilterModel.PriceBoostConfigList.Any())
            {
                foreach (var priceBoost in AddPriceBoostFilterModel.PriceBoostConfigList)
                {
                    if (!string.IsNullOrEmpty(priceBoost.BoostedOddsStr))
                    {
                        if (Convert.ToDecimal(priceBoost.BoostedOddsStr) <= 1
                            || Convert.ToDecimal(priceBoost.BoostedOddsStr) > 99999)
                        {

                            return;
                        }
                    }

                    if (priceBoost.BoostedSelectionTranslations.Any())
                    {
                        foreach (var selection in priceBoost.BoostedSelectionTranslations)
                        {
                            if (string.IsNullOrEmpty(selection.Language.LanguageCode) || string.IsNullOrEmpty(selection.BoostedSelectionName))
                            {
                                selection.IsRequired = true;
                                return;
                            }
                        }
                    }
                }
            }

            IsPreLoader = true;

            var result = await TradingService.AddOrUpdatePriceBoost(AddPriceBoostFilterModel);

            if (result.StatusCode == StatusCode.Success)
            {
                if (result.Result.ExistingPriceBoostConfigs != null)
                {
                    foreach (var existing in result.Result.ExistingPriceBoostConfigs)
                    {
                        var clientBoostedSelectionNames = new List<ClientBoostedSelectionNameMessage>();
                        clientBoostedSelectionNames.Add(new ClientBoostedSelectionNameMessage()
                        {
                            ClientCode = existing.ClientCode,
                            BoostedSelectionName = existing.BoostedSelectionName
                        });

                        var clientBoostedSelectionNameTranslations = new List<ClientBoostedSelectionTranslationMessage>();
                        if (existing.BoostedSelectionTranslations != null && existing.BoostedSelectionTranslations.Any())
                            clientBoostedSelectionNameTranslations.AddRange(existing.BoostedSelectionTranslations.Select(s => new ClientBoostedSelectionTranslationMessage
                            {
                                LanguageCode = s.Language.LanguageCode,
                                ClientCode = s.ClientCode,
                                BoostedSelectionName = s.BoostedSelectionName
                            }));

                        var payload = new OnPriceBoostMessage
                        {
                            ClientCode = existing.ClientCode,
                            SportId = existing.SportId,
                            SegmentId = existing.SegmentId,
                            LeagueId = existing.LeagueId,
                            FixtureTypeId = existing.FixtureTypeId,
                            ProviderFixtureId = existing.ProviderFixtureId,
                            MarketTemplateId = existing.MarketTemplateId,
                            SelectionTemplateId = existing.SelectionTemplateId,
                            SelectionSpecifier = existing.SelectionSpecifier,
                            BoostedOdds = 0,
                            IsBoosted = result.Result.IsBoosted,
                            ClientBoosted = result.Result.ClientBoosted,
                            ClientMarketBoosted = result.Result.ClientMarketBoosted,
                            ClientBoostedSelectionNames = clientBoostedSelectionNames,
                            ClientBoostedSelectionNameTranslations = clientBoostedSelectionNameTranslations
                        };

                        await TradingService.Publish(new MessageRequest()
                        {
                            CorrelationId = Guid.NewGuid().ToString(),
                            MessageType = "OnPriceBoost",
                            RoutingKey = $"r.uof.onpriceboost",
                            Payload = JsonSerializer.Serialize(payload)
                        });
                    }
                }

                var activityLogList = new ActivityLogListRequest();

                foreach (var priceboost in AddPriceBoostFilterModel.PriceBoostConfigList)
                {
                    var clientBoostedSelectionNames = new List<ClientBoostedSelectionNameMessage>();
                    clientBoostedSelectionNames.Add(new ClientBoostedSelectionNameMessage()
                    {
                        ClientCode = priceboost.ClientCode,
                        BoostedSelectionName = priceboost.BoostedSelectionName
                    });

                    var clientBoostedSelectionNameTranslations = new List<ClientBoostedSelectionTranslationMessage>();
                    if (priceboost.BoostedSelectionTranslations != null && priceboost.BoostedSelectionTranslations.Any())
                        clientBoostedSelectionNameTranslations.AddRange(priceboost.BoostedSelectionTranslations.Select(s => new ClientBoostedSelectionTranslationMessage
                        {
                            LanguageCode = s.Language.LanguageCode,
                            ClientCode = s.ClientCode,
                            BoostedSelectionName = s.BoostedSelectionName
                        }));

                    var payload = new OnPriceBoostMessage
                    {
                        ClientCode = priceboost.ClientCode,
                        SportId = priceboost.SportId,
                        SegmentId = priceboost.SegmentId,
                        LeagueId = priceboost.LeagueId,
                        FixtureTypeId = priceboost.FixtureTypeId,
                        ProviderFixtureId = priceboost.ProviderFixtureId,
                        MarketTemplateId = priceboost.MarketTemplateId,
                        SelectionTemplateId = priceboost.SelectionTemplateId,
                        SelectionSpecifier = priceboost.SelectionSpecifier,
                        BoostedOdds = string.IsNullOrEmpty(priceboost.BoostedOddsStr) ? 0 : Convert.ToDecimal(priceboost.BoostedOddsStr),
                        IsBoosted = result.Result.IsBoosted,
                        ClientBoosted = result.Result.ClientBoosted,
                        ClientMarketBoosted = result.Result.ClientMarketBoosted,
                        ClientBoostedSelectionNames = clientBoostedSelectionNames,
                        ClientBoostedSelectionNameTranslations = clientBoostedSelectionNameTranslations
                    };

                    await TradingService.Publish(new MessageRequest()
                    {
                        CorrelationId = Guid.NewGuid().ToString(),
                        MessageType = "OnPriceBoost",
                        RoutingKey = $"r.uof.onpriceboost",
                        Payload = JsonSerializer.Serialize(payload)
                    });

                    if (!string.IsNullOrEmpty(priceboost.BoostedOddsStr))
                    {
                        Backoffice.Shared.Models.PriceBoostLogModel priceBoostLogModel = new Backoffice.Shared.Models.PriceBoostLogModel()
                        {
                            ClientName = priceboost.ClientName,
                            ClientCode = priceboost.ClientCode,
                            SelectionName = priceboost.BoostedSelectionName,
                            MarketName = priceboost.MarketName,
                            SportName = priceboost.SportName,
                            SegmentName = priceboost.SegmentName,
                            LeagueName = priceboost.LeagueName,
                            Odds = decimal.Parse(priceboost.BoostedOddsStr),
                            FixtureName = priceboost.FixtureName,
                            UserName = priceboost.UserName,
                            SelectionTemplateId = priceboost.SelectionTemplateId,
                            MarketSpecifier = priceboost.MarketSpecifier,
                            Translations = priceboost.BoostedSelectionTranslations
                        };

                        var onActivityLogMessage = new ActivityLogModel()
                        {
                            Page = SportsbookClientPermission.SBC_OE_PriceBoost_Manage.ToString().Replace('_', '.'),
                            Payload = $"{priceboost.ClientCode}.{priceboost.SportId}.{priceboost.SegmentId}.{priceboost.LeagueId}",
                            Activity = Activity.Create.ToString(),
                            From = "-",
                            To = JsonSerializer.Serialize(priceBoostLogModel),
                            UserId = _userState.Model.Id,
                            UserName = _userState.Model.Name
                        };

                        activityLogList.ActivityLogs.Add(onActivityLogMessage);
                    }
                }

                await BackofficeService.AddActivityLogList(activityLogList);

                //await JsRuntime.InvokeVoidAsync("ShowNotication", SaveButton, "Priceboost has been updated");

                await Modal.OnSaveClickCallback.InvokeAsync("updated");

                ModalService.Cancel();
            }

            IsPreLoader = false;
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
