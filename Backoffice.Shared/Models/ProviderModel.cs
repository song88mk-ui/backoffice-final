using System.Collections.Generic;

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
        public Item()
        {
            OtPrLi = new ProviderModel<List<int>>
            {
                SrId = new List<int>(),
                BgId = new List<int>()
            };
        }
        public T Id { get; set; }
        public string Name { get; set; }
        public ProviderModel<List<int>> OtPrLi { get; set; }
    }
}
