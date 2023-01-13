using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Pages.OddsEngine
{
    public partial class MessageSettlement : IDisposable
    {
        #region Properties
        [Inject] ITradingService TradingService { get; set; }
        public MarketSettlementFilterModel MarketSettlementFilterModel { get; set; }
        public List<MessageSettlementModel> MessageSettlementModel { get; set; }
        public string PageTitle { get; set; }
        public string IconClass { get; set; }
        private bool IsPreLoader { get; set; }
        private long CursorId { get; set; }
        private int PreviousPage { get; set; }
        public MetaData MetaData { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            IsPreLoader = true;
            PageTitle = "Message Settlement";
            IconClass = string.Empty;

            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 100,
                TotalCount = 0,
                TotalPages = 1,
            };
        }

        public async Task SelectPage(int page)
        {
        }

        public void Dispose()
        {
        }

        #endregion

        #region Utulities
        public async Task GetContents()
        {
            IsPreLoader = true;

            var request = new ActivityLogRequest()
            {
                Page = Page.OE_Administration_MessageSettlement.ToString().Replace("_", "."),
                Payload = BuildPayload(),
                MetaData = MetaData,
                DateFrom = MarketSettlementFilterModel.FromDate.Date.Add(new TimeSpan(0, 0, 0)),
                DateTo = MarketSettlementFilterModel.ToDate.Date.Add(new TimeSpan(23, 59, 59))
            };

            var logs = await TradingService.GetActivityLogs(request);

            var result = logs?.Result?.ActivityLogs?.Select(x => new MessageSettlementModel()
            {
                Id = x.Id,
                BetDate = JsonSerializer.Deserialize<MessageSettlementTo>(x.To).BetDate,
                BetId = JsonSerializer.Deserialize<MessageSettlementTo>(x.To).BetId,
                Sports = JsonSerializer.Deserialize<MessageSettlementTo>(x.To).SportName,
                MessagesSettled = JsonSerializer.Deserialize<MessageSettlementTo>(x.To).FixtureName,
                Product = GetProducts().FirstOrDefault(y => y.Value == JsonSerializer.Deserialize<MessageSettlementTo>(x.To).ProductId).Name,
                SettledBy = x.UserId,
                DateSettled = x.TimeStamp,
                Reason = JsonSerializer.Deserialize<MessageSettlementTo>(x.To).Reason
            }).ToList();

            MetaData = logs?.Result?.MetaData;
            CursorId = result?.LastOrDefault()?.Id ?? 0;
            PreviousPage = MetaData.CurrentPage;
            IsPreLoader = false;
            MessageSettlementModel = result;
        }
        private string BuildPayload()
        {
            return $"{(MarketSettlementFilterModel.SportId > 0 ? MarketSettlementFilterModel.SportId : "%")}." +
                $"{(MarketSettlementFilterModel.SegmentId > 0 ? MarketSettlementFilterModel.SegmentId : "%")}." +
                $"{(MarketSettlementFilterModel.LeagueId > 0 ? MarketSettlementFilterModel.LeagueId : "%")}." +
                $"{(!string.IsNullOrEmpty(MarketSettlementFilterModel.FixtureId) ? MarketSettlementFilterModel.FixtureId : "%")}";
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
