using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using ExcelDataReader;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Data;
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
        #endregion

        #region Ctor
        public TableauService(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri($"{_configuration["Tableau:Uri"]}");

        }
        #endregion

        #region Methods
        public async Task<string> GetTableauUrl(string reportName)
        {
            try
            {
                var response = await _httpClient.PostAsync($"trusted?username={_configuration["Tableau:UserName"]}&target_site={_configuration["Tableau:SiteId"]}", null);

                var ticket = await response.Content.ReadAsStringAsync();

                if (ticket.Equals("-1"))
                    return string.Empty;

                return $"{_configuration["Tableau:Uri"]}/trusted/{ticket}/t/{_configuration["Tableau:SiteId"]}/views/{reportName}"; 
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        #endregion

    }
}
