using Backoffice.Shared.Enums;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class ProviderIndicator
    {
        [Parameter]
        public FeedProvider FeedProvider { get; set; }

        private string _iconClassName;
        private string _label;

        protected override async Task OnParametersSetAsync()
        {
            switch (FeedProvider) {
                case FeedProvider.BetRadar:
                    _iconClassName = "bet-radar";
                    _label = "SR";
                    break;
                case FeedProvider.BetGenius:
                    _iconClassName = "bet-genius";
                    _label = "BG";
                    break;
                default:
                    break;
            }
        }
    }
}
