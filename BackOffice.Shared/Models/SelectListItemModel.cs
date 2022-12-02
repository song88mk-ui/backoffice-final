using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class SelectListItemModel<T>
    {
        public T Value { get; set; }
        public string Name { get; set; }
    }
}
