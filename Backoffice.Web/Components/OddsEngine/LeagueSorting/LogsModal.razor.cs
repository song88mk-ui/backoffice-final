using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models.OddsEngine.LeagueSorting;
using Backoffice.Web.Services;

namespace Backoffice.Web.Components.OddsEngine.LeagueSorting
{
    public partial class LogsModal
    {
        private const int LogDays = 30;

        private List<LeagueSortingLogRow> _rows = new List<LeagueSortingLogRow>();
        private MetaData _metaData = new MetaData { CurrentPage = 1, PageSize = 10 };
        private bool _isPreloader = true;
        private string _error;

        protected override async Task OnInitializedAsync()
        {
            await GetContent();
        }

        public async Task SelectPage(int page)
        {
            _metaData.CurrentPage = page;

            await GetContent();
        }

        public void OnCloseClickEvent()
        {
            _modalService.Cancel();
        }

        private async Task GetContent()
        {
            _isPreloader = true;
            _error = null;

            var response = await _backofficeService.GetOddstableLeagueSortingLogs(new OddstableLeagueSortingLogRequest
            {
                DateFrom = DateTime.UtcNow.AddDays(-LogDays),
                MetaData = _metaData
            });

            if (response.StatusCode == StatusCode.Success && response.Result != null)
            {
                var clients = (_userState.Model.TradingClients ?? new List<PlatformClientResponse>())
                    .Select(c => (c.ClientCode, c.ClientName))
                    .ToList();

                _rows = response.Result.ActivityLogs
                    .Select(l => LeagueSortingHelper.ToLogRow(l, clients, _userState.TimeZoneOffset))
                    .ToList();
                _metaData = response.Result.MetaData ?? _metaData;
            }
            else
            {
                _rows = new List<LeagueSortingLogRow>();
                _error = response.Message ?? "Unable to load the logs.";
            }

            _isPreloader = false;
        }
    }
}
