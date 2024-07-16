using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class ExchangeRatesResponse
    {
        public ExchangeRatesResponse()
        {
            ExchangeRates = new List<ExchangeRateResponse>();
        }

        public List<ExchangeRateResponse> ExchangeRates { get; set; }

        #region Nested Class

        public class ExchangeRateResponse
        {
            public string CurrencyCode { get; set; }
            public decimal ExchangeRate { get; set; }
        }

        #endregion
    }
}
