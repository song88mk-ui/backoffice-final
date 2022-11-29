using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class MetaData
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }
}
