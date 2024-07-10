using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Requests
{
    public class ClientSportSettingRequest
    {
        public string ClientCode { get; set; }
        public List<SportsSortOrder> SportsSortOrder { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public class SportsSortOrder
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public List<SportsSegmentSortOrder> SegmentsSortOrder { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsOpen { get; set; }

    }

    public class SportsSegmentSortOrder
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public List<SportSegmentLeagueSortOrder> LeagueSortOrder { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsOpen { get; set; }
    }

    public class SportSegmentLeagueSortOrder
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CheckBoxModel
    {
        public bool Checked { get; set; }
        public object Param1 { get; set; }
    }
}
