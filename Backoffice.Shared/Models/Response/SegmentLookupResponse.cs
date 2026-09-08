using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class SegmentLookupResponse
    {
        public SegmentLookupResponse()
        {
            SegmentLookup = new List<SegmentLookupModel>();
        }

        public List<SegmentLookupModel> SegmentLookup { get; set; }
    }

    #region Nested Class
    public class SegmentLookupModel
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int SportId { get; set; }
    }
    #endregion
}
