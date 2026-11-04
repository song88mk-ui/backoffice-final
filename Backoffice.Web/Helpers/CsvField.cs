namespace Backoffice.Web.Helpers
{
    // The one CSV cell escape every Back Office export uses (clients, client update errors, static and
    // dynamic translations, league sorting). Besides the RFC4180 quoting, a cell that Excel would run as a
    // formula (starts with = + - @, tab or carriage return) gets a leading ' so Excel shows it as text (BO-137).
    // The parsers call RemoveFormulaMarker on every cell, so an unedited export imports back unchanged.
    public static class CsvField
    {
        private const char Marker = '\'';

        public static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var escaped = NeedsMarker(value) ? Marker + value : value;
            escaped = escaped.Replace("\"", "\"\"");
            if (escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r'))
            {
                escaped = $"\"{escaped}\"";
            }

            return escaped;
        }

        // The exact inverse of the marker added by Escape, applied to an already unquoted cell. Text that
        // itself starts with ' before a formula character ("'=x") is exported as "''=x", so removing exactly
        // one ' here always gives back the original, and a cell such as "'5" or "-5" is left alone.
        public static string RemoveFormulaMarker(string cell)
        {
            return cell.Length > 1 && cell[0] == Marker && NeedsMarker(cell.Substring(1)) ? cell.Substring(1) : cell;
        }

        // Any number of ' followed by a formula character.
        private static bool NeedsMarker(string value)
        {
            var i = 0;
            while (i < value.Length && value[i] == Marker)
            {
                i++;
            }

            return i < value.Length && value[i] is '=' or '+' or '-' or '@' or '\t' or '\r';
        }
    }
}
