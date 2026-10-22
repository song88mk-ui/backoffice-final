using Backoffice.Shared.Data.Sportsbook.Collections;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Response;
using OE.Shared.Data.Sportsbook.SQLEntities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Backoffice.API.Repositories
{
    // BO-42: shapes the fixtures, markets and selections read from the sportsbook Mongo into the lists the Back
    // Office pickers use (the Add Boosted Odds dialog and the Market and Message Settlement filters). It never
    // touches a database, so every rule can be unit tested. Ported from the Trading API (OE.API
    // SportsbookRepository.GetFixtures and GetOutrightFixtures), which is being retired from the Back Office.
    // Every name is English, the language the pickers show.
    public static class SportsbookFixtureBuilder
    {
        // A league without a tier order sorts after every ranked league.
        public const int UnrankedLeagueSortOrder = 1000;

        private static readonly string English = LanguageCode.EN.ToString();
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        // Match fixtures, earliest first, each with its markets and selections.
        public static List<MarketFixtureResponse.FixtureModel> BuildMatchFixtures(
            List<Fixture> fixtures,
            List<MarketFilter> markets,
            List<SelectionFilter> selections)
        {
            var marketsByFixture = markets.ToLookup(m => m.FiId);
            var selectionsByMarket = selections.ToLookup(s => (s.FiId, s.MaId));

            return fixtures
                .Select(f =>
                {
                    var fixture = ToFixture(f);
                    fixture.Markets = ToMarkets(marketsByFixture[f.FiId], selectionsByMarket, outright: false);
                    return fixture;
                })
                .OrderBy(f => f.FixtureStartDate)
                .ThenBy(f => f.FixtureName)
                .ToList();
        }

        // Outright fixtures in tier order of the requested sport (a league without one goes last), then by segment,
        // league, start date and name. A fixture that appears more than once is listed once. The tier order is
        // looked up for the requested sport only, so asking for all sports (0) ranks nothing, as the Trading API did.
        public static List<MarketFixtureResponse.FixtureModel> BuildOutrightFixtures(
            List<Fixture> fixtures,
            List<MarketFilter> markets,
            List<SelectionFilter> selections,
            List<TierLeagueOrder> tierOrder,
            int requestedSportId)
        {
            var ranks = tierOrder.Where(c => c.SportId == requestedSportId).ToList();
            var marketsByFixture = markets.ToLookup(m => m.FiId);
            var selectionsByMarket = selections.ToLookup(s => (s.FiId, s.MaId));

            var ordered = fixtures
                .Select(f =>
                {
                    var fixture = ToFixture(f);
                    var rank = ranks.Where(c => c.SegmentId == f.SegId && c.LeagueId == f.LeId).Select(c => c.SortOrder).FirstOrDefault();
                    fixture.SortOrder = rank == 0 ? UnrankedLeagueSortOrder : rank;
                    return (Fixture: fixture, Source: f);
                })
                .OrderBy(x => x.Fixture.SortOrder)
                .ThenBy(x => x.Fixture.SegmentName)
                .ThenBy(x => x.Fixture.LeagueName)
                .ThenBy(x => x.Fixture.FixtureStartDate)
                .ThenBy(x => x.Fixture.FixtureName)
                .ToList();

            var result = new List<MarketFixtureResponse.FixtureModel>();
            var seen = new HashSet<string>();

            foreach (var (fixture, source) in ordered)
            {
                if (!seen.Add(fixture.FixtureId))
                    continue;

                fixture.Markets = ToMarkets(marketsByFixture[source.FiId], selectionsByMarket, outright: true);
                result.Add(fixture);
            }

            return result;
        }

        private static MarketFixtureResponse.FixtureModel ToFixture(Fixture f) =>
            new MarketFixtureResponse.FixtureModel
            {
                SportId = f.SpId,
                SportName = Translate(f.SpNa, f.SpNaTr, t => t.LaCo, t => t.SpNa),
                SegmentId = f.SegId,
                SegmentName = Translate(f.SegNa, f.SegNaTr, t => t.LaCo, t => t.SegNa),
                LeagueId = f.LeId,
                LeagueName = Translate(f.LeNa, f.LeNaTr, t => t.LaCo, t => t.LeNa),
                ProviderFixtureId = f.PrFiId,
                FixtureId = f.FiId.ToString(Invariant),
                FixtureName = Translate(f.FiNa, f.FiNaTr, t => t.LaCo, t => t.FiNa),
                HomeName = TeamName(f, home: true),
                AwayName = TeamName(f, home: false),
                FixtureTypeId = f.FiTyId,
                FixtureStartDate = f.FiStDa,
                FixtureStatusId = f.FiStId,
                FixtureStatusName = f.FiStNa,
                Score = f.Sc,
                CurrentPeriod = PriceBoostListBuilder.ToShortPeriodName(f.CuPe),
                CurrentMinute = f.CuMi.ToString(Invariant)
            };

        // "Home vs. Away" split into its sides. Only matches have them.
        private static string TeamName(Fixture f, bool home)
        {
            if ((FixtureType)f.FiTyId != FixtureType.Match)
                return string.Empty;

            var sides = (Translate(f.FiNa, f.FiNaTr, t => t.LaCo, t => t.FiNa) ?? string.Empty).Split(" vs. ");

            return (home ? sides.First() : sides.Last()).Trim();
        }

        private static List<MarketFixtureResponse.MarketModel> ToMarkets(
            IEnumerable<MarketFilter> markets,
            ILookup<(long FiId, long MaId), SelectionFilter> selectionsByMarket,
            bool outright)
        {
            var result = new List<MarketFixtureResponse.MarketModel>();

            foreach (var market in markets)
            {
                var marketSelections = selectionsByMarket[(market.FiId, market.MaId)].ToList();

                result.Add(new MarketFixtureResponse.MarketModel
                {
                    MarketTemplateId = market.MaTeId,
                    MarketId = market.MaId.ToString(Invariant),
                    MarketName = Translate(market.MaNa, market.MaNaTr, t => t.LaCo, t => t.MaNa),
                    Line = market.Li,
                    MarketStatusId = market.MaStId,
                    MarketStatusName = market.MaStNa,
                    IsBoosted = market.IsBo,
                    MarketSpecifier = ToMarketSpecifier(marketSelections),
                    Selections = ToSelections(marketSelections, outright)
                });
            }

            return result.OrderBy(o => TemplateOrder(o.MarketTemplateId)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
        }

        // An outright market with more than three runners lists them by odds, a short one by their sort order. The
        // yes and no selections of an outright are numbered 1 and 2 like those of a match.
        private static List<MarketFixtureResponse.SelectionModel> ToSelections(List<SelectionFilter> selections, bool outright)
        {
            var ordered = outright && selections.Count > 3
                ? selections.OrderBy(s => s.Od).ToList()
                : selections.OrderBy(s => s.SeSoOr).ToList();

            return ordered.Select(s => new MarketFixtureResponse.SelectionModel
            {
                SelectionSpecifier = s.SeSp,
                SelectionTemplateId = outright ? OutrightTemplateId(s) : s.SeTeId,
                SelectionId = s.SeId.ToString(Invariant),
                SelectionName = Translate(s.SeNa, s.SeNaTr, t => t.LaCo, t => t.SeNa),
                DecimalOdds = s.Od.ToString(Invariant),
                IsActive = s.IsAc,
                CorrelationId = s.CoId,
                SortOrder = s.SeSoOr
            }).ToList();
        }

        private static string OutrightTemplateId(SelectionFilter s)
        {
            if (string.Equals(s.SeNa, "yes", StringComparison.OrdinalIgnoreCase))
                return "1";

            if (string.Equals(s.SeNa, "no", StringComparison.OrdinalIgnoreCase))
                return "2";

            return s.SeTeId;
        }

        // The line of a market, the part of a selection specifier after the "?".
        private static string ToMarketSpecifier(List<SelectionFilter> selections)
        {
            var specifier = selections.Select(s => s.SeSp).FirstOrDefault(sp => sp != null && sp.Contains('?'));

            return specifier == null ? string.Empty : specifier.Substring(specifier.LastIndexOf('?') + 1);
        }

        private static int TemplateOrder(string marketTemplateId) =>
            int.TryParse(marketTemplateId, NumberStyles.Integer, Invariant, out var order) ? order : int.MaxValue;

        // The English translation when the document has one, else the name it was published with.
        private static string Translate<T>(string name, List<T> translations, Func<T, string> languageCode, Func<T, string> translated) where T : class
        {
            var match = translations?.FirstOrDefault(t => languageCode(t) == English);

            return match != null ? translated(match) : name;
        }
    }
}
