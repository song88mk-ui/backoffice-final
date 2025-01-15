using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Helpers;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.SportSetting;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class ViewLogsModal : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }

        [CascadingParameter] private ModalParameters ModalParameters { get; set; }

        //private List<ClientSportOrderAuditResponse> _clientSportOrderAudit;
        private List<SportSettingsActivityLog> _clientSportOrderAudit;
        private MetaData _metaData;
        private int _selectedPage;
        private string _clientCode;
        private bool _isPreloader;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            TradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();
            //_clientSportOrderAudit = new List<ClientSportOrderAuditResponse>();

            _metaData = new MetaData()
            {
                PageSize = 10,
                CurrentPage = 1,
                TotalPages = 0,
                TotalCount = 0
            };
            _clientCode = ModalParameters.TryGet<string>("ClientCode");

            await GetContent();
            //_clientSportOrderAudit = (await TradingService.GetClientSportOrderAuditList(new ClientSportOrderAuditRequest()
            //{
            //    ClientCode = _clientCode
            //})).Result;
        }
        protected async Task SelectedPage(int page)
        {
            _metaData.CurrentPage = page;

            await GetContent();
        }

        private async Task GetContent()
        {
            _isPreloader = true;
            _clientSportOrderAudit = new List<SportSettingsActivityLog>();

            var response = await _backofficeService.GetActivityLogs(new ActivityLogRequest
            {
                Page = SportsbookClientPermission.SBC_OE_SportsSettings.ToString().Replace("_", "."),
                //Payload = $"{_clientCode}",
                MetaData = _metaData,
                DateFrom = DateTimeHelper.Now(_userState.TimeZoneOffset).AddDays(-30)
            });

            if (response.StatusCode == StatusCode.Success)
            {
                var activityLogs = response.Result.ActivityLogs;

                _clientSportOrderAudit = activityLogs.Select(s => new SportSettingsActivityLog()
                {
                    Timestamp = s.TimeStamp,
                    ClientName = _userState.TradingClients.FirstOrDefault(c => c.ClientCode.Equals(s.Payload, StringComparison.OrdinalIgnoreCase))?.ClientName ?? s.Payload,
                    ChangesFrom = PrepareLogPayload(JsonSerializer.Deserialize<SportSettingSortOrderAudit>(s.From)),
                    ChangesTo = PrepareLogPayload(JsonSerializer.Deserialize<SportSettingSortOrderAudit>(s.To)),
                    UpdatedBy = s.UserName
                }).ToList();

                _metaData = response.Result.MetaData;
            }

            _isPreloader = false;
        }

        public async Task OnCancelClickEvent()
        {
            ModalService.Cancel();
        }

        private string PrepareLogPayload(SportSettingSortOrderAudit? payload)
        {
            if (payload == null)
                return string.Empty;

            var result = new StringBuilder();
            var hasParent = false;

            if (payload.Sports != null && payload.Sports.Any())
            {
                result.Append($"<b>Sports</b></br>");
                result.Append(string.Join(", ", payload.Sports.Select(s => s.SportName.Trim())));
                hasParent = true;
            }

            if (payload.Segments != null && payload.Segments.Any())
            {
                if (hasParent)
                    result.Append("<br/><br/>");

                hasParent = false;

                foreach (var sports in payload.Segments)
                {
                    if (hasParent)
                        result.Append("<br/><br/>");

                    result.Append($"<b>{sports.SportName.Trim()} > Segment</b></br>");
                    result.Append(string.Join(", ", sports.Segments.Select(s => s.SegmentName.Trim())));

                    hasParent = true;
                }
            }

            if (payload.Leagues != null && payload.Leagues.Any())
            {
                if (hasParent)
                    result.Append("<br/><br/>");

                hasParent = false;

                foreach (var sport in payload.Leagues)
                {
                    foreach (var segment in sport.Segments)
                    {
                        if (hasParent)
                            result.Append("<br/><br/>");

                        result.Append($"<b>{sport.SportName} > {segment.SegmentName.Trim()} > League</b></br>");
                        result.Append(string.Join(", ", segment.Leagues.Select(s => s.LeagueName.Trim())));

                        hasParent = true;
                    }
                }
            }

            return result.ToString();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
