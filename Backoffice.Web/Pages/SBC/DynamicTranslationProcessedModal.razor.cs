using Backoffice.Web.Components.Common;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Backoffice.Web.Pages.SBC
{
    public partial class DynamicTranslationProcessedModal
    {
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] public CustomModal Modal { get; set; }

        private string StringId { get; set; } = string.Empty;
        private bool Processed { get; set; }
        private Func<Task<bool>> OnConfirmCallback { get; set; }

        protected override void OnInitialized()
        {
            StringId = ModalParameters.TryGet<string>("StringId") ?? string.Empty;
            Processed = ModalParameters.TryGet<bool>("Processed");
            OnConfirmCallback = ModalParameters.TryGet<Func<Task<bool>>>("OnConfirmCallback");
        }

        private async Task Confirm()
        {
            if (OnConfirmCallback != null)
            {
                var success = await OnConfirmCallback.Invoke();
                if (success)
                {
                    _modalService.Close();
                }
            }
        }

        private void Cancel()
        {
            _modalService.Cancel();
        }
    }
}
