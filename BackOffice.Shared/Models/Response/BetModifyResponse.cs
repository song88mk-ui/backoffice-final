using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class BetModifyResponse
    {
        public BetModifyResponse()
        {
            Data = new List<BetModifyModel>();
        }
        public List<BetModifyModel> Data { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }

        public class BetModifyModel
        {
            public string BetId { get; set; }
            public string OldBetStatus { get; set; }
            public string Message { get; set; }
            public bool IsSuccess { get; set; }
        }
    }
}
