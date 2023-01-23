using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.Common
{
    public partial class Pagination : ComponentBase, IDisposable
    {
        #region Properties
        [Parameter] public MetaData MetaData { get; set; }
        [Parameter] public int Spread { get; set; }
        [Parameter] public EventCallback<int> SelectPageEvent { get; set; }
        [Parameter] public int PaginationColspan { get; set; }

        protected List<PagingLink> _links;
        protected int ToRow => GetToRow();
        protected int FromRow => GetFromRow();

        #endregion

        protected override void OnParametersSet()
        {
            CreatePaginationLinks();
        }

        private int GetFromRow()
        {
            if(MetaData.TotalCount <= 0)
                return 0;
            return ((MetaData.CurrentPage - 1) * MetaData.PageSize) + 1;
        }

        private int GetToRow()
        {
            if (MetaData.CurrentPage * MetaData.PageSize > MetaData.TotalCount)
                return MetaData.TotalCount;
            return (MetaData.CurrentPage * MetaData.PageSize);
        }

        private void CreatePaginationLinks()
        {
            _links = new List<PagingLink>();
            _links.Add(new PagingLink(MetaData.CurrentPage - 1, MetaData.HasPrevious, "Previous"));
            for (int i = 1; i <= MetaData.TotalPages; i++)
            {
                if (i >= MetaData.CurrentPage - Spread && i <= MetaData.CurrentPage + Spread)
                {
                    _links.Add(new PagingLink(i, true, i.ToString()) { Active = MetaData.CurrentPage == i });
                }
            }
            _links.Add(new PagingLink(MetaData.CurrentPage + 1, MetaData.HasNext, "Next"));
        }
        protected async Task OnSelectedPage(PagingLink link)
        {
            if (link.Page == MetaData.CurrentPage || !link.Enabled)
                return;
            MetaData.CurrentPage = link.Page;
            await SelectPageEvent.InvokeAsync(link.Page);
        }

        public void Dispose()
        {
        }

        #region Utilities
        public async void RegeneratePaginationLinks()
        {
            CreatePaginationLinks();
            await InvokeAsync(StateHasChanged);
        }
        #endregion
    }
}
