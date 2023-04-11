using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.SportSettings
{
    public partial class Filter : IDisposable
    {
        #region Properties

        [Inject] public ITradingService TradingService { get; set; }

        [CascadingParameter(Name = "UserModel")] public UserModel UserModel { get; set; }

        [Parameter] public EventCallback<string> OnClientChangedCallback { get; set; }
        [Parameter] public EventCallback OnUpdateClickCallback {get;set; }
        [Parameter] public EventCallback OnCancelClickCallback { get; set; }
        [Parameter] public EventCallback OnSaveClickCallback { get; set; }
        [Parameter] public EventCallback OnCopyClickCallback { get; set; }
        [Parameter] public EventCallback OnViewLogsCallback { get; set; }
        [Parameter] public bool IsPreload { get; set; }
        [Parameter] public string Region { get; set; }

        public List<SelectListItemModel<string>> Clients { get; set; }
        public string SelectedClientCode { get; set; }
        public bool IsUpdate { get; set; }

        #endregion

        #region Methods

        protected override async Task OnInitializedAsync()
        {
            Clients = new List<SelectListItemModel<string>>();
            TradingService.Region = Region;

            var clientResponse = await TradingService.GetClientList();

            if (clientResponse.StatusCode == StatusCode.Success)
            {
                if (UserModel.ClientCodes.Any())
                    clientResponse.Result = clientResponse.Result.Where(c => UserModel.ClientCodes.Contains(c.ClientCode.ToUpper())).ToList();

                Clients = clientResponse.Result.Select(s => new SelectListItemModel<string>
                {
                    Value = s.ClientCode,
                    Name = s.ClientName,
                }).ToList();
            }

            SelectedClientCode = Clients.FirstOrDefault().Value ?? string.Empty;

            if (!string.IsNullOrEmpty(SelectedClientCode))
            {
                await OnClientChangedCallback.InvokeAsync(SelectedClientCode);
            }
        }

        public async void OnClientChangedEvent(ChangeEventArgs e)
        {
            SelectedClientCode = e.Value.ToString();
            IsUpdate = false;

            await OnClientChangedCallback.InvokeAsync(SelectedClientCode);
        }

        public async Task OnUpdateClickEvent()
        {
            if (IsPreload)
                return;

            IsUpdate = true;

            await OnUpdateClickCallback.InvokeAsync();
        }

        public async Task OnCancelClickEvent()
        {
            if (IsPreload)
                return;

            IsUpdate = false;

            await OnCancelClickCallback.InvokeAsync();
        }

        public async Task OnSaveClickEvent()
        {
            if (IsPreload)
                return;

            IsUpdate = false;

            await OnSaveClickCallback.InvokeAsync();
        }

        public async Task OnCopyClickEvent()
        {
            if (IsPreload)
                return;

            if (string.IsNullOrEmpty(SelectedClientCode))
                return;

            await OnCopyClickCallback.InvokeAsync();
        }

        public async Task OnViewLogsClickEvent()
        {
            if (IsPreload)
                return;

            if (string.IsNullOrEmpty(SelectedClientCode))
                return;

            await OnViewLogsCallback.InvokeAsync();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
