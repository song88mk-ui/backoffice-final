using OE.Shared.Data.Sportsbook.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using static Backoffice.Shared.Models.Response.LeagueLookupResponseV2;

namespace Backoffice.API.Repositories
{
    // BO-144: shapes the sportsbook Mongo lookups (LeagueLookup, SportLookup, SegmentLookup) into the league list the
    // OE Config page reads. It used to come from the Trading API. Pure, so the rules are unit tested without Mongo.
    public static class LeagueLookupBuilder
    {
        // "1,2" gives [1, 2]. An empty text means every sport and gives an empty list. A part that is not a number is skipped.
        public static List<int> ParseSportIds(string sportIds) =>
            (sportIds ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var id) ? (int?)id : null)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

        // A league whose sport or segment has no lookup row keeps an empty name instead of being dropped.
        public static List<LeagueLookupModelV2> Build(IEnumerable<LeagueLookup> leagues, IEnumerable<SportLookup> sports, IEnumerable<SegmentLookup> segments)
        {
            var sportNames = sports
                .GroupBy(s => s.SpId)
                .ToDictionary(g => g.Key, g => g.First().SpNa);

            var segmentNames = segments
                .GroupBy(s => (s.SpId, s.SegId))
                .ToDictionary(g => g.Key, g => g.First().SegNa);

            return leagues
                .Select(l => new LeagueLookupModelV2
                {
                    SportId = l.SpId,
                    SportName = sportNames.GetValueOrDefault(l.SpId) ?? string.Empty,
                    SegmentId = l.SegId,
                    SegmentName = segmentNames.GetValueOrDefault((l.SpId, l.SegId)) ?? string.Empty,
                    LeagueId = l.LeId,
                    LeagueName = l.LeNa ?? string.Empty
                })
                .OrderBy(l => l.SportId)
                .ThenBy(l => l.SegmentId)
                .ThenBy(l => l.LeagueName)
                .ToList();
        }
    }
}
