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
            if (!_isPreLoader)
            {
                _isPreLoader = true;
                StateHasChanged();
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), model.LanguageCode, true);
            var response = await BackofficeService.GetAnnouncements(new AnnouncementRequest
            {
                ClientCodes = model.ClientCodes,
                LanguageCode = languageCode,
                SearchText = model.Search
            });

            _model = new AnnouncementsModel
            {
                Announcements = response.Result.Announcements
            };

            MetaData.TotalCount = _model.Announcements.Count;
            MetaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(MetaData.TotalCount) / Convert.ToDecimal(MetaData.PageSize)));

            //var skip = (MetaData.CurrentPage - 1) * MetaData.PageSize;

            //if (MetaData.CurrentPage > 0)
            //{
            //    _model.Announcements = _model.Announcements
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

            //if (response.StatusCode != Backoffice.Shared.Enums.StatusCode.Success)
            //{
            //    _toasterService.AddToast(Toast.NewToast
            //            ("Client Top Fixtures",
            //            $"{topLeaguesResponse.Message}"));
            //    IsPreLoader = false;
            //    return;
            //}

            //TopLeaguesList = topLeaguesResponse.Result;

            //if (TopLeaguesList != null)
            //{
            //    TopLeaguesList.ClientTopLeagues = TopLeaguesList.ClientTopLeagues.Join(model.Clients,
            //        le => le.ClientIdRef,
            //        c => c.ClientId,
            //        (le, c) => new ClientTopLeague
            //        {
            //            ClientIdRef = le.ClientIdRef,
            //            ClientName = c.ClientName,
            //            TopLeagues = le.TopLeagues
            //        }).ToList();

            //    if (!string.IsNullOrWhiteSpace(model.Search))
            //    {
            //        var result = TopLeaguesList.ClientTopLeagues.Where(c => c.ClientName.ToUpper().Contains(model.Search.ToUpper())
            //        || c.TopLeagues.Any(tf => tf.SportName.ToUpper().Contains(model.Search.ToUpper()))
            //        || c.TopLeagues.Any(tf => tf.SegmentName.ToUpper().Contains(model.Search.ToUpper()))
            //        || c.TopLeagues.Any(tf => tf.LeagueName.ToUpper().Contains(model.Search.ToUpper()))).ToList();

            //        if (result != null)
            //            TopLeaguesList.ClientTopLeagues = result;
            //    }
            //}
            //else
            //{
            //    TopLeaguesList = new TopLeaguesModel();
            //    TopLeaguesList.ClientTopLeagues = new List<ClientTopLeague>();
            //}

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