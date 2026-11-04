using System.Text;
using Backoffice.Web.Helpers;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Services
{
    // Builds/parses the Static Translation CSV -- the same mechanism Dynamic Translation's
    // GlossaryCsvHelper uses (StringBuilder + BOM + downloadFileFromText, minimal RFC4180
    // parser), just keyed on the static translation Key and widened to the EN/ZH/JA/BR
    // columns the Static Translation screen supports so the exported file also works as an
    // upload template. Kept dependency-free on purpose: no Excel library, mirrors the
    // existing Dynamic Translation pattern.
    public static class StaticTranslationCsvHelper
    {
        private const char Bom = (char)0xFEFF;

        public static readonly string[] ExpectedHeaders = { "Key", "EN", "ZH", "JA", "BR" };

        public static string BuildCsv(IEnumerable<SportsbookTranslationExportRow> rows)
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine(string.Join(",", ExpectedHeaders));

            foreach (var row in rows)
            {
                csvBuilder.AppendLine(string.Join(",",
                    CsvField.Escape(row.Key),
                    CsvField.Escape(row.SourceText),
                    CsvField.Escape(row.TargetTextZh),
                    CsvField.Escape(row.TargetTextJa),
                    CsvField.Escape(row.TargetTextBr)));
            }

            return Bom + csvBuilder.ToString();
        }

        public static (bool Success, string? ErrorMessage, List<ImportSportsbookTranslationRow> Rows) ParseCsv(string csvText)
        {
            var lines = ParseCsvLines(csvText);
            if (lines.Count == 0)
            {
                return (false, "The uploaded file has no data.", new List<ImportSportsbookTranslationRow>());
            }

            // Excel commonly leaves a trailing blank column (a trailing comma) after a save --
            // harmless for data rows (GetCell already defaults missing/extra cells to empty) but
            // it would otherwise fail an exact SequenceEqual against the header. Strip trailing
            // blanks before comparing so a re-saved export still validates.
            var header = lines[0].Select(h => h.Trim()).ToArray();
            var trimmedHeader = header.ToList();
            while (trimmedHeader.Count > 0 && string.IsNullOrEmpty(trimmedHeader[^1]))
            {
                trimmedHeader.RemoveAt(trimmedHeader.Count - 1);
            }

            if (!trimmedHeader.SequenceEqual(ExpectedHeaders, StringComparer.OrdinalIgnoreCase))
            {
                var found = header.Length == 0 || header.All(string.IsNullOrEmpty)
                    ? "(blank)"
                    : string.Join(", ", header);
                return (false, $"Unexpected header row. Expected columns: {string.Join(", ", ExpectedHeaders)}. Found: {found}. Use the exported template.", new List<ImportSportsbookTranslationRow>());
            }

            var rows = new List<ImportSportsbookTranslationRow>();
            for (var i = 1; i < lines.Count; i++)
            {
                var cells = lines[i];
                if (cells.Count == 1 && string.IsNullOrWhiteSpace(cells[0]))
                {
                    continue; // fully blank line
                }

                rows.Add(new ImportSportsbookTranslationRow
                {
                    Key = GetCell(cells, 0),
                    SourceText = GetCell(cells, 1),
                    TargetTextZh = GetCell(cells, 2),
                    TargetTextJa = GetCell(cells, 3),
                    TargetTextBr = GetCell(cells, 4)
                });
            }

            return (true, null, rows);
        }

        private static string GetCell(List<string> cells, int index) => index < cells.Count ? cells[index] : string.Empty;

        // Minimal RFC4180 parser (the inverse of CsvField.Escape): handles quoted fields with
        // embedded commas, doubled-quote escaping ("") and embedded newlines, since translated
        // text can contain any of those.
        private static List<List<string>> ParseCsvLines(string csvText)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(csvText))
            {
                return rows;
            }

            if (csvText[0] == Bom)
            {
                csvText = csvText.Substring(1);
            }

            var currentRow = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var i = 0;

            void EndField()
            {
                currentRow.Add(CsvField.RemoveFormulaMarker(field.ToString()));
                field.Clear();
            }

            void EndRow()
            {
                EndField();
                rows.Add(currentRow);
                currentRow = new List<string>();
            }

            while (i < csvText.Length)
            {
                var c = csvText[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csvText.Length && csvText[i + 1] == '"')
                        {
                            field.Append('"');
                            i += 2;
                            continue;
                        }

                        inQuotes = false;
                        i++;
                        continue;
                    }

                    field.Append(c);
                    i++;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        i++;
                        break;
                    case ',':
                        EndField();
                        i++;
                        break;
                    case '\r':
                        if (i + 1 < csvText.Length && csvText[i + 1] == '\n')
                        {
                            i++;
                        }
                        EndRow();
                        i++;
                        break;
                    case '\n':
                        EndRow();
                        i++;
                        break;
                    default:
                        field.Append(c);
                        i++;
                        break;
                }
            }

            if (field.Length > 0 || currentRow.Count > 0)
            {
                EndRow();
            }

            return rows;
        }
    }
}
