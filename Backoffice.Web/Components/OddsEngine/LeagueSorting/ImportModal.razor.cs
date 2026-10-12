using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.LeagueSorting;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Text;

namespace Backoffice.Web.Components.OddsEngine.LeagueSorting
{
    public partial class ImportModal
    {
        // A file of at most 50 leagues is a few KB, so anything near this size is not a League Sorting file.
        private const long MaxFileSize = 1024 * 1024;

        private enum Step
        {
            SelectFile,
            Review,
            Done
        }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private string _clientCode;
        private string _clientName;
        private int _sportId;
        private string _sportName;
        private int _currentLeagueCount;

        private Step _step = Step.SelectFile;
        private string _fileName;
        private List<string> _fileErrors = new List<string>();
        private List<ImportOddstableLeagueRow> _rows = new List<ImportOddstableLeagueRow>();
        private ImportClientOddstableLeagueSettingResponse _result;
        private bool _isBusy;
        // Changing the key renders a fresh file input, so the same file can be picked again after a fix.
        private int _inputKey;
        private AlertModel _alert;

        private bool CanImport =>
            !_isBusy && _result != null && _result.StatusCode == StatusCode.Success && _result.HasChanges;

        #region Methods

        protected override void OnInitialized()
        {
            _clientCode = ModalParameters.TryGet<string>("ClientCode");
            _clientName = ModalParameters.TryGet<string>("ClientName");
            _sportId = ModalParameters.TryGet<int>("SportId");
            _sportName = ModalParameters.TryGet<string>("SportName");
            _currentLeagueCount = ModalParameters.TryGet<int>("CurrentLeagueCount");
        }

        public async Task OnFileSelectedEvent(InputFileChangeEventArgs e)
        {
            _alert = new AlertModel();
            _fileErrors = new List<string>();
            _result = null;

            var file = e.File;

            if (file == null)
                return;

            _fileName = file.Name;

            string csvText;

            try
            {
                using var stream = file.OpenReadStream(MaxFileSize);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                csvText = await reader.ReadToEndAsync();
            }
            catch (IOException)
            {
                _fileErrors = new List<string> { "The file is too large. The limit is 1 MB." };
                return;
            }

            var parsed = LeagueSortingCsvHelper.ParseCsv(csvText, _clientCode, _sportId);

            if (!parsed.Success)
            {
                _fileErrors = parsed.Errors;
                return;
            }

            _rows = parsed.Rows;

            await RunImport(commit: false);
        }

        public async Task OnImportClickEvent()
        {
            await RunImport(commit: true);
        }

        public void OnBackClickEvent()
        {
            _step = Step.SelectFile;
            _result = null;
            _rows = new List<ImportOddstableLeagueRow>();
            _fileName = null;
            _fileErrors = new List<string>();
            _alert = new AlertModel();
            _inputKey++;
        }

        public void OnCloseClickEvent()
        {
            _modalService.Cancel();
        }

        #endregion

        #region Utilities

        // Commit = false asks the server to validate the file only. The server decides every time from the rows
        // alone, so the commit applies exactly what the review step showed unless the data changed in between.
        private async Task RunImport(bool commit)
        {
            _isBusy = true;
            _alert = new AlertModel();
            await InvokeAsync(StateHasChanged);

            var response = await _backofficeService.ImportClientOddstableLeagueSettings(new ImportClientOddstableLeagueSettingRequest
            {
                ClientCode = _clientCode,
                SportId = _sportId,
                SportName = _sportName,
                Commit = commit,
                Rows = _rows,
                UserId = _userState.Model.Id,
                UserName = _userState.Model.Name,
                TimeStamp = DateTime.UtcNow
            });

            _isBusy = false;

            if (response.StatusCode != StatusCode.Success || response.Result == null)
            {
                _alert = new AlertModel { Status = AlertStatus.Error, Message = response.Message ?? "Error when importing league sorting." };
                return;
            }

            _result = response.Result;
            _step = Step.Review;

            if (commit && _result.Committed)
            {
                _step = Step.Done;
                _alert = new AlertModel { Status = AlertStatus.Success, Message = _result.Message };

                await Modal.OnSaveClickCallback.InvokeAsync();
            }
        }

        #endregion
    }
}
