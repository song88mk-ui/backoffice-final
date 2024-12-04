using Backoffice.Shared.Enums;
using Backoffice.Shared.IncomingSbEngineMessages;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.HubMessages;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingWebSocketMessages;
using Backoffice.Shared.Utilities;
using Backoffice.WebSocket.Service.Repositories;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Backoffice.WebSocket.Service.Services
{
    public class SportsbookService : ISportsbookService
    {

        private readonly ILogger<SportsbookService> _logger;
        private readonly ISportsbookRepository _sportsbookRepository;
        private readonly IMemCache _memCache;

        private readonly List<string> _asianClientCodes = new List<string>();
        private readonly List<string> _marketTemplateIds = new List<string> { "16", "18", "66", "68" };

        private const string HUB_PUBLISH_ODDSCHANGE = "PublishOddsChange";
        private const string HUB_PUBLISH_BETSTOPFIXTURE = "PublishBetStopFixture";

        public SportsbookService(ILogger<SportsbookService> logger, IConfiguration configuration, IMemCache memCache, ISportsbookRepository sportsbookRepository)
        {
            _logger = logger;
            _sportsbookRepository = sportsbookRepository;
            _memCache = memCache;

            _asianClientCodes = configuration["Custom:AsianClientCodes"].ToString().Split(',').ToList();
        }
        public async Task ProcessMarketTemplatePlayerProps()
        {
            try
            {
                var result = await _sportsbookRepository.GetMarketTemplatePlayerProps();

                var key = $"mtpp";

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
                var fixture = await ConvertToOddsChangeFixture(message);

                if (fixture == null)
                    return;

                var payload = new OutgoingOddsChangeMessage
                {
                    Gr = GetGroupForMonitoringMarkets(fixture),
                    Pa = fixture
                };

                await PublishToWebSocket(HUB_PUBLISH_ODDSCHANGE, hubConnection, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} - {ex.StackTrace}");
            }
        }

        public async Task ProcessOnMonitoringDetailsOddsChange(IncomingOddsChangeMessage message, HubConnection hubConnection)
        {
            try
            {
                var fixture = await ConvertToOddsChangeFixture(message);

                if (fixture == null)
                    return;

                var payload = new OutgoingOddsChangeMessage
                {
                    Gr = GetGroupForMonitoringDetailsMarkets(fixture),
                    Pa = fixture
                };

                await PublishToWebSocket(HUB_PUBLISH_ODDSCHANGE, hubConnection, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} - {ex.StackTrace}");
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
                _logger.LogError($"{ex.Message} - {ex.StackTrace}");
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
                _logger.LogError($"{ex.Message} - {ex.StackTrace}");
            }
        }

        public async Task ProcessNotification(NotificationsDetailsModel message, HubConnection hubConnection)
        {

            var date = DateTime.UtcNow.ToString();
            var payload = new NotificationsModel
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
            };


            await PublishToWebSocket("Notification", hubConnection, payload);
        }

        private async Task<OddsChangeFixtureMessage> ConvertToOddsChangeFixture(IncomingOddsChangeMessage message)
        {
            try
            {
                var markets = new List<OddsChangeMarket>();

                if (message.Markets != null)
                {
                    var marketTemplateGroup = GetMarketTemplatePlayerProps();

                    foreach (var market in message.Markets)
                    {
                        var selections = new List<OddsChangeSelection>();
                        var clientOdds = new Dictionary<string, List<OddsChangeClientOdds>>();
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
                            markets.Add(new OddsChangeMarket()
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
                                Se = market.Selections.Select(selection => new OddsChangeSelection()
                                {
                                    SeId = selection.SelectionId,
                                    SeTeId = selection.SelectionTemplateId,
                                    SeNa = selection.SelectionName,
                                    DecOd = selection.Odds,
                                    IsAc = selection.IsActive,
                                    SoOr = selection.SortOrder,
                                    UnTi = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                }).ToList(),
                                ClSe = clientOdds.Select(odds => new OddsChangeClientSelections()
                                {
                                    ClCo = odds.Key,
                                    ClMa = odds.Value.FirstOrDefault().ClCoMa,
                                    Se = odds.Value.Select(oddsValue => new OddsChangeSelection()
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

                return new OddsChangeFixtureMessage()
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
                _logger.LogError($"{ex.Message} - {ex.StackTrace}");
                return null;
            }
        }

        private List<OddsChangeClientOdds> GetClientOdds(int sportId, int activeSelectionCount, decimal margin, List<IncomingSelectionMessage> selections, string clientMarketConfig, string marketTemplateId, List<MarketTemplatePlayerPropsResponse> marketTemplateGroups)
        {
            if (string.IsNullOrEmpty(clientMarketConfig))
                return new List<OddsChangeClientOdds>();

            var clients = clientMarketConfig.Split('|').Select(s => new Tuple<string, decimal>(s.Split(':').First(), Convert.ToDecimal(s.Split(':').Last()))).ToList();

            var result = new List<OddsChangeClientOdds>();
            var isBinary = selections.Count > 2 ? false : true;

            var playerPropMarketTemplateIds = string.Join(",", marketTemplateGroups.Select(s => s.MarketTemplates).ToList()).Split(",").ToList();

            foreach (var client in clients)
            {
                foreach (var selection in selections)
                {
                    if (sportId == 1 && _marketTemplateIds.Contains(marketTemplateId) && _asianClientCodes.Contains(client.Item1, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Add(new OddsChangeClientOdds()
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

                                result.Add(new OddsChangeClientOdds
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
                            result.Add(new OddsChangeClientOdds
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

                            result.Add(new OddsChangeClientOdds
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

                            result.Add(new OddsChangeClientOdds
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

                            result.Add(new OddsChangeClientOdds
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

                            result.Add(new OddsChangeClientOdds
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

                        result.Add(new OddsChangeClientOdds
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
                        result.Add(new OddsChangeClientOdds
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

        private static decimal GetMasterOddsWithAddedMargin(decimal odds)
        {
            if (Math.Round(odds, 2) <= 1.01m)
                return odds;

            return Math.Round(100 / ((100 / odds) + 2m), 2);
        }

        private static string GetHomeName(string name, FixtureType fixtureType) => fixtureType == FixtureType.Match ? name.Split(" vs. ").First().Trim() : string.Empty;

        private static string GetAwayName(string name, FixtureType fixtureType) => fixtureType == FixtureType.Match ? name.Split(" vs. ").Last().Trim() : string.Empty;

        private async Task PublishToWebSocket<T>(string method, HubConnection hubConnection, T payload)
        {
            try
            {
                if (hubConnection.State != HubConnectionState.Connected)
                {
                    await hubConnection.StartAsync();
                }

                await hubConnection.SendAsync(method, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
            }
        }

        private List<string> GetGroupForMonitoringMarkets(OddsChangeFixtureMessage message)
        {
            var result = new List<string>
            {
                $"leagues:{message.LeId}:fixtures"
            };

            return result;
        }

        private List<string> GetGroupForMonitoringDetailsMarkets(OddsChangeFixtureMessage message)
        {
            var result = new List<string>
            {
                $"fixturedetails:{message.FiId}"
            };

            return result;
        }

        private List<string> GetGroupForBetStopFixture(IncomingBetStopFixtureMessage message)
        {
            var result = new List<string>();

            result.Add($"leagues:{message.LeagueId}:fixtures");
            result.Add($"fixturedetails:{message.FixtureId}");

            return result;
        }
    }
}
