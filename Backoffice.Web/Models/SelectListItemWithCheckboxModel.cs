namespace Backoffice.Web.Models
{
    public class SelectListItemWithCheckboxModel<T>
    {
        public T Value { get; set; }
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public bool IsSelectAll { get; set; }
    }
    public class MaintenanceSelectListItemWithCheckboxModel
    {
        public string Value { get; set; }
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public bool IsMaintenanceEnabled { get; set; }
    }
}
