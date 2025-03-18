using Backoffice.Shared.Enums;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class ProviderIcon
    {
        [Parameter]
        public int ChannelTypeId { get; set; }

        private string _iconClassName;
        private string _label;

        protected override async Task OnParametersSetAsync()
        {
            switch (ChannelTypeId) {
                case (int)ChannelType.SR:
                    _iconClassName = "bet-radar";
                    _label = "SR";
                    break;
                case (int)ChannelType.BG:
                    _iconClassName = "bet-genius";
                    _label = "BG";
                    break;
                default:
                    break;
            }
        }
    }
}
