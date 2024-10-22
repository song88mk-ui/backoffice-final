using Backoffice.Shared.Enums;

namespace Backoffice.Web.Utilities
{
    public static class Utility
    {
        public static string GetSportIcon(int sportId)
        {
            var sportIconMapping = new Dictionary<int, string>()
            {
                  {1, "soccer"},
                  {2, "basketball"},
                  {3, "baseball"},
                  {4, "ice-hockey"},
                  {5, "tennis"},
                  {6, "handball"},
                  {7, "floorball"},
                  {9, "golf"},
                  {10, "boxing"},
                  {12, "rugby"},
                  {13, "aussie-rules"},
                  {15, "bandy"},
                  {16, "american-football"},
                  {17, "cycling"},
                  {18, "specials"},
                  {19, "snooker"},
                  {20, "table-tennis"},
                  {21, "cricket"},
                  {22, "darts"},
                  {23, "volleyball"},
                  {24, "field-hockey"},
                  {26, "water-polo"},
                  {28, "curling"},
                  {29, "futsal"},
                  {30, "olympics"},
                  {31, "badminton"},
                  {32, "bowls"},
                  {33, "chess"},
                  {34, "beach-volley"},
                  {36, "athletics"},
                  {37, "squash"},
                  {38, "rick-hockey"},
                  {39, "lacrosse"},
                  {40, "formula-1"},
                  {43, "alpine-skiing"},
                  {44, "biathlon"},
                  {45, "bobsleigh"},
                  {46, "cross-country"},
                  {47, "nordic-combined"},
                  {48, "ski-jumping"},
                  {49, "snowboard"},
                  {50, "speed-skating"},
                  {51, "luge"},
                  {55, "horse-racing"},
                  {56, "shwingen"},
                  {60, "beach-soccer"},
                  {61, "pesapallo"},
                  {71, "padel"},
                  {101, "rally"},
                  {109, "esport-counter-strike"},
                  {110, "esport-league-of-legends"},
                  {111, "dota-2-asian"},
                  {112, "starcraft"},
                  {117, "mma"},
                  {118, "call-of-duty"},
                  {121, "esport-overwatch"},
                  {125, "rainbow-six"},
                  {128, "rocket-league"},
                  {129, "indy-racing"},
                  {131, "speedway"},
                  {134, "king-glory"},
                  {135, "gaelic-football"},
                  {136, "gaelic-hurling"},
                  {137, "esoccer"},
                  {138, "kabaddi"},
                  {142, "formula-e"},
                  {147, "drag-racing"},
                  {153, "ebasketball"},
                  {155, "basketball-3x3"},
                  {157, "beach-handball"},
                  {188, "touring-car-racing"},
                  {190, "motorcycle-racing"},
                  {191, "stock-car-racing"},
                  {195, "eice-hockey"},
                  {203, "soccer-specials"},
                  {1001, "virtual-soccer"},
                  {1002, "virtual-basketball"},
                  {1003, "virtual-baseball"},
                  {1005, "virtual-tennis"},
                  {1021, "virtual-cricket"},
                  {1055, "virtual-horse-classics"},
                  {1058, "virtual-dog-racing"}
            };

            if (sportIconMapping.TryGetValue(sportId, out string? val))
                return val ?? string.Empty;

            return string.Empty;
        }

        public static string GetLanguageCodeFullLabel(LanguageCode langCode)
        {
            var langFullMapping = new Dictionary<LanguageCode, string>() {
                {LanguageCode.EN, "English" },
                {LanguageCode.TH, "Thai" },
                {LanguageCode.MY, "Burmese" },
                //{LanguageCode.LO, "Lao" },
                {LanguageCode.KO, "Korean" },
                {LanguageCode.ZH, "Simplified Chinese" },
                {LanguageCode.ZHT, "Traditional Chinese" },
                {LanguageCode.VI, "Vietnamese" },
                {LanguageCode.ID, "Indonesian" },
                //{LanguageCode.KM, "Khmer" },
                {LanguageCode.JA, "Japanese" },
                //{LanguageCode.MS, "Malay" },
                {LanguageCode.TR, "Turkish" },
                {LanguageCode.SW, "Swahili" },
                {LanguageCode.FR, "French" },
                {LanguageCode.PT, "Portuguese" },
                //{LanguageCode.AM, "Amharic" },
                {LanguageCode.ES, "Spanish" },
                {LanguageCode.BR, "Brazilian Portuguese" },
                //{LanguageCode.DE, "German" },
                //{LanguageCode.NL, "Dutch" },
                //{LanguageCode.FI, "Finnish" },
            };

            if (langFullMapping.TryGetValue(langCode, out string? val))
                return val ?? string.Empty;

            return string.Empty;
        }
    }
}
