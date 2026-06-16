using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using OE.Shared.Data.Sportsbook;
using OE.Shared.Data.Sportsbook.Collections;
using OE.Shared.Data.Sportsbook.SQLEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using static Backoffice.Shared.Models.Response.LeagueLookupResponse;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Backoffice.API.Repositories
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private static IMongoDatabase _db;
        private static IMongoCollection<MarketLookup> _marketLookupCollection;
        private static IMongoCollection<SportLookup> _sportLookupCollection;
        private static IMongoCollection<SegmentLookup> _segmentLookupCollection;
        private static IMongoCollection<LeagueLookup> _leagueLookupCollection;

        private readonly SportsbookContext _sportsbookContext;
        private readonly IConfiguration _configuration;
        private readonly IPlatformRepository _platformRepository;
        private readonly IBackofficeActivityLogRepository _activityLogRepository;
        private readonly IMemCache _memCache;

        public SportsbookRepository(SportsbookContext sportsbookContext, IConfiguration configuration, IPlatformRepository platformRepository, IBackofficeActivityLogRepository activityLogRepository, IMemCache memCache)
        {
            _sportsbookContext = sportsbookContext;
            _configuration = configuration;
            _platformRepository = platformRepository;
            _activityLogRepository = activityLogRepository;
            _memCache = memCache;

            if (_db == null)
            {
                var client = new MongoClient(_configuration["Custom:MongoDB-Sportsbook"]);
                _db = client.GetDatabase("Sportsbook");

                _sportLookupCollection = _db.GetCollection<SportLookup>("SportLookup").WithReadPreference(new ReadPreference(ReadPreferenceMode.PrimaryPreferred));
                _segmentLookupCollection = _db.GetCollection<SegmentLookup>("SegmentLookup").WithReadPreference(new ReadPreference(ReadPreferenceMode.PrimaryPreferred));
                _leagueLookupCollection = _db.GetCollection<LeagueLookup>("LeagueLookup").WithReadPreference(new ReadPreference(ReadPreferenceMode.PrimaryPreferred));
                _marketLookupCollection = _db.GetCollection<MarketLookup>("MarketLookup").WithReadPreference(new ReadPreference(ReadPreferenceMode.PrimaryPreferred));
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
                // Always ensure "All Markets" exists and is up-to-date
                var marketTemplateIds = (await GetMarketLookup(new MarketLookupRequest
                {
                    FixtureTypeId = (int)FixtureType.Match,
                    SportId = request.SportId
                })).Result.MarketLookup
                .Select(x => x.MarketTemplateId).OrderBy(x => int.Parse(x)).ToList();

                var allMarketTemplateIds = marketTemplateIds.Any()
                    ? string.Join(",", marketTemplateIds)
                    : string.Empty;

                var allMarketsGroup = request.MarketTemplateGroups
                    .FirstOrDefault(x =>
                        string.Equals(
                            x.Name,
                            "All Markets",
                            StringComparison.OrdinalIgnoreCase));

                if (allMarketsGroup != null)
                {
                    // Force correct values
                    allMarketsGroup.SortOrder = 0;
                    allMarketsGroup.MarketTemplateIds = allMarketTemplateIds;
                }
                else
                {
                    request.MarketTemplateGroups.Add(new ClientMarketTemplateGroupDTO
                    {
                        Name = "All Markets",
                        SortOrder = 0,
                        MarketTemplateIds = allMarketTemplateIds,
                        IsActive = true
                    });
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

        #region Sport Settings
        public async Task<BaseResponse<ClientSportSettingResponse>> AddOrUpdateClientSportSettings(ClientSportSettingRequest request)
        {
            if (string.IsNullOrEmpty(request.ClientCode))
                return new BaseResponse<ClientSportSettingResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ClientSportSettingResponse()
                    {
                        StatusCode = StatusCode.Error,
                        Message = "Invalid Client Code",
                    }
                };

            if (!request.SportsSortOrder.Any())
                return new BaseResponse<ClientSportSettingResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ClientSportSettingResponse()
                    {
                        StatusCode = StatusCode.Error,
                        Message = "No Client Sport Order to be updated",
                    }
                };

            // validate duplicate sports
            var duplicateRequest = request.SportsSortOrder.GroupBy(m => m.SportId).Where(m => m.Count() > 1)
                .Select(m => new
                {
                    SportId = m.Key,
                }).ToList();

            if (duplicateRequest.Count > 0)
            {
                return new BaseResponse<ClientSportSettingResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ClientSportSettingResponse()
                    {
                        StatusCode = StatusCode.Error,
                        Message = "Duplicate client sport found",
                    }
                };
            }

            // validate empty clientcode, sportid and sortorder
            foreach (var item in request.SportsSortOrder)

            {
                if (item.SportId == 0 || item.SortOrder == 0)
                    return new BaseResponse<ClientSportSettingResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ClientSportSettingResponse()
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Error! All fields cannot be empty. Please fill in accordingly or remove the empty row",
                        }
                    };
            }

            var clientList = (await _platformRepository.GetClients("HK")).Result;
            var client = clientList.FirstOrDefault(c => c.ClientCode == request.ClientCode);

            if (client == null)
                return new BaseResponse<ClientSportSettingResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ClientSportSettingResponse()
                    {
                        StatusCode = StatusCode.Error,
                        Message = "No client found",
                    }
                };

            var auditList = new List<ClientSportOrderAudit>();
            //var changeFrom = string.Empty;
            //var changeTo = string.Empty;

            try
            {
                #region Delete Existing Client Sport Order

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new System.Transactions.TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                {
                    var existingClientSportOrder = _sportsbookContext.ClientSportOrder.Where(c => c.ClientId == client.ClientId);

                    if (existingClientSportOrder != null && existingClientSportOrder.Any())
                    {
                        //changeFrom = $"<b>Sports</b></br>";
                        //changeFrom = changeFrom + string.Join(", ", existingClientSportOrder.ToList().OrderBy(o => o.SortOrder).Select(s => s.SportName.Trim()).ToList());
                        //changeFrom = changeFrom + "<br/><br/>";
                        _sportsbookContext.ClientSportOrder.RemoveRange(existingClientSportOrder);
                    }

                    var existingClientSegmentOrder = _sportsbookContext.ClientSegmentOrder.Where(c => c.ClientId == client.ClientId);
                    if (existingClientSegmentOrder != null && existingClientSegmentOrder.Any())
                    {
                        //foreach (var segment in existingClientSegmentOrder.ToList().GroupBy(g => g.SportId))
                        //{
                        //    changeFrom = changeFrom + $"<b>{segment.FirstOrDefault().SportName.Trim()} > Segment</b></br>";
                        //    changeFrom = changeFrom + string.Join(", ", segment.ToList().OrderBy(o => o.SortOrder).Select(s => s.SegmentName.Trim()).ToList());
                        //    changeFrom = changeFrom + "<br/><br/>";
                        //}

                        _sportsbookContext.ClientSegmentOrder.RemoveRange(existingClientSegmentOrder);
                    }

                    var existingClientLeagueOrder = _sportsbookContext.ClientLeagueOrder.Where(c => c.ClientId == client.ClientId);
                    if (existingClientLeagueOrder != null && existingClientLeagueOrder.Any())
                    {
                        //foreach (var league in existingClientLeagueOrder.ToList().GroupBy(g => new { g.SportId, g.SegmentId }))
                        //{
                        //    changeFrom = changeFrom + $"<b>{league.FirstOrDefault().SportName.Trim()} > {league.FirstOrDefault().SegmentName.Trim()} > League</b></br>";
                        //    changeFrom = changeFrom + string.Join(", ", league.ToList().OrderBy(o => o.SortOrder).Select(s => s.LeagueName.Trim()).ToList());
                        //    changeFrom = changeFrom + "<br/><br/>";
                        //}
                        _sportsbookContext.ClientLeagueOrder.RemoveRange(existingClientLeagueOrder);
                    }

                    await _sportsbookContext.SaveChangesAsync();
                    scope.Complete();
                }

                #endregion

                #region Insert Client Sport Order

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new System.Transactions.TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                {
                    //Sports
                    //changeTo = $"<b>Sports</b></br>";
                    //changeTo = changeTo + string.Join(", ", request.SportsSortOrder.OrderBy(o => o.SortOrder).Select(s => s.SportName.Trim()).ToList()).Trim();
                    //changeTo = changeTo + "<br/><br/>";

                    foreach (var item in request.SportsSortOrder)
                    {
                        var entity = _sportsbookContext.ClientSportOrder.Add(new ClientSportOrder()
                        {
                            ClientId = client.ClientId,
                            SportId = item.SportId,
                            SportName = item.SportName,
                            SortOrder = item.SortOrder,
                            IsActive = item.IsActive
                        }).Entity;

                        //Segments
                        if (item.SegmentsSortOrder != null && item.SegmentsSortOrder.Any())
                        {
                            //changeTo = changeTo + $"<b>{item.SportName} > Segment</b></br>";
                            //changeTo = changeTo + string.Join(", ", item.SegmentsSortOrder.ToList().OrderBy(o => o.SortOrder).Select(s => s.SegmentName.Trim()).ToList());
                            //changeTo = changeTo + "<br/><br/>";

                            foreach (var segment in item.SegmentsSortOrder)
                            {
                                var segmentEntity = _sportsbookContext.ClientSegmentOrder.Add(new ClientSegmentOrder()
                                {
                                    ClientId = client.ClientId,
                                    SportId = segment.SportId,
                                    SportName = segment.SportName,
                                    SegmentId = segment.SegmentId,
                                    SegmentName = segment.SegmentName,
                                    SortOrder = segment.SortOrder,
                                }).Entity;

                                //Leagues
                                if (segment.LeagueSortOrder != null && segment.LeagueSortOrder.Any())
                                {
                                    //changeTo = changeTo + $"<b>{segment.SportName} > {segment.SegmentName} > League</b></br>";
                                    //changeTo = changeTo + string.Join(", ", segment.LeagueSortOrder.ToList().OrderBy(o => o.SortOrder).Select(s => s.LeagueName.Trim()).ToList());
                                    //changeTo = changeTo + "<br/><br/>";

                                    foreach (var league in segment.LeagueSortOrder)
                                    {
                                        var leagueEntity = _sportsbookContext.ClientLeagueOrder.Add(new ClientLeagueOrder()
                                        {
                                            ClientId = client.ClientId,
                                            SportId = league.SportId,
                                            SportName = league.SportName,
                                            SegmentId = league.SegmentId,
                                            SegmentName = league.SegmentName,
                                            LeagueId = league.LeagueId,
                                            LeagueName = league.LeagueName,
                                            SortOrder = league.SortOrder,
                                        }).Entity;
                                    }
                                }
                            }
                        }
                    }

                    await _sportsbookContext.SaveChangesAsync();
                    scope.Complete();
                }

                #endregion

                //using (var scope = new TransactionScope(TransactionScopeOption.Required, new System.Transactions.TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                //{
                //    auditList.Add(new Shared.Data.Trading.Entities.ClientSportOrderAudit()
                //    {
                //        ClientCode = client.ClientCode,
                //        ChangesFrom = changeFrom.Trim(),
                //        ChangesTo = changeTo,
                //        UserId = request.UserId,
                //        UserName = request.UserName,
                //        TimeStamp = request.TimeStamp,
                //    });

                //    if (!auditList.Any())
                //    {
                //        return new BaseResponseModel<ClientSportSettingResponse>
                //        {
                //            StatusCode = StatusCode.Success,
                //            Result = new ClientSportSettingResponse()
                //            {
                //                StatusCode = StatusCode.Error,
                //                Message = "No Client Sport Order to be updated",
                //            }
                //        };
                //    }

                //    _tradingContext.ClientSportOrderAudit.AddRange(auditList);

                //    await _tradingContext.SaveChangesAsync();
                //    scope.Complete();
                //}

                await Publish(new MessageRequest
                {
                    Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                    {
                        ClientCode = request.ClientCode,
                        CacheType = CacheType.SportSortOrder
                    }),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = "OnSportSortOrderChange",
                    RoutingKey = $"r.oe.cachesync.onsportsortorderchange"
                }, "HK");
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientSportSettingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString(),
                    Result = new ClientSportSettingResponse()
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"Error: {ex.Message}"
                    }
                };
            }


            return new BaseResponse<ClientSportSettingResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientSportSettingResponse()
                {
                    StatusCode = StatusCode.Success,
                    Message = "Success! Client Sport Order has been successfully settled."
                }
            };
        }

        public async Task<BaseResponse<List<ClientSportOrderListResponse>>> GetClientSportOrderList(ClientSportOrderListRequest request)
        {
            var clientList = (await _platformRepository.GetClients("HK")).Result;

            if (clientList == null || !clientList.Any())
                return new BaseResponse<List<ClientSportOrderListResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new List<ClientSportOrderListResponse>(),
                };

            var clientIds = clientList.Where(c => string.IsNullOrEmpty(request.ClientCode) || request.ClientCode.Contains(c.ClientCode)).Select(s => s.ClientId).ToList();

            var list = new List<ClientSportOrderListResponse>();

            list = await _sportsbookContext.ClientSportOrder.AsNoTracking()
                            .Where(c => (!clientIds.Any() || clientIds.Contains(c.ClientId))
                                ).Select(s => new ClientSportOrderListResponse()
                                {
                                    ClientId = s.ClientId,
                                    SportId = s.SportId,
                                    SportName = s.SportName,
                                    SortOrder = s.SortOrder,
                                    IsActive = s.IsActive
                                })
                                .OrderBy(o => o.SortOrder)
                                .ToListAsync();

            if (list.Any())
            {
                list.ForEach(a =>
                {
                    var client = clientList.FirstOrDefault(c => c.ClientId == a.ClientId);
                    if (client != null)
                    {
                        a.ClientCode = client.ClientCode;
                        a.ClientName = client.ClientName;
                    }
                });
            }

            return new BaseResponse<List<ClientSportOrderListResponse>>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = list.OrderByDescending(o => o.ClientName).ToList()
            };
        }

        public async Task<BaseResponse<List<ClientSportOrderAuditResponse>>> GetClientSportOrderAuditList(ClientSportOrderAuditRequest request)
        {
            var clientCodes = string.IsNullOrEmpty(request.ClientCode) ? new List<string>() : request.ClientCode.Split(",").Select(s => s).ToList();

            List<ClientSportOrderAuditResponse> auditList = new List<ClientSportOrderAuditResponse>();

            auditList = await _sportsbookContext.ClientSportOrderAudit.AsNoTracking()
                            .Where(c => (!clientCodes.Any() || clientCodes.Contains(c.ClientCode))
                                ).Select(s => new ClientSportOrderAuditResponse()
                                {
                                    ClientCode = s.ClientCode,
                                    ChangesFrom = s.ChangesFrom,
                                    ChangesTo = s.ChangesTo,
                                    UserId = s.UserId,
                                    UserName = s.UserName,
                                    TimeStamp = s.TimeStamp,
                                })
                                .ToListAsync();

            if (auditList.Any())
            {
                var clientList = (await _platformRepository.GetClients("HK")).Result;
                auditList.ForEach(a =>
                {
                    a.ClientName = clientList.FirstOrDefault(c => c.ClientCode == a.ClientCode)?.ClientName;
                });
            }

            return new BaseResponse<List<ClientSportOrderAuditResponse>>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = auditList.OrderByDescending(o => o.TimeStamp).ToList()
            };
        }

        public async Task<BaseResponse<List<ClientSegmentOrderListResponse>>> GetClientSegmentOrderList(ClientSegmentOrderListRequest request)
        {
            var clientList = (await _platformRepository.GetClients("HK")).Result;

            if (clientList == null || !clientList.Any())
                return new BaseResponse<List<ClientSegmentOrderListResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new List<ClientSegmentOrderListResponse>(),
                };

            var clientIds = clientList.Where(c => string.IsNullOrEmpty(request.ClientCode) || request.ClientCode.Contains(c.ClientCode)).Select(s => s.ClientId).ToList();

            List<ClientSegmentOrderListResponse> list = new List<ClientSegmentOrderListResponse>();

            list = await _sportsbookContext.ClientSegmentOrder.AsNoTracking()
                            .Where(c => (!clientIds.Any() || clientIds.Contains(c.ClientId))
                                ).Select(s => new ClientSegmentOrderListResponse()
                                {
                                    ClientId = s.ClientId,
                                    SportId = s.SportId,
                                    SportName = s.SportName,
                                    SegmentId = s.SegmentId,
                                    SegmentName = s.SegmentName,
                                    SortOrder = s.SortOrder,
                                })
                                .OrderBy(o => o.SortOrder)
                                .ToListAsync();

            if (list.Any())
            {
                list.ForEach(a =>
                {
                    var client = clientList.FirstOrDefault(c => c.ClientId == a.ClientId);
                    if (client != null)
                    {
                        a.ClientCode = client.ClientCode;
                        a.ClientName = client.ClientName;
                    }
                });
            }

            return new BaseResponse<List<ClientSegmentOrderListResponse>>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = list.OrderByDescending(o => o.ClientName).ToList()
            };
        }

        public async Task<BaseResponse<List<ClientLeagueOrderListResponse>>> GetClientLeagueOrderList(ClientLeagueOrderListRequest request)
        {
            var clientList = (await _platformRepository.GetClients("HK")).Result;

            if (clientList == null || !clientList.Any())
                return new BaseResponse<List<ClientLeagueOrderListResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new List<ClientLeagueOrderListResponse>(),
                };

            var clientIds = clientList.Where(c => string.IsNullOrEmpty(request.ClientCode) || request.ClientCode.Contains(c.ClientCode)).Select(s => s.ClientId).ToList();

            List<ClientLeagueOrderListResponse> list = new List<ClientLeagueOrderListResponse>();

            list = await _sportsbookContext.ClientLeagueOrder.AsNoTracking()
                            .Where(c => (!clientIds.Any() || clientIds.Contains(c.ClientId))
                                ).Select(s => new ClientLeagueOrderListResponse()
                                {
                                    ClientId = s.ClientId,
                                    SportId = s.SportId,
                                    SportName = s.SportName,
                                    SegmentId = s.SegmentId,
                                    SegmentName = s.SegmentName,
                                    LeagueId = s.LeagueId,
                                    LeagueName = s.LeagueName,
                                    SortOrder = s.SortOrder,
                                })
                                .OrderBy(o => o.SortOrder)
                                .ToListAsync();

            if (list.Any())
            {
                list.ForEach(a =>
                {
                    var client = clientList.FirstOrDefault(c => c.ClientId == a.ClientId);
                    if (client != null)
                    {
                        a.ClientCode = client.ClientCode;
                        a.ClientName = client.ClientName;
                    }
                });
            }

            return new BaseResponse<List<ClientLeagueOrderListResponse>>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = list.OrderByDescending(o => o.ClientName).ToList()
            };
        }

        public async Task<BaseResponse<List<TierLeagueOrder>>> GetTierLeagueOrderList()
        {
            var leagueOrder = GetTierLeagueOrder().ToList();

            return new BaseResponse<List<TierLeagueOrder>>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = leagueOrder
            };
        }

        private List<TierLeagueOrder> GetTierLeagueOrder()
        {
            return _memCache.GetCache($"tlo", () => _sportsbookContext.TierLeagueOrder.ToList(), 24);
        }

        #endregion

        #region Market Sort Order

        public async Task<BaseResponse<ClientMarketTemplateResponse>> GetClientMarketTemplates(string clientCode, int sportId, int fixtureStatusId, string langCode)
        {
            try
            {

                var clientList = (await _platformRepository.GetClients("HK")).Result;
                var client = clientList.FirstOrDefault(c => c.ClientCode == clientCode);

                if (client == null)
                {
                    return new BaseResponse<ClientMarketTemplateResponse>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')}"
                    };
                }

                var request = new ClientMarketTemplateRequest
                {
                    ClientId = client.ClientId,
                    SportId = sportId,
                    FixtureStatusId = fixtureStatusId
                };

                request.MarketTemplates = (await GetMarketLookup(new MarketLookupRequest
                {
                    FixtureTypeId = (int)FixtureType.Match,
                    SportId = sportId
                }, langCode)).Result.MarketLookup
                .Select(s => new ClientMarketTemplateModel
                {
                    MarketTemplateId = s.MarketTemplateId,
                    MarketTemplateName = s.MarketTemplateName,
                    SortOrder = 0
                })
                //.OrderBy(o => int.Parse(o.MarketTemplateId))
                .ToList();

                var response = await GetHttpClient().PostAsync($"client/markettemplates", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ClientMarketTemplateResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientMarketTemplateResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ClientMarketTemplateResponse>> AddOrUpdateClientMarketTemplatesOrder(ClientMarketTemplateRequest request)
        {
            try
            {
                var response = await GetHttpClient().PostAsync($"client/markettemplatesorder", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    await Publish(new MessageRequest
                    {
                        Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                        {
                            ClientId = request.ClientId,
                            ClientCode = request.ClientCode,
                            CacheType = CacheType.MarketSortOrder
                        }),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnMarketSortOrderChange",
                        RoutingKey = $"r.oe.cachesync.onmarketsortorderchange"
                    }, "HK");
                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponse<ClientMarketTemplateResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse <ClientMarketTemplateResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }


        public async Task<BaseResponse<bool>> CopyClientMarketTemplatesOrder(CopyClientMarketSortOrderRequest request)
        {
            try
            {
                var existingClientMarketTemplateOrder = new List<ClientMarketTemplateOrder>();
                var newClientMarketTemplateOrder = new List<ClientMarketTemplateOrder>();

                var existingClientMarketTemplateGroup = new List<ClientMarketTemplateGroup>();

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new System.Transactions.TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                {
                    existingClientMarketTemplateOrder = await _sportsbookContext.ClientMarketTemplateOrder.Where(x => x.ClientId == request.CopyToClientId && x.SportId == request.CopyToSportId).ToListAsync();
                    if (existingClientMarketTemplateOrder.Any())
                    {
                        _sportsbookContext.ClientMarketTemplateOrder.RemoveRange(existingClientMarketTemplateOrder);
                    }

                    existingClientMarketTemplateGroup = await _sportsbookContext.ClientMarketTemplateGroup.Where(x => x.ClientId == request.CopyToClientId && x.SportId == request.CopyToSportId && x.FixtureStatusId == request.CopyToFixtureStatusId).ToListAsync();
                    if (existingClientMarketTemplateGroup.Any())
                    {
                        _sportsbookContext.ClientMarketTemplateGroup.RemoveRange(existingClientMarketTemplateGroup);
                    }

                    await _sportsbookContext.SaveChangesAsync();

                    var sourceClientMarketTemplateOrder = await _sportsbookContext.ClientMarketTemplateOrder.AsNoTracking().Where(x => x.ClientId == request.CopyFromClientId && x.SportId == request.CopyFromSportId).ToListAsync();
                    if (sourceClientMarketTemplateOrder.Any())
                    {
                        newClientMarketTemplateOrder = sourceClientMarketTemplateOrder.Select(x => new ClientMarketTemplateOrder()
                        {
                            ClientId = request.CopyToClientId,
                            SportId = request.CopyToSportId,
                            MarketTemplateId = x.MarketTemplateId,
                            SortOrder = x.SortOrder
                        }).ToList();
                    }
                    await _sportsbookContext.AddRangeAsync(newClientMarketTemplateOrder);

                    var sourceClientMarketTemplateGroup = await _sportsbookContext.ClientMarketTemplateGroup.AsNoTracking().Where(x => x.ClientId == request.CopyFromClientId && x.SportId == request.CopyFromSportId && x.FixtureStatusId == request.CopyFromFixtureStatusId).ToListAsync();

                    foreach (var group in sourceClientMarketTemplateGroup)
                    {
                        await _sportsbookContext.Database.ExecuteSqlRawAsync(
                            @"INSERT INTO ClientMarketTemplateGroup (ClientId, SportId, FixtureStatusId, Name, SortOrder, IsActive, MarketTemplates)
                              VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})",
                            request.CopyToClientId, request.CopyToSportId, request.CopyToFixtureStatusId,
                            group.Name, group.SortOrder, group.IsActive, (object?)group.MarketTemplates ?? DBNull.Value);
                    }

                    scope.Complete();
                }

                //await _sportsbookContext.ActivityLog.AddAsync(new OE.Shared.Data.Trading.Entities.ActivityLog()
                //{
                //    Page = Page.OE_Administration_MarketSortOrder.ToString().Replace("_", "."),
                //    Payload = JsonSerializer.Serialize(request),
                //    Activity = Activity.Update.ToString(),
                //    From = JsonSerializer.Serialize(existingClientMarketTemplateOrder),
                //    To = JsonSerializer.Serialize(newClientMarketTemplateOrder),
                //    UserId = request.UserId,
                //    TimeStamp = DateTime.UtcNow,
                //});

                //await _sportsbookContext.ActivityLog.AddAsync(new OE.Shared.Data.Trading.Entities.ActivityLog()
                //{
                //    Page = Page.OE_Administration_MarketSortOrder.ToString().Replace("_", "."),
                //    Payload = JsonSerializer.Serialize(request),
                //    Activity = Activity.Update.ToString(),
                //    From = JsonSerializer.Serialize(existingClientMarketTemplateGroup),
                //    To = JsonSerializer.Serialize(newClientMarketTemplateGroup),
                //    UserId = request.UserId,
                //    TimeStamp = DateTime.UtcNow,
                //});

                await Publish(new MessageRequest
                {
                    Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                    {
                        ClientId = request.CopyToClientId,
                        CacheType = CacheType.MarketSortOrder
                    }),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = "OnMarketSortOrderChange",
                    RoutingKey = $"r.oe.cachesync.onmarketsortorderchange"
                }, "HK");

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Success,
                    Result = true,
                    Message = "Copy market sort order successful"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Result = false,
                    Message = ex.Message
                };
            }
        }

        #endregion

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
            try
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
            catch (Exception ex)
            {

                throw;
            }
            
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

    }
}