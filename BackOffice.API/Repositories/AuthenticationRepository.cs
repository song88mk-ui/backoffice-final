using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Backoffice.API.Repositories
{
    public class AuthenticationRepository : IAuthenticationRepository
    {
        private HttpClient _httpClient { get; set; }
        public AuthenticationRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<BaseResponse<TokenModel>> Login(LogInModel loginModel)
        {
            try
            {
                var response = await _httpClient.PostAsync($"login", new StringContent(JsonSerializer.Serialize(loginModel), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<TokenModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<TokenModel>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<TokenModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                            ?? new BaseResponse<TokenModel>();
                    else
                    {
                        return new BaseResponse<TokenModel>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<TokenModel>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public Task<BaseResponse<TokenModel>> Authenticate(string token)
        {
            throw new System.NotImplementedException();
        }

        public Task<BaseResponse<bool>> Logout(string token)
        {
            throw new System.NotImplementedException();
        }
    }
}
