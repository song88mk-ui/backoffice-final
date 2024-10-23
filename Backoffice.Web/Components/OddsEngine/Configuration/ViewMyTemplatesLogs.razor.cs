using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Drawing;
using System.Text;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Configuration
{
    public partial class ViewMyTemplatesLogs : IDisposable
    {
        #region Properties

        [Inject] private IModalService ModalService { get; set; }
        [Inject] private IBackofficeService BackofficeService { get; set; }
        [CascadingParameter(Name = "UserModel")] private UserModel UserModel { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        public List<SelectListItemModel<string>> Sports { get; set; }
        public List<SelectListItemModel<string>> Scopes { get; set; }
        public ConfigActivityLogFilterModel FilterModel { get; set; }
        public List<ActivityLogModel> ActivitLogs { get; set; }
        public MetaData MetaData { get; set; }

        private string _region = string.Empty;

        private bool IsPreLoader = true;
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = true;
            ActivitLogs = new List<ActivityLogModel>();
            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 20
            };
            _region = ModalParameters.TryGet<string>("Region");
            var sports = ModalParameters.TryGet<List<SelectListItemModel<string>>>("Sports") ?? new List<SelectListItemModel<string>>();
            var scopes = ModalParameters.TryGet<List<SelectListItemModel<string>>>("Scopes") ?? new List<SelectListItemModel<string>>();

            Sports = new List<SelectListItemModel<string>>();
            Scopes = new List<SelectListItemModel<string>>();
            Sports.Add(new SelectListItemModel<string>()
            {
                Value = "-1",
                Name = "All Sports"
            });
            Sports.AddRange(sports);
            Scopes.Add(new SelectListItemModel<string>()
            {
                Value = "-1",
                Name = "All Scopes"
            });
            Scopes.AddRange(scopes);

            if (FilterModel == null)
            {
                FilterModel = new ConfigActivityLogFilterModel()
                {
                    SelectedSportId = Sports.FirstOrDefault()?.Value.ToString() ?? "-1",
                    SelectedScopeId = Scopes.FirstOrDefault()?.Value.ToString() ?? "0",
                    DateFrom = DateTime.Now,
                    DateTo = DateTime.Now,
                    SearchText = ""
                };
            }

            await SelectPage(1);
        }
        public string GetLogDetails(ActivityLogModel activityLog)
        {
            StringBuilder stringBuilder = new StringBuilder();

            var payloads = activityLog.Payload.Replace("_", " ").Split('.');
            var sportName = payloads.Count() > 0 ? Sports.FirstOrDefault(x => x.Value == payloads[0])?.Name : "";
            var scopeName = payloads.Count() > 1 ? Scopes.FirstOrDefault(x => x.Value == payloads[1])?.Name : "";
            var template = payloads.Count() > 2 ? payloads[2] : "";

            stringBuilder.Append($"<th class=\"px-4\" scope=\"row\">{activityLog.TimeStamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm")}</th>");
            stringBuilder.Append($"<td class=\"px-4\">{sportName}</td>");
            stringBuilder.Append($"<td class=\"px-4\">{scopeName}</td>");
            stringBuilder.Append($"<td class=\"px-4\">{template}</td>");

            if (activityLog.Activity == Activity.Create.ToString())
            {
                stringBuilder.Append($"<td class=\"px-4\">{_translationState.GetValue("Added New Template")}</td>");
                stringBuilder.Append($"<td class=\"px-4\">{activityLog.From}</td>");
                var toPaylaod = JsonSerializer.Deserialize<ConfigTemplateModel>(activityLog.To);
                stringBuilder.Append($"<td class=\"px-4\">{toPaylaod?.Name ?? "-"}</td>");
            }
            else if(activityLog.Activity == Activity.Copy.ToString())
            {
                stringBuilder.Append($"<td class=\"px-4\">{_translationState.GetValue("Copied Template")}</td>");
                var fromPaylaod = JsonSerializer.Deserialize<ConfigTemplateModel>(activityLog.From);
                var toPaylaod = JsonSerializer.Deserialize<ConfigTemplateModel>(activityLog.To);
                stringBuilder.Append($"<td class=\"px-4\">{fromPaylaod?.Name ?? "-"}</td>");
                stringBuilder.Append($"<td class=\"px-4\">{toPaylaod?.Name ?? "-"}</td>");
            }
            else if (activityLog.Activity == Activity.Delete.ToString())
            {
                stringBuilder.Append($"<td class=\"px-4\">{_translationState.GetValue("Deleted Template")}</td>");
                var fromPaylaod = JsonSerializer.Deserialize<ConfigTemplateModel>(activityLog.From);
                stringBuilder.Append($"<td class=\"px-4\">{activityLog.To}</td>");
                stringBuilder.Append($"<td class=\"px-4\">{fromPaylaod?.Name ?? "-"}</td>");
            }
            else
            {
                if (payloads.Count() > 3)
                {
                    switch (payloads.LastOrDefault()?.ToUpper())
                    {
                        case "T":
                            var tFromPayload = new ConfigTemplateLogModel();
                            if (activityLog.From != "-")
                            {
                                tFromPayload = JsonSerializer.Deserialize<ConfigTemplateLogModel>(activityLog.From);
                            }
                            var tToPayload = new ConfigTemplateLogModel();
                            if (activityLog.To != "-")
                            {
                                tToPayload = JsonSerializer.Deserialize<ConfigTemplateLogModel>(activityLog.To);
                            }

                            stringBuilder.Append($"<td class=\"px-4\">");
                            stringBuilder.Append($"<strong>{_translationState.GetValue("Clients")}</strong>");
                            stringBuilder.Append($"<br>");
                            foreach (var client in tFromPayload.Payloads.Clients)
                            {
                                stringBuilder.Append($"<span>{client}</span>");
                                stringBuilder.Append($"<br>");
                            }
                            stringBuilder.Append($"<br>");
                            stringBuilder.Append($"<strong>{_translationState.GetValue("Leagues")}</strong>");
                            foreach (var league in tFromPayload.Payloads.Leagues)
                            {
                                stringBuilder.Append($"<br>");
                                stringBuilder.Append($"<span>{league}</span>");
                            }
                            stringBuilder.Append($"</td>");
                            stringBuilder.Append($"<td class=\"px-4\">{tFromPayload.TemplateName}</td>");
                            stringBuilder.Append($"<td class=\"px-4\">{tToPayload.TemplateName}</td>");
                            break;
                        case "M":
                            stringBuilder.Append($"<td class=\"px-4\">{payloads[3]}</td>");
                            if (activityLog.From == "-")
                            {
                                stringBuilder.Append($"<td class=\"px-4\">-</td>");
                            }
                            else
                            {
                                stringBuilder.Append($"<td class=\"px-4\">");
                                var payload = JsonSerializer.Deserialize<List<ConfigMarginActivityLogModel>>(activityLog.From) ?? new List<ConfigMarginActivityLogModel>();
                                foreach (var item in payload)
                                {
                                    stringBuilder.Append($"<strong>{item.MarketTemplateName}</strong>");
                                    stringBuilder.Append($"<br>");
                                    stringBuilder.Append(string.Join("", item.Payload.Select(x => $"<span>{x}</span><br>").ToList()));
                                    stringBuilder.Append($"<br>");
                                }
                                stringBuilder.Append($"</td>");
                            }
                            if (activityLog.To == "-")
                            {
                                stringBuilder.Append($"<td class=\"px-4\">-</td>");
                            }
                            else
                            {
                                stringBuilder.Append($"<td class=\"px-4\">");
                                var payload = JsonSerializer.Deserialize<List<ConfigMarginActivityLogModel>>(activityLog.To) ?? new List<ConfigMarginActivityLogModel>();
                                foreach (var item in payload)
                                {
                                    stringBuilder.Append($"<strong>{item.MarketTemplateName}</strong>");
                                    stringBuilder.Append($"<br>");
                                    stringBuilder.Append(string.Join("", item.Payload.Select(x => $"<span>{x}</span><br>").ToList()));
                                    stringBuilder.Append($"<br>");
                                }
                                stringBuilder.Append($"</td>");
                            }
                            break;
                        case "N":
                            stringBuilder.Append($"<td class=\"px-4\">{_translationState.GetValue("Changed Name")}</td>");
                            var fromPaylaod = JsonSerializer.Deserialize<ConfigTemplateModel>(activityLog.From);
                            var toPaylaod = JsonSerializer.Deserialize<ConfigTemplateModel>(activityLog.To);
                            stringBuilder.Append($"<td class=\"px-4\">{fromPaylaod.Name}</td>");
                            stringBuilder.Append($"<td class=\"px-4\">{toPaylaod?.Name ?? "-"}</td>");
                            break;
                        default:
                            stringBuilder.Append($"<td class=\"px-4\">-</td>");
                            stringBuilder.Append($"<td class=\"px-4\">-</td>");
                            stringBuilder.Append($"<td class=\"px-4\">-</td>");
                            break;
                    }
                }
            }
            stringBuilder.Append($"<td class=\"px-4\">{activityLog.UserName}</td>");
            return stringBuilder.ToString();
        }
        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;
            await GetContent(FilterModel);
        }
        public async Task OnSportClickEvent(ChangeEventArgs e)
        {
            if (e != null)
            {
                FilterModel.SelectedSportId = e == null ? Sports.FirstOrDefault().Value : e.Value.ToString();
            }
            await SelectPage(1);
        }
        public async Task OnScopeClickEvent(ChangeEventArgs e)
        {
            if (e != null)
            {
                FilterModel.SelectedScopeId = e == null ? Scopes.FirstOrDefault().Value : e.Value.ToString();
            }

            await SelectPage(1);
        }
        public async Task OnDatePickerClickEvent(DateRange range)
        {
            FilterModel.DateFrom = Convert.ToDateTime(range.Start.ToString("yyyy-MM-dd 00:00:00"));
            FilterModel.DateTo = Convert.ToDateTime(range.End.ToString("yyyy-MM-dd 23:59:59"));

            await SelectPage(1);
        }
        public async Task OnInputSearchEvent()
        {
            await GetContent(FilterModel);
        }
        public async Task OnCloseClickEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {

        }
        #endregion

        #region Utilities
        private async Task GetContent(ConfigActivityLogFilterModel filterModel)
        {
            if (!IsPreLoader)
            {
                IsPreLoader = true;
                StateHasChanged();
            }
            ActivitLogs = new List<ActivityLogModel>();
            var response = await BackofficeService.GetActivityLogs(new ActivityLogRequest
            {
                Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", "."),
                Payload = BuildPayload(filterModel),
                MetaData = MetaData,
                DateFrom = filterModel.DateFrom.Date.Add(new TimeSpan(0, 0, 0)).ToUniversalTime(),
                DateTo = filterModel.DateFrom.Date.Add(new TimeSpan(23, 59, 59)).ToUniversalTime(),
                SearchText = filterModel.SearchText
            });

            if (response.StatusCode == StatusCode.Success)
            {
                ActivitLogs = response.Result.ActivityLogs;
                MetaData = response.Result.MetaData;
            }

            IsPreLoader = false;
        }
        private string BuildPayload(ConfigActivityLogFilterModel filterModel)
        {
            return $"{(filterModel.SelectedSportId != "-1" ? filterModel.SelectedSportId : "%%")}." +
            $"{(filterModel.SelectedScopeId != "-1" ? filterModel.SelectedScopeId : "%%")}." +
            $"%%.%";
        }
        #endregion
    }
}
