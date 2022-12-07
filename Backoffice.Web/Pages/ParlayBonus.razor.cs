using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages
{
    public partial class ParlayBonus : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [CascadingParameter(Name = "UserModel")] UserModel UserModel { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        public MetaData MetaData { get; set; } = new MetaData();
        public ClientParlayBonusResponse ClientParlayBonusResponse { get; set; }
        public long ClientId { get; set; }
        #endregion

        protected override async Task OnInitializedAsync()
        {
            PageTitle = "Parlay Bonus";
            IconClass = "ico-parlay-bonus";
            ClientParlayBonusResponse = new ClientParlayBonusResponse();

            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 10000,
                TotalPages = 100,
            };            
        }

        public async Task OnClientChangedEvent(long ClientId)        
        {
            var result = (await BackOfficeService.GetClientParlayBonus(new GetParlayBonusRequest()
            {
                ClientIdRef = ClientId
            })).Result;

            if (result != null)
            {
                ClientParlayBonusResponse = result;
            }
        }

        protected async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;            
        }

        public void Dispose()
        {
        }
    }
}
