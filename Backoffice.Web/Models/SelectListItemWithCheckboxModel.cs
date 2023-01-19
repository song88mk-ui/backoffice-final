namespace Backoffice.Web.Models
{
    public class SelectListItemWithCheckboxModel<T>
    {
        public T Value { get; set; }
        public string Name { get; set; }
        public bool IsChecked { get; set; }
    }
}
