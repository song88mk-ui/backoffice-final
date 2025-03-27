namespace Backoffice.Shared.Models
{
    public class ProviderModel<T>
    {
        public T SrId { get; set; }
        public T BgId { get; set; }
    }
    public class ProviderItemModel<T>
    {
        public T SrId { get; set; }
        public T BgId { get; set; }
    }

    public class Item<T>
    {
        public T Id { get; set; }
        public string Name { get; set; }
    }
}
