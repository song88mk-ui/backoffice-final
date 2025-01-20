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

        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        #endregion

        #region Fields
        public const string InlineCss = "top:50%";
        private LanguagesModel _languages;
        private PriceBoostConfigModel _priceBoostConfigModel;
        private bool _isPreloader;
        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            _isPreloader = true;
            _languages = ModalParameters.TryGet<LanguagesModel>("Languages");
            
            _priceBoostConfigModel = ModalParameters.TryGet<PriceBoostConfigModel>("PriceBoostConfigModel");
            
            _tradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();
            _isPreloader = false;
        }

        public async Task OnSelectLanguage(Tuple<int, Language> args)
        {
            //args.Item 1 = index

            var prevLanguage = new Language
            {
                LanguageCode = _priceBoostConfigModel.BoostedSelectionTranslations[args.Item1].Language.LanguageCode,
                LanguageName = _priceBoostConfigModel.BoostedSelectionTranslations[args.Item1].Language.LanguageName
            };

            _priceBoostConfigModel.BoostedSelectionTranslations[args.Item1].SelectLanguage = false;
            _priceBoostConfigModel.BoostedSelectionTranslations[args.Item1].Language = args.Item2;

            if (!string.IsNullOrEmpty(_priceBoostConfigModel.BoostedSelectionTranslations[args.Item1].BoostedSelectionName))
                _priceBoostConfigModel.BoostedSelectionTranslations[args.Item1].IsRequired = false;

            _languages.Languages.Remove(args.Item2);
            if (!string.IsNullOrEmpty(prevLanguage.LanguageCode))
                _languages.Languages.Add(prevLanguage);
        }

        public async Task OnAddSelection()
        {
            if(_languages.Languages.Count() - _priceBoostConfigModel.BoostedSelectionTranslations.Where(c => c.Language.LanguageCode == string.Empty).Count() > 0)
            {
                _priceBoostConfigModel.BoostedSelectionTranslations.Add(new BoostedSelectionTranslation
                {
                    BoostedSelectionName = string.Empty,
                    ClientCode = _priceBoostConfigModel.ClientCode,
                    Language = new Language
                    {
                        LanguageCode = string.Empty,
                        LanguageName = string.Empty
                    }
                });

            }

            //await InvokeAsync(StateHasChanged);
        }

        public async Task OnDeleteSelection(BoostedSelectionTranslation selection)
        {
            _priceBoostConfigModel.BoostedSelectionTranslations.Remove(selection);

            var prevLanguage = new Language
            {
                LanguageCode = selection.Language.LanguageCode,
                LanguageName = selection.Language.LanguageName
            };

            _languages.Languages.Add(prevLanguage);

           // await InvokeAsync(StateHasChanged);
        }

        public async Task OnCancelModalClick()
        {
            _modalService.Cancel();

        }

        public async Task OnSaveModalClick()
        {
            _isPreloader = true;
            await InvokeAsync(StateHasChanged);

            if (_priceBoostConfigModel.BoostedSelectionTranslations.Any())
            {
                foreach (var selection in _priceBoostConfigModel.BoostedSelectionTranslations)
                {
                    if (string.IsNullOrEmpty(selection.Language.LanguageCode) || string.IsNullOrEmpty(selection.BoostedSelectionName))
                    {

                        selection.IsRequired = true;
                        return;
                    }
                }
            }

            _priceBoostConfigModel.UserId = _userState.Model.Id;
            _priceBoostConfigModel.UserName = _userState.Model.Name;

            string currentPriceBoostString = ModalParameters.TryGet<string>("PriceBoostConfigModelSerialized");
            var region = ModalParameters.TryGet<string>("Region") ?? string.Empty;

            var result = await _tradingService.AddOrUpdatePriceBoost(new List<PriceBoostConfigModel>() { _priceBoostConfigModel });
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

                        await _tradingService.Publish(new MessageRequest()
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
                    ClientCode = _priceBoostConfigModel.ClientCode,
                    BoostedSelectionName = _priceBoostConfigModel.BoostedSelectionName
                });

                var clientBoostedSelectionNameTranslations2 = new List<ClientBoostedSelectionTranslationMessage>();
                if (_priceBoostConfigModel.BoostedSelectionTranslations != null && _priceBoostConfigModel.BoostedSelectionTranslations.Any())
                    clientBoostedSelectionNameTranslations2.AddRange(_priceBoostConfigModel.BoostedSelectionTranslations.Select(s => new ClientBoostedSelectionTranslationMessage
                    {
                        LanguageCode = s.Language.LanguageCode,
                        ClientCode = s.ClientCode,
                        BoostedSelectionName = s.BoostedSelectionName
                    }));

                var payload2 = new OnPriceBoostMessage
                {
                    ClientCode = _priceBoostConfigModel.ClientCode,
                    SportId = _priceBoostConfigModel.SportId,
                    SegmentId = _priceBoostConfigModel.SegmentId,
                    LeagueId = _priceBoostConfigModel.LeagueId,
                    FixtureTypeId = _priceBoostConfigModel.FixtureTypeId,
                    ProviderFixtureId = _priceBoostConfigModel.ProviderFixtureId,
                    MarketTemplateId = _priceBoostConfigModel.MarketTemplateId,
                    SelectionTemplateId = _priceBoostConfigModel.SelectionTemplateId,
                    SelectionSpecifier = _priceBoostConfigModel.SelectionSpecifier,
                    BoostedOdds = string.IsNullOrEmpty(_priceBoostConfigModel.BoostedOddsStr) ? 0 : Convert.ToDecimal(_priceBoostConfigModel.BoostedOddsStr),
                    IsBoosted = result.Result.IsBoosted,
                    ClientBoosted = result.Result.ClientBoosted,
                    ClientMarketBoosted = result.Result.ClientMarketBoosted,
                    ClientBoostedSelectionNames = clientBoostedSelectionNames2,
                    ClientBoostedSelectionNameTranslations = clientBoostedSelectionNameTranslations2
                };

                await _tradingService.Publish(new MessageRequest()
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
                    ClientName = _priceBoostConfigModel.ClientName,
                    ClientCode = _priceBoostConfigModel.ClientCode,
                    SelectionName = _priceBoostConfigModel.BoostedSelectionName,
                    MarketName = _priceBoostConfigModel.MarketName,
                    SportName = _priceBoostConfigModel.SportName,
                    SegmentName = _priceBoostConfigModel.SegmentName,
                    LeagueName = _priceBoostConfigModel.LeagueName,
                    Odds = decimal.Parse(_priceBoostConfigModel.BoostedOddsStr),
                    FixtureName = _priceBoostConfigModel.FixtureName,
                    UserName = _priceBoostConfigModel.UserName,
                    SelectionTemplateId = _priceBoostConfigModel.SelectionTemplateId,
                    MarketSpecifier = _priceBoostConfigModel.MarketSpecifier,
                    Translations = _priceBoostConfigModel.BoostedSelectionTranslations
                };

                var updateLogResponse = await _backofficeService.AddActivityLog(new ActivityLogModel()
                {
                    Page = SportsbookClientPermission.SBC_OE_PriceBoost_Manage.ToString().Replace('_', '.'),
                    Payload = $"{_priceBoostConfigModel.ClientCode}.{_priceBoostConfigModel.SportId}.{_priceBoostConfigModel.SegmentId}.{_priceBoostConfigModel.LeagueId}",
                    Activity = Activity.Update.ToString(),
                    From = JsonSerializer.Serialize(priceBoostLogModelFrom),
                    To = JsonSerializer.Serialize(priceBoostLogModelTo),
                    UserId = _userState.Model.Id,
                    UserName = _userState.Model.Name
                });
            }

            _isPreloader = false;

            await Modal.OnSaveClickCallback.InvokeAsync("updated");

            _modalService.Cancel();
        }

        public void Dispose()
        {

        }

        #endregion
    }
}
