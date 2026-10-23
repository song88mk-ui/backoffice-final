using Backoffice.Web.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Backoffice.Web.Services
{
    public class TableauService : ITableauService, IDisposable
    {
        #region Fields
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TableauService> _logger;

        private readonly ActivitySource _activitySource;
        private readonly Counter<long> _getTableauUrlSuccessCounter;
        private readonly Counter<long> _getTableauUrlFailureCounter;
        private readonly Histogram<double> _tableauTrustedCallDurationHistogram;
        #endregion

        #region Ctor
        public TableauService(HttpClient httpClient, IConfiguration configuration, Instrumentation instrumentation, ILogger<TableauService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri($"{_configuration["Tableau:Uri"]}");

            _activitySource = instrumentation.ActivitySource;
            _getTableauUrlSuccessCounter = instrumentation.GetTableauUrlSuccessCounter;
            _getTableauUrlFailureCounter = instrumentation.GetTableauUrlFailureCounter;
            _tableauTrustedCallDurationHistogram = instrumentation.TableauTrustedCallDurationHistogram;
        }
        #endregion

        #region Methods
        public async Task<string> GetTableauUrl(string reportName)
        {
            return string.Empty; // TODO: enable when there is tableau
        }

        public void Dispose()
        {
            _logger.LogDebug("service: {service} disposed!", this.GetType().Name);
        }

        #endregion

    }
}
