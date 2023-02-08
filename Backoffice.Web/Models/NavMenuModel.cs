namespace Backoffice.Web.Models
{
    public class NavMenuModel
    {
        public NavMenuModel()
        {
            Menus = new List<MenuModel>();
        }
        public List<MenuModel> Menus { get; set; }

        public class MenuModel
        {
            public MenuModel()
            {
                SubMenus = new List<MenuModel>();
            }
            public string Id { get; set; }
            public string Name { get; set; }
            public string IconName { get; set; }
            public string Url { get; set; }
            public List<MenuModel> SubMenus { get; set; }
        }
    }
}
