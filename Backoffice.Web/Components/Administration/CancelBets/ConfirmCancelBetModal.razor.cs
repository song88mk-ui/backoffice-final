using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Text.Json;

namespace Backoffice.Web.Components.Administration.CancelBets
{
    public partial class ConfirmCancelBetModal : IDisposable
    {
        #region Properties
        [Inject] public IBackOfficeService BackOfficeService { get; set; }
        [Inject] private IModalService ModalService { get; set; }
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }
        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        public CancelBetModel CancelBetModel { get; set; }
        public List<AlertModel> Alerts { get; set; }
        #endregion

        #region Methods
        protected override async Task OnInitializedAsync()
        {
            Alerts = new List<AlertModel>();
            CancelBetModel = ModalParameters.TryGet<CancelBetModel>("CancelBetModel");
        }

        public async Task OnCloseModalClickEvent()
        {
            ModalService.Cancel();
        }

        public async Task OnConfirmCancelClickEvent()
        {
            try
            {
                Alerts.Clear();

                var request = new BetModifyRequest
                {
                    BetIds = CancelBetModel.BetReferenceIds.Select(s => s.BetReferenceId).ToList(),
                    NewStatus = BetStatus.Cancelled,
                    Reason = CancelBetModel.Reason,
                    ModifiedByUserId = 0,
                };


                var response = (await BackOfficeService.CancelBet(request));

                if (response.StatusCode == StatusCode.Success)
                {
                    //TODO: fetch details of each bet and save into activity log payload

                    //activity log
                    foreach(var data in response.Result.Data)
                    {
                        await BackOfficeService.AddActivityLog(new ActivityLogModel
                        {
                            Page = Page.Admin_CancelBets.ToString().Replace("_", "."),
                            Payload = $"{data.BetId}",
                            Activity = Activity.Delete.ToString(),
                            From = data.OldBetStatus,
                            To = BetStatus.Cancelled.ToString(),
                            UserId = UserModel.Id,
                            UserName = UserModel.UserName,
                        });
                    }
                    
                    Alerts.Add(new AlertModel
                    {
                        AlertStatus = "Success",
                        AltertMessage = "User Group successfully deleted",
                        Opacity = 1.0m
                    });

                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(3500);

                    ModalService.Cancel();

                    await Modal.OnSaveClickCallback.InvokeAsync();
                }
                else
                {
                    if(response.Result != null && response.Result.Data != null)
                    {
                        foreach(var data in response.Result.Data)
                        {
                            if(data.IsSuccess == false)
                            {
                                Alerts.Add(new AlertModel
                                {
                                    AlertStatus = "Error",
                                    AltertMessage = $"{data.Message}",
                                    Opacity = 1.0m
                                });
                            }

                        }
                    }
                    else
                    {
                        Alerts.Add(new AlertModel
                        {
                            AlertStatus = "Error",
                            AltertMessage = $"{response.Message}",
                            Opacity = 1.0m
                        });
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public void Dispose()
        {

        }
        #endregion
    }
}
