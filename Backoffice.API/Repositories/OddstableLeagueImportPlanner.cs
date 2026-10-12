using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Collections.Generic;
using System.Linq;
using static Backoffice.Shared.Models.Response.LeagueLookupResponseV2;

namespace Backoffice.API.Repositories
{
    // BO-122: decides what a League Sorting import file does to one client's order for one sport. It never
    // touches the database, so every rule can be unit tested. A file row matches a league of the sport's
    // lookup by (SegmentId, LeagueId); the names in the file are ignored.
    //
    // Merge rule: the matched leagues take the first positions, in the file's Sort Order. The leagues the
    // client already has that are not in the file follow, in their current order. Positions are numbered
    // from 1 again.
    public static class OddstableLeagueImportPlanner
    {
        // The same cap the Edit dialog enforces (LeagueSortingHelper.MaxLeaguesPerClient in Backoffice.Web).
        public const int MaxLeaguesPerClient = 50;

        public class ImportPlan
        {
            // Problems that stop the whole import.
            public List<string> Errors { get; } = new List<string>();
            public List<ImportOddstableLeagueRowResult> Rows { get; } = new List<ImportOddstableLeagueRowResult>();
            // The order the client ends up with. Only meaningful when there are no Errors.
            public List<OddstableLeagueSortOrder> Result { get; } = new List<OddstableLeagueSortOrder>();
            public int RetainedCount { get; set; }
            public int MatchedCount => Rows.Count(r => r.Matched);
            public int UnmatchedCount => Rows.Count(r => !r.Matched);
        }

        public static ImportPlan Build(
            int sportId,
            string sportName,
            IReadOnlyCollection<OddstableLeagueSortOrder> current,
            IReadOnlyCollection<LeagueLookupModelV2> catalog,
            IReadOnlyCollection<ImportOddstableLeagueRow> rows)
        {
            var plan = new ImportPlan();

            if (rows.Count == 0)
            {
                plan.Errors.Add("The file has no league rows.");
                return plan;
            }

            var catalogByKey = catalog
                .GroupBy(c => (c.SegmentId, c.LeagueId))
                .ToDictionary(g => g.Key, g => g.First());

            var firstRowOfKey = new Dictionary<(int SegmentId, int LeagueId), int>();
            var matched = new List<(ImportOddstableLeagueRow Row, LeagueLookupModelV2 League)>();

            foreach (var row in rows)
            {
                if (row.SortOrder <= 0)
                    plan.Errors.Add($"Row {row.RowNo}: Sort Order must be a whole number greater than 0.");

                var key = (row.SegmentId, row.LeagueId);

                if (firstRowOfKey.TryGetValue(key, out var firstRowNo))
                {
                    plan.Errors.Add($"Row {row.RowNo}: league {row.LeagueId} is already listed in row {firstRowNo}.");
                    continue;
                }

                firstRowOfKey[key] = row.RowNo;

                var result = new ImportOddstableLeagueRowResult
                {
                    RowNo = row.RowNo,
                    SegmentId = row.SegmentId,
                    LeagueId = row.LeagueId,
                    SortOrder = row.SortOrder
                };

                if (catalogByKey.TryGetValue(key, out var league))
                {
                    result.Matched = true;
                    result.SegmentName = league.SegmentName;
                    result.LeagueName = league.LeagueName;
                    matched.Add((row, league));
                }
                else
                {
                    result.Reason = catalog.Any(c => c.LeagueId == row.LeagueId)
                        ? "The league belongs to a different segment."
                        : "The league is not available for this sport.";
                }

                plan.Rows.Add(result);
            }

            if (plan.Errors.Any())
                return plan;

            if (!matched.Any())
            {
                plan.Errors.Add("None of the leagues in the file is available for this sport, so there is nothing to import.");
                return plan;
            }

            var importedKeys = matched.Select(m => (m.Row.SegmentId, m.Row.LeagueId)).ToHashSet();

            var imported = matched
                .OrderBy(m => m.Row.SortOrder)
                .ThenBy(m => m.Row.RowNo)
                .Select(m => new OddstableLeagueSortOrder
                {
                    SportId = sportId,
                    SportName = sportName,
                    SegmentId = m.League.SegmentId,
                    SegmentName = m.League.SegmentName,
                    LeagueId = m.League.LeagueId,
                    LeagueName = m.League.LeagueName
                });

            var retained = current
                .Where(c => !importedKeys.Contains((c.SegmentId, c.LeagueId)))
                .OrderBy(c => c.SortOrder)
                .Select(c => new OddstableLeagueSortOrder
                {
                    SportId = c.SportId,
                    SportName = c.SportName,
                    SegmentId = c.SegmentId,
                    SegmentName = c.SegmentName,
                    LeagueId = c.LeagueId,
                    LeagueName = c.LeagueName
                })
                .ToList();

            plan.Result.AddRange(imported.Concat(retained));

            for (var i = 0; i < plan.Result.Count; i++)
                plan.Result[i].SortOrder = i + 1;

            plan.RetainedCount = retained.Count;

            if (plan.Result.Count > MaxLeaguesPerClient)
                plan.Errors.Add($"The result would have {plan.Result.Count} leagues. A client can have at most {MaxLeaguesPerClient}.");

            return plan;
        }
    }
}
