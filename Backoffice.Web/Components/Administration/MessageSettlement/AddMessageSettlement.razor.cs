using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Backoffice.Web.Components.Administration.MessageSettlement
{
    public partial class AddMessageSettlement : IDisposable
    {
        #region Properties

        [Inject] public IModalService ModalService { get; set; }
        [Inject] public ITradingService TradingService { get; set; }
        [Inject] public IBackofficeService BackofficeService { get; set; }

        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public AddMessageSettlementModel AddMessageSettlementModel { get; set; }

        public EventCallback OnModalSave { get; set; }
        public ElementReference SaveButton { get; set; }
        public bool IsPreloader { get; set; }
        private Dictionary<string, object> InputAttributes { get; set; } =
            new Dictionary<string, object>()
            {
                { "required", "required" }
            };

        #endregion

        #region Fields

        private string message = string.Empty;
        private string divStyles = "display:none";
        private string divClass = string.Empty;

        #endregion

        #region Methods

        protected override void OnInitialized()
        {
            IsPreloader = true;
            AddMessageSettlementModel = new AddMessageSettlementModel();
        }

        public async Task<AddMessageSettlementModel> GetBetDetails()
        {
            if (!string.IsNullOrWhiteSpace(AddMessageSettlementModel.BetId) && Guid.TryParse(AddMessageSettlementModel.BetId, out Guid validBetId))
            {
                var manualBetDetailsRequest = new ManualBetDetailsRequest
                {
                    BetIds = new List<string> { AddMessageSettlementModel.BetId }
                };

                var result = (await TradingService.GetManualBetDetails(manualBetDetailsRequest)).Result;
                AddMessageSettlementModel = result?.Bets?.Select(c => new AddMessageSettlementModel
                {
                    BetId = c.BetId.ToString(),
                    BetDate = c.BetDate,
                    BetSelections = c.Selections.Select(x => new BetSelection
                    {
                        IsChecked = c.Selections.Count == 1,
                        BetSelectionId = x.BetSelectionId.ToString(),
                        SportId = x.SportId,
                        SportName = x.SportName,
                        SegmentId = x.SegmentId,
                        LeagueId = x.LeagueId,
                        FixtureId = long.Parse(x.FixtureId),
                        FixtureName = x.FixtureName,
                        InitialBetSelectionStatusId = x.BetSelectionStatusId,
                        MarketTemplateId = x.MarketTemplateId,
                        ProductId = GetProducts().FirstOrDefault(c => c.Value == int.Parse(x.SelectionSpecifier.Split("/sr:sport").FirstOrDefault().Split("uof:").LastOrDefault())).Value,
                        SelectionSpecifier = x.SelectionSpecifier,
                        FixtureSpecifier = x.FixtureSpecifier,
                        RequestId = 0,
                        Reason = string.Empty,
                        Required = c.Selections.Count == 1 ? InputAttributes : new Dictionary<string, object>()
                    }).ToList()
                }).FirstOrDefault();

                if (AddMessageSettlementModel == null)
                    AddMessageSettlementModel = new AddMessageSettlementModel();

                return AddMessageSettlementModel;
            }
            else
                return AddMessageSettlementModel;
        }

        public async Task OnSettleClickEvent()
        {
            var betselectionCount = AddMessageSettlementModel.BetSelections.Count;
            var isAnyChecked = AddMessageSettlementModel.BetSelections.Any(x => x.IsChecked);

            if (betselectionCount == 1 || isAnyChecked)
            {
                var messageSettlementRequest = new List<MessageSettlementRequest>();

                foreach (var item in AddMessageSettlementModel.BetSelections)
                {
                    if ((betselectionCount == 1 && !string.IsNullOrWhiteSpace(item.Reason)) || (item.IsChecked && !string.IsNullOrWhiteSpace(item.Reason)))
                    {
                        int requestId = 318;
                        var activityLog = new ActivityLogModel()
                        {
                            Page = AdminPermission.Admin_Settlements_MessageSettlement.ToString().Replace("_", "."),
                            Payload = $"{item.SportId}.{item.SegmentId}.{item.LeagueId}.{item.FixtureId}",
                            Activity = Activity.Create.ToString(),
                            From = "-",
                            To = JsonSerializer.Serialize(new MessageSettlementTo()
                            {
                                BetId = AddMessageSettlementModel.BetId,
                                BetSelectionId = item.BetSelectionId,
                                BetDate = AddMessageSettlementModel.BetDate,
                                SportName = item.SportName,
                                FixtureName = item.FixtureName,
                                Reason = item.Reason,
                                SelectionSpecifier = item.SelectionSpecifier,
                                FixtureSpecifier = item.FixtureSpecifier,
                                RequestId = requestId,
                                ProductId = item.ProductId,
                                InitialBetSelectionStatusId = item.InitialBetSelectionStatusId,
                                MarketTemplateId = item.MarketTemplateId
                            }),
                            UserId = UserModel.Id,
                            UserName = UserModel.UserName,
                            TimeStamp = DateTime.UtcNow
                        };

                        var productId = int.Parse(item.SelectionSpecifier.Split("/sr:sport").FirstOrDefault().Split("uof:").LastOrDefault());
                        var messageSettlementIodocsRequest = new MessageSettlementIodocsRequest()
                        {
                            Product = GetProducts().FirstOrDefault(c => c.Value == productId).Name,
                            FixtureSpecifier = item.FixtureSpecifier,
                            RequestId = requestId,
                        };

                        var request = new MessageSettlementRequest()
                        {
                            MessageSettlementIodocsRequest = messageSettlementIodocsRequest,
                            OnActivityLogMessage = activityLog
                        };

                        messageSettlementRequest.Add(request);

                        if (messageSettlementIodocsRequest.Product == "pre" || messageSettlementIodocsRequest.Product == "liveodds")
                        {
                            var prod = messageSettlementIodocsRequest.Product == "pre" ? "liveodds" : "pre";
                            var messageSettlementIodocsRequest2 = new MessageSettlementIodocsRequest()
                            {
                                Product = prod,
                                FixtureSpecifier = item.FixtureSpecifier,
                                RequestId = requestId,
                            };

                            var activityLog2 = new ActivityLogModel()
                            {
                                Page = AdminPermission.Admin_Settlements_MessageSettlement.ToString().Replace("_", "."),
                                Payload = $"{item.SportId}.{item.SegmentId}.{item.LeagueId}.{item.FixtureId}",
                                Activity = Activity.Create.ToString(),
                                From = "-",
                                To = JsonSerializer.Serialize(new MessageSettlementTo()
                                {
                                    BetId = AddMessageSettlementModel.BetId,
                                    BetSelectionId = item.BetSelectionId,
                                    BetDate = AddMessageSettlementModel.BetDate,
                                    SportName = item.SportName,
                                    FixtureName = item.FixtureName,
                                    Reason = item.Reason,
                                    SelectionSpecifier = item.SelectionSpecifier,
                                    FixtureSpecifier = item.FixtureSpecifier,
                                    RequestId = requestId,
                                    ProductId = GetProducts().FirstOrDefault(c => c.Name == prod).Value,
                                    InitialBetSelectionStatusId = item.InitialBetSelectionStatusId,
                                    MarketTemplateId = item.MarketTemplateId
                                }),
                                UserId = UserModel.Id,
                                UserName = UserModel.UserName,
                                TimeStamp = DateTime.UtcNow
                            };

                            var request2 = new MessageSettlementRequest()
                            {
                                MessageSettlementIodocsRequest = messageSettlementIodocsRequest2,
                                OnActivityLogMessage = activityLog2
                            };

                            messageSettlementRequest.Add(request2);
                        }
                    }
                }

                if (messageSettlementRequest.Count != 0)
                {
                    var response = await TradingService.AddManualMessageSettlement(messageSettlementRequest);

                    var onActivityLogListMessage = new ActivityLogListRequest();

                    foreach (var item in messageSettlementRequest)
                    {
                        onActivityLogListMessage.ActivityLogs.Add(item.OnActivityLogMessage);
                    }

                    if (onActivityLogListMessage.ActivityLogs.Any())
                    {
                        var activityLogResponse = await BackofficeService.AddActivityLogList(onActivityLogListMessage);
                    }


                    divClass = response.Result.Contains("NOTE") ? "bg-warning" : "bg-success";
                    divStyles = "display: block";
                    if (!response.Result.Contains("NOTE"))
                        message = $"Done: Request to resend all stateful-messages (BetSettlement, RollbackBetSettlement, BetCancel, UndoBetCancel) for a sport event. {response.Result}";
                    else
                        message = response.Result.Replace("<br/>","");

                    await InvokeAsync(StateHasChanged);
                    await Modal.OnSaveClickCallback.InvokeAsync();
                    await Task.Delay(3500);
                    ModalService.Cancel();
                }
            }
            else
            {
                divClass = "bg-danger";
                divStyles = "display: inline";
                message = "Please select an item to settle. ";
            }
        }

        public void ToggleCheckbox(string betSelectionId)
        {
            divClass = string.Empty;
            divStyles = "display:none";
            message = string.Empty;

            foreach (var item in AddMessageSettlementModel.BetSelections)
            {
                if (item.BetSelectionId == betSelectionId)
                {
                    item.IsChecked = !item.IsChecked;
                    item.Required = item.Required == InputAttributes ? new Dictionary<string, object>() : InputAttributes;
                }
            }
        }

        public void OnCloseClickEvent()
        {
            ModalService.Cancel();
        }

        public void Dispose()
        {

        }

        #endregion

        #region Utilities

        private List<SelectListItemModel<int>> GetProducts()
        {
            return new List<SelectListItemModel<int>>()
            {
                new SelectListItemModel<int> { Value = 1, Name = "liveodds" },
                new SelectListItemModel<int> { Value = 3, Name = "pre" },
                //new SelectListItemModel<int> { Value = 4, Name = "betpal" },
                new SelectListItemModel<int> { Value = 5, Name = "premium_cricket" },
                new SelectListItemModel<int> { Value = 6, Name = "vf" },
                //new SelectListItemModel<int> { Value = 7, Name = "wns" },
                new SelectListItemModel<int> { Value = 8, Name = "vbl" },
                new SelectListItemModel<int> { Value = 9, Name = "vto" },
                new SelectListItemModel<int> { Value = 10, Name = "vdr" },
                new SelectListItemModel<int> { Value = 11, Name = "vhc" },
                new SelectListItemModel<int> { Value = 12, Name = "vti" },
                //new SelectListItemModel<int> { Value = 14, Name = "codds" },
                new SelectListItemModel<int> { Value = 15, Name = "vbi" },
                new SelectListItemModel<int> { Value = 17, Name = "vci" },
            };
        }

        #endregion
    }
}