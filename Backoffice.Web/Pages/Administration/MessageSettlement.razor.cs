using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Components.Administration.MessageSettlement;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Backoffice.Web.Pages.Administration
{
    public partial class MessageSettlement : IDisposable
    {
        #region Properties

        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IMessageBroker MessageBroker { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] private ToastService? ToasterService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }
        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }

        public MarketSettlementFilterModel FilterModel { get; set; } = new MarketSettlementFilterModel();
        public List<MessageSettlementModel> MessageSettlementModel { get; set; }
        public MetaData MetaData { get; set; }

        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        private bool IsPreLoader { get; set; }
        private long CursorId { get; set; }
        private int PreviousPage { get; set; }

        #endregion

        #region Fields

        private ToastService _toasterService => ToasterService!;

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = true;
            PageTitle = "Message Settlement";
            IconClass = "ico ico-message-settlement";

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };

            await AuthenticateAccess.InvokeAsync();
        }

        public async Task OnAddClickedEvent()
        {
            var parameters = new ModalParameters();

            parameters.Add("DialogStyle", "max-width: 80%; overflow-y: initial !important");

            var options = new ModalOptions();

            ModalService.Open<AddMessageSettlement>("Add Message Settlement", parameters, options);
        }

        public async Task OnSaveClickedEvent()
        {
            await GetContents();
        }

        public async Task OnExportClickedEvent()
        {
            _toasterService.AddToast(Toast.NewToast
                    ("Message Settlement", 
                    $"Success! Excel file is being generated system will notify once done."));
        }

        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;

            await GetContents();
        }

        public void Dispose()
        {
            BackOfficeService.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utulities

        public async Task GetContents()
        {
            IsPreLoader = true;

            var request = new ActivityLogRequest()
            {
                Page = AdminPermission.Admin_Settlements_MessageSettlement.ToString().Replace("_", "."),
                Payload = BuildPayload(),
                MetaData = MetaData,
                DateFrom = FilterModel.FromDate.Date.Add(new TimeSpan(0, 0, 0)),
                DateTo = FilterModel.ToDate.Date.Add(new TimeSpan(23, 59, 59))
            };

            var logs = await BackOfficeService.GetActivityLogs(request);

            var result = new List<MessageSettlementModel>();

            if (logs != null)
            {
                result = logs?.Result?.ActivityLogs?.Select(x => new MessageSettlementModel()
                {
                    Id = x.Id,
                    BetDate = JsonSerializer.Deserialize<MessageSettlementTo>(x.To).BetDate,
                    BetId = JsonSerializer.Deserialize<MessageSettlementTo>(x.To).BetId,
                    Sports = JsonSerializer.Deserialize<MessageSettlementTo>(x.To).SportName,
                    MessagesSettled = JsonSerializer.Deserialize<MessageSettlementTo>(x.To).FixtureName,
                    Product = GetProducts().FirstOrDefault(y => y.Value == JsonSerializer.Deserialize<MessageSettlementTo>(x.To).ProductId).Name,
                    SettledBy = x.UserName,
                    DateSettled = x.TimeStamp,
                    Reason = JsonSerializer.Deserialize<MessageSettlementTo>(x.To).Reason
                }).ToList();
            }

            MetaData = logs?.Result?.MetaData;
            CursorId = result?.LastOrDefault()?.Id ?? 0;
            PreviousPage = MetaData.CurrentPage;
            IsPreLoader = false;
            MessageSettlementModel = result;
            StateHasChanged();
        }

        private string BuildPayload()
        {
            return $"{(FilterModel.SportId > 0 ? FilterModel.SportId : "%")}." +
                $"{(FilterModel.SegmentId > 0 ? FilterModel.SegmentId : "%")}." +
                $"{(FilterModel.LeagueId > 0 ? FilterModel.LeagueId : "%")}." +
                $"{(!string.IsNullOrEmpty(FilterModel.FixtureId) ? FilterModel.FixtureId : "%")}";
        }

        private List<SelectListItemModel<int>> GetProducts()
        {
            return new List<SelectListItemModel<int>>()
            {
                new SelectListItemModel<int> { Value = 1, Name = "Live Odds" },
                new SelectListItemModel<int> { Value = 3, Name = "Prematch Odds (CTRL)" },
                new SelectListItemModel<int> { Value = 4, Name = "betpal" },
                new SelectListItemModel<int> { Value = 5, Name = "Premium Cricket" },
                new SelectListItemModel<int> { Value = 6, Name = "Virtual Soccer" },
                new SelectListItemModel<int> { Value = 7, Name = "wns" },
                new SelectListItemModel<int> { Value = 8, Name = "Virtual Basketball" },
                new SelectListItemModel<int> { Value = 9, Name = "vto" },
                new SelectListItemModel<int> { Value = 10, Name = "Virtual Dog Racing" },
                new SelectListItemModel<int> { Value = 11, Name = "Virtual Horses" },
                new SelectListItemModel<int> { Value = 12, Name = "Virtual Tennis" },
                new SelectListItemModel<int> { Value = 14, Name = "codds" },
                new SelectListItemModel<int> { Value = 15, Name = "Virtual Baseball" },
                new SelectListItemModel<int> { Value = 17, Name = "Virtual Cricket" },
            };
        }

        #endregion
    }
}
