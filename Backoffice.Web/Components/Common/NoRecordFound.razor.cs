using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class NoRecordFound
    {
        private const string DescriptionKey = "no_record_message";
        private const string DescriptionDefault = "Try adjusting your search or filter to find what you're looking for.";

        [Parameter] public string Message { get; set; } = "";

        // A language with no row for the key (DEV has no translation rows at all) gets the key itself back, so show the
        // English sentence instead of the raw key (BO-151).
        private string Description
        {
            get
            {
                var text = _translationState.GetValue(DescriptionKey);

                return text == DescriptionKey ? DescriptionDefault : text;
            }
        }
    }
}
