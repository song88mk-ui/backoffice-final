using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Backoffice.Web.Components.Common
{
    public partial class BlazoredToolTip : IDisposable
    {
        #region Fields

        #endregion

        #region Properties
        [Inject] private IModalService ModalService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public bool IsVisible { get; set; }
        [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
        [Parameter] public Type contentComponent { get; set; }
        [Parameter] public ModalParameters Parameters { get; set; }
        public ElementReference TooltipReference { get; set; }

        public string ComponentPosition { get; set; }
        public string ComponentClass { get; set; }
        private string Title { get; set; }

        private RenderFragment Content { get; set; }

        private bool IsParameterSet { get; set; } = false;

        #endregion

        #region Methods
        protected override void OnInitialized()
        {
            IsVisible = false;
            ((ModalService)ModalService).CloseModal += CloseModal;
        }


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (IsVisible && IsParameterSet == false)
            {

                Title = Parameters.Get<string>("Title");

                IsParameterSet = true;

                var content = new RenderFragment(s => { s.OpenComponent(1, contentComponent); s.CloseComponent(); });
                Content = content;

                ComponentPosition = "up";
                int MaxWidth = Parameters.Get<int>("MaxWidth");

                if (!string.IsNullOrEmpty(TooltipReference.Id))
                {
                    var rawdimension = await JSRuntime.InvokeAsync<string>("getDimensions", TooltipReference, MaxWidth);
                    if (rawdimension != null)
                    {
                        var dimension = JsonSerializer.Deserialize<DimensionModel>(rawdimension);
                        ComponentPosition = dimension.Position;
                    }
                }

                ComponentClass = Parameters.Get<string>("Class");

                await InvokeAsync(StateHasChanged);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        private async void CloseModal()
        {
            IsParameterSet = false;
            IsVisible = false;
            Content = null;

            await IsVisibleChanged.InvokeAsync(IsVisible);
            await InvokeAsync(StateHasChanged);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((ModalService)ModalService).CloseModal -= CloseModal;
            }
        }

        public void SetTitle(string title)
        {
            Title = title;
            StateHasChanged();
        }

        #endregion
    }
}
