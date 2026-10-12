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
        // ActivityLog2.Page of every League Sorting audit entry, the same value the Web logs dialog filters on.
        private static readonly string OddstableLeagueSortingLogPage = SportsbookClientPermission.SBC_OE_LeagueSorting.ToString().Replace('_', '.');
        private const string OddstableAuditWarning = " The change was saved but its audit log could not be written.";

        private static IMongoDatabase _db;
        private static IMongoCollection<MarketLookup> _marketLookupCollection;
        private static IMongoCollection<SportLookup> _sportLookupCollection;
        private static IMongoCollection<SegmentLookup> _segmentLookupCollection;
        private static IMongoCollection<LeagueLookup> _leagueLookupCollection;

        private readonly SportsbookContext _sportsbookContext;
        private readonly IConfiguration _configuration;
        private readonly IPlatformRepository _platformRepository;
        private readonly IBackofficeActivityLogRepository _activityLogRepository;
        private readonly IRabbitMqRepository _rabbitMqRepository;
        private readonly IMemCache _memCache;

        public SportsbookRepository(SportsbookContext sportsbookContext, IConfiguration configuration, IPlatformRepository platformRepository, IBackofficeActivityLogRepository activityLogRepository, IRabbitMqRepository rabbitMqRepository, IMemCache memCache)
        {
            _sportsbookContext = sportsbookContext;
            _configuration = configuration;
            _platformRepository = platformRepository;
            _rabbitMqRepository = rabbitMqRepository;
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
                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                {
                    await _rabbitMqRepository.Publish(new MessageRequest
                    {
                        Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                        {
                            CacheType = CacheType.MarketSortOrder,
                            ClientId = request.ClientId,
                            ClientCode = string.Empty,
                            CacheKey = string.Empty
                        }),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnMarketSortOrderChange",
                        RoutingKey = $"r.oe.cachesync.onmarketsortorderchange"
                    }, region);

                    return JsonSerializer.Deserialize<BaseResponse<ClientPostMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientPostMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                {
                    await _rabbitMqRepository.Publish(new MessageRequest
                    {
                        Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                        {
                            CacheType = CacheType.MarketSortOrder,
                            ClientId = request.ClientId,
                            ClientCode = string.Empty,
                            CacheKey = string.Empty
                        }),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnMarketSortOrderChange",
                        RoutingKey = $"r.oe.cachesync.onmarketsortorderchange"
                    }, region);

                    return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                {
                    await _rabbitMqRepository.Publish(new MessageRequest
                    {
                        Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                        {
                            CacheType = CacheType.MarketSortOrder,
                            ClientId = request.CopyToClientId,
                            ClientCode = string.Empty,
                            CacheKey = string.Empty
                        }),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnMarketSortOrderChange",
                        RoutingKey = $"r.oe.cachesync.onmarketsortorderchange"
                    }, region);
                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateGroupResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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

            //var changeFrom = string.Empty;
            //var changeTo = string.Empty;

            try
            {
                #region Delete Existing Client Sport Order

                await _sportsbookContext.Database.ExecuteSqlRawAsync("EXEC DeleteClientSportSettings {0}", client.ClientId);

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
                        _sportsbookContext.ClientSportOrder.Add(new ClientSportOrder()
                        {
                            ClientId = client.ClientId,
                            SportId = item.SportId,
                            SportName = item.SportName,
                            SortOrder = item.SortOrder,
                            IsActive = item.IsActive
                        });

                        //Segments
                        if (item.SegmentsSortOrder != null && item.SegmentsSortOrder.Any())
                        {
                            //changeTo = changeTo + $"<b>{item.SportName} > Segment</b></br>";
                            //changeTo = changeTo + string.Join(", ", item.SegmentsSortOrder.ToList().OrderBy(o => o.SortOrder).Select(s => s.SegmentName.Trim()).ToList());
                            //changeTo = changeTo + "<br/><br/>";

                            foreach (var segment in item.SegmentsSortOrder)
                            {
                                _sportsbookContext.ClientSegmentOrder.Add(new ClientSegmentOrder()
                                {
                                    ClientId = client.ClientId,
                                    SportId = segment.SportId,
                                    SportName = segment.SportName,
                                    SegmentId = segment.SegmentId,
                                    SegmentName = segment.SegmentName,
                                    SortOrder = segment.SortOrder,
                                });

                                //Leagues
                                if (segment.LeagueSortOrder != null && segment.LeagueSortOrder.Any())
                                {
                                    //changeTo = changeTo + $"<b>{segment.SportName} > {segment.SegmentName} > League</b></br>";
                                    //changeTo = changeTo + string.Join(", ", segment.LeagueSortOrder.ToList().OrderBy(o => o.SortOrder).Select(s => s.LeagueName.Trim()).ToList());
                                    //changeTo = changeTo + "<br/><br/>";

                                    foreach (var league in segment.LeagueSortOrder)
                                    {
                                        _sportsbookContext.ClientLeagueOrder.Add(new ClientLeagueOrder()
                                        {
                                            ClientId = client.ClientId,
                                            SportId = league.SportId,
                                            SportName = league.SportName,
                                            SegmentId = league.SegmentId,
                                            SegmentName = league.SegmentName,
                                            LeagueId = league.LeagueId,
                                            LeagueName = league.LeagueName,
                                            SortOrder = league.SortOrder,
                                        });
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

                await _rabbitMqRepository.Publish(new MessageRequest
                {
                    Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                    {
                        CacheType = CacheType.SportSortOrder,
                        ClientId = Guid.NewGuid(),
                        ClientCode = request.ClientCode,
                        CacheKey = string.Empty
                    }),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = "OnSportSortOrderChange",
                    RoutingKey = $"r.oe.cachesync.onsportsortorderchange"
                });

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

        public async Task<BaseResponse<List<ClientOddstableLeagueOrderListResponse>>> GetClientOddstableLeagueOrderList(ClientOddstableLeagueOrderListRequest request)
        {
            var clientList = (await _platformRepository.GetClients("HK")).Result ?? new List<PlatformClientResponse>();

            var clients = clientList
                .Where(c => string.IsNullOrEmpty(request.ClientCode) || c.ClientCode == request.ClientCode)
                .ToList();

            if (!clients.Any())
                return new BaseResponse<List<ClientOddstableLeagueOrderListResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new List<ClientOddstableLeagueOrderListResponse>(),
                };

            var clientIds = clients.Select(c => c.ClientId).ToList();

            var list = await _sportsbookContext.ClientOddstableLeagueOrder.AsNoTracking()
                .Where(c => clientIds.Contains(c.ClientId) && (request.SportId == 0 || c.SportId == request.SportId))
                .Select(s => new ClientOddstableLeagueOrderListResponse()
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
                .ToListAsync();

            list.ForEach(a =>
            {
                var client = clients.First(c => c.ClientId == a.ClientId);
                a.ClientCode = client.ClientCode;
                a.ClientName = client.ClientName;
            });

            return new BaseResponse<List<ClientOddstableLeagueOrderListResponse>>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = list.OrderBy(o => o.ClientName).ThenBy(o => o.SportId).ThenBy(o => o.SortOrder).ToList()
            };
        }

        public async Task<BaseResponse<ClientOddstableLeagueSettingResponse>> AddOrUpdateClientOddstableLeagueSettings(ClientOddstableLeagueSettingRequest request)
        {
            if (string.IsNullOrEmpty(request.ClientCode))
                return InvalidOddstableLeagueSetting("Invalid Client Code");

            if (request.SportId == 0)
                return InvalidOddstableLeagueSetting("Invalid Sport");

            if (request.Leagues == null || !request.Leagues.Any())
                return InvalidOddstableLeagueSetting("No Client League Order to be updated");

            if (request.Leagues.Any(l => l.SportId != request.SportId || l.SegmentId == 0 || l.LeagueId == 0 || l.SortOrder == 0))
                return InvalidOddstableLeagueSetting("Error! All fields cannot be empty. Please fill in accordingly or remove the empty row");

            if (request.Leagues.GroupBy(l => new { l.SegmentId, l.LeagueId }).Any(g => g.Count() > 1))
                return InvalidOddstableLeagueSetting("Duplicate client league found");

            var client = ((await _platformRepository.GetClients("HK")).Result ?? new List<PlatformClientResponse>())
                .FirstOrDefault(c => c.ClientCode == request.ClientCode);

            if (client == null)
                return InvalidOddstableLeagueSetting("No client found");

            var auditWritten = true;

            try
            {
                var previous = await ReplaceClientOddstableLeagueOrder(client.ClientId, request.SportId, request.Leagues);
                await _sportsbookContext.SaveChangesAsync();

                var current = request.Leagues.OrderBy(l => l.SortOrder).ToList();
                var logs = new List<OnActivityLogMessage>();

                if (!IsSameOddstableOrder(previous, current))
                {
                    var sportName = request.Leagues.Select(l => l.SportName).FirstOrDefault(n => !string.IsNullOrEmpty(n));

                    logs.Add(BuildOddstableLeagueSortingLog(client.ClientCode, ClassifyOddstableLeagueChange(previous, current), request.SportId, sportName,
                        previous, current, null, request.UserId, request.UserName, request.TimeStamp));
                }

                auditWritten = await TryWriteOddstableLeagueSortingLogs(logs);

                await PublishOddstableLeagueSortOrderChange(client.ClientId, client.ClientCode);
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientOddstableLeagueSettingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString(),
                    Result = new ClientOddstableLeagueSettingResponse()
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"Error: {ex.Message}"
                    }
                };
            }

            return new BaseResponse<ClientOddstableLeagueSettingResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientOddstableLeagueSettingResponse()
                {
                    StatusCode = StatusCode.Success,
                    Message = "Success! Client League Order has been successfully saved." + (auditWritten ? string.Empty : OddstableAuditWarning)
                }
            };
        }

        public async Task<BaseResponse<ClientOddstableLeagueSettingResponse>> CopyClientOddstableLeagueSettings(CopyClientOddstableLeagueSettingRequest request)
        {
            if (string.IsNullOrEmpty(request.SourceClientCode))
                return InvalidOddstableLeagueSetting("Invalid Source Client Code");

            if (request.SportId == 0)
                return InvalidOddstableLeagueSetting("Invalid Sport");

            var targetCodes = (request.TargetClientCodes ?? new List<string>())
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            if (!targetCodes.Any())
                return InvalidOddstableLeagueSetting("No target client selected");

            if (targetCodes.Contains(request.SourceClientCode))
                return InvalidOddstableLeagueSetting("Source client cannot be a copy target");

            var clientList = (await _platformRepository.GetClients("HK")).Result ?? new List<PlatformClientResponse>();
            var source = clientList.FirstOrDefault(c => c.ClientCode == request.SourceClientCode);
            var targets = clientList.Where(c => targetCodes.Contains(c.ClientCode)).ToList();

            if (source == null || targets.Count != targetCodes.Count)
                return InvalidOddstableLeagueSetting("No client found");

            var auditWritten = true;

            try
            {
                var sourceRows = await _sportsbookContext.ClientOddstableLeagueOrder.AsNoTracking()
                    .Where(c => c.ClientId == source.ClientId && c.SportId == request.SportId)
                    .ToListAsync();

                if (!sourceRows.Any())
                    return InvalidOddstableLeagueSetting("Source client has no league sorting to copy");

                var leagues = sourceRows.Select(r => new OddstableLeagueSortOrder()
                {
                    SportId = r.SportId,
                    SportName = r.SportName,
                    SegmentId = r.SegmentId,
                    SegmentName = r.SegmentName,
                    LeagueId = r.LeagueId,
                    LeagueName = r.LeagueName,
                    SortOrder = r.SortOrder,
                }).ToList();

                var previousByTarget = new Dictionary<Guid, List<OddstableLeagueSortOrder>>();

                foreach (var target in targets)
                    previousByTarget[target.ClientId] = await ReplaceClientOddstableLeagueOrder(target.ClientId, request.SportId, leagues);

                await _sportsbookContext.SaveChangesAsync();

                var sportName = leagues.Select(l => l.SportName).FirstOrDefault(n => !string.IsNullOrEmpty(n));

                auditWritten = await TryWriteOddstableLeagueSortingLogs(targets
                    .Select(t => BuildOddstableLeagueSortingLog(t.ClientCode, "Copy", request.SportId, sportName,
                        previousByTarget[t.ClientId], leagues, source.ClientCode, request.UserId, request.UserName, request.TimeStamp))
                    .ToList());

                foreach (var target in targets)
                    await PublishOddstableLeagueSortOrderChange(target.ClientId, target.ClientCode);
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientOddstableLeagueSettingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString(),
                    Result = new ClientOddstableLeagueSettingResponse()
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"Error: {ex.Message}"
                    }
                };
            }

            return new BaseResponse<ClientOddstableLeagueSettingResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientOddstableLeagueSettingResponse()
                {
                    StatusCode = StatusCode.Success,
                    Message = "Success! Client League Order has been successfully copied." + (auditWritten ? string.Empty : OddstableAuditWarning)
                }
            };
        }

        // BO-122: imports a League Sorting file for one client and one sport. Commit = false only validates and
        // changes nothing. The matching and merge rules live in OddstableLeagueImportPlanner; a commit saves
        // through the same path as Edit: one SaveChanges, an audit log entry and the cache-sync publish.
        public async Task<BaseResponse<ImportClientOddstableLeagueSettingResponse>> ImportClientOddstableLeagueSettings(ImportClientOddstableLeagueSettingRequest request)
        {
            if (string.IsNullOrEmpty(request.ClientCode))
                return InvalidOddstableLeagueImport("Invalid Client Code");

            if (request.SportId == 0)
                return InvalidOddstableLeagueImport("Invalid Sport");

            var client = ((await _platformRepository.GetClients("HK")).Result ?? new List<PlatformClientResponse>())
                .FirstOrDefault(c => c.ClientCode == request.ClientCode);

            if (client == null)
                return InvalidOddstableLeagueImport("No client found");

            var auditWritten = true;

            try
            {
                var current = (await _sportsbookContext.ClientOddstableLeagueOrder.AsNoTracking()
                        .Where(c => c.ClientId == client.ClientId && c.SportId == request.SportId)
                        .ToListAsync())
                    .OrderBy(c => c.SortOrder)
                    .Select(c => new OddstableLeagueSortOrder()
                    {
                        SportId = c.SportId,
                        SportName = c.SportName,
                        SegmentId = c.SegmentId,
                        SegmentName = c.SegmentName,
                        LeagueId = c.LeagueId,
                        LeagueName = c.LeagueName,
                        SortOrder = c.SortOrder,
                    })
                    .ToList();

                var catalog = await GetOddstableLeagueCatalog(request.SportId, request.SportName);
                var plan = OddstableLeagueImportPlanner.Build(request.SportId, request.SportName, current, catalog, request.Rows ?? new List<ImportOddstableLeagueRow>());

                var result = new ImportClientOddstableLeagueSettingResponse
                {
                    StatusCode = StatusCode.Success,
                    MatchedCount = plan.MatchedCount,
                    UnmatchedCount = plan.UnmatchedCount,
                    RetainedCount = plan.RetainedCount,
                    ResultCount = plan.Result.Count,
                    Errors = plan.Errors,
                    Rows = plan.Rows
                };

                if (plan.Errors.Any())
                {
                    result.StatusCode = StatusCode.Error;
                    result.ResultCount = 0;
                    result.Message = "The file cannot be imported. Nothing was saved.";

                    return ImportOddstableLeagueResponse(result);
                }

                result.HasChanges = !IsSameOddstableOrder(current, plan.Result);

                if (!request.Commit)
                {
                    result.Message = result.HasChanges
                        ? "The file is valid."
                        : "The file is valid, but the League Sorting is already in this order.";

                    return ImportOddstableLeagueResponse(result);
                }

                if (!result.HasChanges)
                {
                    result.Message = "No changes: the League Sorting is already in this order.";

                    return ImportOddstableLeagueResponse(result);
                }

                await ReplaceClientOddstableLeagueOrder(client.ClientId, request.SportId, plan.Result);
                await _sportsbookContext.SaveChangesAsync();

                var sportName = plan.Result.Select(l => l.SportName).FirstOrDefault(n => !string.IsNullOrEmpty(n));

                auditWritten = await TryWriteOddstableLeagueSortingLogs(new List<OnActivityLogMessage>
                {
                    BuildOddstableLeagueSortingLog(client.ClientCode, "Import", request.SportId, sportName,
                        current, plan.Result, null, request.UserId, request.UserName, request.TimeStamp)
                });

                await PublishOddstableLeagueSortOrderChange(client.ClientId, client.ClientCode);

                result.Committed = true;
                result.Message = "Success! Client League Order has been successfully imported." + (auditWritten ? string.Empty : OddstableAuditWarning);

                return ImportOddstableLeagueResponse(result);
            }
            catch (Exception ex)
            {
                return new BaseResponse<ImportClientOddstableLeagueSettingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString(),
                    Result = new ImportClientOddstableLeagueSettingResponse()
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"Error: {ex.Message}"
                    }
                };
            }
        }

        // The leagues a client can be given for a sport: the segments and leagues the Edit dialog offers. Virtual
        // only so a test can supply them, because the lookups are static MongoDB collections.
        protected virtual async Task<List<LeagueLookupResponseV2.LeagueLookupModelV2>> GetOddstableLeagueCatalog(int sportId, string sportName)
        {
            var segments = await GetSegmentLookup(sportId);
            var leagues = await GetLeagueLookup(sportId, 0);

            if (segments.StatusCode != StatusCode.Success || segments.Result == null
                || leagues.StatusCode != StatusCode.Success || leagues.Result == null)
                throw new InvalidOperationException("Unable to load the leagues of the sport.");

            var segmentNames = segments.Result.SegmentLookup
                .Where(s => s.SportId == sportId)
                .GroupBy(s => s.SegmentId)
                .ToDictionary(g => g.Key, g => g.First().SegmentName);

            return leagues.Result.LeagueLookup
                .Where(l => l.SportId == sportId && segmentNames.ContainsKey(l.SegmentId))
                .Select(l => new LeagueLookupResponseV2.LeagueLookupModelV2
                {
                    LeagueId = l.LeagueId,
                    LeagueName = l.LeagueName,
                    SportId = l.SportId,
                    SportName = sportName,
                    SegmentId = l.SegmentId,
                    SegmentName = segmentNames[l.SegmentId]
                })
                .ToList();
        }

        public Task<BaseResponse<ActivityLogResponse>> GetOddstableLeagueSortingLogs(OddstableLeagueSortingLogRequest request)
        {
            return _activityLogRepository.GetPagedActivityLogs(
                OddstableLeagueSortingLogPage,
                request.ClientCode,
                request.DateFrom == default ? DateTime.UtcNow.AddDays(-30) : request.DateFrom,
                request.MetaData);
        }

        // Rows are updated in place rather than deleted and re-added: EF cannot track a removed and a
        // re-added row with the same key in one SaveChanges, and a single SaveChanges keeps the replace atomic.
        // Returns the order as it was before the replace, for the audit log.
        private async Task<List<OddstableLeagueSortOrder>> ReplaceClientOddstableLeagueOrder(Guid clientId, int sportId, List<OddstableLeagueSortOrder> leagues)
        {
            var existing = await _sportsbookContext.ClientOddstableLeagueOrder
                .Where(c => c.ClientId == clientId && c.SportId == sportId)
                .ToListAsync();

            var previous = existing
                .OrderBy(e => e.SortOrder)
                .Select(e => new OddstableLeagueSortOrder()
                {
                    SportId = e.SportId,
                    SportName = e.SportName,
                    SegmentId = e.SegmentId,
                    SegmentName = e.SegmentName,
                    LeagueId = e.LeagueId,
                    LeagueName = e.LeagueName,
                    SortOrder = e.SortOrder,
                })
                .ToList();

            var existingByKey = existing.ToDictionary(e => (e.SegmentId, e.LeagueId));
            var requestedKeys = leagues.Select(l => (l.SegmentId, l.LeagueId)).ToHashSet();

            _sportsbookContext.ClientOddstableLeagueOrder.RemoveRange(existing.Where(e => !requestedKeys.Contains((e.SegmentId, e.LeagueId))));

            foreach (var league in leagues)
            {
                if (existingByKey.TryGetValue((league.SegmentId, league.LeagueId), out var row))
                {
                    row.SportName = league.SportName;
                    row.SegmentName = league.SegmentName;
                    row.LeagueName = league.LeagueName;
                    row.SortOrder = league.SortOrder;
                }
                else
                {
                    _sportsbookContext.ClientOddstableLeagueOrder.Add(new ClientOddstableLeagueOrder()
                    {
                        ClientId = clientId,
                        SportId = sportId,
                        SportName = league.SportName,
                        SegmentId = league.SegmentId,
                        SegmentName = league.SegmentName,
                        LeagueId = league.LeagueId,
                        LeagueName = league.LeagueName,
                        SortOrder = league.SortOrder,
                    });
                }
            }

            return previous;
        }

        private static (int SegmentId, int LeagueId) OddstableLeagueKey(OddstableLeagueSortOrder league) =>
            (league.SegmentId, league.LeagueId);

        // Same leagues in the same order. Renamed leagues do not count as a change.
        private static bool IsSameOddstableOrder(List<OddstableLeagueSortOrder> previous, List<OddstableLeagueSortOrder> current) =>
            previous.OrderBy(l => l.SortOrder).Select(OddstableLeagueKey)
                .SequenceEqual(current.OrderBy(l => l.SortOrder).Select(OddstableLeagueKey));

        // Add: there was no order before. Reorder: the same leagues in another order. Edit: leagues added, removed or replaced.
        private static string ClassifyOddstableLeagueChange(List<OddstableLeagueSortOrder> previous, List<OddstableLeagueSortOrder> current)
        {
            if (!previous.Any())
                return "Add";

            return previous.Select(OddstableLeagueKey).ToHashSet().SetEquals(current.Select(OddstableLeagueKey))
                ? "Reorder"
                : "Edit";
        }

        private static OnActivityLogMessage BuildOddstableLeagueSortingLog(
            string clientCode, string activity, int sportId, string sportName,
            List<OddstableLeagueSortOrder> previous, List<OddstableLeagueSortOrder> current,
            string sourceClientCode, string userId, string userName, DateTime timeStamp)
        {
            return new OnActivityLogMessage
            {
                Page = OddstableLeagueSortingLogPage,
                Payload = clientCode,
                Activity = activity,
                From = JsonSerializer.Serialize(ToOddstableLeagueSortingAudit(sportId, sportName, previous, null)),
                To = JsonSerializer.Serialize(ToOddstableLeagueSortingAudit(sportId, sportName, current, sourceClientCode)),
                UserId = userId,
                UserName = userName,
                TimeStamp = timeStamp == default ? DateTime.UtcNow : timeStamp
            };
        }

        private static OddstableLeagueSortingAudit ToOddstableLeagueSortingAudit(
            int sportId, string sportName, List<OddstableLeagueSortOrder> leagues, string sourceClientCode)
        {
            return new OddstableLeagueSortingAudit
            {
                SportId = sportId,
                SportName = sportName,
                SourceClientCode = sourceClientCode,
                Leagues = leagues
                    .OrderBy(l => l.SortOrder)
                    .Select(l => new OddstableLeagueSortingAuditLeague
                    {
                        SegmentId = l.SegmentId,
                        SegmentName = l.SegmentName,
                        LeagueId = l.LeagueId,
                        LeagueName = l.LeagueName,
                        SortOrder = l.SortOrder
                    })
                    .ToList()
            };
        }

        // The change is already saved when this runs, so a failed audit write is logged and reported as a
        // warning instead of turning the whole request into an error.
        private async Task<bool> TryWriteOddstableLeagueSortingLogs(List<OnActivityLogMessage> logs)
        {
            if (!logs.Any())
                return true;

            try
            {
                await _activityLogRepository.ProcessActivityLog(new OnActivityLogListMessage { OnActivityLogMessages = logs });
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "League Sorting audit log could not be written for {ClientCodes}", string.Join(",", logs.Select(l => l.Payload)));
                return false;
            }
        }

        private Task PublishOddstableLeagueSortOrderChange(Guid clientId, string clientCode)
        {
            return _rabbitMqRepository.Publish(new MessageRequest
            {
                Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                {
                    CacheType = CacheType.OddstableLeagueSortOrder,
                    ClientId = clientId,
                    ClientCode = clientCode,
                    CacheKey = string.Empty
                }),
                CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                MessageType = "OnOddstableLeagueSortOrderChange",
                RoutingKey = "r.oe.cachesync.onoddstableleaguesortorderchange"
            });
        }

        private static BaseResponse<ClientOddstableLeagueSettingResponse> InvalidOddstableLeagueSetting(string message)
        {
            return new BaseResponse<ClientOddstableLeagueSettingResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientOddstableLeagueSettingResponse()
                {
                    StatusCode = StatusCode.Error,
                    Message = message,
                }
            };
        }

        private static BaseResponse<ImportClientOddstableLeagueSettingResponse> ImportOddstableLeagueResponse(ImportClientOddstableLeagueSettingResponse result)
        {
            return new BaseResponse<ImportClientOddstableLeagueSettingResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = result
            };
        }

        private static BaseResponse<ImportClientOddstableLeagueSettingResponse> InvalidOddstableLeagueImport(string message)
        {
            return ImportOddstableLeagueResponse(new ImportClientOddstableLeagueSettingResponse
            {
                StatusCode = StatusCode.Error,
                Message = message
            });
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
                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    await _rabbitMqRepository.Publish(new MessageRequest
                    {
                        Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                        {
                            CacheType = CacheType.MarketSortOrder,
                            ClientId = request.ClientId,
                            ClientCode = request.ClientCode,
                            CacheKey = string.Empty
                        }),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnMarketSortOrderChange",
                        RoutingKey = $"r.oe.cachesync.onmarketsortorderchange"
                    });

                    return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponse<ClientMarketTemplateResponse>>(await response.Content.ReadAsStringAsync(), JsonSerializerOptionsProvider.CaseInsensitive);
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
                    await _sportsbookContext.SaveChangesAsync();

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

                await _rabbitMqRepository.Publish(new MessageRequest
                {
                    Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                    {
                        CacheType = CacheType.MarketSortOrder,
                        ClientId = request.CopyToClientId,
                        ClientCode = string.Empty,
                        CacheKey = string.Empty
                    }),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = "OnMarketSortOrderChange",
                    RoutingKey = $"r.oe.cachesync.onmarketsortorderchange"
                });

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

        #region Client Selector Market Templates

        public async Task<BaseResponse<List<ClientSelectorMarketTemplate>>> GetClientSelectorMarketTemplates(Guid clientId)
        {
            try
            {
                var result = await _sportsbookContext.ClientSelectorMarketTemplate
                    .AsNoTracking()
                    .Where(c => c.ClientId == clientId)
                    .OrderBy(o => o.SportId).ThenBy(o => o.AsiaSortOrder)
                    .ToListAsync();

                return new BaseResponse<List<ClientSelectorMarketTemplate>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ClientSelectorMarketTemplate>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<List<SelectorMarketTemplate>>> GetSelectorMarketTemplateLookup()
        {
            try
            {
                var result = await _sportsbookContext.SelectorMarketTemplate
                    .AsNoTracking()
                    .OrderBy(o => o.SportId).ThenBy(o => o.SortOrder)
                    .ToListAsync();

                return new BaseResponse<List<SelectorMarketTemplate>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<SelectorMarketTemplate>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<bool>> AddOrUpdateClientSelectorMarketTemplates(ClientSelectorMarketTemplateRequest request)
        {
            try
            {
                if (request == null || request.ClientId == Guid.Empty)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Result = false,
                        Message = "ClientId is required"
                    };
                }

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new System.Transactions.TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                {
                    var existingTemplates = await _sportsbookContext.ClientSelectorMarketTemplate.Where(x => x.ClientId == request.ClientId).ToListAsync();
                    if (existingTemplates.Any())
                    {
                        _sportsbookContext.ClientSelectorMarketTemplate.RemoveRange(existingTemplates);
                        await _sportsbookContext.SaveChangesAsync();
                    }

                    var newTemplates = request.Templates.Select(x => new ClientSelectorMarketTemplate
                    {
                        ClientId = request.ClientId,
                        SportId = x.SportId,
                        MarketTemplateId = x.MarketTemplateId,
                        MarketTemplateCode = x.MarketTemplateCode,
                        AsiaSortOrder = x.AsiaSortOrder,
                        EuroSortOrder = x.EuroSortOrder,
                        Identifier = x.Identifier
                    }).ToList();

                    await _sportsbookContext.AddRangeAsync(newTemplates);
                    await _sportsbookContext.SaveChangesAsync();

                    scope.Complete();
                }

                await PublishSelectorMarketTemplateCacheSync(request.ClientId, request.ClientCode);

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Success,
                    Result = true,
                    Message = "Selector market templates saved successfully"
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

        public async Task<BaseResponse<bool>> CopyClientSelectorMarketTemplates(CopyClientSelectorMarketTemplateRequest request)
        {
            try
            {
                if (request == null || request.CopyFromClientId == Guid.Empty || request.CopyToClientId == Guid.Empty)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Result = false,
                        Message = "CopyFromClientId and CopyToClientId are required"
                    };
                }

                if (request.CopyFromClientId == request.CopyToClientId)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Result = false,
                        Message = "Cannot copy selector market templates to the same client"
                    };
                }

                var sourceTemplates = await _sportsbookContext.ClientSelectorMarketTemplate.AsNoTracking().Where(x => x.ClientId == request.CopyFromClientId).ToListAsync();
                if (!sourceTemplates.Any())
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Result = false,
                        Message = "The source client has no selector market templates to copy"
                    };
                }

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new System.Transactions.TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                {
                    var existingTemplates = await _sportsbookContext.ClientSelectorMarketTemplate.Where(x => x.ClientId == request.CopyToClientId).ToListAsync();
                    if (existingTemplates.Any())
                    {
                        _sportsbookContext.ClientSelectorMarketTemplate.RemoveRange(existingTemplates);
                        await _sportsbookContext.SaveChangesAsync();
                    }

                    var newTemplates = sourceTemplates.Select(x => new ClientSelectorMarketTemplate
                    {
                        ClientId = request.CopyToClientId,
                        SportId = x.SportId,
                        MarketTemplateId = x.MarketTemplateId,
                        MarketTemplateCode = x.MarketTemplateCode,
                        AsiaSortOrder = x.AsiaSortOrder,
                        EuroSortOrder = x.EuroSortOrder,
                        Identifier = x.Identifier
                    }).ToList();

                    await _sportsbookContext.AddRangeAsync(newTemplates);
                    await _sportsbookContext.SaveChangesAsync();

                    scope.Complete();
                }

                await PublishSelectorMarketTemplateCacheSync(request.CopyToClientId, request.CopyToClientCode);

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Success,
                    Result = true,
                    Message = "Copy selector market templates successful"
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

        // The csmt cache in Platform.Sportsbook.API is only purged by the MarketSortOrder
        // cache type, so selector market template writes reuse the existing
        // OnMarketSortOrderChange cache-sync route instead of introducing a new message type.
        private async Task PublishSelectorMarketTemplateCacheSync(Guid clientId, string clientCode)
        {
            await _rabbitMqRepository.Publish(new MessageRequest
            {
                Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                {
                    CacheType = CacheType.MarketSortOrder,
                    ClientId = clientId,
                    ClientCode = clientCode ?? string.Empty,
                    CacheKey = string.Empty
                }),
                CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                MessageType = "OnMarketSortOrderChange",
                RoutingKey = $"r.oe.cachesync.onmarketsortorderchange"
            });
        }

        #endregion

        #region Lookup

        public async Task<BaseResponse<SportsLookupResponse>> GetSportLookup(string langCode = "EN")
            => await _memCache.GetCache($"sportLookUp-{langCode}", async () =>
            {
                try
                {
                    var sportsLookup = await _sportLookupCollection.Find(_ => true).ToListAsync();

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
                catch (Exception)
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
            catch (Exception)
            {

                throw;
            }

        }
        //, 1);


        public async Task<BaseResponse<MarketLookupResponse>> GetMarketLookup(MarketLookupRequest request, string langCode = "")
        {
            var marketLookupFilter = Builders<MarketLookup>.Filter.Where(c => c.SpId == request.SportId && c.FiTyId == request.FixtureTypeId);
            var marketTemplates = await _marketLookupCollection.Find(marketLookupFilter).ToListAsync();

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