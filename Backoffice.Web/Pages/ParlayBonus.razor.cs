using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.ParlayBonus;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.ClientParlayBonusResponse;

namespace Backoffice.Web.Pages
{
    public partial class ParlayBonus : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
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
                ClientIdRef = ClientId,
                Page = MetaData.CurrentPage,
                PageSize = MetaData.PageSize
            })).Result;

            if (result != null)
            {
                MetaData = new MetaData()
                {
                    CurrentPage = result.Page,
                    PageSize = result.PageSize,
                    TotalCount = result.Total,
                    TotalPages = result.TotalPage
                };

                ClientParlayBonusResponse = result;
            }
        }

        public async Task OnDeleteConfig(ParlayBonusConfig parlayBonusConfig)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ParlayBonusConfig", parlayBonusConfig);

            var options = new ModalOptions();
            ModalService.Open<DeleteModal>($"Delete Parlay Bonus", parameters, options);
        }

        public async Task OnViewConfigLog(ParlayBonusConfig parlayBonusConfig)
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("ParlayBonusConfig", parlayBonusConfig);

            var options = new ModalOptions();
            options.Tooltip = "<i class=\"fa fa-list-alt\"></i>";
            ModalService.Open<ViewLogModal>($"Logs", parameters, options);
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
