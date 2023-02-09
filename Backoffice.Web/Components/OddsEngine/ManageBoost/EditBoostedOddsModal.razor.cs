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
    public partial class EditBoostedOddsModal : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private ITradingService TradingService { get; set; }
        [Inject] private IBackOfficeService BackOfficeService { get; set; }

        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public LanguagesModel Languages { get; set; }
        public PriceBoostConfigModel PriceBoostConfigModel { get; set; }  
        
        public bool IsPreloader { get; set; }

        #endregion

        #region Fields

        public const string InlineCss = "top:50%";

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreloader = true;            
            Languages = ModalParameters.TryGet<LanguagesModel>("Languages");
            PriceBoostConfigModel = ModalParameters.TryGet<PriceBoostConfigModel>("PriceBoostConfigModel");
            IsPreloader = false;
        }

        public async Task OnSelectLanguage(Tuple<int, Language> args)
        {           
            //args.Item 1 = index

            var prevLanguage = new Language
            {
                LanguageCode = PriceBoostConfigModel.BoostedSelectionTranslations[args.Item1].Language.LanguageCode,
                LanguageName = PriceBoostConfigModel.BoostedSelectionTranslations[args.Item1].Language.LanguageName
            };

            PriceBoostConfigModel.BoostedSelectionTranslations[args.Item1].SelectLanguage = false;
            PriceBoostConfigModel.BoostedSelectionTranslations[args.Item1].Language = args.Item2;

            if (!string.IsNullOrEmpty(PriceBoostConfigModel.BoostedSelectionTranslations[args.Item1].BoostedSelectionName))
                PriceBoostConfigModel.BoostedSelectionTranslations[args.Item1].IsRequired = false;

            Languages.Languages.Remove(args.Item2);
            if (!string.IsNullOrEmpty(prevLanguage.LanguageCode))
                Languages.Languages.Add(prevLanguage);            
        }

        public async Task OnAddSelection()
        {
            PriceBoostConfigModel.BoostedSelectionTranslations.Add(new BoostedSelectionTranslation
            {
                BoostedSelectionName = string.Empty,
                ClientCode = PriceBoostConfigModel.ClientCode,
                Language = new Language
                {
                    LanguageCode = string.Empty,
                    LanguageName = string.Empty
                }
            });

            await InvokeAsync(StateHasChanged);
        }

        public async Task OnDeleteSelection(BoostedSelectionTranslation selection)
        {
            PriceBoostConfigModel.BoostedSelectionTranslations.Remove(selection);

            await InvokeAsync(StateHasChanged);
        }        

        public async Task OnCancelModalClick()
        {            
            ModalService.Cancel();
        }

        public async Task OnSaveModalClick()
        {
            IsPreloader = true;

            if (PriceBoostConfigModel.BoostedSelectionTranslations.Any())
            {
                foreach (var selection in PriceBoostConfigModel.BoostedSelectionTranslations)
                {
                    if (string.IsNullOrEmpty(selection.Language.LanguageCode) || string.IsNullOrEmpty(selection.BoostedSelectionName))
                    {

                        selection.IsRequired = true;
                        return;
                    }
                }
            }

            PriceBoostConfigModel.UserId = UserModel.Id;
            PriceBoostConfigModel.UserName = UserModel.UserName;

            string currentPriceBoostString = ModalParameters.TryGet<string>("PriceBoostConfigModelSerialized");

            var result = await TradingService.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel>() { PriceBoostConfigModel });
            if (result.StatusCode == StatusCode.Success)
            {
                //remove existing from mongodb
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

                var clientBoostedSelectionNames2 = new List<ClientBoostedSelectionNameMessage>();
                clientBoostedSelectionNames2.Add(new ClientBoostedSelectionNameMessage()
                {
                    ClientCode = PriceBoostConfigModel.ClientCode,
                    BoostedSelectionName = PriceBoostConfigModel.BoostedSelectionName
                });

                var clientBoostedSelectionNameTranslations2 = new List<ClientBoostedSelectionTranslationMessage>();
                if (PriceBoostConfigModel.BoostedSelectionTranslations != null && PriceBoostConfigModel.BoostedSelectionTranslations.Any())
                    clientBoostedSelectionNameTranslations2.AddRange(PriceBoostConfigModel.BoostedSelectionTranslations.Select(s => new ClientBoostedSelectionTranslationMessage
                    {
                        LanguageCode = s.Language.LanguageCode,
                        ClientCode = s.ClientCode,
                        BoostedSelectionName = s.BoostedSelectionName
                    }));

                var payload2 = new OnPriceBoostMessage
                {
                    ClientCode = PriceBoostConfigModel.ClientCode,
                    SportId = PriceBoostConfigModel.SportId,
                    SegmentId = PriceBoostConfigModel.SegmentId,
                    LeagueId = PriceBoostConfigModel.LeagueId,
                    FixtureTypeId = PriceBoostConfigModel.FixtureTypeId,
                    ProviderFixtureId = PriceBoostConfigModel.ProviderFixtureId,
                    MarketTemplateId = PriceBoostConfigModel.MarketTemplateId,
                    SelectionTemplateId = PriceBoostConfigModel.SelectionTemplateId,
                    SelectionSpecifier = PriceBoostConfigModel.SelectionSpecifier,
                    BoostedOdds = string.IsNullOrEmpty(PriceBoostConfigModel.BoostedOddsStr) ? 0 : Convert.ToDecimal(PriceBoostConfigModel.BoostedOddsStr),
                    IsBoosted = result.Result.IsBoosted,
                    ClientBoosted = result.Result.ClientBoosted,
                    ClientMarketBoosted = result.Result.ClientMarketBoosted,
                    ClientBoostedSelectionNames = clientBoostedSelectionNames2,
                    ClientBoostedSelectionNameTranslations = clientBoostedSelectionNameTranslations2
                };

                await TradingService.Publish(new MessageRequest()
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    MessageType = "OnPriceBoost",
                    RoutingKey = $"r.uof.onpriceboost",
                    Payload = JsonSerializer.Serialize(payload2)
                });

                var currentPriceBoost = JsonSerializer.Deserialize<PriceBoostConfigModel>(currentPriceBoostString);

                Backoffice.Shared.Models.PriceBoostLogModel priceBoostLogModelFrom = new Backoffice.Shared.Models.PriceBoostLogModel()
                {
                    ClientName = currentPriceBoost.ClientName,
                    ClientCode = currentPriceBoost.ClientCode,
                    SelectionName = currentPriceBoost.BoostedSelectionName,
                    MarketName = currentPriceBoost.MarketName,
                    SportName = currentPriceBoost.SportName,
                    SegmentName = currentPriceBoost.SegmentName,
                    LeagueName = currentPriceBoost.LeagueName,
                    Odds = currentPriceBoost.BoostedOdds,
                    FixtureName = currentPriceBoost.FixtureName,
                    UserName = currentPriceBoost.UserName,
                    SelectionTemplateId = currentPriceBoost.SelectionTemplateId,
                    MarketSpecifier = currentPriceBoost.MarketSpecifier,
                    Translations = currentPriceBoost.BoostedSelectionTranslations
                };

                Backoffice.Shared.Models.PriceBoostLogModel priceBoostLogModelTo = new Backoffice.Shared.Models.PriceBoostLogModel()
                {
                    ClientName = PriceBoostConfigModel.ClientName,
                    ClientCode = PriceBoostConfigModel.ClientCode,
                    SelectionName = PriceBoostConfigModel.BoostedSelectionName,
                    MarketName = PriceBoostConfigModel.MarketName,
                    SportName = PriceBoostConfigModel.SportName,
                    SegmentName = PriceBoostConfigModel.SegmentName,
                    LeagueName = PriceBoostConfigModel.LeagueName,
                    Odds = decimal.Parse(PriceBoostConfigModel.BoostedOddsStr),
                    FixtureName = PriceBoostConfigModel.FixtureName,
                    UserName = PriceBoostConfigModel.UserName,
                    SelectionTemplateId = PriceBoostConfigModel.SelectionTemplateId,
                    MarketSpecifier = PriceBoostConfigModel.MarketSpecifier,
                    Translations = PriceBoostConfigModel.BoostedSelectionTranslations
                };

                var updateLogResponse = await BackOfficeService.AddActivityLog(new ActivityLogModel()
                {
                    Page = SportsbookClientPermission.SBC_OE_PriceBoost_Manage.ToString().Replace('_','.'),
                    Payload = $"{PriceBoostConfigModel.ClientCode}.{PriceBoostConfigModel.SportId}.{PriceBoostConfigModel.SegmentId}.{PriceBoostConfigModel.LeagueId}",
                    Activity = Activity.Update.ToString(),
                    From = JsonSerializer.Serialize(priceBoostLogModelFrom),
                    To = JsonSerializer.Serialize(priceBoostLogModelTo),
                    UserId = UserModel.Id,
                    UserName = UserModel.UserName,
                    TimeStamp = DateTime.UtcNow
                });
            }

            IsPreloader = false;

            await Modal.OnSaveClickCallback.InvokeAsync("updated");

            ModalService.Cancel();
        }

        public void Dispose()
        {
            
        }

        #endregion
    }
}
