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

            using(var activity = _activitySource.StartActivity("gettableauurl"))
            {
                try
                {
                    Stopwatch? duration = _tableauTrustedCallDurationHistogram.Enabled ? Stopwatch.StartNew() : null;

                    var response = await _httpClient.PostAsync($"trusted?username={_configuration["Tableau:UserName"]}&target_site={_configuration["Tableau:SiteId"]}", null);

                    if (duration != null)
                      _tableauTrustedCallDurationHistogram.Record(duration.Elapsed.TotalMilliseconds,
                          new KeyValuePair<string, object?>("report", reportName));

                    var ticket = await response.Content.ReadAsStringAsync();

                    if (ticket.Equals("-1")) {
                        activity?.SetStatus(ActivityStatusCode.Error, "disabled");
                        _getTableauUrlFailureCounter.Add(1);
                        return string.Empty;
                    }

                    activity?.SetStatus(ActivityStatusCode.Ok);
                    _getTableauUrlSuccessCounter.Add(1);

                    return $"{_configuration["Tableau:Uri"]}/trusted/{ticket}/t/{_configuration["Tableau:SiteId"]}/views/{reportName}";
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("error getting tableau url: {exception}, stacktrace: {@stacktrace}", ex.Message, ex.StackTrace);
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _getTableauUrlFailureCounter.Add(1);
                    return string.Empty;
                }
            }
        }

        public void Dispose()
        {
            _logger.LogDebug("service: {service} disposed!", this.GetType().Name);
        }

        #endregion

    }
}
