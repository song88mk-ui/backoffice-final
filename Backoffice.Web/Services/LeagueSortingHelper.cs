using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models.OddsEngine.LeagueSorting;
using System.Text.Json;

namespace Backoffice.Web.Services
{
    // Pure logic behind the League Sorting page and dialogs (BO-107), kept out of the components so it
    // can be unit tested without rendering.
    public static class LeagueSortingHelper
    {
        public const int MaxLeaguesPerClient = 50;

        // One row per client the user can manage, including clients that have no league order yet,
        // so their first order can still be created from the page.
        public static List<LeagueSortingClient> BuildClientRows(
            IEnumerable<(string ClientCode, string ClientName)> clients,
            IEnumerable<ClientOddstableLeagueOrderListResponse> orders)
        {
            var ordersByClient = orders
                .GroupBy(o => o.ClientCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderBy(o => o.SortOrder).ToList(), StringComparer.OrdinalIgnoreCase);

            return clients
                .OrderBy(c => c.ClientName, StringComparer.OrdinalIgnoreCase)
                .Select(c => new LeagueSortingClient
                {
                    ClientCode = c.ClientCode,
                    ClientName = c.ClientName,
                    Leagues = ordersByClient.TryGetValue(c.ClientCode, out var list)
                        ? list.Select(o => new LeagueSortingLeague
                        {
                            SegmentId = o.SegmentId,
                            SegmentName = o.SegmentName,
                            LeagueId = o.LeagueId,
                            LeagueName = o.LeagueName,
                            SortOrder = o.SortOrder
                        }).ToList()
                        : new List<LeagueSortingLeague>()
                })
                .ToList();
        }

        // Keeps the clients that are ticked in the client filter and match the search text (client name,
        // segment or league), then numbers the rows from 1.
        public static List<LeagueSortingClient> Filter(
            IEnumerable<LeagueSortingClient> clients,
            ISet<string> selectedClientCodes,
            string search)
        {
            var term = search?.Trim();

            return clients
                .Where(c => selectedClientCodes.Contains(c.ClientCode))
                .Where(c => string.IsNullOrEmpty(term)
                    || Contains(c.ClientName, term)
                    || c.Leagues.Any(l => Contains(l.SegmentName, term) || Contains(l.LeagueName, term)))
                .Select((c, index) => new LeagueSortingClient
                {
                    RowNo = index + 1,
                    ClientCode = c.ClientCode,
                    ClientName = c.ClientName,
                    Leagues = c.Leagues
                })
                .ToList();
        }

        // The dialog edits one row per league plus empty rows waiting for a league. A client without
        // any league starts with a single empty row.
        public static List<LeagueSortingEditRow> ToEditRows(IEnumerable<LeagueSortingLeague> leagues)
        {
            var rows = leagues
                .OrderBy(l => l.SortOrder)
                .Select(l => new LeagueSortingEditRow
                {
                    SegmentId = l.SegmentId,
                    SegmentName = l.SegmentName,
                    LeagueId = l.LeagueId,
                    LeagueName = l.LeagueName
                })
                .ToList();

            if (!rows.Any())
                rows.Add(new LeagueSortingEditRow());

            return rows;
        }

        // Null when the rows can be saved, otherwise the message to show.
        public static string? ValidateRows(IReadOnlyList<LeagueSortingEditRow> rows)
        {
            if (rows.All(r => r.IsEmpty))
                return "Select at least 1 League";

            if (rows.Any(r => r.IsEmpty))
                return "Please remove empty row";

            return null;
        }

        // Reorders the rows to the key order the browser reports after a drag. Returns null when the
        // keys do not match the rows exactly, which means the browser and the model diverged.
        public static List<LeagueSortingEditRow>? Reorder(IReadOnlyList<LeagueSortingEditRow> rows, IReadOnlyList<string>? rowKeys)
        {
            if (rowKeys == null || rowKeys.Count != rows.Count)
                return null;

            var byKey = rows.ToDictionary(r => r.Key.ToString());
            var ordered = new List<LeagueSortingEditRow>(rows.Count);

            foreach (var key in rowKeys)
            {
                if (!byKey.Remove(key, out var row))
                    return null;

                ordered.Add(row);
            }

            return ordered;
        }

        // The visual row order becomes the sort order, numbered from 1.
        public static List<OddstableLeagueSortOrder> ToSortOrders(int sportId, string sportName, IReadOnlyList<LeagueSortingEditRow> rows)
        {
            return rows
                .Select((r, index) => new OddstableLeagueSortOrder
                {
                    SportId = sportId,
                    SportName = sportName,
                    SegmentId = r.SegmentId,
                    SegmentName = r.SegmentName,
                    LeagueId = r.LeagueId,
                    LeagueName = r.LeagueName,
                    SortOrder = index + 1
                })
                .ToList();
        }

        // Shapes an ActivityLog2 entry written by the League Sorting audit for display. Times are stored in UTC
        // and shown in the user's timezone. An entry whose From or To cannot be read still shows, just without
        // that side's leagues, so one bad row never hides the rest of the log.
        public static LeagueSortingLogRow ToLogRow(
            ActivityLogModel log,
            IReadOnlyCollection<(string ClientCode, string ClientName)> clients,
            TimeSpan timeZoneOffset)
        {
            var previous = ParseAudit(log.From);
            var current = ParseAudit(log.To);

            string ClientNameOf(string code) =>
                clients.FirstOrDefault(c => string.Equals(c.ClientCode, code, StringComparison.OrdinalIgnoreCase)).ClientName ?? code;

            return new LeagueSortingLogRow
            {
                Timestamp = DateTime.SpecifyKind(log.TimeStamp, DateTimeKind.Utc).Add(timeZoneOffset),
                ClientName = ClientNameOf(log.Payload),
                Action = log.Activity,
                SportName = current?.SportName ?? previous?.SportName,
                CopiedFromClientName = string.IsNullOrEmpty(current?.SourceClientCode) ? null : ClientNameOf(current.SourceClientCode),
                PreviousLeagues = DescribeLeagues(previous),
                NewLeagues = DescribeLeagues(current),
                UpdatedBy = log.UserName
            };
        }

        private static OddstableLeagueSortingAudit ParseAudit(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<OddstableLeagueSortingAudit>(json, JsonSerializerOptionsProvider.CaseInsensitive);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // "England > Premier League" for each league, in sort order.
        private static List<string> DescribeLeagues(OddstableLeagueSortingAudit audit) =>
            audit?.Leagues?
                .OrderBy(l => l.SortOrder)
                .Select(l => $"{l.SegmentName} > {l.LeagueName}")
                .ToList()
            ?? new List<string>();

        private static bool Contains(string value, string term) =>
            value != null && value.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
