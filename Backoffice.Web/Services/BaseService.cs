using Azure.Core;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Models.Events;
using Backoffice.Web.States;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;

namespace Backoffice.Web.Services
{
    public class BaseService
    {
        public string Page { get; set; } = string.Empty;
        public LanguageCode LanguageCode { get; set; } = LanguageCode.EN;
        public event EventHandler<UnauthorizedEventArgs> Unauthorized;

        private HttpClient _client;
        private HttpClient _httpClient
        {
            get
            {
                if (_client == null)
                {
                    _client = new HttpClient();
                }

                ConfigureHttpClientHeaders();

                return _client;
            }
            set => _client = value;
        }
        protected readonly UserState _userState;


        public BaseService(HttpClient httpClient, UserState userState)
        {
            _userState = userState;
            _client = httpClient;
        }


        protected async Task<BaseResponse<T1>?> PostAsJsonAsync<T1, T2>(
            string requestUri,
            T2 requestPayload,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var response = await _httpClient.PostAsync(requestUri, new StringContent(JsonSerializer.Serialize(requestPayload), System.Text.Encoding.UTF8, "application/json"), cancellationToken);

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<T1>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                        ?? new BaseResponse<T1>();
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<T1>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        var result = new BaseResponse<T1>
                        {
                            StatusCode = StatusCode.Unauthorized,
                            Message = $"{StatusCode.Unauthorized.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };

                        OnUnauthorized(
                            new UnauthorizedEventArgs(result.StatusCode, result.Message)
                        );

                        return result;
                    }
                    else
                    {
                        return new BaseResponse<T1>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected async Task<BaseResponse<T1>?> GetAsync<T1>(string requestUri, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(requestUri, cancellationToken);

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<T1>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<T1>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        var result = new BaseResponse<T1>
                        {
                            StatusCode = StatusCode.Unauthorized,
                            Message = $"{StatusCode.Unauthorized.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };

                        OnUnauthorized(
                            new UnauthorizedEventArgs(result.StatusCode, result.Message)
                        );

                        return result;
                    }
                    else
                    {
                        return new BaseResponse<T1>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        protected virtual void OnUnauthorized(UnauthorizedEventArgs e)
        {
            Unauthorized?.Invoke(this, e);
        }
        private void ConfigureHttpClientHeaders()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userState.Model.AccessToken);

            RemoveHeader("x-page");
            RemoveHeader("x-language-code");

            if (!string.IsNullOrEmpty(Page))
            {
                _client.DefaultRequestHeaders.Add("x-page", Page);
            }

            _client.DefaultRequestHeaders.Add("x-language-code", LanguageCode.ToString());
        }
        private void RemoveHeader(string headerName)
        {
            if (_client.DefaultRequestHeaders.Contains(headerName))
            {
                _client.DefaultRequestHeaders.Remove(headerName);
            }
        }
    }
}
