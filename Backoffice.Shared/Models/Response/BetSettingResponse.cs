using Backoffice.Shared.Data.Backoffice.Entities;
using System.Collections.Generic;
using static Backoffice.Shared.Models.Response.ClientListResponse;

namespace Backoffice.Shared.Models.Response
{
    public class BetSettingResponse
    {
        public BetSettingResponse()
        {
            BetSettings = new List<BetSetting>();
        }

        public List<BetSetting> BetSettings { get; set; }
    }
}
