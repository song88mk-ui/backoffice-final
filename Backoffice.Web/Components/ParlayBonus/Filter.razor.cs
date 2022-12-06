using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.ParlayBonus
{
    public partial class Filter : IDisposable
    {
        [Parameter] public UserModel UserModel { get; set; }
        [Parameter] public ClientParlayBonusResponse ClientParlayBonusResponse { get; set; }

        public string selectedClientsCount { get; set; }
        #region Methods

        protected override async Task OnInitializedAsync()
        {

        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
