using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Components.SBC.ParlayBonus;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.SBC
{
    public partial class Announcements : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }
        [CascadingParameter(Name = "UserClientCodes")] List<string>? UserClientCodes { get; set; }

        public MetaData MetaData { get; set; }

        public long ClientId { get; set; }
        public bool IsPreloading { get; set; }
        public int Skip { get; set; }
        public int TotalCount { get; set; }

        #endregion

        #region Fields
        private PageHeaderModel _pageHeaderModel;
        private bool _isSaving;
        #endregion

        #region Methods     

        protected override async Task OnInitializedAsync()
        {
            _pageHeaderModel = new()
            {
                Title = "Announcements",
                IconClass = "ico ico-parlay-bonus"
            };
            
            MetaData = new MetaData()
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 10000,
                TotalPages = 100,
            };

            await AuthenticateAccess.InvokeAsync();

        }

        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;

            Skip = (MetaData.CurrentPage - 1) * MetaData.PageSize;
        }

        public void Dispose()
        {
            BackofficeService.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities

        #endregion
    }
}