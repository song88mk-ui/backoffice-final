using Backoffice.Shared.Enums;
using Backoffice.Shared.IncomingSbEngineMessages;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingWebSocketMessages;
using Backoffice.Shared.Utilities;
using Backoffice.WebSocket.Service.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backoffice.WebSocket.Service.Repositories
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private readonly ILogger<SportsbookRepository> _logger;
        private readonly ISportsbookService _sportsbookService;
        private readonly IMemCache _memCache;

        private const string HUB_PUBLISH_MONITORING_ODDSCHANGE = "PublishMonitoringOddsChange";
        private const string HUB_PUBLISH_ODDSCHANGE = "PublishOddsChange";
        private const string HUB_PUBLISH_BETSTOPFIXTURE = "PublishBetStopFixture";

        public SportsbookRepository(ILogger<SportsbookRepository> logger, IConfiguration configuration, IMemCache memCache, ISportsbookService sportsbookService)
        {
            _logger = logger;
            _sportsbookService = sportsbookService;
            _memCache = memCache;
        }
        public async Task ProcessMarketTemplatePlayerProps()
        {
            try
            {
                var key = $"mtpp";

                var result = await _sportsbookService.GetMarketTemplatePlayerProps();
                _memCache.AddOrUpdateCache(key, result.Result, DateTimeOffset.UtcNow.AddYears(1));
            }
            catch (Exception)
            {
            }
        }
        public async Task ProcessClientsWithoutMargin()
        {
            try
            {
                var key = "acnm";

                var result = await _sportsbookService.GetAsianclientCodes();
                _memCache.AddOrUpdateCache(key, result.Result, DateTimeOffset.UtcNow.AddYears(1));
            }
            catch (Exception)
            {
            }
        }
        public async Task ProcessOnMonitoringOddsChange(IncomingOddsChangeMessage message, HubConnection hubConnection)
        {
            try
            {
                var fixture = await ConvertToMonitoringOddsChangeFixture(message);

                if (fixture == null)
                    return;

                var payload = new OutgoingMonitoringFixtureOddsChangeMessage
                {
                    Gr = GetGroupForMonitoringMarkets(fixture),
                    Pa = fixture
                };

                await PublishToWebSocket(HUB_PUBLISH_MONITORING_ODDSCHANGE, hubConnection, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ex.Message} - {ex.StackTrace}");
            }
        }

        public async Task ProcessOnMonitoringDetailsOddsChange(IncomingOddsChangeMessage message, HubConnection hubConnection)
        {
            try
            {
                var fixture = await ConvertToOddsChangeFixture(message);

                if (fixture == null)
                    return;

                var payload = new OutgoingFixtureDetailsOddsChangeMessage
                {
                    Gr = GetGroupForMonitoringDetailsMarkets(fixture),
                    Pa = fixture
                };

                await PublishToWebSocket(HUB_PUBLISH_ODDSCHANGE, hubConnection, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ex.Message} - {ex.StackTrace}");
            }
        }

        public async Task ProcessOnBetStopFixture(IncomingBetStopFixtureMessage message, HubConnection hubConnection)
        {
            try
            {
                var fixture = new BetStopFixtureMessage()
                {
                    SportId = message.SportId,
                    FixtureId = message.FixtureId,
                    ProviderFixtureId = message.ProviderFixtureId,
                    IsBetStop = message.IsBetStop,
                    MarketTemplateGroups = message.MarketTemplateGroups,
                    CorrelationId = message.CorrelationId,
                };

                var payload = new OutgoingBetStopFixtureMessage
                {
                    Gr = GetGroupForBetStopFixture(message),
                    Pa = fixture
                };

                await PublishToWebSocket(HUB_PUBLISH_BETSTOPFIXTURE, hubConnection, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ex.Message} - {ex.StackTrace}");
            }
        }

        public async Task ProcessOnBetStopGlobal(IncomingBetStopGlobalMessage message, HubConnection hubConnection)
        {
            try
            {
                var fixture = new BetStopGlobalMessage()
                {
                    IsBetStop = message.IsBetStop,
                    CorrelationId = message.CorrelationId
                };

                var payload = new OutgoingBetStopGlobalMessage
                {
                    Pa = fixture
                };

                await PublishToWebSocket(HUB_PUBLISH_BETSTOPFIXTURE, hubConnection, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ex.Message} - {ex.StackTrace}");
            }
        }

        public async Task ProcessNotification(NotificationsDetailsModel message, HubConnection hubConnection)
        {
            var date = DateTime.UtcNow.ToString();
            var payload = new OutgoingNotificationMessage
            {
                Gr = null,
                UsId = message.UserId?.ToString(),
                Pa = new NotificationsModel
                {
                    Id = message.Id,
                    Timestamp = date,
                    Page = message.Page,
                    Details = message.Details,
                    PageRedirect = message.PageRedirect,
                    IsRead = false,
                    UserId = message.UserId?.ToString(),
                    DateText = date,
                    ClaimsType = message.ClaimType,
                    ClientCode = message.ClientCode,
                }
            };

            await PublishToWebSocket("PublishNotification", hubConnection, payload);
        }

        private async Task<OddsChangeMonitoringMessage> ConvertToMonitoringOddsChangeFixture(IncomingOddsChangeMessage message)
        {
            try
            {
                var ctrl = "CTRL";
                var result = new List<OddsChangeMonitoringClient>();

                if (message.Markets != null)
                {
                    var marketTemplateGroup = GetMarketTemplatePlayerProps();

                    var topMarkets = GetTopMarketTemplates(message.SportId);
                    var topMarketTemplateIds = new HashSet<string>(topMarkets.Select(tm => tm.MainMarketTemplateId));

                    var tempMarkets = new List<OddsChangeMonitoringMarket>();
                    var clientWithouMargins = GetAsianClientsWithoutMargin();

                    foreach (var market in message.Markets.Where(c => topMarketTemplateIds.Contains(c.MarketTemplateId)))
                    {
                        var clients = new List<ClientMarginModel>()
                        {
                            new ClientMarginModel(){ ClientCode = ctrl, Margin = -999 }
                        };

                        if (!string.IsNullOrEmpty(market.ClientCodeMargin))
                            clients.AddRange(market.ClientCodeMargin.Split('|').Select(s =>
                            {
                                var clientMargin = s.Split(":");

                                return new ClientMarginModel() { ClientCode = clientMargin.FirstOrDefault(), Margin = Convert.ToDecimal(clientMargin.LastOrDefault()) };
                            }));
                        var providerMarketId = $"{market.ProviderFixtureId}:{market.MarketTemplateId}:{market.MarketSpecifier}";
                        tempMarkets.AddRange(clients.Select(client => new OddsChangeMonitoringMarket()
                        {
                            PrFiId = market.ProviderFixtureId,
                            MaTeId = market.MarketTemplateId,
                            MaId = market.MarketId.ToString(),
                            PrMaId = providerMarketId,
                            MaNa = market.MarketName,
                            Li = market.Line,
                            MaStId = (int)market.MarketStatus,
                            MaStNa = market.MarketStatus.ToString(),
                            MaSp = market.MarketSpecifier,
                            ClCo = client.ClientCode,
                            ClCoMa = client.Margin,
                            Se = GetMonitoringClientOdds(market.Selections, clientWithouMargins.ClientCodes.Contains(client.ClientCode) && clientWithouMargins.MarketTemplateIds.Contains(market.MarketTemplateId) && message.SportId == 1 ? "-999" : client.Margin.ToString(), market.MarketTemplateId, marketTemplateGroup)
                        }));
                    }

                    var clientMarkets = tempMarkets.GroupBy(market => new { market.ClCo, market.PrFiId }).ToList();

                    foreach (var clientMarket in clientMarkets)
                    {
                        var marketTemplates = clientMarket.GroupBy(market => market.MaTeId);

                        var newClientMarkets = new OddsChangeMonitoringClient()
                        {
                            ClCo = clientMarket.Key.ClCo,
                            Ma = new List<OddsChangeMonitoringMarket>()
                        };

                        foreach (var marketTemplate in marketTemplates)
                        {
                            var nearestToTwo = GetNearestLineToTwo(marketTemplate.ToList());

                            if (nearestToTwo != null)
                                newClientMarkets.Ma.Add(nearestToTwo);
                        }

                        result.Add(newClientMarkets);
                    }
                }

                return new OddsChangeMonitoringMessage()
                {
                    SpId = message.SportId,
                    SpNa = message.SportName,
                    SegId = message.SegmentId,
                    SegNa = message.SegmentName,
                    LeId = message.LeagueId,
                    LeNa = message.LeagueName,
                    PrFiId = message.ProviderFixtureId,
                    FiId = message.FixtureId.ToString(),
                    FiNa = message.FixtureName,
                    HoNa = GetHomeName(message.FixtureName, (FixtureType)message.FixtureTypeId),
                    AwNa = GetAwayName(message.FixtureName, (FixtureType)message.FixtureTypeId),
                    FiStDa = message.FixtureStartDate,
                    FiStId = message.FixtureStatusId,
                    FiStNa = message.FixtureStatusName,
                    Sc = message.Score,
                    CuPe = message.CurrentPeriod,
                    CuMi = message.CurrentMinute,
                    SoOr = message.SortOrder,
                    FiTyId = message.FixtureTypeId,
                    UnTi = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Cl = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ex.Message} - {ex.StackTrace}");
                return null;
            }
        }
        private async Task<OddsChangeFixtureDetailsMessage> ConvertToOddsChangeFixture(IncomingOddsChangeMessage message)
        {
            try
            {
                var markets = new List<OddsChangeFixtureDetailsMarket>();

                if (message.Markets != null)
                {
                    var marketTemplateGroup = GetMarketTemplatePlayerProps();

                    var topMarkets = GetTopMarketTemplates(message.SportId);
                    var topMarketTemplateIds = new HashSet<string>(topMarkets.Select(tm => tm.MainMarketTemplateId));

                    foreach (var market in message.Markets.Where(c => topMarketTemplateIds.Contains(c.MarketTemplateId)))
                    {
                        var selections = new List<OddsChangeFixtureDetailsSelection>();
                        var clientOdds = new Dictionary<string, List<OddsChangeFixtureDetailsClientOdds>>();
                        var margin = 0m;

                        if (market.Selections.Count > 0)
                        {
                            var activeSelections = market.Selections.Where(c => c.IsActive == true && c.Odds > 0m).ToList();

                            if (activeSelections.Count == 1)
                                activeSelections = market.Selections.Where(c => c.Odds > 0m).ToList();

                            margin = (Math.Round(activeSelections.Sum(s => 100 / s.Odds)) - 100) / 100;

                            var odds = GetClientOdds(message.SportId, activeSelections.Count, margin, market.Selections, market.ClientCodeMargin, market.MarketTemplateId, marketTemplateGroup.Where(c => c.SportId == message.SportId).ToList());

                            clientOdds = odds.GroupBy(c => c.ClCo).ToDictionary(s => s.Key, s => s.ToList());
                        }

                        try
                        {
                            markets.Add(new OddsChangeFixtureDetailsMarket()
                            {
                                MaTeId = market.MarketTemplateId,
                                MaId = market.MarketId.ToString(),
                                PrMaId = market.ProviderMarketId,
                                MaNa = market.MarketName,
                                Li = market.Line,
                                MaStId = (int)market.MarketStatus,
                                MaStNa = market.MarketStatus.ToString(),
                                MaSp = market.MarketSpecifier,
                                ClCo = market.ClientCodes,
                                ClCoMa = market.ClientCodeMargin,
                                ClCoMaLi = !string.IsNullOrEmpty(market.ClientCodeMargin) ? market.ClientCodeMargin.Split("|").ToDictionary(s => s.Split(":").FirstOrDefault(), s => Convert.ToDecimal(s.Split(":").LastOrDefault())) : new Dictionary<string, decimal>(),
                                SoOr = market.SortOrder,
                                UnTi = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                                Se = market.Selections.Select(selection => new OddsChangeFixtureDetailsSelection()
                                {
                                    SeId = selection.SelectionId,
                                    SeTeId = selection.SelectionTemplateId,
                                    SeNa = selection.SelectionName,
                                    DecOd = selection.Odds,
                                    IsAc = selection.IsActive,
                                    SoOr = selection.SortOrder,
                                    UnTi = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                }).ToList(),
                                ClSe = clientOdds.Select(odds => new OddsChangeFixtureDetailsClientSelections()
                                {
                                    ClCo = odds.Key,
                                    ClMa = odds.Value.FirstOrDefault().ClCoMa,
                                    Se = odds.Value.Select(oddsValue => new OddsChangeFixtureDetailsSelection()
                                    {
                                        SeId = oddsValue.SeId,
                                        SeTeId = oddsValue.SeTeId,
                                        SeNa = oddsValue.SeNa,
                                        DecOd = oddsValue.Od,
                                        IsAc = oddsValue.IsAc,
                                        SoOr = oddsValue.SoOr,
                                        UnTi = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                    }).ToList()
                                }).ToList()
                            });
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }

                return new OddsChangeFixtureDetailsMessage()
                {
                    SpId = message.SportId,
                    SpNa = message.SportName,
                    SegId = message.SegmentId,
                    SegNa = message.SegmentName,
                    LeId = message.LeagueId,
                    LeNa = message.LeagueName,
                    PrFiId = message.ProviderFixtureId,
                    FiId = message.FixtureId.ToString(),
                    FiNa = message.FixtureName,
                    HoNa = GetHomeName(message.FixtureName, (FixtureType)message.FixtureTypeId),
                    AwNa = GetAwayName(message.FixtureName, (FixtureType)message.FixtureTypeId),
                    FiStDa = message.FixtureStartDate,
                    FiSId = message.FixtureStatusId,
                    FiStNa = message.FixtureStatusName,
                    Sc = message.Score,
                    CuPe = message.CurrentPeriod,
                    CuMi = message.CurrentMinute,
                    SoOr = message.SortOrder,
                    FiTyId = message.FixtureTypeId,
                    UnTi = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Ma = markets
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ex.Message} - {ex.StackTrace}");
                return null;
            }
        }

        private List<OddsChangeFixtureDetailsClientOdds> GetClientOdds(int sportId, int activeSelectionCount, decimal margin, List<IncomingSelectionMessage> selections, string clientMarketConfig, string marketTemplateId, List<MarketTemplatePlayerPropsResponse> marketTemplateGroups)
        {
            if (string.IsNullOrEmpty(clientMarketConfig))
                return new List<OddsChangeFixtureDetailsClientOdds>();

            var clients = clientMarketConfig.Split('|').Select(s => new Tuple<string, decimal>(s.Split(':').First(), Convert.ToDecimal(s.Split(':').Last()))).ToList();

            var clientWithouMargins = GetAsianClientsWithoutMargin();

            var result = new List<OddsChangeFixtureDetailsClientOdds>();
            var isBinary = selections.Count > 2 ? false : true;

            var playerPropMarketTemplateIds = string.Join(",", marketTemplateGroups.Select(s => s.MarketTemplates).ToList()).Split(",").ToList();

            foreach (var client in clients)
            {
                foreach (var selection in selections)
                {
                    if (sportId == 1 && clientWithouMargins.MarketTemplateIds.Contains(marketTemplateId) && clientWithouMargins.ClientCodes.Contains(client.Item1, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Add(new OddsChangeFixtureDetailsClientOdds()
                        {
                            ClCo = client.Item1,
                            Od = selection.Odds,
                            SeTeId = selection.SelectionTemplateId,
                            SeId = selection.SelectionId,
                            IsAc = selection.IsActive,
                            SeNa = selection.SelectionName,
                            SoOr = selection.SortOrder,
                            ClCoMa = client.ToString()
                        });

                        continue;
                    }

                    if (activeSelectionCount == 1)
                        activeSelectionCount = selections.Count;

                    if (selection.IsActive == true && selection.Odds > 0m)
                    {
                        var targetMargin = client.Item2;

                        if (playerPropMarketTemplateIds.Contains(marketTemplateId))
                        {
                            if (selection.SelectionTemplateId != "12" || selection.SelectionTemplateId != "13")
                            {
                                var tempOdds = Math.Round(selection.Odds - (selection.Odds * targetMargin), 2);

                                result.Add(new OddsChangeFixtureDetailsClientOdds
                                {
                                    ClCo = client.Item1,
                                    Od = tempOdds,
                                    SeTeId = selection.SelectionTemplateId,
                                    SeId = selection.SelectionId,
                                    IsAc = selection.IsActive,
                                    SeNa = selection.SelectionName,
                                    SoOr = selection.SortOrder,
                                    ClCoMa = client.ToString()
                                });

                                continue;
                            }
                        }

                        if (margin == targetMargin)
                        {
                            result.Add(new OddsChangeFixtureDetailsClientOdds
                            {
                                ClCo = client.Item1,
                                Od = selection.Odds,
                                SeTeId = selection.SelectionTemplateId,
                                SeId = selection.SelectionId,
                                IsAc = selection.IsActive,
                                SeNa = selection.SelectionName,
                                SoOr = selection.SortOrder,
                                ClCoMa = client.ToString()
                            });

                            continue;
                        }

                        var fairOddsInitialDivideBy = activeSelectionCount - (margin * selection.Odds);

                        if (fairOddsInitialDivideBy <= 0)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Odds);

                            result.Add(new OddsChangeFixtureDetailsClientOdds
                            {
                                ClCo = client.Item1,
                                Od = tempOdds,
                                SeTeId = selection.SelectionTemplateId,
                                SeId = selection.SelectionId,
                                IsAc = selection.IsActive,
                                SeNa = selection.SelectionName,
                                SoOr = selection.SortOrder,
                                ClCoMa = client.ToString()
                            });

                            continue;
                        }

                        var fairOdds = (activeSelectionCount * selection.Odds) / fairOddsInitialDivideBy;

                        if (fairOdds <= 0)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Odds);

                            result.Add(new OddsChangeFixtureDetailsClientOdds
                            {
                                ClCo = client.Item1,
                                Od = tempOdds,
                                SeTeId = selection.SelectionTemplateId,
                                SeId = selection.SelectionId,
                                IsAc = selection.IsActive,
                                SeNa = selection.SelectionName,
                                SoOr = selection.SortOrder,
                                ClCoMa = client.ToString()
                            });

                            continue;
                        }

                        var finalOddsInitialDivideBy = ((fairOdds * targetMargin) / activeSelectionCount) + 1;

                        if (finalOddsInitialDivideBy <= 0)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Odds);

                            result.Add(new OddsChangeFixtureDetailsClientOdds
                            {
                                ClCo = client.Item1,
                                Od = tempOdds,
                                SeTeId = selection.SelectionTemplateId,
                                SeId = selection.SelectionId,
                                IsAc = selection.IsActive,
                                SeNa = selection.SelectionName,
                                SoOr = selection.SortOrder,
                                ClCoMa = client.ToString()
                            });

                            continue;
                        }

                        var odds = Math.Round(fairOdds / finalOddsInitialDivideBy, 2);

                        if (odds < 1.00m)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(1m);

                            result.Add(new OddsChangeFixtureDetailsClientOdds
                            {
                                ClCo = client.Item1,
                                Od = tempOdds,
                                SeTeId = selection.SelectionTemplateId,
                                SeId = selection.SelectionId,
                                IsAc = selection.IsActive,
                                SeNa = selection.SelectionName,
                                SoOr = selection.SortOrder,
                                ClCoMa = client.ToString()
                            });

                            continue;
                        }

                        if (odds >= selection.Odds)
                            odds = GetMasterOddsWithAddedMargin(selection.Odds);

                        result.Add(new OddsChangeFixtureDetailsClientOdds
                        {
                            ClCo = client.Item1,
                            Od = odds,
                            SeTeId = selection.SelectionTemplateId,
                            SeId = selection.SelectionId,
                            IsAc = selection.IsActive,
                            SeNa = selection.SelectionName,
                            SoOr = selection.SortOrder,
                            ClCoMa = client.ToString()
                        });
                    }
                    else
                    {
                        result.Add(new OddsChangeFixtureDetailsClientOdds
                        {
                            ClCo = client.Item1,
                            Od = selection.Odds,
                            SeTeId = selection.SelectionTemplateId,
                            SeId = selection.SelectionId,
                            IsAc = selection.IsActive,
                            SeNa = selection.SelectionName,
                            SoOr = selection.SortOrder,
                            ClCoMa = client.ToString()
                        });
                    }
                }
            }

            return result;
        }

        private List<MarketTemplatePlayerPropsResponse> GetMarketTemplatePlayerProps() => _memCache.GetCache<List<MarketTemplatePlayerPropsResponse>>($"mtpp") ?? new List<MarketTemplatePlayerPropsResponse>();
        private NoMarginClientsResponse GetAsianClientsWithoutMargin() => _memCache.GetCache<NoMarginClientsResponse>($"acnm") ?? new NoMarginClientsResponse();

        private static decimal GetMasterOddsWithAddedMargin(decimal odds)
        {
            if (Math.Round(odds, 2) <= 1.01m)
                return odds;

            return Math.Round(100 / ((100 / odds) + 2m), 2);
        }

        private static string GetHomeName(string name, FixtureType fixtureType) => fixtureType == FixtureType.Match && !string.IsNullOrEmpty(name) ? name.Split(" vs. ").First().Trim() : string.Empty;

        private static string GetAwayName(string name, FixtureType fixtureType) => fixtureType == FixtureType.Match && !string.IsNullOrEmpty(name) ? name.Split(" vs. ").Last().Trim() : string.Empty;

        private async Task PublishToWebSocket<T>(string method, HubConnection hubConnection, T payload)
        {
            try
            {
                await hubConnection.SendAsync(method, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                _logger.LogError(ex.StackTrace);
            }
        }

        private List<string> GetGroupForMonitoringMarkets(OddsChangeMonitoringMessage message) => new List<string>
            {
                $"leagues:{message.LeId}:fixtures"
            };

        private List<string> GetGroupForMonitoringDetailsMarkets(OddsChangeFixtureDetailsMessage message) => new List<string>
            {
                $"fixturedetails:{message.PrFiId}"
            };

        private List<string> GetGroupForBetStopFixture(IncomingBetStopFixtureMessage message)
        {
            var result = new List<string>();

            result.Add($"leagues:{message.LeagueId}:fixtures");
            result.Add($"fixturedetails:{message.FixtureId}");

            return result;
        }
        private OddsChangeMonitoringMarket GetNearestLineToTwo(List<OddsChangeMonitoringMarket> markets)
        {
            if (!markets.Any())
                return null;

            if (markets.Count == 1)
                return markets.FirstOrDefault();

            var market = markets.FirstOrDefault(s => s.MaStId <= (int)MarketStatus.Active);

            if (market == null)
                return null;

            if (market.Se.Count > 2)
                return market;

            var evenLine = 2m;

            var selections = markets.SelectMany(s => s.Se.Select(se => new
            {
                s.MaId,
                s.Li,
                Odds = Convert.ToDecimal(se.DeOd),
                Diff = Math.Abs(evenLine - Convert.ToDecimal(se.DeOd))
            }))
                            .OrderBy(o => o.Diff)
                            .ThenBy(o => Convert.ToDecimal(o.Li))
                            .Distinct()
                            .ToList();

            if (!selections.Any())
                return null;

            var nearestSelections = selections.Where(x => x.Diff == selections.FirstOrDefault()?.Diff);

            if (nearestSelections.Any() && nearestSelections.Count() > 1)
            {
                nearestSelections = nearestSelections
                    .Select(s => new
                    {
                        s.MaId,
                        s.Li,
                        s.Odds,
                        Diff = Math.Abs(0.5m - (1 / s.Odds))
                    })
                    .OrderBy(o => o.Diff)
                    .ThenBy(o => Convert.ToDecimal(o.Li))
                    .Distinct();
            }

            var nearestSelection = nearestSelections.FirstOrDefault();

            return markets.FirstOrDefault(c => c.MaId == nearestSelection?.MaId);
        }
        private List<OddsChangeMonitoringSelection> GetMonitoringClientOdds(List<IncomingSelectionMessage> selections, string clientMargin, string marketTemplateId, List<MarketTemplatePlayerPropsResponse> marketTemplateGroups)
        {
            var result = new List<OddsChangeMonitoringSelection>();

            if (string.IsNullOrEmpty(clientMargin) || clientMargin == "-999")
                return selections.Select(selection => new OddsChangeMonitoringSelection
                {
                    SeTeId = selection.SelectionTemplateId,
                    SeId = selection.SelectionId.ToString(),
                    PrSeId = selection.ProviderSelectionId,
                    SeNa = selection.SelectionName,
                    SeSp = selection.SelectionSpecifier,
                    DeOd = selection.Odds.ToString("0.00"),
                    IsAc = selection.IsActive,
                    SoOr = selection.SortOrder
                }).ToList();

            var activeSelections = selections.Where(c => c.IsActive == true && c.Odds > 0m).ToList();
            var margin = (Math.Round(activeSelections.Sum(s => 100 / s.Odds)) - 100) / 100;

            if (activeSelections.Count == 1)
                margin = (Math.Round(selections.Sum(s => 100 / s.Odds)) - 100) / 100;

            int activeSelectionCount = activeSelections.Count;

            // prepare market player props
            var playerPropMarketTemplateIds = string.Join(",", marketTemplateGroups.Select(s => s.MarketTemplates).ToList()).Split(",").ToList();

            foreach (var selection in selections)
            {

                if (activeSelectionCount == 1)
                    activeSelectionCount = selections.Count;

                if (selection.IsActive == true && selection.Odds > 0m)
                {
                    var targetMargin = Convert.ToDecimal(clientMargin);

                    if (playerPropMarketTemplateIds.Contains(marketTemplateId))
                    {
                        if (selection.SelectionTemplateId != "12" || selection.SelectionTemplateId != "13")
                        {
                            var tempOdds = Math.Round(selection.Odds - (selection.Odds * targetMargin), 2);

                            result.Add(new OddsChangeMonitoringSelection
                            {
                                SeTeId = selection.SelectionTemplateId,
                                SeId = selection.SelectionId.ToString(),
                                PrSeId = selection.ProviderSelectionId,
                                SeNa = selection.SelectionName,
                                SeSp = selection.SelectionSpecifier,
                                DeOd = GetMasterOddsWithAddedMargin(tempOdds).ToString("0.00"),
                                IsAc = selection.IsActive,
                                SoOr = selection.SortOrder
                            });

                            continue;
                        }
                    }

                    if (margin == targetMargin)
                    {
                        result.Add(new OddsChangeMonitoringSelection
                        {
                            SeTeId = selection.SelectionTemplateId,
                            SeId = selection.SelectionId.ToString(),
                            PrSeId = selection.ProviderSelectionId,
                            SeNa = selection.SelectionName,
                            SeSp = selection.SelectionSpecifier,
                            DeOd = selection.Odds.ToString("0.00"),
                            IsAc = selection.IsActive,
                            SoOr = selection.SortOrder
                        });

                        continue;
                    }


                    var fairOddsInitialDivideBy = activeSelectionCount - (margin * selection.Odds);

                    if (fairOddsInitialDivideBy <= 0)
                    {
                        result.Add(new OddsChangeMonitoringSelection
                        {
                            SeTeId = selection.SelectionTemplateId,
                            SeId = selection.SelectionId.ToString(),
                            PrSeId = selection.ProviderSelectionId,
                            SeNa = selection.SelectionName,
                            SeSp = selection.SelectionSpecifier,
                            DeOd = GetMasterOddsWithAddedMargin(selection.Odds).ToString("0.00"),
                            IsAc = selection.IsActive,
                            SoOr = selection.SortOrder
                        });

                        continue;
                    }

                    var fairOdds = (activeSelectionCount * selection.Odds) / fairOddsInitialDivideBy;

                    if (fairOdds <= 0)
                    {
                        result.Add(new OddsChangeMonitoringSelection
                        {
                            SeTeId = selection.SelectionTemplateId,
                            SeId = selection.SelectionId.ToString(),
                            PrSeId = selection.ProviderSelectionId,
                            SeNa = selection.SelectionName,
                            SeSp = selection.SelectionSpecifier,
                            DeOd = GetMasterOddsWithAddedMargin(selection.Odds).ToString("0.00"),
                            IsAc = selection.IsActive,
                            SoOr = selection.SortOrder
                        });

                        continue;
                    }

                    var finalOddsInitialDivideBy = ((fairOdds * targetMargin) / activeSelectionCount) + 1;

                    if (finalOddsInitialDivideBy <= 0)
                    {
                        result.Add(new OddsChangeMonitoringSelection
                        {
                            SeTeId = selection.SelectionTemplateId,
                            SeId = selection.SelectionId.ToString(),
                            PrSeId = selection.ProviderSelectionId,
                            SeNa = selection.SelectionName,
                            SeSp = selection.SelectionSpecifier,
                            DeOd = GetMasterOddsWithAddedMargin(selection.Odds).ToString("0.00"),
                            IsAc = selection.IsActive,
                            SoOr = selection.SortOrder
                        });

                        continue;
                    }

                    var odds = Math.Round(fairOdds / finalOddsInitialDivideBy, 2);

                    if (odds < 1.00m)
                    {
                        result.Add(new OddsChangeMonitoringSelection
                        {
                            SeTeId = selection.SelectionTemplateId,
                            SeId = selection.SelectionId.ToString(),
                            PrSeId = selection.ProviderSelectionId,
                            SeNa = selection.SelectionName,
                            SeSp = selection.SelectionSpecifier,
                            DeOd = GetMasterOddsWithAddedMargin(1m).ToString("0.00"),
                            IsAc = selection.IsActive,
                            SoOr = selection.SortOrder
                        });

                        continue;
                    }

                    if (odds >= selection.Odds)
                        odds = GetMasterOddsWithAddedMargin(selection.Odds);

                    result.Add(new OddsChangeMonitoringSelection
                    {
                        SeTeId = selection.SelectionTemplateId,
                        SeId = selection.SelectionId.ToString(),
                        PrSeId = selection.ProviderSelectionId,
                        SeNa = selection.SelectionName,
                        SeSp = selection.SelectionSpecifier,
                        DeOd = odds.ToString("0.00"),
                        IsAc = selection.IsActive,
                        SoOr = selection.SortOrder
                    });
                }
                else
                {
                    result.Add(new OddsChangeMonitoringSelection
                    {
                        SeTeId = selection.SelectionTemplateId,
                        SeId = selection.SelectionId.ToString(),
                        PrSeId = selection.ProviderSelectionId,
                        SeNa = selection.SelectionName,
                        SeSp = selection.SelectionSpecifier,
                        DeOd = selection.Odds.ToString("0.00"),
                        IsAc = selection.IsActive,
                        SoOr = selection.SortOrder
                    });
                }
            }

            return result;
        }
        public List<MainMarketTemplateResponse> GetTopMarketTemplates(int sportId)
        {
            switch (sportId)
            {
                case 16: // American Football
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "219", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "223", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "225", Name = "", SortId = 3 } };
                case 36: // Athletics
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 } };
                case 13: // Aussie Rules
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "16", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 3 } };
                case 31: // Badminton
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "237", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "238", Name = "", SortId = 3 } };
                case 15: // Bandy
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 2 } };
                case 2: // Basketball
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "219", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "223", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "225", Name = "", SortId = 3 } };
                case 155: // Basketball 3x3
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "219", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "223", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "225", Name = "", SortId = 3 } };
                case 3: // Baseball
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "155", Name = "", SortId = 3 } };
                case 60: // Beach Soccer
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 1 } };
                case 34: // Beach Volley
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "238", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "237", Name = "", SortId = 3 } };
                case 32: // Bowls
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "314", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "188", Name = "", SortId = 3 } };
                case 10: // Boxing
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 3 } };
                case 21: // Cricket
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "340", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 2 } };
                case 17: // Cycling
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "539", Name = "", SortId = 1 } };
                case 22: // Darts
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "188", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "314", Name = "", SortId = 3 } };
                case 153: // eBasketball
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "219", Name = "", SortId = 1 } };
                case 137: // eSoccer
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "16", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 3 } };
                case 118: // eSport Call of Duty
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 } };
                case 109: // eSport CS GO
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "327", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "328", Name = "", SortId = 3 } };
                case 111: // eSport Dota 2
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "327", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "328", Name = "", SortId = 3 } };
                case 134: // eSport King of Glory
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 } };
                case 110: // eSport LOL
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "327", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "328", Name = "", SortId = 3 } };
                case 113: // eSport Hearthstone
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 } };
                case 121: // eSport Overwatch
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 } };
                case 128: // eSport Rocket League
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 } };
                case 125: // eSport Rainbow Six
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 } };
                case 112: // eSport StarCraft
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "327", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "328", Name = "", SortId = 3 } };
                case 7: // Floorball
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 2 } };
                case 40: // Formula 1
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "539", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "538", Name = "", SortId = 2 } };
                case 29: // Futsal
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 2 } };
                case 136: // Gaelic Hurling
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 } };
                case 9: // Golf
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "539", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "538", Name = "", SortId = 2 } };
                case 6: // Handball
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "16", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 3 } };
                case 4: // Ice Hockey
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "406", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "410", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "412", Name = "", SortId = 3 } };
                case 117: // MMA
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 3 } };
                case 61: // Pesapallo
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 } };
                case 12: // Rugby
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "16", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 3 } };
                case 19: // Snooker
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 2 } };
                case 1: // Soccer
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "16", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 3 } };
                case 37: // Squash
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "237", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "238", Name = "", SortId = 3 } };
                case 20: // Table Tennis
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "237", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "238", Name = "", SortId = 3 } };
                case 5: // Tennis
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "187", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "189", Name = "", SortId = 3 } };
                case 1003: // Virtual Baseball
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "251", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "256", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "258", Name = "", SortId = 3 } };
                case 1002: // Virtual Basketball
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "219", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "223", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "225", Name = "", SortId = 3 } };
                case 1058: // Virtual Dog Racing
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "866", Name = "", SortId = 1 } };
                case 1055: // Virtual Horse Racing
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "866", Name = "", SortId = 1 } };
                case 1001: // Virtual Soccer
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "16", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 3 } };
                case 1005: // Virtual Tennis
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "187", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "189", Name = "", SortId = 3 } };
                case 23: // Volleyball
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "186", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "237", Name = "", SortId = 2 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "238", Name = "", SortId = 3 } };
                case 26: // Waterpolo
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 2 } };
                case 138: // Kabaddi
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 },
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "16", Name = "", SortId = 2 } ,
                        new MainMarketTemplateResponse() { MainMarketTemplateId = "18", Name = "", SortId = 3 } };
                case 24: // Field Hockey
                    return new List<MainMarketTemplateResponse> { new MainMarketTemplateResponse() { MainMarketTemplateId = "1", Name = "", SortId = 1 } };
                default:
                    return new List<MainMarketTemplateResponse>();
            }
        }
    }
}
