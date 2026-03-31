using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using OE.Shared.Data.Sportsbook.Collections;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Backoffice.Shared.Models.Response.LeagueLookupResponse;
using JsonSerializer = System.Text.Json.JsonSerializer;
using SportLookup = OE.Shared.Data.Sportsbook.Collections.SportLookup;

namespace Backoffice.API.Repositories
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private static IMongoDatabase _db;
        private static IMongoCollection<MarketLookup> _marketLookupCollection;
        private static IMongoCollection<SportLookup> _sportLookupCollection;
        private static IMongoCollection<SegmentLookup> _segmentLookupCollection;
        private static IMongoCollection<LeagueLookup> _leagueLookupCollection;

        private readonly IConfiguration _configuration;
        private readonly IBackofficeActivityLogRepository _activityLogRepository;
        private readonly IMemCache _memCache;

        public SportsbookRepository(IConfiguration configuration, IBackofficeActivityLogRepository activityLogRepository, IMemCache memCache)
        {
            _configuration = configuration;
            _activityLogRepository = activityLogRepository;
            _memCache = memCache;

            if (_db == null)
            {
                var client = new MongoClient(_configuration["Custom:MongoDB-Sportsbook"]);
                _db = client.GetDatabase("Sportsbook");

                _sportLookupCollection = _db.GetCollection<SportLookup>("SportLookup").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
                _segmentLookupCollection = _db.GetCollection<SegmentLookup>("SegmentLookup").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
                _leagueLookupCollection = _db.GetCollection<LeagueLookup>("LeagueLookup").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
                _marketLookupCollection = _db.GetCollection<MarketLookup>("MarketLookup").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
            }
        }

        #region Client

        public async Task<BaseResponse<ClientMarketTemplateGroupResponse>> GetClientMarketTemplateGroups(ClientMarketTemplateRequest request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"client/markettemplategroups", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ClientMarketTemplateGroupResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientMarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        //public async Task<BaseResponse<ClientMarketTemplateResponse>> GetClientMarketTemplates(string clientCode, int sportId, int fixtureStatusId, string langCode, string region)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient(region, langCode).GetAsync($"client/markettemplate/{clientCode}/{sportId}/{fixtureStatusId}");

        //        if (response.IsSuccessStatusCode)
        //            return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<ClientMarketTemplateResponse>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<ClientMarketTemplateResponse>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}
        //public async Task<BaseResponse<ClientMarketTemplateResponse>> AddOrUpdateClientMarketTemplatesOrder(ClientMarketTemplateRequest request, string region)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient(region).PostAsync($"client/markettemplate", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        //        if (response.IsSuccessStatusCode)
        //        {
        //            await Publish(new MessageRequest
        //            {
        //                Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
        //                {
        //                    ClientId = request.ClientId,
        //                    ClientCode = request.ClientCode,
        //                    CacheType = CacheType.MarketSortOrder
        //                }),
        //                CorrelationId = Guid.NewGuid().ToString().ToUpper(),
        //                MessageType = "OnMarketSortOrderChange",
        //                RoutingKey = $"r.oe.cachesync.onmarketsortorderchange"
        //            }, "HK");

        //            return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        }
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<ClientMarketTemplateResponse>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<ClientMarketTemplateResponse>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}
        //public async Task<BaseResponse<bool>> CopyClientMarketTemplatesOrder(CopyClientMarketSortOrderRequest request, string region)
        //{
        //    try
        //    {
        //        var response = await GetHttpClient(region).PostAsync($"client/markettemplate/copy", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        //        if (response.IsSuccessStatusCode)
        //            return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //        else
        //        {
        //            if (response.StatusCode == HttpStatusCode.BadRequest)
        //                return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        //            else
        //            {
        //                return new BaseResponse<bool>
        //                {
        //                    StatusCode = StatusCode.Error,
        //                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponse<bool>
        //        {
        //            StatusCode = StatusCode.Error,
        //            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
        //        };
        //    }
        //}

        public async Task<BaseResponse<ClientPostMarketTemplateGroupResponse>> AddOrUpdateClientMarketTemplateGroup(ClientPostMarketTemplateGroupRequest request, string region)
        {
            try
            {
                if (request.IsAdd)
                {
                    foreach (var marketGroup in request.MarketTemplateGroups)
                    {
                        if (marketGroup.Name.ToLower() == "all markets" && string.IsNullOrEmpty(marketGroup.MarketTemplateIds))
                        {
                            var marketTemplateIds = (await GetMarketLookup(new MarketLookupRequest
                            {
                                FixtureTypeId = (int)FixtureType.Match,
                                SportId = request.SportId
                            })).Result.MarketLookup
                                .Select(s => s.MarketTemplateId)
                                .OrderBy(o => int.Parse(o))
                                .ToList();

                            marketGroup.MarketTemplateIds = marketTemplateIds != null && marketTemplateIds.Any() ? string.Join(",", marketTemplateIds) : string.Empty;
                        }
                    }
                }

                var response = await GetHttpClient(region).PostAsync($"client/markettemplategroup/post", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientPostMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientPostMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ClientPostMarketTemplateGroupResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientPostMarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<bool>> DeleteClientMarketTemplateGroup(ClientDeleteMarketTemplateGroupRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/markettemplategroup/delete", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponse<ClientMarketTemplateGroupResponse>> CopyClientMarketTemplateGroup(ClientCopyMarketTemplateGroupRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"client/markettemplategroup/copy", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ClientMarketTemplateGroupResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientMarketTemplateGroupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        #endregion

        public async Task<BaseResponse<MessageResponse>> Publish(MessageRequest request, string region)
        {
            try
            {
                var response = await GetHttpClient(region).PostAsync($"publish", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<MessageResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<MessageResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<MessageResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<MessageResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        private HttpClient GetHttpClient(string langCode = "")
        {
            var uri = GetBaseUrl("");
            if (uri.LastOrDefault() != '/')
                uri += "/";

            var client = new HttpClient()
            {
                BaseAddress = new Uri($"{uri}v1/sportsbook/")
            };

            client.DefaultRequestHeaders.Remove("x-language-code");
            if (!string.IsNullOrEmpty(langCode))
                client.DefaultRequestHeaders.Add("x-language-code", langCode);

            return client;
        }

        private string GetBaseUrl(string region)
        {
            if (region.ToUpper() == "SA")
                return _configuration[$"API:SportsbookAPI:SA:Uri"];
            else
                return _configuration[$"API:SportsbookAPI:HK:Uri"];
        }

        #region Lookup

        public async Task<BaseResponse<SportsLookupResponse>> GetSportLookup(string langCode = "EN")
            => await _memCache.GetCache($"sportLookUp-{langCode}", async () =>
            {
                try
                {
                    var sportsLookup = await _sportLookupCollection.Find(_ => true).ToListAsync();

                    var sportIds = sportsLookup.Select(s => $"{langCode}:{s.SpId}").ToList();
                    //var translationLookup = sportTranslationLookupCollection.Find(c => sportIds.Contains(c.SpTrLoId)).ToList();

                    return new BaseResponse<SportsLookupResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new SportsLookupResponse
                        {
                            SportLookup = sportsLookup.Select(s => new SportLookupModel
                            {
                                SportId = s.SpId,
                                SportName = s.SpNa //translationLookup.FirstOrDefault(c => c.SpTrLoId == $"{langCode}:{s.SpId}")?.SpNa ?? s.SpNa
                            }).OrderBy(o => o.SportName).ToList()
                        }
                    };
                }
                catch (Exception ex)
                {

                    throw;
                }
            }, 24);

        public async Task<BaseResponse<LeagueLookupResponse>> GetLeagueLookup(int sportId, int segmentId, string langCode = "EN")
        {
            try
            {
                var leagueLookupFilter = Builders<LeagueLookup>.Filter.Empty;
                if (sportId > 0 && segmentId > 0)
                {
                    leagueLookupFilter = Builders<LeagueLookup>.Filter.Where(c => c.SpId == sportId && c.SegId == segmentId);
                }
                else if (sportId > 0 && segmentId == 0)
                {
                    leagueLookupFilter = Builders<LeagueLookup>.Filter.Where(c => c.SpId == sportId);
                }
                else if (sportId == 0 && segmentId > 0)
                {
                    leagueLookupFilter = Builders<LeagueLookup>.Filter.Where(c => c.SegId == segmentId);
                }

                var leagueLookup = await _leagueLookupCollection.Find(leagueLookupFilter).ToListAsync();
                var leagueIds = leagueLookup.Select(s => $"{langCode}:{s.SpId}:{s.SegId}:{s.LeId}").ToList();
                //var translationLookup = leagueTranslationLookupCollection.Find(c => leagueIds.Contains(c.LeTrLoId)).ToList();


                return new BaseResponse<LeagueLookupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new LeagueLookupResponse
                    {
                        LeagueLookup = leagueLookup.Select(s => new LeagueLookupModel
                        {
                            LeagueId = s.LeId,
                            LeagueName = s.LeNa, //translationLookup.FirstOrDefault(c => c.LeTrLoId == $"{langCode}:{s.SpId}:{s.SegId}:{s.LeId}")?.LeNa ?? s.LeNa,
                            SportId = s.SpId,
                            SegmentId = s.SegId
                        }).OrderBy(o => o.SportId).ThenBy(o => o.SegmentId).ThenBy(o => o.LeagueName).ToList()
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<LeagueLookupResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message
                };
            }
        }

        public async Task<BaseResponse<SegmentLookupResponse>> GetSegmentLookup(int sportId, string langCode = "EN")
        //=> await _memCache.GetCache($"sportLookUp-{langCode}", async () =>
        {
            var segmentLookupFilter = sportId == 0 ? Builders<SegmentLookup>.Filter.Empty : Builders<SegmentLookup>.Filter.Where(c => c.SpId == sportId);
            var segmentLookup = await _segmentLookupCollection.Find(segmentLookupFilter).ToListAsync();

            var segmentIds = segmentLookup.Select(s => $"{langCode}:{s.SpId}:{s.SegId}").ToList();
            //var translationLookup = segmentTranslationLookupCollection.Find(c => segmentIds.Contains(c.SegTrLoId)).ToList();

            return new BaseResponse<SegmentLookupResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new SegmentLookupResponse
                {
                    SegmentLookup = segmentLookup.Select(s => new SegmentLookupModel
                    {
                        SegmentId = s.SegId,
                        SegmentName = s.SegNa, //translationLookup.FirstOrDefault(c => c.SegTrLoId == $"{langCode}:{s.SpId}:{s.SegId}")?.SegNa ?? s.SegNa,
                        SportId = s.SpId
                    }).OrderBy(o => o.SportId).ThenBy(o => o.SegmentName).ToList()
                }
            };
        }
        //, 1);


        public async Task<BaseResponse<MarketLookupResponse>> GetMarketLookup(MarketLookupRequest request, string langCode = "")
        {
            var marketLookupFilter = Builders<MarketLookup>.Filter.Where(c => c.SpId == request.SportId && c.FiTyId == request.FixtureTypeId);
            var marketTemplates = await _marketLookupCollection.Find(marketLookupFilter).ToListAsync();

            var marketTemplateIds = marketTemplates.Select(s => $"{langCode}:{s.MaTeId}").ToList();
            //var translations = marketTranslationLookupCollection.Find(c => marketTemplateIds.Contains(c.MaTeTrLoId)).ToList();

            return new BaseResponse<MarketLookupResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new MarketLookupResponse
                {
                    MarketLookup = marketTemplates.Select(s => new MarketLookupModel
                    {
                        MarketTemplateId = s.MaTeId,
                        MarketTemplateName = s.MaTeNa
                    }).ToList()
                }
            };
        }

        #endregion
    }
}