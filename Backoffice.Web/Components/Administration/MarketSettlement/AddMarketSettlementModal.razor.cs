using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using System.Text.Json;

namespace Backoffice.Web.Components.Administration.MarketSettlement
{
    public partial class AddMarketSettlementModal : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }

        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        public AddMarketSettlementModel AddMarketSettlement { get; set; }
        public MarketFixtureResponse MarketFixtures { get; set; }
        public MarketFixtureResponse MarketOutrightFixtures { get; set; }
        public AlertModel AlertModel { get; set; }

        public bool IsPreLoader { get; set; }
        public bool ShowDiscardConfirm { get; set; }

        #endregion


        #region Methods

        protected override async Task OnInitializedAsync()
        {
            AlertModel = new AlertModel();
            AddMarketSettlement = new AddMarketSettlementModel();
        }

        public async Task OnAddStatusChange(ChangeEventArgs e, int rowCount)
        {
            var row = AddMarketSettlement.AddMarketSettlementSelections.FirstOrDefault(r => r.RowCount == rowCount);

            var status = e?.Value?.ToString();
            row.ToStatus = int.Parse(status);

        }

        public async Task OnCheckboxClicked(ChangeEventArgs e, AddMarketSettlementSelectionModel addMarketSettlementSelection)
        {
            addMarketSettlementSelection.Selected = !addMarketSettlementSelection.Selected;
        }

        public async Task SetMarketFixtures(Tuple<MarketFixtureResponse, MarketFixtureResponse> args)
        {
            MarketFixtures = args.Item1;
            MarketOutrightFixtures = args.Item2;
        }

        public async Task UpdateRowDetails(AddMarketSettlementModel AddMarketSettlement)
        {
            IsPreLoader = true;
            this.AddMarketSettlement = AddMarketSettlement;

            if (!string.IsNullOrEmpty(AddMarketSettlement.BetId))
            {
                Guid.TryParse(AddMarketSettlement.BetId, out var betId);

                var betRequest = new ManualBetDetailsRequest
                {
                    BetIds = new List<string> { betId.ToString() }
                };
                //var betSelections = (await TradingService.GetBetSelectionDetails(betRequest)).Result;
                var betSelections = (await BackofficeService.GetManualBetDetails(betRequest)).Result?.Bets?.FirstOrDefault();
                if (betSelections?.Selections != null && betSelections.Selections.Any())
                {
                    foreach (var selection in betSelections.Selections.OrderBy(s => s.FixtureName).ThenBy(s => s.MarketId))
                    {
                        if (AddMarketSettlement.SportId > 0 && selection.SportId != AddMarketSettlement.SportId)
                            continue;

                        if (AddMarketSettlement.SegmentId > 0 && selection.SegmentId != AddMarketSettlement.SegmentId)
                            continue;

                        if (AddMarketSettlement.LeagueId > 0 && selection.LeagueId != AddMarketSettlement.LeagueId)
                            continue;

                        if (!string.IsNullOrEmpty(AddMarketSettlement.FixtureId) && selection.FixtureId != AddMarketSettlement.FixtureId)
                            continue;

                        if (!string.IsNullOrEmpty(AddMarketSettlement.MarketId) && selection.MarketId != AddMarketSettlement.MarketId)
                            continue;

                        if (AddMarketSettlement.AddMarketSettlementSelections.Any(s => s.FixtureId == selection.FixtureId
                                                                                    && s.MarketId == selection.MarketId
                                                                                    && s.SelectionId == selection.SelectionId))
                            continue;

                        AddMarketSettlement.AddMarketSettlementSelections.Add(new AddMarketSettlementSelectionModel()
                        {
                            RowCount = AddMarketSettlement.AddMarketSettlementSelections.Count + 1,
                            FixtureId = selection.FixtureId,
                            MarketId = selection.MarketId,
                            SelectionId = selection.SelectionId,
                            SportId = selection.SportId,
                            SportName = selection.SportName,
                            SegmentId = selection.SegmentId,
                            SegmentName = selection.SegmentName,
                            LeagueId = selection.LeagueId,
                            LeagueName = selection.LeagueName,
                            FixtureName = selection.FixtureName,
                            FixtureStartDate = selection.FixtureStartDate,
                            FixtureTypeId = selection.FixtureTypeId,
                            FixtureTypeName = selection.FixtureTypeName,
                            FixtureStatusId = selection.FixtureStatusId,
                            FixtureStatusName = selection.FixtureStatusName,
                            MarketName = selection.MarketName,
                            SelectionName = selection.SelectionName,
                            Line = selection.Line,
                            Odds = selection.Odds,
                            SettlementScore = selection.SettlementScore,
                            MarketTemplateId = selection.MarketTemplateId,
                            SelectionTemplateId = selection.SelectionTemplateId,
                            MarketSpecifier = string.Empty,
                            ProviderFixtureId = selection.FixtureSpecifier.Split(":").LastOrDefault(),
                            FixtureSpecifier = selection.FixtureSpecifier,
                            SelectionSpecifier = selection.SelectionSpecifier,
                            Probability = selection.Probability,
                            WonProbability = selection.WonProbability,
                            LostProbability = selection.LostProbability,
                            HalfLostProbability = selection.HalfLostProbability,
                            HalfWonProbability = selection.HalfWonProbability,
                            VoidProbability = selection.VoidProbability,
                            CorrelationId = selection.CorrelationId,
                            Statuses = await GetStatuses(),
                            DeadHeat = 1m
                        });
                    }
                }


                if (!AddMarketSettlement.AddMarketSettlementSelections.Any())
                {
                    AddMarketSettlement.AddMarketSettlementSelections.Add(new AddMarketSettlementSelectionModel()
                    {
                        RowCount = AddMarketSettlement.AddMarketSettlementSelections.Count + 1,
                        FixtureId = AddMarketSettlement.FixtureId,
                        MarketId = AddMarketSettlement.MarketId,
                        SelectionId = string.Empty,
                        Statuses = await GetStatuses(),
                        DeadHeat = 1m
                    });
                }
                IsPreLoader = false;
                return;
            }
            else
            {
                var selections = await GetSelections(AddMarketSettlement.FixtureId, AddMarketSettlement.MarketId);

                if (selections.Any())
                {
                    AddMarketSettlement.AddMarketSettlementSelections.Clear();

                    foreach (var selection in selections)
                    {
                        AddMarketSettlement.AddMarketSettlementSelections.Add(new AddMarketSettlementSelectionModel()
                        {
                            RowCount = AddMarketSettlement.AddMarketSettlementSelections.Count + 1,
                            FixtureId = AddMarketSettlement.FixtureId,
                            MarketId = AddMarketSettlement.MarketId,
                            SelectionId = selection.Value,
                            Statuses = await GetStatuses(),
                            DeadHeat = 1m
                        });
                    }
                }
            }

            if (!AddMarketSettlement.AddMarketSettlementSelections.Any())
            {
                AddMarketSettlement.AddMarketSettlementSelections.Add(new AddMarketSettlementSelectionModel()
                {
                    RowCount = AddMarketSettlement.AddMarketSettlementSelections.Count + 1,
                    FixtureId = AddMarketSettlement.FixtureId,
                    MarketId = AddMarketSettlement.MarketId,
                    SelectionId = string.Empty,
                    Statuses = await GetStatuses(),
                    DeadHeat = 1m
                });
            }

            foreach (var row in AddMarketSettlement.AddMarketSettlementSelections)
            {
                row.SegmentName = string.Empty;
                row.LeagueName = string.Empty;
                row.FixtureName = string.Empty;
                row.SportId = 0;
                row.SportName = string.Empty;
                row.FixtureStartDate = new DateTime();
                row.MarketName = string.Empty;
                row.SelectionName = string.Empty;
                row.Line = string.Empty;
                row.Odds = 0;
                row.SettlementScore = string.Empty;
                row.MarketTemplateId = string.Empty;
                row.SelectionTemplateId = string.Empty;
                row.MarketSpecifier = string.Empty;
                row.ProviderFixtureId = string.Empty;
                row.FixtureSpecifier = string.Empty;

                if (string.IsNullOrEmpty(row.FixtureId) || string.IsNullOrEmpty(row.MarketId) || string.IsNullOrEmpty(row.SelectionId))
                    continue;

                if (AddMarketSettlement.AddMarketSettlementSelections.Any(r => r.FixtureId == row.FixtureId && r.MarketId == row.MarketId && r.SelectionId == row.SelectionId && r.RowCount != row.RowCount))
                {
                    IsPreLoader = false;
                    return;
                }

                long fixtureId = 0;
                long marketId = 0;
                long selectionId = 0;

                try
                {
                    fixtureId = long.Parse(row.FixtureId);
                    marketId = long.Parse(row.MarketId);
                    selectionId = long.Parse(row.SelectionId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                    if (ex.StackTrace != null)
                        Log.Error(ex.StackTrace.ToString());

                    IsPreLoader = false;
                    return;
                }

                var detail = (await TradingService.GetManualMarketSettlementDetails(new ManualMarketSettlementDetailsRequest()
                {
                    FixtureId = fixtureId.ToString(),
                    MarketId = marketId.ToString(),
                    SelectionId = selectionId.ToString(),
                })).Result;

                if (detail == null)
                {
                    IsPreLoader = false;
                    return;
                }

                row.FixtureId = fixtureId.ToString();
                row.MarketId = marketId.ToString();
                row.SelectionId = selectionId.ToString();
                row.SportId = detail.SportId;
                row.SportName = detail.SportName;
                row.SegmentId = detail.SegmentId;
                row.SegmentName = detail.SegmentName;
                row.LeagueId = detail.LeagueId;
                row.LeagueName = detail.LeagueName;
                row.FixtureName = detail.FixtureName;
                row.FixtureTypeId = detail.FixtureTypeId;
                row.FixtureTypeName = detail.FixtureTypeName;
                row.FixtureStartDate = detail.FixtureStartDate;
                row.MarketTemplateId = detail.MarketTemplateId;
                row.MarketName = detail.MarketName;
                row.SelectionTemplateId = detail.SelectionTemplateId;
                row.SelectionName = detail.SelectionName;
                row.FixtureStatusId = detail.FixtureStatusId;
                row.FixtureStatusName = detail.FixtureStatusName;
                row.Line = detail.Line;
                row.Odds = detail.Odds;
                row.ProviderFixtureId = detail.ProviderFixtureId;
                row.SettlementScore = detail.SettlementScore;
                row.MarketTemplateId = detail.MarketTemplateId;
                row.SelectionTemplateId = detail.SelectionTemplateId;
                row.MarketSpecifier = detail.MarketSpecifier;
                row.ProviderFixtureId = detail.ProviderFixtureId;
                row.FixtureSpecifier = detail.FixtureSpecifier;
                row.SelectionSpecifier = detail.SelectionSpecifier;
                row.FixtureStartDate = detail.FixtureStartDate;
                row.Probability = detail.Probability;
                row.LostProbability = detail.LostProbability;
                row.WonProbability = detail.WonProbability;
                row.HalfLostProbability = detail.HalfLostProbability;
                row.HalfWonProbability = detail.HalfWonProbability;
                row.VoidProbability = detail.VoidProbability;
                row.CorrelationId = detail.CorrelationId;
                row.Statuses = await GetStatuses();

            }

            IsPreLoader = false;
        }

        public async Task OnMarketSettlement()
        {
            var request = new List<ManualMarketSettlementRequest>();

            foreach (var row in AddMarketSettlement.AddMarketSettlementSelections)
            {
                if (!string.IsNullOrEmpty(AddMarketSettlement.BetId) && AddMarketSettlement.AddMarketSettlementSelections.Count > 1 && row.Selected == false)
                    continue;

                if (string.IsNullOrEmpty(row.FixtureId) || string.IsNullOrEmpty(row.MarketId) || string.IsNullOrEmpty(row.SelectionId)
                    || string.IsNullOrEmpty(row.FixtureName) || row.ToStatus == 0 || string.IsNullOrEmpty(row.Reason))
                {
                    AlertModel = new AlertModel()
                    {
                        Status = AlertStatus.Error,
                        Message = "Error! All fields cannot be empty. Please fill in accordingly or remove the empty row",
                        Opacity = 1.0m,
                    };

                    return;
                }

                var marketSpecifier = row.MarketSpecifier;
                if (string.IsNullOrEmpty(row.MarketSpecifier) && row.SelectionSpecifier.Contains("?"))
                    marketSpecifier = row.SelectionSpecifier.Split("?").LastOrDefault();

                request.Add(new ManualMarketSettlementRequest()
                {
                    CorrelationId = row.CorrelationId,
                    Probability = row.Probability,
                    LostProbability = row.LostProbability,
                    VoidProbability = row.VoidProbability,
                    HalfLostProbability = row.HalfLostProbability,
                    HalfWonProbability = row.HalfWonProbability,
                    WonProbability = row.WonProbability,
                    ProviderFixtureId = row.ProviderFixtureId,
                    FixtureStatusId = row.FixtureStatusId,
                    FixtureStartDate = row.FixtureStartDate,
                    FixtureSpecifier = row.FixtureSpecifier,
                    FixtureName = row.FixtureName,
                    FixtureStatusName = row.FixtureStatusName,
                    FixtureTypeId = row.FixtureTypeId,
                    FixtureTypeName = row.FixtureTypeName,
                    LeagueId = row.LeagueId,
                    LeagueName = row.LeagueName,
                    Line = row.Line,
                    MarketName = row.MarketName,
                    MarketSpecifier = marketSpecifier,
                    MarketTemplateId = row.MarketTemplateId,
                    Odds = row.Odds,
                    SegmentId = row.SegmentId,
                    SegmentName = row.SegmentName,
                    SelectionName = row.SelectionName,
                    SelectionSpecifier = row.SelectionSpecifier,
                    SelectionTemplateId = row.SelectionTemplateId,
                    SettlementScore = row.SettlementScore,
                    SportId = row.SportId,
                    SportName = row.SportName,
                    FixtureId = row.FixtureId,
                    MarketId = row.MarketId,
                    SelectionId = row.SelectionId,
                    DeadHeatFactor = row.DeadHeat,
                    SelectionStatus = (BetStatus)row.ToStatus,
                    Reason = row.Reason,
                    ManualSettledBy = _userState.Model.Name,
                });

            }

            IsPreLoader = true;
            await InvokeAsync(StateHasChanged);

            // Activity logging for manual market settlements is owned server-side by
            // TradingRepository.AddOrUpdateManualMarketSettlement (written before the
            // OnBetSettlement publish, so it survives post-save failures). Logging here
            // as well would produce duplicate history rows.
            var response = (await TradingService.AddOrUpdateManualMarketSettlement(request, _userState.Model.Id)).Result;

            if (response != null && response.StatusCode == StatusCode.Success)
            {

                IsPreLoader = false;
                AlertModel = new AlertModel()
                {
                    Status = AlertStatus.Success,
                    Message = $"Markets {string.Join(",", request.Select(x => x.MarketName).ToList())} has been successfully settled.",
                    Opacity = 1.0m
                };

                StateHasChanged();
                await Task.Delay(3500);

                await Modal.OnSaveClickCallback.InvokeAsync();
                ModalService.Cancel();
            }
            else
            {
                IsPreLoader = false;
                AlertModel = new AlertModel()
                {
                    Status = AlertStatus.Error,
                    Message = "Market Settlement Failed!",
                    Opacity = 1.0m,
                };
            }
        }

        public async Task OnCancelModal()
        {
            if (HasUnsavedChanges())
            {
                ShowDiscardConfirm = true;
                return;
            }

            ModalService.Cancel();
        }

        public async Task OnConfirmDiscardChanges()
        {
            ShowDiscardConfirm = false;
            ModalService.Cancel();
        }

        public async Task OnCancelDiscardChanges()
        {
            ShowDiscardConfirm = false;
        }

        private bool HasUnsavedChanges()
        {
            return AddMarketSettlement.AddMarketSettlementSelections.Any(r => r.ToStatus != 0 || !string.IsNullOrWhiteSpace(r.Reason));
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities

        private async Task<List<SelectListItemModel<string>>> GetSelections(string fixtureId, string marketId)
        {
            if (MarketFixtures == null && MarketOutrightFixtures == null)
                return new List<SelectListItemModel<string>>();

            var selections = new List<SelectListItemModel<string>>();

            if (MarketFixtures != null && MarketFixtures.Fixtures.Any())
            {
                var fixture = MarketFixtures.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
                if (fixture != null)
                {
                    var market = fixture.Markets.FirstOrDefault(m => m.MarketId == marketId);
                    if (market != null)
                    {
                        var selection2 = market.Selections.Select(s => new SelectListItemModel<string>()
                        {
                            Value = s.SelectionId,
                            Name = s.SelectionName
                        }).Distinct().ToList();

                        if (selection2 != null && selection2.Any())
                            selections.AddRange(selection2);
                    }
                }
            }

            if (MarketOutrightFixtures != null && MarketOutrightFixtures.Fixtures.Any())
            {
                var fixture = MarketOutrightFixtures.Fixtures.Where(f => f.FixtureId == fixtureId).FirstOrDefault();
                if (fixture != null)
                {
                    var market = fixture.Markets.FirstOrDefault(m => m.MarketId == marketId);
                    if (market != null)
                    {
                        var selection2 = market.Selections.Select(s => new SelectListItemModel<string>()
                        {
                            Value = s.SelectionId,
                            Name = s.SelectionName
                        }).Distinct().ToList();

                        if (selection2 != null && selection2.Any())
                            selections.AddRange(selection2);
                    }
                }
            }

            return selections.Distinct().ToList();
        }

        private async Task<List<SelectListItemModel<int>>> GetStatuses()
        {
            var statuses = new List<SelectListItemModel<int>>()
            {
                new SelectListItemModel<int>()
                {
                    Value = (int)BetStatus.Won,
                    Name = BetStatus.Won.ToString()
                },
                new SelectListItemModel<int>()
                {
                    Value = (int)BetStatus.Lost,
                    Name = BetStatus.Lost.ToString()
                },
                new SelectListItemModel<int>()
                {
                    Value = (int)BetStatus.Void,
                    Name = BetStatus.Void.ToString()
                },
                new SelectListItemModel<int>()
                {
                    Value = (int)BetStatus.HalfWon,
                    Name = BetStatus.HalfWon.ToString()
                },
                new SelectListItemModel<int>()
                {
                    Value = (int)BetStatus.HalfLost,
                    Name = BetStatus.HalfLost.ToString()
                }
            };

            return statuses;
        }

        protected string GetMarketLine(string marketName, string line)
        {
            if (!string.IsNullOrEmpty(marketName))
            {
                if (string.IsNullOrEmpty(line))
                {
                    return string.Empty;
                }
                else
                {
                    if (marketName.ToUpper().Contains("TOTAL"))
                        return $"o {decimal.Parse(line)} u";

                    if (marketName.ToUpper().Contains("HANDICAP"))
                        return $"h {decimal.Parse(line)} a";
                }
            }
            return string.Empty;
        }

        private bool GetResult(BetStatus status)
        {
            return status == BetStatus.Won || status == BetStatus.HalfWon ? true : false;
        }

        private VoidFactor GetVoidFactor(BetStatus status)
        {
            var voidFactor = VoidFactor.Zero;

            if (status == BetStatus.Won || status == BetStatus.Lost)
                return VoidFactor.Zero;

            if (status == BetStatus.Void)
                return VoidFactor.One;

            if (status == BetStatus.HalfWon || status == BetStatus.HalfLost)
                return VoidFactor.Half;

            return voidFactor;
        }

        #endregion
    }
}
