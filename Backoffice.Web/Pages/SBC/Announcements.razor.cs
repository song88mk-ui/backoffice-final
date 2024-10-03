using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class Announcements : IDisposable
    {
        #region Properties

        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }

        [CascadingParameter(Name = "AuthenticateAccessEvent")] private EventCallback AuthenticateAccess { get; set; }

        public MetaData MetaData { get; set; }

        #endregion

        #region Fields
        private CancellationTokenSource _cts;
        private AnnouncementsModel _model;
        private PageHeaderModel _pageHeaderModel;
        private bool _isPreLoader = true;
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

        public async Task OnInitEvent(AnnouncementsFilterModel model)
        {
            await GetContent(model);
        }

        public async Task OnFilterEvent(AnnouncementsFilterModel model)
        {
            MetaData = new MetaData
            {
                CurrentPage = 1,
                PageSize = 25,
                TotalCount = 0,
                TotalPages = 1,
            };

            await GetContent(model);
        }

        public async Task SelectPage(int page)
        {
            MetaData.CurrentPage = page;

            //await OnInitEvent(FilterModel);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Utilities
        private async Task GetContent(AnnouncementsFilterModel model)
        {
            _cts?.Cancel();

            _cts = new CancellationTokenSource();

            if (!_isPreLoader)
            {
                _isPreLoader = true;
                StateHasChanged();
            }

            var token = _cts.Token;
            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), model.LanguageCode, true);
            var response = await BackofficeService.GetAnnouncements(new AnnouncementRequest
            {
                ClientCodes = model.ClientCodes,
                LanguageCode = languageCode,
                SearchText = model.Search
            }, token);

            if (token.IsCancellationRequested) 
                return;

            MetaData.TotalCount = response.Result.Announcements.Count;
            MetaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(MetaData.TotalCount) / Convert.ToDecimal(MetaData.PageSize)));

            _model = new AnnouncementsModel
            {
                Announcements = response.Result.Announcements.Select((s, index) => new Models.Announcements
                {
                    ClientCodes = s.ClientCodes.Where(c => model.UserClientCodes.Contains(c)).ToList(),
                    EndDate = s.EndDate,
                    StartDate = s.StartDate,
                    IsActive = s.IsActive,
                    Message = s.Message,
                    Title = s.Title,
                    Row = index + ((MetaData.CurrentPage - 1) * MetaData.PageSize) + 1,
                }).ToList()
            };

            //if (response.StatusCode != Backoffice.Shared.Enums.StatusCode.Success)
            //{
            //    _toasterService.AddToast(Toast.NewToast
            //            ("Client Top Fixtures",
            //            $"{topLeaguesResponse.Message}"));
            //    IsPreLoader = false;
            //    return;
            //}

            //TopLeaguesList = topLeaguesResponse.Result;

            //var skip = (MetaData.CurrentPage - 1) * MetaData.PageSize;

            //if (MetaData.CurrentPage > 0)
            //{
            //    TopLeaguesList.ClientTopLeagues = TopLeaguesList.ClientTopLeagues
            //        .Skip(skip).Take(MetaData.PageSize)
            //        .Select((s, index) => new ClientTopLeague
            //        {
            //            RowNo = index + ((MetaData.CurrentPage - 1) * MetaData.PageSize) + 1,
            //            ClientIdRef = s.ClientIdRef,
            //            ClientName = s.ClientName,
            //            TopLeagues = s.TopLeagues
            //        })
            //        .ToList();
            //}

            _isPreLoader = false;
        }
        #endregion
    }
}