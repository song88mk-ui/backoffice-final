using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class Filter : IDisposable
    {
        #region Properties
        [Inject] IModalService ModalService { get; set; }

        public SearchFormModel SearchForm { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            SearchForm = new SearchFormModel();
        }

        public async Task OnInputSearchEvent()
        {
        }

        public async Task OnAddBoostedOdds()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");

            var options = new ModalOptions();
            ModalService.Open<AddBoostedOddsModal>($"Add Boosted Odds", parameters, options);
        }

        public async Task OnViewAllLogs()
        {
            var parameters = new ModalParameters();
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-lg");
            parameters.Add("DialogStyle","max-width: 65rem;");

            var options = new ModalOptions();
            ModalService.Open<ViewAllLogsModal>($"View Logs", parameters, options);
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
