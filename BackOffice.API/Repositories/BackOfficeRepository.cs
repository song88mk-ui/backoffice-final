using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public class BackofficeRepository : IBackofficeRepository
    {
        private readonly IConfiguration _configuration;
        private BackofficeContext _backofficeContext;
        private IPlatformRepository _platformRepository;
        public BackofficeRepository(IConfiguration configuration, BackofficeContext backofficeContext, IPlatformRepository platformRepository)
        {
            _configuration = configuration;
            _backofficeContext = backofficeContext;
            _platformRepository = platformRepository;
        }

        public async Task<BaseResponse<ClientListResponse>> GetClients()
        {
            try
            {
                var clients = _backofficeContext.ClientInformation
                    .Select(client => new ClientResponse
                    {
                        ClientId = client.ClientId,
                        BusinessName = client.Name,
                        Brand = client.Brand,
                        Region = client.Region
                    }).ToList();

                return new BaseResponse<ClientListResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ClientListResponse
                    {
                        Clients = clients ?? new List<ClientResponse>()
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientListResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = null
                };
            }
        }
        public async Task<BaseResponse<string>> GetClientRegion(int clientId)
        {
            try
            {
                return new BaseResponse<string>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Success.ToString(),
                    Result = _backofficeContext.ClientInformation.Where(s => s.ClientId == clientId).Select(s => s.ClientId).FirstOrDefault().ToString()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<string>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = String.Empty
                };
            }
        }
        public async Task<BaseResponse<Dictionary<string, string>>> GetClientsByRegion(string clientIdRefs)
        {
            try
            {
                var result = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(clientIdRefs))
                {
                    var clients = clientIdRefs.Split(',').ToList().ConvertAll(int.Parse);

                    var hkResult = _backofficeContext.ClientInformation.Where(s => clients.Contains(s.ClientId) && s.Region == "HK").Select(s => s.ClientId).ToList();
                    if (hkResult.Any())
                        result.Add("HK", string.Join(",", hkResult));

                    var saResult = _backofficeContext.ClientInformation.Where(s => clients.Contains(s.ClientId) && s.Region == "SA").Select(s => s.ClientId).ToList();
                    if (saResult.Any())
                        result.Add("SA", string.Join(",", saResult));
                }

                return new BaseResponse<Dictionary<string, string>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<Dictionary<string, string>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = null
                };
            }
        }

        #region TOP FIXTURES
        public async Task<BaseResponse<bool>> CopyClientTopFixtures(CopyClientTopFixtureRequest request)
        {
            try
            {
                var response = new BaseResponse<PlatformMessageResponse>();

                var hkClientIds = new List<int>();
                var saClientIds = new List<int>();

                foreach (var clientId in request.ClientIds)
                {
                    var region = (await GetClientRegion(clientId)).Result;

                    if (region == "HK") hkClientIds.Add(clientId);
                    else if (region == "SA") saClientIds.Add(clientId);
                }

                if (!hkClientIds.Any() && !saClientIds.Any())
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString()
                    };
                }

                if (hkClientIds.Any())
                {
                    var hkResponse = await _platformRepository.CopyClientTopFixtures(new CopyClientTopFixtureRequest
                    {
                        ClientIds = hkClientIds,
                        TopFixtures = request.TopFixtures
                    }, "HK");

                    response = hkResponse;
                }

                // only proceed to SA if result from HK is null or HK has a result and the status code is success
                if (saClientIds.Any() && (response.Result == null || (response.Result != null && response.StatusCode == StatusCode.Success)))
                {
                    var saResponse = await _platformRepository.CopyClientTopFixtures(new CopyClientTopFixtureRequest
                    {
                        ClientIds = saClientIds,
                        TopFixtures = request.TopFixtures
                    }, "SA");

                    response = saResponse;
                }

                //return response;
                return new BaseResponse<bool>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = true
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = false
                };
            }
        }
        #endregion

        #region TOP LEAGUES
        public async Task<BaseResponse<bool>> CopyClientTopLeagues(CopyClientTopLeaguesRequest request)
        {
            try
            {
                var response = new BaseResponse<PlatformMessageResponse>();

                var hkClientIds = new List<int>();
                var saClientIds = new List<int>();

                foreach (var clientId in request.ClientIds)
                {
                    var region = (await GetClientRegion(clientId)).Result;

                    if (region == "HK") hkClientIds.Add(clientId);
                    else if (region == "SA") saClientIds.Add(clientId);
                }

                if (!hkClientIds.Any() && !saClientIds.Any())
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString().Replace("_", " "),
                        Result = false
                    };
                }

                if (hkClientIds.Any())
                {
                    var hkResponse = await _platformRepository.CopyClientTopLeagues(new CopyClientTopLeaguesRequest
                    {
                        ClientIds = hkClientIds,
                        SportId = request.SportId,
                        TopLeagues = request.TopLeagues
                    }, "HK");

                    response = hkResponse;
                }

                // only proceed to SA if result from HK is null or HK has a result and the status code is success
                if (saClientIds.Any() && (response.Result == null || (response.Result != null && response.Result.StatusCode == StatusCode.Success)))
                {
                    var saResponse = await _platformRepository.CopyClientTopLeagues(new CopyClientTopLeaguesRequest
                    {
                        ClientIds = saClientIds,
                        SportId = request.SportId,
                        TopLeagues = request.TopLeagues
                    }, "SA");

                    response = saResponse;
                }

                return new BaseResponse<bool>()
                {
                    StatusCode = response.StatusCode,
                    Message = response.Message,
                    Result = true
                };

            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = false
                };
            }
        }
        #endregion

        public async Task<BaseResponse<List<CurrenciesResponse>>> GetCurrencies()
        {
            try
            {
                var result = Enum.GetValues(typeof(Currency)).Cast<Currency>().Select(s => new CurrenciesResponse()
                {
                    Id = (int)s,
                    Name = s.ToString(),
                    Value = GetFEDisplayValue(s.ToString())
                }).ToList();

                return new BaseResponse<List<CurrenciesResponse>>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CurrenciesResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = null
                };
            }
        }

        #region Methods
        private string GetFEDisplayValue(string currency)
        {
            var newCurrency = currency;
            switch (currency.ToUpper())
            {
                case "PHP":
                    newCurrency = "CRC";
                    break;
                default:
                    break;
            }

            return newCurrency;
        }
        #endregion
    }
}