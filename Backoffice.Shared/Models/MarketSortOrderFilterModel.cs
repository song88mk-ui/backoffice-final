using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models
{
    public class MarketSortOrderFilterModel
    {
        public string ClientId { get; set; }
        public string SelectedClientCode { get; set; }
        public int SelectedSportId { get; set; }
        public int SelectedFixtureStatusId { get; set; }
    }
}
