using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.SBC.Clients
{
    public partial class ClientImportHistoryModal
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        public List<ClientImportBatchSummary> Batches { get; set; } = new();
        public Guid? SelectedBatchId { get; set; }
        public List<ClientFieldChangeResponse> SelectedBatchChanges { get; set; } = new();
        public bool IsLoadingChanges { get; set; }
        public string LoadError { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            var response = await BackofficeService.GetClientImportBatchHistory(ClientImportOperationType.Update);
            if (response.StatusCode != StatusCode.Success || response.Result == null)
            {
                LoadError = $"Could not load update history: {response.Message ?? "unknown error"}.";
                return;
            }

            Batches = response.Result;
        }

        public async Task OnViewChangesClickEvent(Guid batchId)
        {
            SelectedBatchId = batchId;
            IsLoadingChanges = true;
            LoadError = null;
            SelectedBatchChanges = new List<ClientFieldChangeResponse>();

            var response = await BackofficeService.GetClientImportFieldChanges(batchId);

            IsLoadingChanges = false;
            if (response.StatusCode != StatusCode.Success || response.Result == null)
            {
                LoadError = $"Could not load field changes: {response.Message ?? "unknown error"}.";
                return;
            }

            SelectedBatchChanges = response.Result;
        }

        public void OnBackToListClickEvent()
        {
            SelectedBatchId = null;
            LoadError = null;
            SelectedBatchChanges = new List<ClientFieldChangeResponse>();
        }

        #endregion
    }
}
