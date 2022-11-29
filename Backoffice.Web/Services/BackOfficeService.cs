using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using System.Text.Json;

namespace Backoffice.Web.Services
{
    public class BackOfficeService : IBackOfficeService, IDisposable
    {
        #region Fields
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        #endregion

        #region Ctor
        public BackOfficeService(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = httpClient;

            _httpClient.BaseAddress = new Uri(_configuration["API:BoApi:Uri"]);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["API:BoApi:Security:APIKey"]);

        }
        #endregion

        #region Methods
        public async Task<BaseResponse<ClientTopFixtureResponse>> GetClientTopFixtures(string clientIds)
        {
            try
            {
                var result = await JsonSerializer.DeserializeAsync<BaseResponse<ClientTopFixtureResponse>>
                (await _httpClient.GetStreamAsync($"sportsbook/settings/topfixtures"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                return result ?? new BaseResponse<ClientTopFixtureResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success.ToString()}",
                    Result = new ClientTopFixtureResponse()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientTopFixtureResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
        #endregion
    }
}
