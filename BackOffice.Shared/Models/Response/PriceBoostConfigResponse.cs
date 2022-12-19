using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class PriceBoostConfigResponse
    {
        public StatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public bool IsBoosted { get; set; }
        public string ClientBoosted { get; set; }
        public string ClientMarketBoosted { get; set; }
        public List<PriceBoostConfigModel> ExistingPriceBoostConfigs { get; set; }
    }
}
