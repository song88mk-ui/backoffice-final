namespace Backoffice.Web.Interfaces.Pages
{
    public interface IPage
    {
        /// <summary>
        /// Call method to refresh the page component
        /// </summary>
        Task RefreshPage();
    }
}
