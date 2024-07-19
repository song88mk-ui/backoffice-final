using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Drawing;
using System.Text;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ViewLogs : IDisposable
    {
        #region Properties
        [Inject] private ITradingService TradingService { get; set; }
        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        private ConfigLogsFilterModel ConfigLogsFilterModel { get; set; }
        private MetaData MetaData { get; set; } = new MetaData();
        private int CursorId { get; set; }
        private int PreviousPage { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            PreviousPage = 1;
            ConfigLogsFilterModel = new ConfigLogsFilterModel()
            {
                ConfigLogsDetails = new List<ConfigLogsDetailsModel>(),
                FixtureTypeId = 0,
                ClientId = Guid.Empty,
                SportId = 0,
                SegmentId = 0,
                LeagueId = 0,
                MarketTemplateGroupId = 0,
                MarketTemplateIds = null,
                Scope = string.Empty,
                MarketId = string.Empty,
                FromDate = DateTime.Now,
                ToDate = DateTime.Now,
                Filter = string.Empty,
            };

            TradingService.Region = ModalParameters.TryGet<string>("Region").ToUpper();
            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 0,
                TotalPages = 0,
            };            
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                                
            }
        }

        public async Task GetContents()
        {
            var request = new ConfigLogsRequest()
            {
                FixtureTypeId = ConfigLogsFilterModel.FixtureTypeId,
                ClientId = ConfigLogsFilterModel.ClientId,
                SportId = ConfigLogsFilterModel.SportId,
                SegmentId = ConfigLogsFilterModel.SegmentId,
                LeagueId = ConfigLogsFilterModel.LeagueId,
                MarketTemplateIds = ConfigLogsFilterModel.MarketTemplateIds,
                MarketId = ConfigLogsFilterModel.MarketId,
                FromDate = ConfigLogsFilterModel.FromDate,
                ToDate = ConfigLogsFilterModel.ToDate,
                Scope = ConfigLogsFilterModel.Scope,
                Filter = ConfigLogsFilterModel.Filter,
                MetaData = MetaData,
                Id = CursorId,
                CursorDirection = PreviousPage >= MetaData.CurrentPage ? "Previous" : "Next"
            };

            var logs = (await TradingService.GetConfigLogs(request)).Result;

            MetaData = logs?.Item2;

            var result = logs?.Item1.Select(c => new ConfigLogsDetailsModel
            {
                Id = c.Id,
                Date = c.TimeStamp.ToString("dd/MM/yyyy hh:m tt"),
                Client = c.ClientCode,
                Sports = c.Sports,
                Segment = c.Segment,
                League = c.League,
                ChangesMade = c.MarketTemplate,
                OldValue = FormatChanges(c, "oldValue"),
                NewValue = FormatChanges(c, null),
                UpdatedBy = c.Username
            }).ToList();
            CursorId = result?.LastOrDefault()?.Id ?? 0;
            PreviousPage = MetaData.CurrentPage;

            ConfigLogsFilterModel.ConfigLogsDetails = result.ToList();

            await InvokeAsync(StateHasChanged);
        }

        public async Task OnCloseModalClickEvent()
        {
            ModalService.Cancel();
        }

        private async Task SelectedPage(int page)
        {
            MetaData.CurrentPage = page;

            await GetContents();
        }

        public void Dispose()
        {
            
        }
        #endregion

        #region Utilities
        private string FormatChanges(ConfigLogsResponse configLogs, string status)
        {
            var value = new StringBuilder();
            if (configLogs != null)
            {
                if (status == "oldValue")
                {
                    if (configLogs.OldPrematchSettings != configLogs.NewPrematchSettings)
                        value.Append($"<b>{@TranslationState.GetValue("Prematch")}</b><br/>{Format(configLogs.OldPrematchSettings)}</br>");

                    if (configLogs.OldLiveSettings != configLogs.NewLiveSettings)
                        value.Append($"<b>{@TranslationState.GetValue("Live")}</b></br>{Format(configLogs.OldLiveSettings)}</br>");

                    if (configLogs.OldStatus != configLogs.NewStatus)
                        value.Append($"<b>Market Status</b></br>{(configLogs.OldStatus == true ? "Active" : "Inactive")}");
                }
                else
                {
                    if (configLogs.OldPrematchSettings != configLogs.NewPrematchSettings)
                        value.Append($"<b>{@TranslationState.GetValue("Prematch")}</b></br>{Format(configLogs.NewPrematchSettings)}</br>");

                    if (configLogs.OldLiveSettings != configLogs.NewLiveSettings)
                        value.Append($"<b>{@TranslationState.GetValue("Live")}</b></br>{Format(configLogs.NewLiveSettings)}</br>");

                    if (configLogs.OldStatus != configLogs.NewStatus)
                        value.Append($"<b>Market Status</b></br>{(configLogs.NewStatus == true ? "Active" : "Inactive")}");
                }
            }
            return value.ToString();
        }

        private string Format(string marketMargin)
        {
            var value = string.Empty;
            if (!string.IsNullOrWhiteSpace(marketMargin))
            {
                var collection = JsonSerializer.Deserialize<List<MarketMargin>>(marketMargin);
                if (collection != null)
                {
                    foreach (var item in collection.ToList())
                    {
                        value += $"{(MockData.PrematchSettings.FirstOrDefault(c => c.Time == item.T)?.TimeDisplay) ?? "0"} {item.M}</br>";
                    }
                }
            }
            return value;
        }
        #endregion
    }
}
