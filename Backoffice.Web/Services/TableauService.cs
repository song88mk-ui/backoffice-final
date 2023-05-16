using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Backoffice.Web.Telemetry;
using ExcelDataReader;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Backoffice.Web.Services
{
    public class TableauService : ITableauService
    {
        #region Fields
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        private readonly ActivitySource _activitySource;
        private readonly Counter<long> _getTableauUrlSuccessCounter;
        private readonly Counter<long> _getTableauUrlFailureCounter;
        #endregion

        #region Ctor
        public TableauService(HttpClient httpClient, IConfiguration configuration, Instrumentation instrumentation)
        {
            _configuration = configuration;

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri($"{_configuration["Tableau:Uri"]}");

            this._activitySource = instrumentation.ActivitySource;
            this._getTableauUrlSuccessCounter = instrumentation.GetTableauUrlSuccessCounter;
            this._getTableauUrlFailureCounter = instrumentation.GetTableauUrlFailureCounter;
        }
        #endregion

        #region Methods
        public async Task<string> GetTableauUrl(string reportName)
        {
            using var activity = this._activitySource.StartActivity("gettableauurl");

            try
            {
                var response = await _httpClient.PostAsync($"trusted?username={_configuration["Tableau:UserName"]}&target_site={_configuration["Tableau:SiteId"]}", null);

                var ticket = await response.Content.ReadAsStringAsync();

                if (ticket.Equals("-1")) {
                    activity?.SetStatus(ActivityStatusCode.Error, "disabled");
                    this._getTableauUrlFailureCounter.Add(1);
                    return string.Empty;
                }

                activity?.SetStatus(ActivityStatusCode.Ok);
                this._getTableauUrlSuccessCounter.Add(1);

                return $"{_configuration["Tableau:Uri"]}/trusted/{ticket}/t/{_configuration["Tableau:SiteId"]}/views/{reportName}"; 
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                this._getTableauUrlFailureCounter.Add(1);
                return string.Empty;
            }
        }
        #endregion

    }
}
