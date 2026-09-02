using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Backoffice.Web.Components.SBC.Clients
{
    public partial class ImportClientsModal : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [CascadingParameter] public CustomModal Modal { get; set; }

        public int Pane { get; set; }
        public string Environment { get; set; }
        public bool IsBusy { get; set; }
        public bool ConfirmingCommit { get; set; }
        public bool EnvironmentDetectionFailed { get; set; }
        public List<AlertModel> Alerts { get; set; }

        public string SelectedFileName { get; set; }
        public List<ClientImportRowRequest> ParsedRows { get; set; }
        public ValidateClientImportResponse ValidateResponse { get; set; }
        public CommitClientImportResponse CommitResponse { get; set; }
        public ClientImportProgressResponse Progress { get; set; }

        private CancellationTokenSource _progressPollCts;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Pane = 1;
            Alerts = new List<AlertModel>();
            ParsedRows = new List<ClientImportRowRequest>();

            var envResponse = await BackofficeService.GetClientImportEnvironment();
            if (envResponse.StatusCode != StatusCode.Success)
            {
                EnvironmentDetectionFailed = true;
                Alerts.Add(new AlertModel { Status = AlertStatus.Error, Message = envResponse.Message ?? "Could not determine the environment." });
                return;
            }

            Environment = envResponse.Result;
        }

        public async Task OnFileSelectedEvent(InputFileChangeEventArgs e)
        {
            Alerts.Clear();
            SelectedFileName = e.File.Name;

            using var stream = new MemoryStream();
            await e.File.OpenReadStream(10 * 1024 * 1024).CopyToAsync(stream);
            var text = System.Text.Encoding.UTF8.GetString(stream.ToArray());

            var parsed = ClientImportCsvParser.Parse(text);

            if (parsed.Errors.Any())
            {
                ParsedRows = new List<ClientImportRowRequest>();
                foreach (var error in parsed.Errors)
                {
                    Alerts.Add(new AlertModel { Status = AlertStatus.Error, Message = error });
                }
                return;
            }

            ParsedRows = parsed.Rows;
        }

        public async Task OnParseClickEvent()
        {
            if (!ParsedRows.Any())
            {
                Alerts.Add(new AlertModel { Status = AlertStatus.Error, Message = "Choose a CSV file first." });
                return;
            }

            IsBusy = true;
            Alerts.Clear();

            var response = await BackofficeService.ValidateClientImport(new ValidateClientImportRequest
            {
                Environment = Environment,
                Rows = ParsedRows
            });

            IsBusy = false;

            if (response.StatusCode != StatusCode.Success)
            {
                Alerts.Add(new AlertModel { Status = AlertStatus.Error, Message = response.Message ?? "Validation failed." });
                return;
            }

            ValidateResponse = response.Result;
            Pane = 2;
        }

        public void OnBackClickEvent()
        {
            Pane = 1;
            ValidateResponse = null;
        }

        public void OnRequestCommitEvent()
        {
            ConfirmingCommit = true;
        }

        public void OnCancelConfirmEvent()
        {
            ConfirmingCommit = false;
        }

        public async Task OnConfirmCommitEvent()
        {
            ConfirmingCommit = false;

            var readyCodes = ValidateResponse.Rows
                .Where(r => r.Status == "Ready")
                .Select(r => r.ClientCode.ToUpperInvariant())
                .ToHashSet();

            var readyRows = ParsedRows
                .Where(r => readyCodes.Contains((r.ClientCode ?? string.Empty).Trim().ToUpperInvariant()))
                .ToList();

            if (!readyRows.Any())
            {
                Alerts.Add(new AlertModel { Status = AlertStatus.Error, Message = "There are no ready rows to import." });
                return;
            }

            var batchId = Guid.NewGuid();
            var totalClients = readyRows.Select(r => (r.ClientCode ?? string.Empty).Trim().ToUpperInvariant()).Distinct().Count();

            await BackofficeService.AddActivityLog(new ActivityLogModel
            {
                Page = "Clients - Import",
                Activity = "Confirmed client import",
                Payload = $"BatchId={batchId}; Environment={Environment}; ClientCount={totalClients}",
                UserId = _userState.Model.Id,
                UserName = _userState.Model.Name
            });

            IsBusy = true;
            Alerts.Clear();
            Progress = new ClientImportProgressResponse { TotalClients = totalClients };

            await JSRuntime.InvokeVoidAsync("blockPageClose", "An import is still running. Leaving now may leave clients half-created.");

            _progressPollCts = new CancellationTokenSource();
            var pollingTask = PollProgress(batchId, _progressPollCts.Token);

            var response = await BackofficeService.CommitClientImport(new CommitClientImportRequest
            {
                BatchId = batchId,
                Environment = Environment,
                Rows = readyRows
            });

            _progressPollCts.Cancel();
            await JSRuntime.InvokeVoidAsync("allowPageClose");

            IsBusy = false;

            if (response.StatusCode != StatusCode.Success)
            {
                Alerts.Add(new AlertModel { Status = AlertStatus.Error, Message = response.Message ?? "Import already running or failed to start." });
                return;
            }

            CommitResponse = response.Result;
            Pane = 3;
        }

        private async Task PollProgress(Guid batchId, CancellationToken cancellationToken)
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    var response = await BackofficeService.GetClientImportProgress(batchId);

                    if (response.StatusCode == StatusCode.Success && response.Result != null)
                    {
                        Progress = response.Result;
                        StateHasChanged();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public async Task OnDoneClickEvent()
        {
            ModalService.Cancel();
            await Modal.OnSaveClickCallback.InvokeAsync(true);
        }

        public async Task OnCancelClickEvent()
        {
            if (IsBusy)
                return;

            ModalService.Cancel();
        }

        public void Dispose()
        {
            _progressPollCts?.Cancel();
            _progressPollCts?.Dispose();
        }

        #endregion
    }
}
