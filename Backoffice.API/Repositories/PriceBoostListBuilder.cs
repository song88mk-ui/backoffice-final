using Backoffice.Shared.Data.Sportsbook.Collections;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Backoffice.API.Repositories
{
    // BO-42: builds the Manage Boost list from the saved boosts (Trading DB) and what the sportsbook Mongo holds
    // for their fixtures. It never touches a database, so every rule can be unit tested. Ported from the Trading
    // API (OE.API SportsbookRepository.GetPriceBoostConfig and its helpers), which is being retired from the
    // Back Office, so the results stay the same as the screen shows today.
    public static class PriceBoostListBuilder
    {
        // The markets of the boosted fixtures with the odds each client sees. The client odds come from the
        // margin the market is published with (ClCoMa, "CLIENT:margin|CLIENT:margin") versus the client's margin.
        public static List<MarketFixtureResponse.MarketModel> BuildMarkets(
            List<MarketFilter> markets,
            List<SelectionFilter> selections,
            List<string> clientCodes,
            ICollection<string> playerPropMarketTemplateIds)
        {
            var result = new List<MarketFixtureResponse.MarketModel>();

            foreach (var market in markets)
            {
                var clientOdds = new List<MarketFixtureResponse.ClientOddsModel>();
                var marketSelections = selections.Where(c => c.MaId == market.MaId).ToList();

                if (marketSelections.Count > 0)
                {
                    var activeSelections = marketSelections.Where(c => c.IsAc == true && c.Od > 0m).ToList();

                    // a market with a single active selection falls back to every selection that has odds
                    if (activeSelections.Count == 1)
                        activeSelections = marketSelections.Where(c => c.Od > 0m).ToList();

                    var margin = (Math.Round(activeSelections.Sum(s => 100 / s.Od)) - 100) / 100;

                    clientOdds = CalculateClientOdds(activeSelections.Count, margin, marketSelections, market.ClCoMa, clientCodes, market.MaTeId, playerPropMarketTemplateIds);
                }

                result.Add(new MarketFixtureResponse.MarketModel
                {
                    MarketTemplateId = market.MaTeId,
                    MarketId = market.MaId.ToString(),
                    MarketName = market.MaNa,
                    Line = market.Li,
                    MarketStatusId = market.MaStId,
                    MarketStatusName = market.MaStNa,
                    ClientCode = market.ClCo,
                    ClientCodeMargin = market.ClCoMa,
                    IsBoosted = market.IsBo,
                    Selections = marketSelections.Select(s => new MarketFixtureResponse.SelectionModel
                    {
                        SelectionTemplateId = s.SeTeId,
                        SelectionId = s.SeId.ToString(),
                        SelectionName = s.SeNa,
                        SelectionSpecifier = s.SeSp,
                        DecimalOdds = s.Od.ToString("0.00", CultureInfo.InvariantCulture),
                        IsActive = s.IsAc,
                        CorrelationId = s.CoId,
                        SortOrder = s.SeSoOr,
                        ClientOdds = clientOdds.Where(c => c.SelectionId == s.SeId).ToList()
                    }).OrderBy(o => o.SortOrder).ToList()
                });
            }

            return result.OrderBy(o => o.MarketTemplateId).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
        }

        public static List<MarketFixtureResponse.ClientOddsModel> CalculateClientOdds(
            int activeSelectionCount,
            decimal margin,
            List<SelectionFilter> selections,
            string clientMarketConfig,
            List<string> clientCodes,
            string marketTemplateId,
            ICollection<string> playerPropMarketTemplateIds)
        {
            var result = new List<MarketFixtureResponse.ClientOddsModel>();

            void Add(string clientCode, SelectionFilter selection, decimal odds) =>
                result.Add(new MarketFixtureResponse.ClientOddsModel
                {
                    ClientCode = clientCode,
                    Odds = odds,
                    SelectionTemplateId = selection.SeTeId,
                    SelectionId = selection.SeId
                });

            // A market published without client margins gives every client the provider odds: nothing in the v2
            // pipeline stamps ClCoMa yet, and with no odds the Add Boosted Odds dialog could not take a boost for the
            // client. A market that is stamped but does not name the client still leaves that client without odds.
            // To require the margins again, return the empty result here as before.
            if (string.IsNullOrEmpty(clientMarketConfig))
            {
                foreach (var clientCode in clientCodes)
                {
                    foreach (var selection in selections)
                        Add(clientCode, selection, selection.Od);
                }

                return result;
            }

            if (!clientCodes.Any(c => clientMarketConfig.Contains(c)))
                return result;

            var clients = clientMarketConfig.Split('|')
                .Select(s => new { Code = s.Split(':').First(), Margin = decimal.Parse(s.Split(':').Last(), CultureInfo.InvariantCulture) })
                .Where(c => clientCodes.Contains(c.Code))
                .ToList();

            foreach (var client in clients)
            {
                foreach (var selection in selections)
                {
                    if (!(selection.IsAc == true && selection.Od > 0m))
                    {
                        Add(client.Code, selection, selection.Od);
                        continue;
                    }

                    var targetMargin = client.Margin;

                    // The Trading API also required SeTeId != "12" || SeTeId != "13" here, which is always true.
                    if (playerPropMarketTemplateIds.Contains(marketTemplateId))
                    {
                        Add(client.Code, selection, Math.Round(selection.Od - (selection.Od * targetMargin), 2));
                        continue;
                    }

                    if (margin == targetMargin)
                    {
                        Add(client.Code, selection, selection.Od);
                        continue;
                    }

                    var fairOddsInitialDivideBy = activeSelectionCount - (margin * selection.Od);

                    if (fairOddsInitialDivideBy <= 0)
                    {
                        Add(client.Code, selection, MasterOddsWithAddedMargin(selection.Od));
                        continue;
                    }

                    var fairOdds = (activeSelectionCount * selection.Od) / fairOddsInitialDivideBy;

                    if (fairOdds <= 0)
                    {
                        Add(client.Code, selection, MasterOddsWithAddedMargin(selection.Od));
                        continue;
                    }

                    var finalOddsInitialDivideBy = ((fairOdds * targetMargin) / activeSelectionCount) + 1;

                    if (finalOddsInitialDivideBy <= 0)
                    {
                        Add(client.Code, selection, MasterOddsWithAddedMargin(selection.Od));
                        continue;
                    }

                    var odds = Math.Round(fairOdds / finalOddsInitialDivideBy, 2);

                    if (odds < 1.00m)
                    {
                        Add(client.Code, selection, MasterOddsWithAddedMargin(1m));
                        continue;
                    }

                    if (odds >= selection.Od)
                        odds = MasterOddsWithAddedMargin(selection.Od);

                    Add(client.Code, selection, odds);
                }
            }

            return result;
        }

        public static decimal MasterOddsWithAddedMargin(decimal odds)
        {
            if (Math.Round(odds, 2) <= 1.01m)
                return odds;

            return Math.Round(100 / ((100 / odds) + 2m), 2);
        }

        // Joins the saved boosts with the live data of their fixtures. A boost whose fixture is no longer in Mongo
        // (it ended or was removed) is left out and its provider fixture id is returned in endedProviderFixtureIds
        // so the caller can delete it. When the request names one full selection and nothing is saved for it yet,
        // an empty row (no boosted odds) is returned for each client, or for the requested client, to fill in.
        public static List<PriceBoostConfigModel> Build(
            List<PriceBoostConfigModel> priceBoost,
            PriceBoostConfigRequest request,
            List<PlatformClientResponse> clients,
            List<Fixture> fixtures,
            List<SelectionFilter> selections,
            List<MarketFixtureResponse.MarketModel> markets,
            out List<string> endedProviderFixtureIds)
        {
            var rows = priceBoost?.ToList() ?? new List<PriceBoostConfigModel>();
            endedProviderFixtureIds = new List<string>();

            if (!string.IsNullOrEmpty(request.SportId) && !string.IsNullOrEmpty(request.SegmentId)
                && !string.IsNullOrEmpty(request.LeagueId) && !string.IsNullOrEmpty(request.ProviderFixtureId)
                && !string.IsNullOrEmpty(request.MarketTemplateId) && !string.IsNullOrEmpty(request.SelectionTemplateId))
            {
                if (string.IsNullOrEmpty(request.ClientCode))
                {
                    foreach (var client in clients)
                    {
                        if (!rows.Any(c => c.ClientCode == client.ClientCode))
                            rows.Add(EmptyRow(request, client.ClientCode));
                    }
                }
                else if (!rows.Any())
                {
                    rows.Add(EmptyRow(request, request.ClientCode));
                }
            }

            var languages = new LanguagesModel();
            var result = new List<PriceBoostConfigModel>();

            foreach (var price in rows)
            {
                var client = clients.FirstOrDefault(c => c.ClientCode == price.ClientCode);
                if (client == null)
                    continue;

                price.ClientId = client.ClientId.ToString();
                price.ClientName = client.ClientName;

                var fixture = fixtures.FirstOrDefault(f => f.PrFiId == price.ProviderFixtureId);
                if (fixture == null)
                {
                    endedProviderFixtureIds.Add(price.ProviderFixtureId);
                    continue;
                }

                if (fixture.FiStId > (int)FixtureStatus.Live)
                    continue;

                price.SportId = fixture.SpId;
                price.SportName = fixture.SpNa;
                price.SegmentName = fixture.SegNa;
                price.LeagueName = fixture.LeNa;
                price.FixtureName = fixture.FiNa;
                price.FixtureStatusId = fixture.FiStId;
                price.CurrentPeriod = ToShortPeriodName(fixture.CuPe);
                price.CurrentMinute = fixture.CuMi.ToString();
                price.Score = fixture.Sc;
                price.FixtureStartDate = fixture.FiStDa;
                price.FixtureTypeId = fixture.FiTyId;

                var selection = selections.FirstOrDefault(w => w.FiId == fixture.FiId && w.MaTeId == price.MarketTemplateId && w.SeTeId == price.SelectionTemplateId && w.SeSp == price.SelectionSpecifier)
                    ?? selections.FirstOrDefault(w => w.FiId == fixture.FiId && w.MaTeId == price.MarketTemplateId && w.SeTeId == price.SelectionTemplateId && (w.SeSp ?? string.Empty).Replace("uof:1", "uof:3") == price.SelectionSpecifier);

                if (selection == null)
                    continue;

                price.SelectionName = selection.SeNa;

                if (string.IsNullOrEmpty(price.BoostedSelectionName))
                {
                    price.BoostedSelectionName = price.SelectionName;

                    if (selection.SeNaTr != null && selection.SeNaTr.Any())
                        price.BoostedSelectionTranslations = selection.SeNaTr.Select(s => new BoostedSelectionTranslation
                        {
                            BoostedSelectionName = s.SeNa,
                            ClientCode = price.ClientCode,
                            Language = new Language
                            {
                                LanguageCode = s.LaCo,
                                LanguageName = languages.Languages.FirstOrDefault(f => f.LanguageCode == s.LaCo)?.LanguageName
                            },
                        }).ToList();
                }

                var market = markets.FirstOrDefault(m => m.MarketId == selection.MaId.ToString() && m.MarketTemplateId == price.MarketTemplateId);
                if (market == null)
                    continue;

                price.MarketName = market.MarketName;
                price.BROdds = selection.Od;

                var marketSelection = market.Selections.FirstOrDefault(s => s.SelectionId == selection.SeId.ToString());

                if (price.SportId > 1000 || price.FixtureTypeId == (int)FixtureType.Outright)
                {
                    price.ClientOdds = selection.Od;
                }
                else if (marketSelection?.ClientOdds != null)
                {
                    var clientOdds = marketSelection.ClientOdds.FirstOrDefault(c => c.ClientCode == client.ClientCode && c.SelectionTemplateId == marketSelection.SelectionTemplateId);
                    if (clientOdds != null)
                        price.ClientOdds = clientOdds.Odds;
                }

                result.Add(price);
            }

            return result.OrderByDescending(o => o.FixtureStartDate).ThenBy(o => o.ClientCode).ToList();
        }

        private static PriceBoostConfigModel EmptyRow(PriceBoostConfigRequest request, string clientCode) =>
            new PriceBoostConfigModel
            {
                ClientCode = clientCode,
                SportId = Convert.ToInt32(request.SportId),
                SegmentId = Convert.ToInt32(request.SegmentId),
                LeagueId = Convert.ToInt32(request.LeagueId),
                ProviderFixtureId = request.ProviderFixtureId,
                MarketTemplateId = request.MarketTemplateId,
                SelectionTemplateId = request.SelectionTemplateId,
                SelectionSpecifier = request.SelectionSpecifier,
                MarketSpecifier = request.MarketSpecifier,
                BoostedOdds = 0,
                BoostedOddsStr = string.Empty,
            };

        // Outright fixtures carry yes/no selections by name, so their template id is derived from the name
        // ("yes" is 1, "no" is 2) before the selections are matched to the saved boosts.
        public static void NormalizeOutrightSelections(List<Fixture> fixtures, List<SelectionFilter> selections)
        {
            foreach (var selection in selections)
            {
                var fixture = fixtures.FirstOrDefault(f => f.FiId == selection.FiId);
                if (fixture == null || fixture.FiTyId != (int)FixtureType.Outright)
                    continue;

                if (string.Equals(selection.SeNa, "yes", StringComparison.OrdinalIgnoreCase))
                    selection.SeTeId = "1";
                else if (string.Equals(selection.SeNa, "no", StringComparison.OrdinalIgnoreCase))
                    selection.SeTeId = "2";
            }
        }

        // The short label the fixture's current period is shown with in the list.
        public static string ToShortPeriodName(int currentPeriod) => currentPeriod switch
        {
            1 => "1P",
            2 => "2P",
            3 => "3P",
            4 => "4P",
            5 => "5P",
            6 => "1H",
            7 => "2H",
            8 => "1S",
            9 => "2S",
            10 => "3S",
            11 => "4S",
            12 => "5S",
            13 => "1Q",
            14 => "2Q",
            15 => "3Q",
            16 => "4Q",
            17 => "Golden set",
            20 => "Started",
            21 => "In progress",
            22 => "About to start",
            30 => "Break",
            31 => "Halftime",
            32 => "Awaiting extra time",
            33 => "Extra time halftime",
            34 or 35 or 36 => "Awaiting penalties",
            37 => "Awaiting golden set",
            40 => "Overtime",
            41 => "1E",
            42 => "2E",
            50 or 51 or 52 => "Penalties",
            60 => "Postponed",
            61 => "Start delayed",
            70 => "Cancelled",
            71 => "G1",
            72 => "G2",
            73 => "G3",
            74 => "G4",
            75 => "G5",
            76 => "G6",
            77 => "G7",
            80 => "Interrupted",
            81 => "Suspended",
            90 => "Abandoned",
            91 => "Walkover",
            92 => "Retired",
            93 => "Walkover, player 1 won",
            94 => "Walkover, player 2 won",
            95 => "Player 1 retired, player 2 won",
            96 => "Player 2 retired, player 1 won",
            97 => "Player 1 defaulted, player 2 won",
            98 => "Player 2 defaulted, player 1 won",
            99 => "Only Result",
            100 => "Ended",
            110 => "Awaiting extra time",
            111 => "Awaiting over time",
            120 => "Awaiting Penalties",
            130 => "After golden set",
            141 => "1st map",
            142 => "2nd map",
            143 => "3rd map",
            144 => "4th map",
            145 => "5th map",
            146 => "6th map",
            147 => "7th map",
            151 => "1st Game",
            152 => "2nd Game",
            153 => "3rd Game",
            154 => "4th Game",
            155 => "5th Game",
            161 => "1st end",
            162 => "2nd end",
            163 => "3rd end",
            164 => "4th end",
            165 => "5th end",
            166 => "6th end",
            167 => "7th end",
            168 => "8th end",
            169 => "9th end",
            170 => "10th end",
            171 => "Extra end",
            301 => "First break",
            302 => "Second break",
            303 => "Third break",
            304 => "Fourth break",
            305 => "Fifth break",
            306 => "Sixth break",
            401 => "1st inning top",
            402 => "1st inning bottom",
            403 => "2nd inning top",
            404 => "2nd inning bottom",
            405 => "3rd inning top",
            406 => "3rd inning bottom",
            407 => "4th inning top",
            408 => "4th inning bottom",
            409 => "5th inning top",
            410 => "5th inning bottom",
            411 => "6th inning top",
            412 => "6th inning bottom",
            413 => "7th inning top",
            414 => "7th inning bottom",
            415 => "8th inning top",
            416 => "8th inning bottom",
            417 => "9th inning top",
            418 => "9th inning bottom",
            419 => "Extra inning top",
            420 => "Extra inning bottom",
            421 => "Break top1-bottom1",
            422 => "Break top2-bottom1",
            423 => "Break top2-bottom2",
            424 => "Break top3-bottom2",
            425 => "Break top3-bottom3",
            426 => "Break top4-bottom3",
            427 => "Break top4-bottom4",
            428 => "Break top5-bottom4",
            429 => "Break top5-bottom5",
            430 => "Break top6-bottom5",
            431 => "Break top6-bottom6",
            432 => "Break top7-bottom6",
            433 => "Break top7-bottom7",
            434 => "Break top8-bottom7",
            435 => "Break top8-bottom8",
            436 => "Break top9-bottom8",
            437 => "Break top9-bottom9",
            438 => "Break topEI-bottom9",
            439 => "Break topEI-bottomEI",
            440 => "Sudden death",
            441 => "6th set",
            442 => "7th set",
            443 => "Awaiting sudden death",
            444 => "After sudden death",
            445 => "Break",
            501 => "First innings, home team",
            502 => "First innings, away team",
            503 => "Second innings, home team",
            504 => "Second innings, away team",
            505 => "Awaiting super over",
            506 => "Super over, home team",
            507 => "Super over, away team",
            508 => "After super over",
            509 => "Innings break",
            510 => "Super over break",
            511 => "Lunch break",
            512 => "Tea break",
            513 => "Stumps",
            514 => "8th set",
            515 => "9th set",
            516 => "10th set",
            517 => "11th set",
            518 => "12th set",
            519 => "13th set",
            520 => "Third innings, home team",
            521 => "Third innings, away team",
            522 => "Fourth innings, home team",
            523 => "Fourth innings, away team",
            524 => "Dinner Break",
            525 => "Drinks",
            526 => "Super over",
            531 => "1st inning",
            532 => "2nd inning",
            533 => "3rd inning",
            534 => "4th inning",
            535 => "5th inning",
            536 => "6th inning",
            537 => "7th inning",
            538 => "8th inning",
            539 => "9th inning",
            _ => string.Empty
        };
    }
}
