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
using System.Linq;
using System.Threading.Tasks;
using static Backoffice.Shared.Models.Response.FastOddsFixturesResponse;

namespace Backoffice.WebSocket.Service.Services
{
    public class SportsbookService : ISportsbookService
    {

        private readonly ILogger<SportsbookService> _logger;
        private readonly ISportsbookRepository _sportsbookRepository;
        private readonly IMemCache _memCache;

        private readonly string HubPublishOddsChange;
        private readonly List<string> _asianClientCodes = new List<string>();
        private readonly List<string> _marketTemplateIds = new List<string> { "16", "18", "66", "68" };

        public SportsbookService(ILogger<SportsbookService> logger, IConfiguration configuration, IMemCache memCache, ISportsbookRepository sportsbookRepository)
        {
            _logger = logger;
            _sportsbookRepository = sportsbookRepository;
            _memCache = memCache;

            HubPublishOddsChange = "PublishOddsChange";
            _asianClientCodes = configuration["Custom:AsianClientCodes"].ToString().Split(',').ToList();
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

                await PublishToWebSocket(HubPublishOddsChange, hubConnection, payload);
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

                await PublishToWebSocket(HubPublishOddsChange, hubConnection, payload);
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

                await PublishToWebSocket("PublishBetStopFixture", hubConnection, payload);
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

                await PublishToWebSocket("PublishBetStopFixture", hubConnection, payload);
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
                        //if (string.IsNullOrEmpty(market.ClientCodes) || string.IsNullOrEmpty(market.ClientCodes))
                        //    continue;

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

                            clientOdds = odds.GroupBy(c => c.ClientCode).ToDictionary(s => s.Key, s => s.ToList());
                        }

                        try
                        {
                            markets.Add(new OddsChangeMarket()
                            {
                                MarketTemplateId = market.MarketTemplateId,
                                MarketId = market.MarketId.ToString(),
                                ProviderMarketId = market.ProviderMarketId,
                                MarketName = market.MarketName,
                                Line = market.Line,
                                MarketStatusId = (int)market.MarketStatus,
                                MarketStatusName = market.MarketStatus.ToString(),
                                MarketSpecifier = market.MarketSpecifier,
                                ClientCode = market.ClientCodes,
                                ClientCodeMargin = market.ClientCodeMargin,
                                ClientCodeMargins = !string.IsNullOrEmpty(market.ClientCodeMargin) ? market.ClientCodeMargin.Split("|").ToDictionary(s => s.Split(":").FirstOrDefault(), s => Convert.ToDecimal(s.Split(":").LastOrDefault())) : new Dictionary<string, decimal>(),
                                SortOrder = market.SortOrder,
                                UnixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                                Selections = market.Selections.Select(selection => new OddsChangeSelection()
                                {
                                    SelectionId = selection.SelectionId,
                                    SelectionTemplateId = selection.SelectionTemplateId,
                                    SelectionName = selection.SelectionName,
                                    //SelectionSpecifier = selection.SelectionSpecifier,
                                    DecimalOdds = selection.Odds,
                                    IsActive = selection.IsActive,
                                    SortOrder = selection.SortOrder,
                                    UnixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                }).ToList(),
                                ClientSelections = clientOdds.Select(odds => new OddsChangeClientSelections()
                                {
                                    ClientCode = odds.Key,
                                    ClientMargin = odds.Value.FirstOrDefault().ClientMargin,
                                    Selections = odds.Value.Select(oddsValue => new OddsChangeSelection()
                                    {
                                        SelectionId = oddsValue.SelectionId,
                                        SelectionTemplateId = oddsValue.SelectionTemplateId,
                                        SelectionName = oddsValue.SelectionName,
                                        //SelectionSpecifier = oddsValue.SelectionSpecifier,
                                        DecimalOdds = oddsValue.Odds,
                                        IsActive = oddsValue.IsActive,
                                        SortOrder = oddsValue.SortOrder,
                                        UnixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
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
                    SportId = message.SportId,
                    SportName = message.SportName,
                    SegmentId = message.SegmentId,
                    SegmentName = message.SegmentName,
                    LeagueId = message.LeagueId,
                    LeagueName = message.LeagueName,
                    ProviderFixtureId = message.ProviderFixtureId,
                    FixtureId = message.FixtureId.ToString(),
                    FixtureName = message.FixtureName,
                    HomeName = GetHomeName(message.FixtureName, (FixtureType)message.FixtureTypeId),
                    AwayName = GetAwayName(message.FixtureName, (FixtureType)message.FixtureTypeId),
                    FixtureStartDate = message.FixtureStartDate,
                    IsSuspended = message.IsSuspended,
                    FixtureStatusId = message.FixtureStatusId,
                    FixtureStatusName = message.FixtureStatusName,
                    Score = message.Score,
                    CurrentPeriod = message.CurrentPeriod,
                    CurrentMinute = message.CurrentMinute,
                    SortOrder = message.SortOrder,
                    FixtureTypeId = message.FixtureTypeId,
                    UnixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Markets = markets
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
                            ClientCode = client.Item1,
                            Odds = selection.Odds,
                            SelectionTemplateId = selection.SelectionTemplateId,
                            SelectionId = selection.SelectionId,
                            IsActive = selection.IsActive,
                            SelectionName = selection.SelectionName,
                            SortOrder = selection.SortOrder,
                            ClientMargin = client.ToString()
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
                                    ClientCode = client.Item1,
                                    Odds = tempOdds,
                                    SelectionTemplateId = selection.SelectionTemplateId,
                                    SelectionId = selection.SelectionId,
                                    IsActive = selection.IsActive,
                                    SelectionName = selection.SelectionName,
                                    SortOrder = selection.SortOrder,
                                    ClientMargin = client.ToString()
                                });

                                continue;
                            }
                        }

                        if (margin == targetMargin)
                        {
                            result.Add(new OddsChangeClientOdds
                            {
                                ClientCode = client.Item1,
                                Odds = selection.Odds,
                                SelectionTemplateId = selection.SelectionTemplateId,
                                SelectionId = selection.SelectionId,
                                IsActive = selection.IsActive,
                                SelectionName = selection.SelectionName,
                                SortOrder = selection.SortOrder,
                                ClientMargin = client.ToString()
                            });

                            continue;
                        }

                        var fairOddsInitialDivideBy = activeSelectionCount - (margin * selection.Odds);

                        if (fairOddsInitialDivideBy <= 0)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Odds);

                            result.Add(new OddsChangeClientOdds
                            {
                                ClientCode = client.Item1,
                                Odds = tempOdds,
                                SelectionTemplateId = selection.SelectionTemplateId,
                                SelectionId = selection.SelectionId,
                                IsActive = selection.IsActive,
                                SelectionName = selection.SelectionName,
                                SortOrder = selection.SortOrder,
                                ClientMargin = client.ToString()
                            });

                            continue;
                        }

                        var fairOdds = (activeSelectionCount * selection.Odds) / fairOddsInitialDivideBy;

                        if (fairOdds <= 0)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Odds);

                            result.Add(new OddsChangeClientOdds
                            {
                                ClientCode = client.Item1,
                                Odds = tempOdds,
                                SelectionTemplateId = selection.SelectionTemplateId,
                                SelectionId = selection.SelectionId,
                                IsActive = selection.IsActive,
                                SelectionName = selection.SelectionName,
                                SortOrder = selection.SortOrder,
                                ClientMargin = client.ToString()
                            });

                            continue;
                        }

                        var finalOddsInitialDivideBy = ((fairOdds * targetMargin) / activeSelectionCount) + 1;

                        if (finalOddsInitialDivideBy <= 0)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Odds);

                            result.Add(new OddsChangeClientOdds
                            {
                                ClientCode = client.Item1,
                                Odds = tempOdds,
                                SelectionTemplateId = selection.SelectionTemplateId,
                                SelectionId = selection.SelectionId,
                                IsActive = selection.IsActive,
                                SelectionName = selection.SelectionName,
                                SortOrder = selection.SortOrder,
                                ClientMargin = client.ToString()
                            });

                            continue;
                        }

                        var odds = Math.Round(fairOdds / finalOddsInitialDivideBy, 2);

                        if (odds < 1.00m)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(1m);

                            result.Add(new OddsChangeClientOdds
                            {
                                ClientCode = client.Item1,
                                Odds = tempOdds,
                                SelectionTemplateId = selection.SelectionTemplateId,
                                SelectionId = selection.SelectionId,
                                IsActive = selection.IsActive,
                                SelectionName = selection.SelectionName,
                                SortOrder = selection.SortOrder,
                                ClientMargin = client.ToString()
                            });

                            continue;
                        }

                        if (odds >= selection.Odds)
                            odds = GetMasterOddsWithAddedMargin(selection.Odds);

                        result.Add(new OddsChangeClientOdds
                        {
                            ClientCode = client.Item1,
                            Odds = odds,
                            SelectionTemplateId = selection.SelectionTemplateId,
                            SelectionId = selection.SelectionId,
                            IsActive = selection.IsActive,
                            SelectionName = selection.SelectionName,
                            SortOrder = selection.SortOrder,
                            ClientMargin = client.ToString()
                        });
                    }
                    else
                    {
                        result.Add(new OddsChangeClientOdds
                        {
                            ClientCode = client.Item1,
                            Odds = selection.Odds,
                            SelectionTemplateId = selection.SelectionTemplateId,
                            SelectionId = selection.SelectionId,
                            IsActive = selection.IsActive,
                            SelectionName = selection.SelectionName,
                            SortOrder = selection.SortOrder,
                            ClientMargin = client.ToString()
                        });
                    }
                }
            }

            return result;
        }
        private List<MarketTemplatePlayerPropsResponse> GetMarketTemplatePlayerProps() => _memCache.GetCache($"mtpp", async () => (await _sportsbookRepository.GetMarketTemplatePlayerProps()).Result.ToList(), (int)(DateTimeOffset.UtcNow - DateTimeOffset.UtcNow.AddYears(1)).TotalHours).Result;
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
            var result = new List<string>();

            result.Add($"leagues:{message.LeagueId}:fixtures");

            return result;
        }
        private List<string> GetGroupForMonitoringDetailsMarkets(OddsChangeFixtureMessage message)
        {
            var result = new List<string>();

            result.Add($"fixturedetails:{message.FixtureId}");

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
