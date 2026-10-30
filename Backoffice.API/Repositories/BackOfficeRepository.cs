using Backoffice.API.Models;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Data.Translation;
using Backoffice.Shared.Data.Translation.Entities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Extensions;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using static Backoffice.Shared.Models.ManualBetDetailsModel;
using static Backoffice.Shared.Models.Requests.AddOrUpdateBetConfigRequest;
using static Backoffice.Shared.Models.Response.ClientListResponse;
using ClientLeagueConfig = Backoffice.Shared.Data.Trading.SQLEntities.ClientLeagueConfig;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace Backoffice.API.Repositories
{
    public class BackofficeRepository : IBackofficeRepository
    {
        #region Fields
        private readonly BackofficeContext _backofficeContext;
        private readonly TranslationContext _translationContext;

        private readonly IPlatformRepository _platformRepository;
        private readonly ISportsbookRepository _sportsbookRepository;
        private readonly ITradingRepository _tradingRepository;
        private readonly IRabbitMqRepository _rabbitMqRepository;
        private readonly IMemCache _memCache;

        private string[] cryptoCurrencies = new string[] { Currency.BTC.ToString(), Currency.ETH.ToString() };

        #endregion

        #region Ctor

        public BackofficeRepository(BackofficeContext backofficeContext, TranslationContext translationContext, IPlatformRepository platformRepository, ISportsbookRepository sportsbookRepository, ITradingRepository tradingRepository, IMemCache memCache, IRabbitMqRepository rabbitMqRepository)
        {
            _backofficeContext = backofficeContext;
            _translationContext = translationContext;
            _platformRepository = platformRepository;
            _sportsbookRepository = sportsbookRepository;
            _tradingRepository = tradingRepository;
            _memCache = memCache;
            _rabbitMqRepository = rabbitMqRepository;
        }

        #endregion

        #region Methods - Activity Logs
        // Note: Will Remove
        public async Task<BaseResponse<bool>> AddActivityLog(ActivityLogModel request)
        {
            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (request.TimeStamp == DateTime.MinValue)
                    {
                        request.TimeStamp = DateTime.UtcNow;
                    }

                    var activityLog = new ActivityLog
                    {
                        Page = request.Page,
                        Payload = request.Payload,
                        From = request.From,
                        To = request.To,
                        Activity = request.Activity,
                        UserId = request.UserId,
                        UserName = request.UserName,
                        TimeStamp = request.TimeStamp
                    };

                    _backofficeContext.ActivityLog.Add(activityLog);

                    await _backofficeContext.SaveChangesAsync();

                    scope.Complete();
                }

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = true
                };
            }
            catch (Exception)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString(),
                    Result = false
                };
            }

        }
        public async Task<BaseResponse<bool>> AddActivityLogList(ActivityLogListRequest request)
        {
            try
            {
                var activityLogs = new List<ActivityLog>();
                foreach (var activity in request.ActivityLogs)
                {
                    activity.TimeStamp = activity.TimeStamp == DateTime.MinValue ? DateTime.UtcNow : activity.TimeStamp;

                    activityLogs.Add(new ActivityLog
                    {
                        Page = activity.Page,
                        Payload = activity.Payload,
                        From = activity.From,
                        To = activity.To,
                        Activity = activity.Activity,
                        UserId = activity.UserId,
                        UserName = activity.UserName,
                        TimeStamp = activity.TimeStamp
                    });
                }

                _backofficeContext.ActivityLog.AddRange(activityLogs);

                await _backofficeContext.SaveChangesAsync();

                return new BaseResponse<bool>
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
                    Message = ex.Message,
                    Result = false
                };
            }
        }
        public async Task<BaseResponse<ActivityLogResponse>> GetPriceboostActivityLogs(PriceboostActivityLogRequest request)
        {
            try
            {
                var metaData = request.MetaData;
                if (metaData == null)
                    metaData = new MetaData()
                    {
                        CurrentPage = 0,
                        PageSize = 10,
                    };
                if (request.DateFrom == DateTime.MinValue)
                    request.DateFrom = DateTime.UtcNow.AddDays(-1).Date;
                if (request.DateTo == DateTime.MinValue)
                    request.DateTo = DateTime.UtcNow.Date;

                request.DateFrom = request.DateFrom.Date.Add(new TimeSpan(0, 0, 0));
                request.DateTo = request.DateTo.Date.Add(new TimeSpan(23, 59, 59));

                var activityLogs = (await _backofficeContext.ReadActivityLog
                        .FromSql($"GetActivityLogs {request.Page}, {request.Payload}, {request.DateFrom.ToString("yyyyMMdd HH:mm:ss")}, {request.DateTo.ToString("yyyyMMdd HH:mm:ss")}")
                        .AsNoTracking()
                    .ToListAsync())
                    .Select(s => new ActivityLogModel
                    {
                        Id = s.Id,
                        Page = s.Page,
                        Payload = s.Payload,
                        Activity = s.Activity,
                        From = s.From,
                        To = s.To,
                        UserId = s.UserId,
                        UserName = s.UserName,
                        TimeStamp = s.TimeStamp,
                    }).Where(x => request.UserClients.Contains(x.Payload.Split('.')[0], StringComparer.OrdinalIgnoreCase)).ToList();

                //TODO: get username from Authentication Repository

                metaData.TotalCount = activityLogs.Count();
                metaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(metaData.TotalCount) / Convert.ToDecimal(metaData.PageSize)));

                var skip = (metaData.CurrentPage - 1) * metaData.PageSize;

                if (metaData.CurrentPage > 0)
                    activityLogs = activityLogs.Skip(skip).Take(metaData.PageSize).ToList();

                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ActivityLogResponse
                    {
                        ActivityLogs = activityLogs,
                        MetaData = metaData
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ActivityLogResponse>> GetManualBetSettlementHistoryLogs(ManualBetSettlementActivityLogRequest request)
        {
            try
            {
                var metaData = request.MetaData;
                if (metaData == null)
                    metaData = new MetaData()
                    {
                        CurrentPage = 0,
                        PageSize = 10,
                    };
                if (request.ManualBetSettlementCallbackModel.DateFrom == DateTime.MinValue)
                    request.ManualBetSettlementCallbackModel.DateFrom = DateTime.UtcNow.AddDays(-1).Date;
                if (request.ManualBetSettlementCallbackModel.DateTo == DateTime.MinValue)
                    request.ManualBetSettlementCallbackModel.DateTo = DateTime.UtcNow.Date;

                request.ManualBetSettlementCallbackModel.DateFrom = request.ManualBetSettlementCallbackModel.DateFrom.Date.Add(new TimeSpan(0, 0, 0));
                request.ManualBetSettlementCallbackModel.DateTo = request.ManualBetSettlementCallbackModel.DateTo.Date.Add(new TimeSpan(23, 59, 59));

                var activityLogs = (await _backofficeContext.ReadActivityLog
                        .FromSql($"GetActivityLogs {AdminPermission.Admin_Settlements_ManualBetSettlement.ToString().Replace('_', '.')}, '', {request.ManualBetSettlementCallbackModel.DateFrom.ToString("yyyyMMdd HH:mm:ss")}, {request.ManualBetSettlementCallbackModel.DateTo.ToString("yyyyMMdd HH:mm:ss")}")
                        .AsNoTracking()
                    .ToListAsync())
                    .Select(s => new ActivityLogModel
                    {
                        Id = s.Id,
                        Page = s.Page,
                        Payload = s.Payload,
                        Activity = s.Activity,
                        From = s.From,
                        To = s.To,
                        UserId = s.UserId,
                        UserName = s.UserName,
                        TimeStamp = s.TimeStamp,
                    }).OrderByDescending(x => x.TimeStamp).ToList();

                if (request.ManualBetSettlementCallbackModel != null)
                {
                    activityLogs = FilterManualSettlementLogs(activityLogs, request.ManualBetSettlementCallbackModel);
                }

                metaData.TotalCount = activityLogs.Count();
                metaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(metaData.TotalCount) / Convert.ToDecimal(metaData.PageSize)));

                var skip = (metaData.CurrentPage - 1) * metaData.PageSize;

                if (metaData.CurrentPage > 0)
                    activityLogs = activityLogs.Skip(skip).Take(metaData.PageSize).ToList();

                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ActivityLogResponse
                    {
                        ActivityLogs = activityLogs,
                        MetaData = metaData
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new ActivityLogResponse { ActivityLogs = new List<ActivityLogModel>() }
                };
            }
        }

        public async Task<BaseResponse<ActivityLogResponse>> GetActivityLogs(ActivityLogRequest request)
        {
            try
            {
                var metaData = request.MetaData;

                if (request.DateFrom == DateTime.MinValue)
                    request.DateFrom = DateTime.UtcNow.Date.Add(new TimeSpan(0, 0, 0));
                if (request.DateTo == DateTime.MinValue)
                    request.DateTo = DateTime.UtcNow.Date.Add(new TimeSpan(23, 59, 59));

                var fromUTC = request.DateFrom.ToString("yyyy-MM-dd HH:mm:ss");
                var toUTC = request.DateTo.ToString("yyyy-MM-dd HH:mm:ss");
                var activityLogs = (await _backofficeContext.ReadActivityLog
                        .FromSql($"GetActivityLogs {request.Page}, {request.Payload}, {fromUTC}, {toUTC}")
                        .AsNoTracking()
                    .ToListAsync())
                    .Select(s => new ActivityLogModel
                    {
                        Id = s.Id,
                        Page = s.Page,
                        Payload = s.Payload,
                        Activity = s.Activity,
                        From = s.From,
                        To = s.To,
                        UserId = s.UserId,
                        UserName = s.UserName,
                        TimeStamp = s.TimeStamp,
                    }).ToList();

                activityLogs = FilterActivityLogsBySearch(activityLogs, request);

                if (metaData == null)
                {
                    metaData = new MetaData()
                    {
                        CurrentPage = 0,
                        PageSize = activityLogs.Count(),
                    };
                }

                metaData.TotalCount = activityLogs.Count();
                metaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(metaData.TotalCount) / Convert.ToDecimal(metaData.PageSize)));

                var skip = (metaData.CurrentPage - 1) * metaData.PageSize;

                if (metaData.CurrentPage > 0)
                    activityLogs = activityLogs.Skip(skip).Take(metaData.PageSize).ToList();

                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ActivityLogResponse
                    {
                        ActivityLogs = activityLogs,
                        MetaData = metaData
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ActivityLogResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #endregion

        #region Methods - Client
        public async Task<BaseResponse<ClientResponse>> GetClients()
        {
            try
            {
                var clients = GetClientInformation()
                    .Where(c => c.IsDeleted == false && c.StatusId == (int)ClientStatus.Normal && (c.ApplicationId == (int)Application2.SBC || c.ApplicationId == (int)Application2.SB))
                    .Select(client => new ClientResponse.Client
                    {
                        ClientId = client.ClientId,
                        ClientName = client.Name,
                        ClientCode = client.Brand,
                        Region = client.Region
                    }).ToList();

                return new BaseResponse<ClientResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ClientResponse
                    {
                        Clients = clients ?? new List<ClientResponse.Client>()
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ClientResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = null
                };
            }
        }
        public async Task<BaseResponse<ClientListResponse>> GetClientList(GetClientListRequest request, List<string> brands = null)
        {
            try
            {
                var query = GetClientInformation().Where(c => c.IsDeleted == false && (c.ApplicationId == (int)Application2.SBC || c.ApplicationId == (int)Application2.SB));

                if (brands != null)
                {
                    query = query.Where(c => brands.Contains(c.Brand));
                }
                if (!string.IsNullOrEmpty(request.Search))
                    query = query.Where(c => c.Name.Contains(request.Search, StringComparison.InvariantCultureIgnoreCase));

                // sort order
                if (request.SortBy == ClientSorting.SortByBusinessNameAsc)
                {
                    query = query.OrderBy(o => o.Name);
                }
                else if (request.SortBy == ClientSorting.SortByBusinessNameDesc)
                {
                    query = query.OrderByDescending(o => o.Name);
                }
                else if (request.SortBy == ClientSorting.SortByCreatedDateAsc)
                {
                    query = query.OrderBy(o => o.CreatedDate);
                }
                else if (request.SortBy == ClientSorting.SortByCreatedDateDesc)
                {
                    query = query.OrderByDescending(o => o.CreatedDate);
                }

                var totalClientCount = query.Count();

                var skip = (request.Page - 1) * request.PageSize;
                var totalPage = ((totalClientCount - 1) / request.PageSize) + 1;

                var clients = query.Skip(skip).Take(request.PageSize).ToList();

                var platformClients = request.IncludePlatformFields
                    ? (await GetPlatformClients()).Result ?? new List<PlatformClientResponse>()
                    : new List<PlatformClientResponse>();

                var clientList = new List<ClientListResponse.Client>();
                foreach (var client in clients)
                {
                    var platformClient = platformClients.FirstOrDefault(p => p.ClientIdRef == client.ClientId)
                        ?? platformClients.FirstOrDefault(p => string.Equals(p.ClientCode, client.Brand, StringComparison.OrdinalIgnoreCase));

                    var clientResponse = new ClientListResponse.Client
                    {
                        ClientId = client.ClientId,
                        ClientCode = client.Brand,
                        Name = client.Name,
                        Website = client.Website,
                        StatusId = client.StatusId,
                        StatusName = ((ClientStatus)client.StatusId).ToString(),
                        Region = client.Region,
                        IsEmailNotification = client.IsEmailNotification,
                        Email = client.Email,
                        TelephoneNumber = client.TelephoneNumber,
                        MonthlyFee = client.MonthlyFee,
                        BetSlipMaxSelection = client.BetSlipMaxSelection,
                        IsTierRevenue = client.IsTierRevenueShare,
                        TierJackpotShare = client.RevenueShareJackpot,
                        TierSportsbookShare = client.RevenueShareSportsbook,
                        PlatformClientId = platformClient?.ClientId,
                        LimitId = platformClient?.LimitId,
                        Endpoint = platformClient?.Endpoint,
                    };

                    var taxConfig = string.IsNullOrEmpty(client.TaxConfig) ? new TaxConfig() : JsonSerializer.Deserialize<TaxConfig>(client.TaxConfig);
                    var taxConfigPercent = new TaxConfig
                    {
                        StakeTax = taxConfig.StakeTax * 100m,
                        WinningsTax = taxConfig.WinningsTax * 100m,
                        ExcludeStakeSportTypes = taxConfig.ExcludeStakeSportTypes ?? string.Empty,
                        ExcludeWinningsSportTypes = taxConfig.ExcludeWinningsSportTypes ?? string.Empty
                    };

                    clientResponse.TaxConfig = taxConfigPercent;


                    var revenueShare = new ClientListResponse.RevenueShare()
                    {
                        SportsbookShare = client.SportsbookShare * 100m,
                        JackpotShare = client.JackpotShare * 100m
                    };

                    clientResponse.RevenueShare = revenueShare;

                    clientList.Add(clientResponse);
                }

                return new BaseResponse<ClientListResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ClientListResponse
                    {
                        Page = request.Page,
                        PageSize = request.PageSize,
                        Total = totalClientCount,
                        TotalPage = totalPage,
                        Clients = clientList ?? new List<ClientListResponse.Client>()
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
                    Result = GetClientInformation().Where(s => s.IsDeleted == false && s.ClientId == clientId).Select(s => s.Region).FirstOrDefault().ToString()
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
        public async Task<BaseResponse<string>> GetClientRegionByClientCode(string clientCode)
        {
            try
            {
                return new BaseResponse<string>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Success.ToString(),
                    Result = GetClientInformation().Where(s => s.IsDeleted == false && s.Brand == clientCode).Select(s => s.ClientId).FirstOrDefault().ToString()
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
                    var clientInformation = GetClientInformation();

                    var hkResult = clientInformation.Where(s => s.IsDeleted == false && clients.Contains(s.ClientId) && s.Region == "HK").Select(s => s.ClientId).ToList();
                    if (hkResult.Any())
                        result.Add("HK", string.Join(",", hkResult));

                    //var saResult = clientInformation.Where(s => s.IsDeleted == false && clients.Contains(s.ClientId) && s.Region == "SA").Select(s => s.ClientId).ToList();
                    //if (saResult.Any())
                    //    result.Add("SA", string.Join(",", saResult));
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
        public async Task<BaseResponse<bool>> AddOrUpdateClient(ClientListResponse.Client request)
        {
            try
            {
                var events = new List<Event>();
                var timestampStartedValidation = DateTime.UtcNow;

                #region VALIDATION
                var userId = 0;

                long clientId = 0;
                var clientInformation = GetClientInformation();

                if (clientInformation.Any(c => c.Brand == request.ClientCode && c.ClientId != request.ClientId))
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "Brand ID is already in use. Please add another ID.",
                        Result = false
                    };
                }
                if (clientInformation.Any(c => c.Name == request.Name && c.ClientId != request.ClientId))
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "Business name already exist.",
                        Result = false
                    };
                }
                if (!request.Website.Contains("https://"))
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "Website URL should start with https://",
                        Result = false
                    };
                }
                var isValid = Uri.IsWellFormedUriString(request.Website, UriKind.Absolute);

                if (isValid)
                {
                    string url = request.Website;

                    var urlProtocols = new List<string>() { "https://www.", "https://", "http://www.", "http://" };

                    var websites = new List<string>();

                    foreach (var urlProtocol in urlProtocols)
                    {
                        url = url.Replace(urlProtocol, string.Empty);
                    }

                    foreach (var urlProtocol in urlProtocols)
                    {
                        var urlToValidate = $"{urlProtocol}{url}";
                        websites.Add(urlToValidate);
                    }

                    if (clientInformation.Any(c => websites.Contains(c.Website) && c.ClientId != request.ClientId))
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Website is already used by another client.",
                            Result = false
                        };
                    }
                }
                else
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "Website URL should start with https://",
                        Result = false
                    };
                }

                //validate email
                try
                {
                    _ = new MailAddress(request.Email);
                }
                catch
                {
                    if (request.IsEmailNotification)
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Invalid email address",
                            Result = false
                        };
                    }
                }

                if (request.IsTierRevenue)
                {
                    //Validate Revenue Share Sportsbook
                    if (string.IsNullOrEmpty(request.TierSportsbookShare))
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Revenue share sportsbook is required",
                            Result = false
                        };
                    }

                    //Validate Revenue Share Jackpot 
                    if (string.IsNullOrEmpty(request.TierJackpotShare))
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Revenue share jackpot is required",
                            Result = false
                        };
                    }

                    var revenueShareSB = JsonSerializer.Deserialize<TierRevenueShareModel>(request.TierSportsbookShare);
                    var revenueShareJp = JsonSerializer.Deserialize<TierRevenueShareModel>(request.TierJackpotShare);

                    if (revenueShareSB.Tiers.Count > 6)
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Row count for revenue share sportsbook must be maximum of 6 items only",
                            Result = false
                        };
                    }

                    if (revenueShareSB.Tiers.Select(x => x.Tier).ToList().Min() != 1)
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Revenue Share Sportsbook Tier must start at 1",
                            Result = false
                        };
                    }

                    //Check if Tier is sequential
                    bool isSequential = Enumerable.Range(revenueShareSB.Tiers.Select(x => x.Tier).ToList().Min(), revenueShareSB.Tiers.Select(x => x.Tier).ToList().Count()).SequenceEqual(revenueShareSB.Tiers.Select(x => x.Tier).ToList());
                    if (!isSequential)
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Revenue share sportsbook incorrect sequence for Tier",
                            Result = false
                        };
                    }



                    if (revenueShareJp.Tiers.Count > 6)
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Row count for revenue share sportsbook must be maximum of 6 items only",
                            Result = false
                        };
                    }

                    if (revenueShareJp.Tiers.Select(x => x.Tier).ToList().Min() != 1)
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Revenue Share Jackpot Tier must start at 1",
                            Result = false
                        };
                    }

                    //Check if Tier is sequential
                    isSequential = Enumerable.Range(revenueShareJp.Tiers.Select(x => x.Tier).ToList().Min(), revenueShareJp.Tiers.Select(x => x.Tier).ToList().Count()).SequenceEqual(revenueShareJp.Tiers.Select(x => x.Tier).ToList());
                    if (!isSequential)
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Revenue share jackpot incorrect sequence for Tier",
                            Result = false
                        };
                    }

                    //VALIDATE RANGE
                    //revenueShareSB = revenueShareSB.Tiers.OrderBy(a => a.Tier).ToList();
                    for (int i = 0; i < revenueShareSB.Tiers.Count(); i++)
                    {
                        if (i == 0 && revenueShareSB.Tiers[i].Tier == 1 && revenueShareSB.Tiers[i].From != 0.01m)
                        {
                            return new BaseResponse<bool>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Revenue share sportsbook starting value for Tier 1 must be 0.01",
                                Result = false
                            };
                        }

                        if (revenueShareSB.Tiers[i].Percentage <= 0m)
                        {
                            return new BaseResponse<bool>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Percentage must be greater than 0",
                                Result = false
                            };
                        }

                        if (revenueShareSB.Tiers[i].From >= revenueShareSB.Tiers[i].To)
                        {
                            return new BaseResponse<bool>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Revenue share sportsbook TO must be greater than FROM",
                                Result = false
                            };
                        }

                        if (i != 0 && (revenueShareSB.Tiers[i].From != revenueShareSB.Tiers[i - 1].To + 0.01m))
                        {
                            return new BaseResponse<bool>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Revenue share sportsbook incorrect range for tier " + revenueShareSB.Tiers[i - 1].Tier + " and " + revenueShareSB.Tiers[i].Tier,
                                Result = false
                            };
                        }

                        //validation that % GGR can only be lower in the next tier
                        if (i != 0 && revenueShareSB.Tiers[i].Percentage > revenueShareSB.Tiers[i - 1].Percentage)
                        {
                            return new BaseResponse<bool>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "% GGR for sportsbook can only be lower in the next tier",
                                Result = false
                            };
                        }
                    }

                    //validate aboveTierPercentage
                    var aboveTierPercentage = revenueShareSB.AboveTierPercentage;
                    var minTierPercentage = revenueShareSB.Tiers.Min(a => a.Percentage);

                    if (aboveTierPercentage <= 0m)
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Above Tier Percentage sportsbook must be greater than 0",
                            Result = false
                        };
                    }

                    if (aboveTierPercentage > minTierPercentage)
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Above tier % GGR for sportsbook can only be lower in the next tier",
                            Result = false
                        };
                    }

                    //revenueShareJp = revenueShareJp.OrderBy(a => a.Tier).ToList();
                    for (int i = 0; i < revenueShareJp.Tiers.Count(); i++)
                    {
                        if (i == 0 && revenueShareJp.Tiers[i].Tier == 1 && revenueShareJp.Tiers[i].From != 0.01m)
                        {
                            return new BaseResponse<bool>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Revenue share jackpot starting value for Tier 1 must be 0.01",
                                Result = false
                            };
                        }

                        if (revenueShareJp.Tiers[i].Percentage <= 0m)
                        {
                            return new BaseResponse<bool>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Percentage must be greater than 0",
                                Result = false
                            };
                        }

                        if (revenueShareJp.Tiers[i].From >= revenueShareJp.Tiers[i].To)
                        {
                            return new BaseResponse<bool>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Revenue share Jackpot TO must be greater than FROM",
                                Result = false
                            };
                        }

                        if (i != 0 && (revenueShareJp.Tiers[i].From != revenueShareJp.Tiers[i - 1].To + 0.01m))
                        {
                            return new BaseResponse<bool>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "Revenue share jackpot incorrect range for tier " + revenueShareJp.Tiers[i - 1].Tier + " and " + revenueShareJp.Tiers[i].Tier,
                                Result = false
                            };
                        }

                        //validation that % GGR can only be lower in the next tier
                        if (i != 0 && revenueShareJp.Tiers[i].Percentage > revenueShareJp.Tiers[i - 1].Percentage)
                        {
                            return new BaseResponse<bool>
                            {
                                StatusCode = StatusCode.Error,
                                Message = "% GGR for jackpot can only be lower in the next tier",
                                Result = false
                            };
                        }
                    }

                    //validate aboveTierPercentage
                    aboveTierPercentage = revenueShareJp.AboveTierPercentage;
                    minTierPercentage = revenueShareJp.Tiers.Min(a => a.Percentage);

                    if (aboveTierPercentage <= 0m)
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Above Tier Percentage jackpot must be greater than 0",
                            Result = false
                        };
                    }

                    if (aboveTierPercentage > minTierPercentage)
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Above tier % GGR for jackpot can only be lower in the next tier",
                            Result = false
                        };
                    }

                    if (request.RevenueShare.SportsbookShare < 0 || request.RevenueShare.SportsbookShare > 1m)
                    {
                        request.RevenueShare.SportsbookShare = 0m;
                    }

                    if (request.RevenueShare.JackpotShare < 0 || request.RevenueShare.JackpotShare > 1m)
                    {
                        request.RevenueShare.JackpotShare = 0;
                    }
                }
                else
                {
                    if (request.RevenueShare.SportsbookShare < 0 || request.RevenueShare.SportsbookShare > 1m)
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Sportsbook revenue share must be between 0 - 100%.",
                            Result = false
                        };
                    }

                    if (request.RevenueShare.JackpotShare < 0 || request.RevenueShare.JackpotShare > 1m)
                    {
                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = "JackpotShare revenue share must be between 0 - 100%.",
                            Result = false
                        };
                    }
                }


                if (clientInformation.Any(c => c.Brand == request.ClientCode && c.ClientId != request.ClientId))
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "Brand is already used by another client.",
                        Result = false
                    };
                }

                if (request.ClientCode.Length > 3)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "Brand must not be greater than 3 characters.",
                        Result = false
                    };
                }

                if (!Regex.Match(request.ClientCode, @"^[a-zA-Z0-9]+$", RegexOptions.IgnoreCase).Success)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "Brand must be alphanumeric.",
                        Result = false
                    };
                }
                #endregion

                events.Add(GetEvent("VALIDATION", timestampStartedValidation, DateTime.UtcNow));

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                {

                    var timestampStartedUpdate = DateTime.UtcNow;

                    if (request.ClientId == 0)
                    {
                        var clientRecord = new ClientInformation
                        {
                            Name = request.Name,
                            TelephoneNumber = request.TelephoneNumber,
                            Website = request.Website,
                            CreatedBy = 0,
                            CreatedDate = DateTime.UtcNow,
                            ModifiedBy = 0,
                            ModifiedDate = DateTime.UtcNow,
                            StatusId = request.StatusId,
                            ApplicationId = (int)Application2.SB,
                            SportsbookShare = request.RevenueShare.SportsbookShare,
                            JackpotShare = request.RevenueShare.JackpotShare,
                            ClientShare = 0,
                            MonthlyFee = request.MonthlyFee,
                            Brand = request.ClientCode,
                            IsTierRevenueShare = request.IsTierRevenue,
                            RevenueShareJackpot = request.IsTierRevenue == true ? request.TierJackpotShare : null,
                            RevenueShareSportsbook = request.IsTierRevenue == true ? request.TierSportsbookShare : null,
                            TaxConfig = JsonSerializer.Serialize(request.TaxConfig),
                            Region = request.Region,
                            Email = request.Email,
                            IsEmailNotification = request.IsEmailNotification,
                            BetSlipMaxSelection = request.BetSlipMaxSelection,
                        };

                        clientId = InsertClientRecord(clientRecord);
                    }
                    else
                    {
                        var client = _backofficeContext.ClientInformation.Find(request.ClientId);

                        if (client != null)
                        {
                            client.ApplicationId = (int)Application2.SB;
                            client.Name = request.Name;
                            client.Email = request.Email;
                            client.TelephoneNumber = request.TelephoneNumber;
                            client.Website = request.Website;
                            client.ModifiedBy = userId;
                            client.ModifiedDate = DateTime.UtcNow;
                            client.StatusId = request.StatusId;
                            client.ClientShare = 0;
                            client.SportsbookShare = request.RevenueShare.SportsbookShare;
                            client.JackpotShare = request.RevenueShare.JackpotShare;
                            client.MonthlyFee = request.MonthlyFee;
                            client.IsTierRevenueShare = request.IsTierRevenue;
                            client.RevenueShareJackpot = request.IsTierRevenue == true ? request.TierJackpotShare : null;
                            client.RevenueShareSportsbook = request.IsTierRevenue == true ? request.TierSportsbookShare : null;
                            client.TaxConfig = JsonSerializer.Serialize(request.TaxConfig);
                            client.Region = request.Region;
                            client.IsEmailNotification = request.IsEmailNotification;
                            client.BetSlipMaxSelection = request.BetSlipMaxSelection;
                        }

                        clientId = UpdateClientRecord(client);
                    }

                    events.Add(GetEvent("UPDATE CLIENT", timestampStartedUpdate, DateTime.UtcNow));

                    timestampStartedUpdate = DateTime.UtcNow;

                    var clientBetSettings = _backofficeContext.ClientBetSetting.Where(b => b.ClientId == clientId).ToList();

                    var clientBetSettingList = new ConcurrentBag<ClientBetSetting>();
                    var baseResponse = new List<BaseResponse<bool>>();

                    Parallel.ForEach(request.BetSettings, (betSetting, state) =>
                    {
                        int[] categories = { (int)BetSettingCategory.Soccer, (int)BetSettingCategory.OtherSports, (int)BetSettingCategory.Parlay, (int)BetSettingCategory.Cricket, (int)BetSettingCategory.Priceboost, (int)BetSettingCategory.BetBuilder };

                        decimal suggestedMinimumBet = 0;
                        suggestedMinimumBet = GetSuggestedMinimumBet(betSetting.CurrencyCode).Result;

                        foreach (var i in categories)
                        {
                            var clientBetSetting = string.Empty;
                            var minStake = 0m;
                            var maxStake = 0m;
                            var maxWinnings = 0m;
                            byte decimalPlaces = new byte();

                            switch (i)
                            {
                                #region SOCCER
                                case (int)BetSettingCategory.Soccer:

                                    minStake = betSetting.BetSettingSoccer.MinPerTicket ?? 0;
                                    maxStake = betSetting.BetSettingSoccer.MaxPerTicket ?? 0;
                                    clientBetSetting = JsonSerializer.Serialize(betSetting.BetSettingSoccer);

                                    break;
                                #endregion

                                #region ALL SPORTS
                                case (int)BetSettingCategory.OtherSports:

                                    minStake = betSetting.BetSettingAllSports.MinPerTicket ?? 0;
                                    maxStake = betSetting.BetSettingAllSports.MaxPerTicket ?? 0;

                                    clientBetSetting = JsonSerializer.Serialize(betSetting.BetSettingAllSports);

                                    break;
                                #endregion
                                #region PARLAY
                                case (int)BetSettingCategory.Parlay:
                                    minStake = betSetting.BetSettingParlay.MinPerTicket ?? 0;
                                    maxStake = betSetting.BetSettingParlay.MaxPerTicket ?? 0;
                                    maxWinnings = betSetting.BetSettingParlay.MaxWinningsBonus ?? 0;

                                    clientBetSetting = JsonSerializer.Serialize(betSetting.BetSettingParlay);
                                    break;
                                #endregion

                                #region CRICKET
                                case (int)BetSettingCategory.Cricket:
                                    minStake = betSetting.BetSettingCricket.MinPerTicket ?? 0;
                                    maxStake = betSetting.BetSettingCricket.MaxPerTicket ?? 0;

                                    clientBetSetting = JsonSerializer.Serialize(betSetting.BetSettingCricket);
                                    break;
                                #endregion

                                #region PRICEBOOST
                                case (int)BetSettingCategory.Priceboost:
                                    clientBetSetting = JsonSerializer.Serialize(betSetting.BetSettingPriceboost);

                                    break;
                                #endregion
                                #region BETBUILDER
                                case (int)BetSettingCategory.BetBuilder:
                                    minStake = betSetting.BetSettingBetBuilder.MinStake ?? 0;
                                    maxStake = betSetting.BetSettingBetBuilder.MaxStake ?? 0;

                                    decimalPlaces = BitConverter.GetBytes(decimal.GetBits(betSetting.BetSettingBetBuilder.MaxStake ?? 0)[3])[2];

                                    clientBetSetting = JsonSerializer.Serialize(betSetting.BetSettingBetBuilder);
                                    break;
                                    #endregion
                            }

                            if ((int)BetSettingCategory.Priceboost != i)
                            {

                                if (minStake < suggestedMinimumBet)
                                {
                                    baseResponse.Add(new BaseResponse<bool>
                                    {
                                        StatusCode = StatusCode.Error,
                                        Message = $"Suggested minimum bet not met. ({betSetting.CurrencyCode})",
                                        Result = false
                                    });

                                    state.Stop();
                                }

                                if (minStake > maxStake)
                                {
                                    baseResponse.Add(new BaseResponse<bool>
                                    {
                                        StatusCode = StatusCode.Error,
                                        Message = $"Max amount must be greater than minimum amount. ({betSetting.CurrencyCode})",
                                        Result = false
                                    });

                                    state.Stop();
                                }
                            }

                            if (i == (int)BetSettingCategory.Parlay)
                            {
                                if (maxWinnings < 0.02m)
                                {
                                    baseResponse.Add(new BaseResponse<bool>
                                    {
                                        StatusCode = StatusCode.Error,
                                        Message = $"Max Winnings Bonus must be at least 0.02. ({betSetting.CurrencyCode})",
                                        Result = false
                                    });

                                    state.Stop();
                                }
                            }

                            if (i == (int)BetSettingCategory.Priceboost || i == (int)BetSettingCategory.BetBuilder)
                            {
                                if (maxStake > 99999999999 || decimalPlaces > 12)
                                {
                                    baseResponse.Add(new BaseResponse<bool>
                                    {
                                        StatusCode = StatusCode.Error,
                                        Message = $"Maximum stake must be lower or equal 99999999999. ({betSetting.CurrencyCode})",
                                        Result = false
                                    });

                                    state.Stop();
                                }
                            }

                            if (clientBetSetting != null)
                            {
                                clientBetSettingList.Add(new ClientBetSetting
                                {
                                    CategoryId = i,
                                    ClientId = clientId,
                                    Setting = clientBetSetting,
                                    CreatedBy = userId,
                                    CreatedDate = DateTime.UtcNow,
                                    ModifiedBy = userId,
                                    ModifiedDate = DateTime.UtcNow,
                                    CurrencyCode = betSetting.CurrencyCode
                                });
                            }
                        }
                    });

                    if (baseResponse.Any())
                        return baseResponse.FirstOrDefault();

                    if (clientBetSettings.Any())
                        _backofficeContext.ClientBetSetting.RemoveRange(clientBetSettings);

                    if (clientBetSettingList.Count > 0)
                        _backofficeContext.ClientBetSetting.AddRange(clientBetSettingList.ToList());

                    await _backofficeContext.SaveChangesAsync();

                    events.Add(GetEvent("UPDATE BET SETTINGS", timestampStartedUpdate, DateTime.UtcNow));

                    try
                    {
                        var betConfig = new ConcurrentBag<BetConfig>();
                        Parallel.ForEach(request.BetSettings, betSetting =>
                        {
                            betConfig.Add(new BetConfig
                            {
                                CurrencyCode = betSetting.CurrencyCode,
                                Football = new FootballBetSetting()
                                {
                                    MinBet = betSetting.BetSettingSoccer.MinPerTicket ?? 0,
                                    MaxBet = betSetting.BetSettingSoccer.MaxPerTicket ?? 0,
                                    MaxFixtureBet = betSetting.BetSettingSoccer.MaxPerGame ?? 0
                                },
                                OtherSports = new AnySportBetSetting()
                                {
                                    MinBet = betSetting.BetSettingAllSports.MinPerTicket ?? 0,
                                    MaxBet = betSetting.BetSettingAllSports.MaxPerTicket ?? 0,
                                    MaxFixtureBet = betSetting.BetSettingAllSports.MaxPerGame ?? 0
                                },
                                Parlay = new ParlayBetSetting()
                                {
                                    MinBet = betSetting.BetSettingParlay.MinPerTicket ?? 0,
                                    MaxBet = betSetting.BetSettingParlay.MaxPerTicket ?? 0,
                                    MaxPayout = betSetting.BetSettingParlay.MaxPayoutPerTicket ?? 0,
                                    ParlayBonusMaxWinnings = betSetting.BetSettingParlay.MaxWinningsBonus ?? 0
                                },
                                Cricket = new CricketBetSetting()
                                {
                                    MinBet = betSetting.BetSettingCricket.MinPerTicket ?? 0,
                                    MaxBet = betSetting.BetSettingCricket.MaxPerTicket ?? 0,
                                    MaxFixtureBet = betSetting.BetSettingCricket.MaxPerGame ?? 0
                                },
                                Priceboost = new PriceboostBetSetting()
                                {
                                    MaxStake = betSetting.BetSettingPriceboost.MaxStake ?? 0
                                },
                                BetBuilder = new BetBuilderBetSetting()
                                {
                                    MinStake = betSetting.BetSettingBetBuilder.MinStake ?? 0,
                                    MaxStake = betSetting.BetSettingBetBuilder.MaxStake ?? 0
                                },
                                TaxConfig = request.TaxConfig
                            });
                        });

                        //Update client in NewPlatformDB
                        decimal companyRevenueShare = request.RevenueShare.SportsbookShare;
                        if (request.IsTierRevenue)
                        {
                            //Get highiest value from SB table
                            var revenueShareSB = JsonSerializer.Deserialize<TierRevenueShareModel>(request.TierSportsbookShare);
                            companyRevenueShare = revenueShareSB.Tiers.Select(a => a.Percentage).Max();
                        }

                        //Insert client in NewPlatformDB
                        var clientPlatform = new ClientRequest
                        {
                            // Platform uses this only when inserting a new client (empty = Platform generates one);
                            // an existing client's ClientId never changes.
                            ClientId = request.RequestedPlatformClientId ?? Guid.Empty,
                            ClientName = request.Name,
                            ClientCode = request.ClientCode,
                            IsActive = request.StatusId == (int)ClientStatus.Normal ? true : false,
                            ClientIdRef = (int)clientId,
                            LimitId = string.IsNullOrEmpty(request.LimitId) ? string.Empty : request.LimitId,
                            Endpoint = string.IsNullOrEmpty(request.Endpoint) ? string.Empty : request.Endpoint,
                            MonthlyFee = request.MonthlyFee,
                            CurrentCompanyShare = companyRevenueShare,
                            EffectiveDate = DateTime.UtcNow,
                        };

                        timestampStartedUpdate = DateTime.UtcNow;

                        var newPlatformResponse = await _platformRepository.AddOrUpdateClient(clientPlatform, request.Region, events);
                        if (newPlatformResponse.StatusCode == StatusCode.Success)
                        {
                            request.PlatformClientId = newPlatformResponse.Result.ClientId;

                            events.Add(GetEvent("UPDATE PLATFORM CLIENT", timestampStartedUpdate, DateTime.UtcNow));

                            var betConfigRequest = new AddOrUpdateBetConfigRequest
                            {
                                ClientId = newPlatformResponse.Result.ClientId,
                                BetConfigPayload = betConfig.ToList()
                            };
                            int retry = 0;
                            while (retry < 3)
                            {
                                timestampStartedUpdate = DateTime.UtcNow;

                                var newPlatformBettingResponse = await _platformRepository.ProcessBackofficeClientBetConfig(betConfigRequest, request.Region);
                                if (newPlatformBettingResponse.StatusCode == StatusCode.Success)
                                {
                                    events.Add(GetEvent("UPDATE PLATFORM BETCONFIG", timestampStartedUpdate, DateTime.UtcNow));
                                    break;
                                }
                                retry += 1;
                            }

                            //new trading
                            //await _tradingClient.UpdateClientEmail(clientPlatform);
                        }
                        else
                        {
                            Log.Error("Platform registration returned {StatusCode} for client {ClientCode}: {Message}",
                                newPlatformResponse.StatusCode, request.ClientCode, newPlatformResponse.Message);
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Platform registration failed for client {ClientCode}", request.ClientCode);
                    }

                    scope.Complete();


                    //_telemetryClient.TrackEvent($"e:backoffice.client={request.ClientCode}", properties);

                    //return response;
                    return new BaseResponse<bool>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = true
                    };
                }

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

        public async Task<BaseResponse<BetSettingResponse>> GetBetSetting(BetSettingRequest request)
        {
            try
            {
                var query = _backofficeContext.ClientBetSetting.AsNoTracking();

                if (request.ClientId > 0)
                    query = query.Where(c => c.ClientId == request.ClientId);

                var result = query.ToList();

                if (result == null || !result.Any())
                {
                    return new BaseResponse<BetSettingResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new BetSettingResponse
                        {
                            BetSettings = new List<ClientListResponse.BetSetting>()
                        }
                    };
                }

                var currencies = result.Select(s => s.CurrencyCode).Distinct().ToList();

                var betSettings = new List<ClientListResponse.BetSetting>();

                foreach (var currency in currencies)
                {
                    var betSetting = new ClientListResponse.BetSetting
                    {
                        CurrencyCode = currency
                    };

                    var currencySettings = result.Where(b => b.CurrencyCode == currency).ToList();

                    foreach (var setting in currencySettings)
                    {
                        if (setting.CategoryId == (int)BetSettingCategory.Soccer)
                        {
                            betSetting.BetSettingSoccer = JsonSerializer.Deserialize<BetSettingSoccer>(setting.Setting);
                        }
                        else if (setting.CategoryId == (int)BetSettingCategory.Parlay)
                        {
                            betSetting.BetSettingParlay = JsonSerializer.Deserialize<BetSettingParlay>(setting.Setting);
                        }
                        else if (setting.CategoryId == (int)BetSettingCategory.Cricket)
                        {
                            betSetting.BetSettingCricket = JsonSerializer.Deserialize<BetSettingCricket>(setting.Setting);
                        }
                        else if (setting.CategoryId == (int)BetSettingCategory.OtherSports)
                        {
                            betSetting.BetSettingAllSports = JsonSerializer.Deserialize<BetSettingAllSports>(setting.Setting);
                        }
                        else if (setting.CategoryId == (int)BetSettingCategory.Priceboost)
                        {
                            betSetting.BetSettingPriceboost = JsonSerializer.Deserialize<BetSettingPriceboost>(setting.Setting);
                        }
                        else if (setting.CategoryId == (int)BetSettingCategory.BetBuilder)
                        {
                            betSetting.BetSettingBetBuilder = JsonSerializer.Deserialize<BetSettingBetBuilder>(setting.Setting);
                        }
                    }

                    betSettings.Add(betSetting);
                }

                return new BaseResponse<BetSettingResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetSettingResponse
                    {
                        BetSettings = betSettings ?? new List<ClientListResponse.BetSetting>()
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<BetSettingResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = null
                };
            }
        }

        public async Task<BaseResponse<Dictionary<int, List<ClientListResponse.BetSetting>>>> GetBetSettingsBulk(List<int> clientIds)
        {
            try
            {
                var result = new Dictionary<int, List<ClientListResponse.BetSetting>>();
                if (clientIds == null || !clientIds.Any())
                {
                    return new BaseResponse<Dictionary<int, List<ClientListResponse.BetSetting>>>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = result
                    };
                }

                var clientIdsLong = clientIds.Select(id => (long)id).ToList();
                var allSettings = _backofficeContext.ClientBetSetting.AsNoTracking()
                    .Where(c => clientIdsLong.Contains(c.ClientId))
                    .ToList();

                foreach (var clientGroup in allSettings.GroupBy(s => s.ClientId))
                {
                    var betSettings = new List<ClientListResponse.BetSetting>();

                    foreach (var currencyGroup in clientGroup.GroupBy(s => s.CurrencyCode))
                    {
                        var betSetting = new ClientListResponse.BetSetting
                        {
                            CurrencyCode = currencyGroup.Key
                        };

                        foreach (var setting in currencyGroup)
                        {
                            if (setting.CategoryId == (int)BetSettingCategory.Soccer)
                            {
                                betSetting.BetSettingSoccer = JsonSerializer.Deserialize<BetSettingSoccer>(setting.Setting);
                            }
                            else if (setting.CategoryId == (int)BetSettingCategory.Parlay)
                            {
                                betSetting.BetSettingParlay = JsonSerializer.Deserialize<BetSettingParlay>(setting.Setting);
                            }
                            else if (setting.CategoryId == (int)BetSettingCategory.Cricket)
                            {
                                betSetting.BetSettingCricket = JsonSerializer.Deserialize<BetSettingCricket>(setting.Setting);
                            }
                            else if (setting.CategoryId == (int)BetSettingCategory.OtherSports)
                            {
                                betSetting.BetSettingAllSports = JsonSerializer.Deserialize<BetSettingAllSports>(setting.Setting);
                            }
                            else if (setting.CategoryId == (int)BetSettingCategory.Priceboost)
                            {
                                betSetting.BetSettingPriceboost = JsonSerializer.Deserialize<BetSettingPriceboost>(setting.Setting);
                            }
                            else if (setting.CategoryId == (int)BetSettingCategory.BetBuilder)
                            {
                                betSetting.BetSettingBetBuilder = JsonSerializer.Deserialize<BetSettingBetBuilder>(setting.Setting);
                            }
                        }

                        betSettings.Add(betSetting);
                    }

                    result[(int)clientGroup.Key] = betSettings;
                }

                return new BaseResponse<Dictionary<int, List<ClientListResponse.BetSetting>>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<Dictionary<int, List<ClientListResponse.BetSetting>>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = null
                };
            }
        }

        public async Task<BaseResponse<ExchangeRatesResponse>> GetExchangeRates()
        {
            try
            {
                return await _platformRepository.GetExchangeRates("HK");
            }
            catch (Exception ex)
            {
                return new BaseResponse<ExchangeRatesResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = null
                };
            }
        }

        public async Task<BaseResponse<List<PlatformClientResponse>>> GetPlatformClients()
        {
            try
            {
                return await _platformRepository.GetClients("HK");
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<PlatformClientResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = null
                };
            }
        }

        #endregion

        #region Config
        private async Task<List<ConfigTemplateModel>> GetFilteredPaginatedConfigTemplates(string searchText, List<int> sportIds, List<int> scopeIds)
        {
            var sportIdsParam = new SqlParameter("@SportIds", SqlDbType.Structured)
            {
                TypeName = "dbo.IntList",
                Value = CreateDataTable(sportIds)
            };

            var scopeIdsParam = new SqlParameter("@ScopeIds", SqlDbType.Structured)
            {
                TypeName = "dbo.IntList",
                Value = CreateDataTable(scopeIds)
            };

            var results = await _backofficeContext.ConfigTemplateModel
                .FromSqlRaw("EXEC GetAssignedConfigTemplates @SearchText, @SportIds, @ScopeIds",
                    new SqlParameter("@SearchText", (object)searchText ?? DBNull.Value),
                    sportIdsParam,
                    scopeIdsParam)
                .AsNoTracking()
                .ToListAsync();

            return results;
        }
        private DataTable CreateDataTable(List<int> ids = null)
        {
            var table = new DataTable();
            table.Columns.Add("Value", typeof(int));

            if (ids != null && ids.Any())
                foreach (var id in ids)
                {
                    table.Rows.Add(id);
                }

            return table;
        }
        public async Task<BaseResponse<ConfigTemplateResponse>> GetConfigTemplates(string searchText, List<int> sportIds = null, List<int> scopeIds = null)
        {
            try
            {
                var result = await GetFilteredPaginatedConfigTemplates(searchText, sportIds, scopeIds);

                return new BaseResponse<ConfigTemplateResponse>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ConfigTemplateResponse()
                    {
                        ConfigTemplates = result
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ConfigTemplateResponse>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString()}: {ex.Message}"
                };
            }
        }
        public async Task<BaseResponse<int>> GetConfigTemplatesCount()
        {
            try
            {
                var result = _backofficeContext.ConfigTemplate.AsNoTracking().Count();

                return new BaseResponse<int>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<int>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString()}: {ex.Message}",
                    Result = -1
                };
            }
        }

        public async Task<BaseResponse<ConfigTemplateModel>> GetConfigTemplateById(long templateId)
        {
            try
            {
                var query = _backofficeContext.ConfigTemplate.AsNoTracking();

                var result = query.Where(e => e.Id == templateId).Select(x => new ConfigTemplateModel()
                {
                    Id = x.Id,
                    Name = x.Name,
                    SportId = x.SportId,
                    FixtureTypeId = x.FixtureTypeId,
                    FixtureStatusId = x.FixtureStatusId,
                    Timestamp = x.Timestamp
                }).FirstOrDefault();

                return new BaseResponse<ConfigTemplateModel>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ConfigTemplateModel>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString()}: {ex.Message}",
                    Result = null
                };
            }
        }
        public async Task<BaseResponse<long>> AddOrUpdateConfigTemplate(ConfigTemplateModel request)
        {
            try
            {
                var config = _backofficeContext.ConfigTemplate.Where(x => x.Id == request.Id).AsNoTracking().FirstOrDefault();

                if (config != null)
                {
                    var isExistingName = _backofficeContext.ConfigTemplate.Any(e => e.Name.Trim().ToLower() == request.Name.Trim().ToLower() && e.Id != config.Id);

                    if (isExistingName)
                    {
                        return new BaseResponse<long>()
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Sorry, that name already exists!"
                        };
                    }
                    else if (config.Name.Trim().ToLower() != request.Name.Trim().ToLower())
                    {
                        config.Name = request.Name.Trim();
                        _backofficeContext.ConfigTemplate.Update(config);
                        await _backofficeContext.SaveChangesAsync();
                    }
                }
                else
                {
                    var isExistingName = _backofficeContext.ConfigTemplate.Any(e => e.Name.Trim().ToLower() == request.Name.Trim().ToLower());

                    if (isExistingName)
                    {
                        return new BaseResponse<long>()
                        {
                            StatusCode = StatusCode.Error,
                            Message = "Sorry, that name already exists!"
                        };
                    }

                    config = new ConfigTemplate()
                    {
                        Name = request.Name.Trim(),
                        SportId = request.SportId,
                        FixtureTypeId = request.FixtureTypeId,
                        FixtureStatusId = request.FixtureStatusId,
                        Timestamp = DateTime.UtcNow
                    };

                    _backofficeContext.ConfigTemplate.Add(config);
                    await _backofficeContext.SaveChangesAsync();
                }

                return new BaseResponse<long>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = config.Id
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<long>() { StatusCode = StatusCode.Error, Message = ex.Message };
            }
        }
        // BO-151: assign leagues to clients as Active or Restricted, or unassign them. A league is assigned to a client
        // when the client has a row for it in Trading.dbo.ClientLeagueConfig, and IsActive = 0 means Restricted (hidden for
        // that client). Every change is written to the activity log with the user, the league and the old and new state.
        public async Task<BaseResponse<bool>> SetConfigState(ConfigState configState, ConfigSetLeagueStateRequest request, List<string> userClientCodes, RequesterInfo requesterInfo)
        {
            try
            {
                var assign = configState == ConfigState.Assigned;
                if (!assign && configState != ConfigState.UnAssigned)
                    return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = "Invalid state type" };

                var leagues = request?.Leagues;
                if (leagues == null || leagues.Count == 0)
                    return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = "No leagues selected" };

                if (leagues.Any(l => string.IsNullOrWhiteSpace(l.ClientCode) || !Guid.TryParse(l.ClientId, out _)))
                    return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = "A selected league has no valid client" };

                // a user can only change the leagues of the clients they can see
                var allowedCodes = userClientCodes ?? new List<string>();
                var seesAllClients = allowedCodes.Count == 1 && allowedCodes[0].Equals("ALL", StringComparison.OrdinalIgnoreCase);
                if (!seesAllClients)
                {
                    var notAllowed = leagues
                        .Select(l => l.ClientCode)
                        .Where(code => !allowedCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (notAllowed.Any())
                        return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = $"Not allowed to change the leagues of {string.Join(", ", notAllowed)}" };
                }

                var rows = leagues.Select(l => new ClientLeagueConfig
                {
                    ClientCode = l.ClientCode.ToUpperInvariant(),
                    ClientId = Guid.Parse(l.ClientId),
                    SportId = l.SportId,
                    SegmentId = l.SegmentId,
                    LeagueId = l.LeagueId,
                    IsActive = !request.Restricted
                }).ToList();

                var beforeResponse = await _tradingRepository.GetClientLeagueConfigRows(rows.Select(r => r.ClientCode).Distinct().ToList());
                if (beforeResponse.StatusCode != StatusCode.Success || beforeResponse.Result == null)
                    return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = beforeResponse.Message ?? "League settings could not be read" };

                var before = beforeResponse.Result.GroupBy(ConfigRowKey).ToDictionary(g => g.Key, g => g.First());

                var write = assign
                    ? await _tradingRepository.UpsertClientLeagueConfigRows(rows)
                    : await _tradingRepository.DeleteClientLeagueConfigRows(rows);

                if (write.StatusCode != StatusCode.Success)
                    return write;

                var logs = new List<ActivityLogModel>();
                foreach (var row in rows.GroupBy(ConfigRowKey).Select(g => g.Last()))
                {
                    var hadRow = before.TryGetValue(ConfigRowKey(row), out var old);
                    var from = hadRow ? ConfigStateName(old.IsActive) : "Unassigned";
                    var to = assign ? ConfigStateName(row.IsActive) : "Unassigned";

                    if (from == to)
                        continue; // nothing changed, so nothing to log

                    logs.Add(new ActivityLogModel
                    {
                        Page = requesterInfo?.Page ?? "Config",
                        Payload = $"{row.ClientCode}.{row.SportId}.{row.SegmentId}.{row.LeagueId}",
                        Activity = (!hadRow ? Shared.Enums.Activity.Create : assign ? Shared.Enums.Activity.Update : Shared.Enums.Activity.Delete).ToString(),
                        From = from,
                        To = to,
                        UserId = requesterInfo?.UserId,
                        UserName = requesterInfo?.UserName,
                        TimeStamp = DateTime.UtcNow
                    });
                }

                if (logs.Any())
                {
                    var logResponse = await AddActivityLogList(new ActivityLogListRequest { ActivityLogs = logs });

                    // the leagues are saved, so a failed log is reported but does not undo or fail the change
                    if (logResponse.StatusCode != StatusCode.Success)
                    {
                        Log.Error("The activity log of {LogCount} league changes could not be written: {Message}", logs.Count, logResponse.Message);

                        return new BaseResponse<bool>
                        {
                            StatusCode = StatusCode.Success,
                            Message = $"Saved, but the activity log could not be written ({logResponse.Message})",
                            Result = true
                        };
                    }
                }

                return new BaseResponse<bool>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = true
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = ex.Message };
            }
        }

        private static (string ClientCode, int SportId, int SegmentId, int LeagueId) ConfigRowKey(ClientLeagueConfig row) =>
            ((row.ClientCode ?? string.Empty).ToUpperInvariant(), row.SportId, row.SegmentId, row.LeagueId);

        private static string ConfigStateName(bool isActive) => isActive ? "Active" : "Restricted";

        // BO-151: the leagues the clients have a saved setting for, with the names of the sportsbook league lookup. A
        // saved league that is no longer in the lookup is still listed, so that it can be unassigned.
        private static List<ConfigClientModel> BuildAssignedLeagues(List<ClientLeagueConfig> savedRows, List<LeagueLookupResponseV2.LeagueLookupModelV2> leagueLookup, List<PlatformClientResponse> clients, ConfigTemplateLeagueRequest payload)
        {
            var clientsByCode = clients
                .GroupBy(c => c.ClientCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var leaguesByKey = leagueLookup
                .GroupBy(l => (l.SportId, l.SegmentId, l.LeagueId))
                .ToDictionary(g => g.Key, g => g.First());

            var sportId = payload?.SportId ?? -1;

            var assigned = savedRows
                .Where(r => r.ClientCode != null && clientsByCode.ContainsKey(r.ClientCode))
                .Where(r => sportId == -1 || r.SportId == sportId)
                .Select(r =>
                {
                    var client = clientsByCode[r.ClientCode];
                    leaguesByKey.TryGetValue((r.SportId, r.SegmentId, r.LeagueId), out var league);

                    return new ConfigClientModel
                    {
                        Id = -1,
                        ClientId = client.ClientId.ToString(),
                        ClientCode = client.ClientCode.ToUpper(),
                        ClientName = client.ClientName,
                        SportId = r.SportId,
                        SportName = league?.SportName ?? "-",
                        SegmentId = r.SegmentId,
                        SegmentName = league?.SegmentName ?? "-",
                        LeagueId = r.LeagueId,
                        LeagueName = league?.LeagueName ?? $"League {r.LeagueId}",
                        IsRestricted = !r.IsActive
                    };
                })
                .ToList();

            if (!string.IsNullOrEmpty(payload?.SearchText))
            {
                var search = payload.SearchText.ToLower();
                assigned = assigned.Where(x => x.ClientName.ToLower().Contains(search) || x.SportName.ToLower().Contains(search) || x.SegmentName.ToLower().Contains(search) || x.LeagueName.ToLower().Contains(search)).ToList();
            }

            return assigned;
        }
        public async Task<BaseResponse<List<ConfigTemplateModel>>> DeleteConfigTemplate(string id, RequesterInfo requesterInfo)
        {
            try
            {
                var activity = new ActivityLogModel()
                {
                    Page = requesterInfo.Page,
                    To = "-",
                    Activity = Shared.Enums.Activity.Delete.ToString(),
                    UserId = requesterInfo.UserId,
                    UserName = requesterInfo.UserName,
                    TimeStamp = DateTime.UtcNow
                };

                if (!long.TryParse(id, out long configTemplateId))
                    return new BaseResponse<List<ConfigTemplateModel>>() { StatusCode = StatusCode.Error, Message = "Invalid ID", Result = null };

                var config = await _backofficeContext.ConfigTemplate.FindAsync(configTemplateId);

                activity.Payload = $"{config.SportId}.{config.FixtureStatusId}.{config.Name}.T".Replace(" ", "_");
                activity.From = JsonSerializer.Serialize(config);

                if (config != null)
                {
                    _backofficeContext.ConfigTemplate.Remove(config);

                    await _rabbitMqRepository.Publish(new MessageRequest()
                    {
                        CorrelationId = Guid.NewGuid().ToString(),
                        MessageType = "DeleteConfigTemplate",
                        RoutingKey = $"r.bo.config.deleteconfigtemplate",
                        Payload = JsonSerializer.Serialize(config)
                    });
                }

                await _backofficeContext.SaveChangesAsync();
                await AddActivityLog(activity);

                var configTemplate = await GetConfigTemplates("");

                return new BaseResponse<List<ConfigTemplateModel>>()
                {
                    StatusCode = configTemplate.StatusCode,
                    Message = configTemplate.Message,
                    Result = configTemplate.Result?.ConfigTemplates
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ConfigTemplateModel>>() { StatusCode = StatusCode.Error, Message = ex.Message, Result = null };
            }
        }
        public async Task<BaseResponse<ConfigTemplateLeagueResponse>> GetConfigState(List<string> clientCodes, ConfigTemplateLeagueRequest payload, ConfigState configState, int templateId = -1, LanguageCode langCode = LanguageCode.EN)
        {
            try
            {
                var clientConfigs = new List<ConfigClientModel>();
                var isCompleteClient = false;

                var clientList = (await _platformRepository.GetClients("HK")).Result;
                var allClientsCount = clientList.Count;
                var metaData = payload?.MetaData;

                if (clientCodes.Count() == 1)
                {
                    if (clientCodes.FirstOrDefault().ToUpper() != "ALL")
                    {
                        clientList = new List<PlatformClientResponse>();
                    }
                }
                else
                {
                    clientList = clientList.Where(x => clientCodes.Contains(x.ClientCode)).ToList();
                }

                if (payload != null)
                {
                    clientList = clientList.Where(x => payload.ClientCodes?.ToList()?.Contains(x.ClientCode) ?? false).ToList();
                }

                // BO-151: the Assigned and Unassigned leagues come from the saved league settings of the clients
                // (Trading.dbo.ClientLeagueConfig) and the sportsbook league lookup. Only the dialog that lists the leagues
                // of one template (templateId) still reads the old template assignments. Nothing else touches them: the
                // template tables do not exist on every environment.
                var templateAssignments = templateId != -1;

                IQueryable<ConfigTemplateAssigned> query = null;

                if (templateAssignments)
                {
                    query = _backofficeContext.ConfigTemplateAssigned.AsNoTracking().Where(x => clientList.Select(cl => cl.ClientCode).Contains(x.ClientCode.ToUpper()));

                    query = query.Where(x => x.PrematchConfigTemplateId == templateId || x.LiveConfigTemplateId == templateId);

                    if (payload != null && payload.SportId != -1)
                    {
                        query = query.Where(x => x.SportId == payload.SportId);
                    }

                    if (payload != null && !string.IsNullOrEmpty(payload.SearchText))
                    {
                        query = query.Where(x => x.ClientName.ToLower().Contains(payload.SearchText.ToLower()) || x.SegmentName.ToLower().Contains(payload.SearchText.ToLower()) || x.LeagueName.ToLower().Contains(payload.SearchText.ToLower()));
                    }
                }

                if (templateAssignments && query != null && query.Any())
                {
                    isCompleteClient = clientList.Count() == allClientsCount;
                }

                var sportsTranslationLookup = templateAssignments ? GetSportsLookup(langCode) : null;

                List<LeagueLookupResponseV2.LeagueLookupModelV2> leagueLookup = null;
                List<ClientLeagueConfig> savedRows = null;

                if (!templateAssignments)
                {
                    var leagueLookupResponse = await _tradingRepository.GetAllLeagueLookup(payload == null || payload.SportId == -1 ? "" : payload.SportId.ToString());
                    if (leagueLookupResponse.StatusCode != StatusCode.Success || leagueLookupResponse.Result == null)
                    {
                        return new BaseResponse<ConfigTemplateLeagueResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = leagueLookupResponse.Message ?? "League lookup failed",
                            Result = null
                        };
                    }

                    leagueLookup = leagueLookupResponse.Result.LeagueLookup;

                    // BO-144: a league is assigned to a client when the client has a saved setting for it (restricted or
                    // allowed). A league without a saved setting is unassigned and visible by default.
                    var savedRowsResponse = await _tradingRepository.GetClientLeagueConfigRows(clientList.Select(c => c.ClientCode).ToList());
                    if (savedRowsResponse.StatusCode != StatusCode.Success || savedRowsResponse.Result == null)
                    {
                        return new BaseResponse<ConfigTemplateLeagueResponse>
                        {
                            StatusCode = StatusCode.Error,
                            Message = savedRowsResponse.Message ?? "League settings could not be read",
                            Result = null
                        };
                    }

                    savedRows = savedRowsResponse.Result;
                }

                switch (configState)
                {
                    case ConfigState.Assigned:
                        if (!templateAssignments)
                        {
                            clientConfigs = BuildAssignedLeagues(savedRows, leagueLookup, clientList, payload);
                            break;
                        }

                        clientConfigs = query.Select(x => new ConfigClientModel()
                        {
                            Id = x.Id,
                            ClientId = x.ClientId.ToString(),
                            ClientCode = x.ClientCode,
                            ClientName = x.ClientName,
                            SportId = x.SportId,
                            SportName = langCode == LanguageCode.EN ? x.SportName : sportsTranslationLookup.FirstOrDefault(c => c.SportId == x.SportId).SportName,
                            SegmentId = x.SegmentId,
                            SegmentName = x.SegmentName,
                            LeagueId = x.LeagueId,
                            LeagueName = x.LeagueName,
                            PrematchTemplateId = x.PrematchConfigTemplateId,
                            LiveTemplateId = x.LiveConfigTemplateId,
                            PrematchTemplateName = _backofficeContext.ConfigTemplate.Where(cl => cl.Id == x.PrematchConfigTemplateId).Select(x => x.Name).FirstOrDefault(),
                            LiveTemplateName = _backofficeContext.ConfigTemplate.Where(cl => cl.Id == x.LiveConfigTemplateId).Select(x => x.Name).FirstOrDefault()
                        }).ToList();
                        break;
                    case ConfigState.UnAssigned:
                        var savedLeagueKeys = savedRows
                            .GroupBy(r => r.ClientCode ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.Select(r => (r.SportId, r.SegmentId, r.LeagueId)).ToHashSet(), StringComparer.OrdinalIgnoreCase);

                        foreach (var client in clientList)
                        {
                            savedLeagueKeys.TryGetValue(client.ClientCode, out var savedForClient);
                            var leagues = leagueLookup.Where(x => savedForClient == null || !savedForClient.Contains((x.SportId, x.SegmentId, x.LeagueId))).ToList();

                            if (leagues != null && leagues.Any())
                            {
                                clientConfigs.AddRange(leagues.Select(x => new ConfigClientModel()
                                {
                                    Id = -1,
                                    ClientId = client.ClientId.ToString(),
                                    ClientCode = client.ClientCode.ToUpper(),
                                    ClientName = client.ClientName,
                                    SportId = x.SportId,
                                    SportName = x.SportName,
                                    SegmentId = x.SegmentId,
                                    SegmentName = x.SegmentName,
                                    LeagueId = x.LeagueId,
                                    LeagueName = x.LeagueName
                                }));
                            }
                        }

                        if (!string.IsNullOrEmpty(payload.SearchText))
                        {
                            clientConfigs = clientConfigs.Where(x => x.ClientName.ToLower().Contains(payload.SearchText.ToLower()) || x.LeagueName.ToLower().Contains(payload.SearchText.ToLower())).ToList();
                        }
                        break;

                }

                if (!templateAssignments)
                    isCompleteClient = clientConfigs.Any() && clientList.Count() == allClientsCount;

                if (clientConfigs != null && clientConfigs.Any())
                {
                    clientConfigs = clientConfigs
                        .OrderBy(x => x.ClientCode)
                        .ThenBy(x => x.SportName)
                        .ThenBy(x => x.LeagueName)
                        .ToList();
                }

                if (metaData != null)
                {
                    metaData.TotalCount = clientConfigs.Count();
                    metaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(metaData.TotalCount) / Convert.ToDecimal(metaData.PageSize)));

                    var skip = (metaData.CurrentPage - 1) * metaData.PageSize;
                    metaData.PageSize = metaData.PageSize == -1 ? metaData.TotalCount : metaData.PageSize;
                    if (metaData.CurrentPage > 0)
                        clientConfigs = clientConfigs.Skip(skip).Take(metaData.PageSize).ToList();
                    else
                        clientConfigs = clientConfigs.Skip(0).Take(metaData.PageSize).ToList();
                }

                return new BaseResponse<ConfigTemplateLeagueResponse>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ConfigTemplateLeagueResponse()
                    {
                        IsComplete = isCompleteClient,
                        ClientConfigs = clientConfigs,
                        MetaData = metaData
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ConfigTemplateLeagueResponse>() { StatusCode = StatusCode.Error, Message = ex.Message, Result = null };
            }
        }
        public async Task<BaseResponse<int>> GetTemplateConfigCount(List<string> clientCodes, string state)
        {
            try
            {
                var count = 0;
                if (state.ToUpper() == ConfigState.Assigned.ToString().ToUpper())
                {
                    var assigned = await GetAssignedCount(clientCodes);
                    if (assigned.StatusCode != StatusCode.Success)
                        return assigned;

                    count = assigned.Result;
                }
                else if (state.ToUpper() == ConfigState.UnAssigned.ToString().ToUpper())
                {
                    var unAssigned = await GetUnAssignedCount(clientCodes);
                    if (unAssigned.StatusCode != StatusCode.Success)
                        return unAssigned;

                    count = unAssigned.Result;
                }
                else
                {
                    var configTemplate = await GetConfigTemplatesCount();
                    if (configTemplate.StatusCode != StatusCode.Success)
                        return configTemplate;

                    count = configTemplate.Result;
                }

                return new BaseResponse<int>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = count
                };
            }
            catch (Exception ex)
            {

                return new BaseResponse<int>()
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message,
                    Result = -1
                };
            }
        }
        public async Task<BaseResponse<ConfigTemplateMarginModel>> GetConfigTemplateMarginById(long templateId)
        {
            try
            {
                var query = _backofficeContext.ConfigTemplateMargin.AsNoTracking();

                var result = query.Where(e => e.ConfigTemplateId == templateId).Select(x => new Shared.Models.ConfigTemplateMargin()
                {
                    ConfigTemplateId = x.ConfigTemplateId,
                    MarketTemplateId = x.MarketTemplateId,
                    SportId = x.SportId,
                    FixtureTypeId = x.FixtureTypeId,
                    FixtureStatusId = x.FixtureStatusId,
                    IsActive = x.IsActive,
                    Config = x.Config,
                }).ToList();

                return new BaseResponse<ConfigTemplateMarginModel>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ConfigTemplateMarginModel()
                    {
                        ConfigTemplateMargins = result ?? new List<Shared.Models.ConfigTemplateMargin>()
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ConfigTemplateMarginModel>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString()}: {ex.Message}",
                    Result = null
                };
            }
        }
        public async Task<BaseResponse<bool>> AddOrUpdateConfigTemplateMargin(ConfigTemplateMarginModel request)
        {
            try
            {
                var configTemplateIds = request.ConfigTemplateMargins.Select(s => s.ConfigTemplateId).Distinct().ToList();

                foreach (var configTemplateId in configTemplateIds)
                {
                    var configMargins = request.ConfigTemplateMargins.Where(c => c.ConfigTemplateId == configTemplateId);

                    // Create a DataTable to hold the values
                    var dataTable = new DataTable();
                    dataTable.Columns.Add("ConfigTemplateId", typeof(int));
                    dataTable.Columns.Add("FixtureTypeId", typeof(int));
                    dataTable.Columns.Add("FixtureStatusId", typeof(int));
                    dataTable.Columns.Add("MarketTemplateId", typeof(int));
                    dataTable.Columns.Add("SportId", typeof(int));
                    dataTable.Columns.Add("Config", typeof(string));
                    dataTable.Columns.Add("IsActive", typeof(bool));

                    // Populate the DataTable
                    foreach (var config in configMargins)
                    {
                        dataTable.Rows.Add(config.ConfigTemplateId, config.FixtureTypeId, config.FixtureStatusId,
                                            config.MarketTemplateId, config.SportId, config.Config, config.IsActive);
                    }

                    var parameter = new SqlParameter
                    {
                        ParameterName = "@ConfigTemplateMargins",
                        SqlDbType = SqlDbType.Structured,
                        TypeName = "ConfigTemplateMarginType",
                        Value = dataTable
                    };

                    var configTemplateIdParam = new SqlParameter
                    {
                        ParameterName = "@ConfigTemplateId",
                        SqlDbType = SqlDbType.BigInt,
                        Value = configTemplateId
                    };

                    await _backofficeContext.Database.ExecuteSqlRawAsync("EXEC AddOrUpdateConfigTemplateMarginV2 @ConfigTemplateId, @ConfigTemplateMargins", configTemplateIdParam, parameter);

                    await _rabbitMqRepository.Publish(new MessageRequest()
                    {
                        CorrelationId = Guid.NewGuid().ToString(),
                        MessageType = "UpdateClientMargin",
                        RoutingKey = $"r.bo.config.updateclientmargin",
                        Payload = JsonSerializer.Serialize(configTemplateId)
                    });
                }

                return new BaseResponse<bool>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = true
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>() { StatusCode = StatusCode.Error, Message = ex.Message };
            }
        }

        public async Task<BaseResponse<CopyClientConfigResponse>> CopyClientConfig(CopyClientConfigRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FromClientCode) || string.IsNullOrEmpty(request.ToClientCode))
                    return new BaseResponse<CopyClientConfigResponse>()
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString(),
                        Result = new CopyClientConfigResponse
                        {
                            Result = false,
                        }
                    };

                var toBeRemovedConfigTemplate = new List<ConfigTemplateAssigned>();

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                {
                    using (var context = _backofficeContext)
                    {
                        //check if there are existing config template assigned to ToClientCode that needs to be removed in OE.Adapter cache
                        var fromClientConfigTemplateAssigned = context.ConfigTemplateAssigned.Where(c => c.ClientCode == request.FromClientCode).ToList();
                        var toClientConfigTemplateAssigned = context.ConfigTemplateAssigned.Where(c => c.ClientCode == request.ToClientCode).ToList();

                        if (toClientConfigTemplateAssigned.Any())
                        {
                            foreach (var config in toClientConfigTemplateAssigned)
                            {
                                var fromConfig = fromClientConfigTemplateAssigned.FirstOrDefault(c => c.SportId == config.SportId && c.SegmentId == config.SegmentId && c.LeagueId == config.LeagueId);
                                if (fromConfig == null)
                                    toBeRemovedConfigTemplate.Add(config);
                            }

                            context.ConfigTemplateAssigned.RemoveRange(toClientConfigTemplateAssigned);
                        }

                        //assign all config templates assigned from FromClientCode into ToClientCode
                        foreach (var config in fromClientConfigTemplateAssigned)
                        {
                            context.ConfigTemplateAssigned.Add(new ConfigTemplateAssigned()
                            {
                                ClientId = request.ToClientId,
                                ClientCode = request.ToClientCode,
                                ClientName = request.ToClientName,
                                SportId = config.SportId,
                                SportName = config.SportName,
                                SegmentId = config.SegmentId,
                                SegmentName = config.SegmentName,
                                LeagueId = config.LeagueId,
                                LeagueName = config.LeagueName,
                                PrematchConfigTemplateId = config.PrematchConfigTemplateId,
                                LiveConfigTemplateId = config.LiveConfigTemplateId
                            });
                        }
                        context.SaveChanges();

                        scope.Complete();
                    }
                }

                return new BaseResponse<CopyClientConfigResponse>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new CopyClientConfigResponse
                    {
                        Result = true,
                        ToBeRemoved = toBeRemovedConfigTemplate
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<CopyClientConfigResponse>() { StatusCode = StatusCode.Error, Message = ex.Message };
            }
        }


        #endregion

        #region Methods - Top Fixtures

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

                    if (region == "HK")
                        hkClientIds.Add(clientId);
                    else if (region == "SA")
                        saClientIds.Add(clientId);
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

        #region Utilities
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
        private List<ClientInformation> GetClientInformation()
        {
            return _backofficeContext.ClientInformation.AsNoTracking().ToList() ?? new List<ClientInformation>();
        }
        //private List<ClientInformationModel> GetTradingClientInformation()
        //{
        //    return _tradingRepository.GetClientList("").Result?.Result ?? new List<ClientInformationModel>();
        //}
        private long InsertClientRecord(ClientInformation client)
        {
            _backofficeContext.ClientInformation.Add(client);
            _backofficeContext.SaveChanges();
            //_memCache.AddOrUpdateCache<List<ClientInformation>>("boci", _backofficeContext.ClientInformation.AsNoTracking().ToList(), 24);

            return client.ClientId;
        }
        private long UpdateClientRecord(ClientInformation client)
        {
            _backofficeContext.ClientInformation.Update(client);
            _backofficeContext.SaveChanges();
            //_memCache.AddOrUpdateCache<List<ClientInformation>>("boci", _backofficeContext.ClientInformation.AsNoTracking().ToList(), 24);

            return client.ClientId;
        }
        private async Task<decimal> GetSuggestedMinimumBet(string currencyCode)
        {
            var currency = (Currency)Enum.Parse(typeof(Currency), currencyCode);

            var suggestedMinimumBet = 0.02m;

            if (currency == Currency.BTC || currency == Currency.ETH)
            {
                suggestedMinimumBet = 0.000000000001m;
            }

            return Truncate(currencyCode, suggestedMinimumBet);
        }

        private decimal Truncate(string currencyCode, decimal value)
        {

            int places = 2;
            var formatter = "N2";

            if (cryptoCurrencies.Contains(currencyCode))
            {
                places = 12;
                formatter = "N12";

                var result = Truncate(value, places, formatter).TrimEnd('0').TrimEnd('.');
                return Convert.ToDecimal(result.Split(".").LastOrDefault()?.Length <= 1 ? Truncate(value, 2, "N2") : result);
            }

            return Convert.ToDecimal(Truncate(value, places, formatter));
        }

        private string Truncate(decimal value, int places = 2, string formatter = "")
        {
            try
            {
                return (Math.Truncate(value * (decimal)Math.Pow(10, places)) / (decimal)Math.Pow(10, places)).ToString(formatter, CultureInfo.InvariantCulture);
            }
            catch
            {
                return "0";
            }
        }

        private List<SportLookupModel> GetSportsLookup(LanguageCode langCode)
        {
            if (langCode == LanguageCode.EN)
                return null;

            var cache = _memCache.GetCache($"sp-tr-{langCode.ToString().ToUpper()}", async () => await _tradingRepository.GetSportLookup(langCode.ToString().ToUpper()), 1);

            return cache.Result?.Result?.SportLookup ?? null;
        }

        // Shared by every "View Logs" modal. Null-safe so a single row with a null From/To/UserName
        // cannot throw into GetActivityLogs' catch-all, which the modals render as "No Record Found".
        // PayloadSearchTerms (BO-104) lets a caller match values that only exist in Payload, e.g. the
        // client GUID behind a client name typed into the Client Site Config logs search.
        public static List<ActivityLogModel> FilterActivityLogsBySearch(List<ActivityLogModel> activityLogs, ActivityLogRequest request)
        {
            if (string.IsNullOrEmpty(request.SearchText))
                return activityLogs;

            var payloadTerms = (request.PayloadSearchTerms ?? new List<string>())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            bool Matches(string value, string term) =>
                value != null && value.Contains(term, StringComparison.OrdinalIgnoreCase);

            return activityLogs.Where(x =>
                    Matches(x.From, request.SearchText) ||
                    Matches(x.To, request.SearchText) ||
                    Matches(x.UserName, request.SearchText) ||
                    payloadTerms.Any(t => Matches(x.Payload, t)))
                .ToList();
        }

        private static ManualBetDetails TryParseManualBetDetails(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<ManualBetDetails>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private List<ActivityLogModel> FilterManualSettlementLogs(List<ActivityLogModel> activityLogs, ManualBetSettlementCallbackModel callbackModel)
        {
            string[] clientIds = null;
            string[] sportIds = null;
            string[] currencyCodes = null;
            string[] fromStatusesIds = null;
            string[] toStatusesIds = null;

            if (!string.IsNullOrEmpty(callbackModel.MemberName)) //
            {
                activityLogs = activityLogs
                .Where(x => x.To.Contains($"\"UserId\":\"{callbackModel.MemberName}\"", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(callbackModel.SettledBy))
            {
                activityLogs = activityLogs
                .Where(x => x.UserName.ToLower() == callbackModel.SettledBy.ToLower()).ToList();
            }

            if (!string.IsNullOrEmpty(callbackModel.BetId))
            {
                activityLogs = activityLogs
                .Where(x => x.To.Contains($"BetId\":\"{callbackModel.BetId}\",", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (callbackModel.Clients != null && callbackModel.Clients.Any())
            {
                clientIds = string.Join(",", callbackModel.Clients.Select(client => client.ClientId)).Split(',');

                activityLogs = activityLogs
                .Where(x => clientIds.Contains(TryParseManualBetDetails(x.To)?.ClientIdRef.ToString())).ToList();
            }

            if (callbackModel.Sports != null && callbackModel.Sports.Any())
            {
                sportIds = string.Join(",", callbackModel.Sports.Select(sport => sport.SportId)).Split(',');

                activityLogs = activityLogs
                .Where(x => sportIds.Contains(TryParseManualBetDetails(x.To)?.Selections?.FirstOrDefault()?.SportId.ToString())).ToList();
            }

            if (callbackModel.Currencies != null && callbackModel.Currencies.Any())
            {
                currencyCodes = string.Join(",", callbackModel.Currencies.Select(currency => currency.CurrencyCode)).Split(',');

                activityLogs = activityLogs
                .Where(x => currencyCodes.Contains(TryParseManualBetDetails(x.To)?.CurrencyCode)).ToList();
            }

            if (callbackModel.FromBetStatuses.Any())
            {
                fromStatusesIds = string.Join(",", callbackModel.FromBetStatuses.Select(status => status.BetStatusId)).Split(',');

                activityLogs = activityLogs
                .Where(x => fromStatusesIds.Contains(TryParseManualBetDetails(x.To)?.BetStatusId.ToString())).ToList();
            }

            if (callbackModel.ToBetStatuses != null && callbackModel.ToBetStatuses.Any())
            {
                toStatusesIds = string.Join(",", callbackModel.ToBetStatuses.Select(status => status.BetStatusId)).Split(',');

                activityLogs = activityLogs
                .Where(x => toStatusesIds.Contains(TryParseManualBetDetails(x.To)?.NewBetStatusId.ToString())).ToList();
            }

            return activityLogs;
        }
        private List<BackofficeStatic> GetTranslations()
        {
            //return _memCache.GetCache("tr", () => _translationContext.BackofficeStatic.AsNoTracking().ToList(), 24);
            return _translationContext.BackofficeStatic.AsNoTracking().ToList();
        }
        // BO-144: unassigned = every league, for every client the user can see, minus the leagues the client has a saved
        // setting for (restricted or allowed). A saved setting for a league that is no longer in the lookup takes nothing
        // away from the count.
        private async Task<BaseResponse<int>> GetUnAssignedCount(List<string> clientCodes)
        {
            var leagueLookupResponse = await _tradingRepository.GetAllLeagueLookup("");

            if (leagueLookupResponse.StatusCode != StatusCode.Success || leagueLookupResponse.Result?.LeagueLookup == null)
                return new BaseResponse<int> { StatusCode = StatusCode.Error, Message = leagueLookupResponse.Message ?? "League lookup failed", Result = -1 };

            var clientListResponse = await GetClientsOfUser(clientCodes);

            if (clientListResponse.StatusCode != StatusCode.Success)
                return new BaseResponse<int> { StatusCode = StatusCode.Error, Message = clientListResponse.Message, Result = -1 };

            var clients = clientListResponse.Result;

            var savedRowsResponse = await _tradingRepository.GetClientLeagueConfigRows(clients.Select(c => c.ClientCode).ToList());

            if (savedRowsResponse.StatusCode != StatusCode.Success || savedRowsResponse.Result == null)
                return new BaseResponse<int> { StatusCode = StatusCode.Error, Message = savedRowsResponse.Message ?? "League settings could not be read", Result = -1 };

            var leagueKeys = leagueLookupResponse.Result.LeagueLookup.Select(l => (l.SportId, l.SegmentId, l.LeagueId)).ToHashSet();

            var savedCount = savedRowsResponse.Result
                .Where(r => leagueKeys.Contains((r.SportId, r.SegmentId, r.LeagueId)))
                .Select(r => ((r.ClientCode ?? string.Empty).ToUpperInvariant(), r.SportId, r.SegmentId, r.LeagueId))
                .Distinct()
                .Count();

            return new BaseResponse<int>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = (leagueKeys.Count * clients.Count) - savedCount
            };
        }

        // BO-151: assigned = the leagues the clients the user can see have a saved setting for (restricted or allowed).
        private async Task<BaseResponse<int>> GetAssignedCount(List<string> clientCodes)
        {
            var clientListResponse = await GetClientsOfUser(clientCodes);

            if (clientListResponse.StatusCode != StatusCode.Success)
                return new BaseResponse<int> { StatusCode = StatusCode.Error, Message = clientListResponse.Message, Result = -1 };

            var savedRowsResponse = await _tradingRepository.GetClientLeagueConfigRows(clientListResponse.Result.Select(c => c.ClientCode).ToList());

            if (savedRowsResponse.StatusCode != StatusCode.Success || savedRowsResponse.Result == null)
                return new BaseResponse<int> { StatusCode = StatusCode.Error, Message = savedRowsResponse.Message ?? "League settings could not be read", Result = -1 };

            return new BaseResponse<int>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = savedRowsResponse.Result.Select(ConfigRowKey).Distinct().Count()
            };
        }

        // The Platform clients the user can see: every client for ALL, else the clients of the user's client codes.
        private async Task<BaseResponse<List<PlatformClientResponse>>> GetClientsOfUser(List<string> clientCodes)
        {
            var clientListResponse = await _platformRepository.GetClients("HK");

            if (clientListResponse.StatusCode != StatusCode.Success || clientListResponse.Result == null)
                return new BaseResponse<List<PlatformClientResponse>> { StatusCode = StatusCode.Error, Message = clientListResponse.Message ?? "Client list failed" };

            var clients = clientListResponse.Result;

            if ((clientCodes.Count() == 1 && clientCodes.FirstOrDefault().ToUpper() == "ALL") == false)
                clients = clients.Where(c => clientCodes.Contains(c.ClientCode, StringComparer.OrdinalIgnoreCase)).ToList();

            return new BaseResponse<List<PlatformClientResponse>> { StatusCode = StatusCode.Success, Message = StatusCode.Success.ToString(), Result = clients };
        }

        private Event GetEvent(string action, DateTime timestampStarted, DateTime timestampFinished)
        {
            var model = new Event
            {
                Action = action,
                TimestampStarted = timestampStarted,
                TimestampFinished = timestampFinished,
                ProcessingTime = timestampFinished.ToEpochTimestamp() - timestampStarted.ToEpochTimestamp()
            };

            return model;
        }

        #endregion

        #region Translation
        public async Task<BaseResponse<bool>> CachePreload(OnDeleteCacheMessage request)
        {

            await _rabbitMqRepository.Publish(new MessageRequest()
            {
                CorrelationId = Guid.NewGuid().ToString(),
                MessageType = GetMessageType(request.CacheType),
                RoutingKey = $"r.oe.cachesync.{GetMessageType(request.CacheType).ToLower()}",
                Payload = JsonSerializer.Serialize(request)
            });

            return new BaseResponse<bool>()
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = true
            };
        }

        public async Task<BaseResponse<TranslationResponse>> GetTranslationsByLanguageCode(LanguageCode langCode)
        {
            var languageCode = langCode.ToString().ToUpper();
            var key = $"tr-{languageCode}";

            var cache = _memCache.GetCache<TranslationResponse>(key);

            if (cache != null)
                return new BaseResponse<TranslationResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = cache
                };

            var translations = GetTranslations();
            var englishTranslations = translations.Where(c => c.LanguageCode == "EN").ToList();
            var reqTranslations = translations.Where(c => c.LanguageCode == languageCode).ToList();


            var combinedResult = englishTranslations.Concat(reqTranslations)
                .GroupBy(g => g.Name)
                .Select(s => new
                {
                    Name = s.Key,
                    s.OrderByDescending(x => reqTranslations.Contains(x)).First().Value
                })
                .ToDictionary(s => s.Name, x => x.Value);

            var result = new TranslationResponse()
            {
                LanguageCode = languageCode,
                Translations = combinedResult
            };

            _memCache.AddOrUpdateCache(key, result, 24);

            return new BaseResponse<TranslationResponse>()
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = result
            };
        }

        public async Task<BaseResponse<List<SportsbookTranslationResponse>>> GetSportsbookTranslations(string languageCode)
        {
            try
            {
                // A key can be missing a row for this language entirely (never created), not just
                // have a blank Value -- return every known key (from any language) so the UI can
                // show/filter those gaps too, not only rows that already exist for this language.
                var allNames = await _translationContext.SportsbookStatic
                    .AsNoTracking()
                    .Select(t => t.Name)
                    .Distinct()
                    .ToListAsync();

                var languageRows = await _translationContext.SportsbookStatic
                    .AsNoTracking()
                    .Where(t => t.LanguageCode == languageCode)
                    .ToListAsync();
                var languageValues = languageRows.ToDictionary(t => t.Name, t => t.Value);

                var translations = allNames
                    .Select(name => new SportsbookTranslationResponse
                    {
                        LanguageCode = languageCode,
                        Name = name,
                        Value = languageValues.TryGetValue(name, out var value) ? value : string.Empty
                    })
                    .ToList();

                return new BaseResponse<List<SportsbookTranslationResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = translations
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<SportsbookTranslationResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message
                };
            }
        }

        public async Task<BaseResponse<TranslationStatsResponse>> GetSportsbookTranslationStats()
        {
            try
            {
                var allRows = await _translationContext.SportsbookStatic
                    .AsNoTracking()
                    .Select(t => new { t.LanguageCode, t.Name, t.Value })
                    .ToListAsync();

                var totalKeys = allRows.Select(t => t.Name).Distinct().Count();

                var languages = allRows
                    .GroupBy(t => t.LanguageCode)
                    .Select(g => new LanguageTranslationStat
                    {
                        LanguageCode = g.Key,
                        TotalCount = totalKeys,
                        TranslatedCount = g.Count(t => !string.IsNullOrEmpty(t.Value))
                    })
                    .OrderBy(s => s.LanguageCode)
                    .ToList();

                return new BaseResponse<TranslationStatsResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TranslationStatsResponse { Languages = languages }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<TranslationStatsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message
                };
            }
        }

        public async Task<BaseResponse<bool>> AddOrUpdateSportsbookTranslation(AddOrUpdateSportsbookTranslationRequest request, RequesterInfo requesterInfo)
        {
            try
            {
                var existing = await _translationContext.SportsbookStatic
                    .FirstOrDefaultAsync(t => t.LanguageCode == request.LanguageCode && t.Name == request.Name);

                var activity = new ActivityLogModel
                {
                    Page = requesterInfo.Page,
                    UserId = requesterInfo.UserId,
                    UserName = requesterInfo.UserName,
                    Payload = $"{request.LanguageCode}.{request.Name}",
                    TimeStamp = DateTime.UtcNow
                };

                if (existing != null)
                {
                    activity.Activity = Activity.Update.ToString();
                    activity.From = existing.Value;
                    activity.To = request.Value;

                    existing.Value = request.Value;
                    _translationContext.SportsbookStatic.Update(existing);
                }
                else
                {
                    activity.Activity = Activity.Create.ToString();
                    activity.From = "-";
                    activity.To = request.Value;

                    _translationContext.SportsbookStatic.Add(new SportsbookStatic
                    {
                        LanguageCode = request.LanguageCode,
                        Name = request.Name,
                        Value = request.Value
                    });
                }

                await _translationContext.SaveChangesAsync();
                await AddActivityLog(activity);
                await PublishStaticTranslationChange(request.LanguageCode);

                return new BaseResponse<bool>
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
                    Message = ex.Message,
                    Result = false
                };
            }
        }

        public async Task<BaseResponse<bool>> DeleteSportsbookTranslation(DeleteSportsbookTranslationRequest request, RequesterInfo requesterInfo)
        {
            try
            {
                var existing = await _translationContext.SportsbookStatic
                    .FirstOrDefaultAsync(t => t.LanguageCode == request.LanguageCode && t.Name == request.Name);

                if (existing == null)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "Translation not found",
                        Result = false
                    };
                }

                _translationContext.SportsbookStatic.Remove(existing);
                await _translationContext.SaveChangesAsync();

                await AddActivityLog(new ActivityLogModel
                {
                    Page = requesterInfo.Page,
                    UserId = requesterInfo.UserId,
                    UserName = requesterInfo.UserName,
                    Activity = Activity.Delete.ToString(),
                    Payload = $"{request.LanguageCode}.{request.Name}",
                    From = existing.Value,
                    To = "-",
                    TimeStamp = DateTime.UtcNow
                });

                await PublishStaticTranslationChange(request.LanguageCode);

                return new BaseResponse<bool>
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
                    Message = ex.Message,
                    Result = false
                };
            }
        }

        // Removes a static translation key across ALL languages in one call. The per-language
        // DeleteSportsbookTranslation above stays for backward compatibility, but since the UI
        // no longer offers an EN view, this is the only way to fully retire a key (previously
        // that took one delete per language through the dropdown).
        public async Task<BaseResponse<bool>> DeleteSportsbookTranslationKey(DeleteSportsbookTranslationKeyRequest request, RequesterInfo requesterInfo)
        {
            try
            {
                var rows = await _translationContext.SportsbookStatic
                    .Where(t => t.Name == request.Name)
                    .ToListAsync();

                if (rows.Count == 0)
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "Translation key not found",
                        Result = false
                    };
                }

                _translationContext.SportsbookStatic.RemoveRange(rows);
                await _translationContext.SaveChangesAsync();

                await AddActivityLog(new ActivityLogModel
                {
                    Page = requesterInfo.Page,
                    UserId = requesterInfo.UserId,
                    UserName = requesterInfo.UserName,
                    Activity = Activity.Delete.ToString(),
                    Payload = request.Name,
                    From = string.Join(", ", rows.Select(r => $"{r.LanguageCode}={r.Value}")),
                    To = "-",
                    TimeStamp = DateTime.UtcNow
                });

                foreach (var languageCode in rows.Select(r => r.LanguageCode).Distinct())
                {
                    await PublishStaticTranslationChange(languageCode);
                }

                return new BaseResponse<bool>
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
                    Message = ex.Message,
                    Result = false
                };
            }
        }

        // Returns every static translation key as one row carrying the EN source text and each
        // supported target-language value -- Backoffice.Web builds the downloadable CSV from
        // this, mirroring the Dynamic Translation glossary export. A key missing a row for a
        // given language comes back with an empty string for that column.
        public async Task<BaseResponse<List<SportsbookTranslationExportRow>>> ExportSportsbookTranslations()
        {
            try
            {
                var allRows = await _translationContext.SportsbookStatic
                    .AsNoTracking()
                    .ToListAsync();

                var byLanguage = allRows
                    .GroupBy(r => r.LanguageCode ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.GroupBy(r => r.Name).ToDictionary(k => k.Key, k => k.First().Value ?? string.Empty),
                        StringComparer.OrdinalIgnoreCase);

                string ValueFor(string language, string name) =>
                    byLanguage.TryGetValue(language, out var map) && map.TryGetValue(name, out var value) ? value : string.Empty;

                var rows = allRows
                    .Select(r => r.Name)
                    .Distinct()
                    .OrderBy(name => name)
                    .Select(name => new SportsbookTranslationExportRow
                    {
                        Key = name,
                        SourceText = ValueFor("EN", name),
                        TargetTextZh = ValueFor("ZH", name),
                        TargetTextJa = ValueFor("JA", name),
                        TargetTextBr = ValueFor("BR", name)
                    })
                    .ToList();

                return new BaseResponse<List<SportsbookTranslationExportRow>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = rows
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<SportsbookTranslationExportRow>>
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message
                };
            }
        }

        // Upserts static translation rows parsed from an uploaded export/template file (parsing
        // happens in Backoffice.Web; this only ever sees structured rows). Mirrors the Dynamic
        // Translation glossary import: idempotent by design -- a target-language cell is skipped
        // (no write, no activity log, no publish) when it's blank (leave the existing translation
        // untouched), equal to the EN source text (treated as "not yet translated"), or equal to
        // the value already stored. The EN column only ever creates a brand-new key; it never
        // rewrites an existing key's source text.
        public async Task<BaseResponse<ImportSportsbookTranslationResponse>> ImportSportsbookTranslations(ImportSportsbookTranslationRequest request, RequesterInfo requesterInfo)
        {
            try
            {
                if (request?.Rows == null || request.Rows.Count == 0)
                {
                    return new BaseResponse<ImportSportsbookTranslationResponse> { StatusCode = StatusCode.Error, Message = "The uploaded file has no data rows." };
                }

                var allRows = await _translationContext.SportsbookStatic.ToListAsync();
                var rowsByKey = allRows
                    .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                var result = new ImportSportsbookTranslationResponse();
                var activityLogs = new List<ActivityLogModel>();
                var changedLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var targetLanguages = new[] { "ZH", "JA", "BR" };

                for (var i = 0; i < request.Rows.Count; i++)
                {
                    var row = request.Rows[i];
                    var rowNumber = i + 2; // +1 for 1-based, +1 because row 1 is the header in the source file
                    var key = row.Key?.Trim();

                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    if (!seenKeys.Add(key))
                    {
                        result.SkippedCount++;
                        result.RowMessages.Add($"Row {rowNumber}: duplicate Key '{key}' already processed earlier in this file — skipped.");
                        continue;
                    }

                    var uploadedSourceText = row.SourceText?.Trim() ?? string.Empty;
                    rowsByKey.TryGetValue(key, out var existingRows);
                    existingRows ??= new List<SportsbookStatic>();

                    var storedSource = existingRows.FirstOrDefault(r => string.Equals(r.LanguageCode, "EN", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
                    var keyExists = existingRows.Count > 0;
                    var sourceText = keyExists ? storedSource : uploadedSourceText;

                    if (!keyExists)
                    {
                        if (string.IsNullOrWhiteSpace(uploadedSourceText))
                        {
                            result.SkippedCount++;
                            result.RowMessages.Add($"Row {rowNumber}: EN is required to create new Key '{key}' — skipped.");
                            continue;
                        }

                        var enRow = new SportsbookStatic { LanguageCode = "EN", Name = key, Value = uploadedSourceText };
                        _translationContext.SportsbookStatic.Add(enRow);
                        existingRows.Add(enRow);
                        rowsByKey[key] = existingRows;
                        changedLanguages.Add("EN");
                        activityLogs.Add(BuildStaticTranslationActivityLog(requesterInfo, Activity.Create, $"EN.{key}", "-", uploadedSourceText));
                    }

                    var targetValues = new[] { row.TargetTextZh, row.TargetTextJa, row.TargetTextBr };
                    var rowChanged = false;

                    for (var t = 0; t < targetLanguages.Length; t++)
                    {
                        var language = targetLanguages[t];
                        var uploadedRaw = targetValues[t];
                        if (string.IsNullOrWhiteSpace(uploadedRaw))
                        {
                            continue; // blank cell => leave the existing translation untouched
                        }

                        var uploaded = uploadedRaw.Trim();
                        if (string.Equals(uploaded, sourceText?.Trim(), StringComparison.Ordinal))
                        {
                            continue; // matches source text => treat as not-yet-translated, skip
                        }

                        var existing = existingRows.FirstOrDefault(r => string.Equals(r.LanguageCode, language, StringComparison.OrdinalIgnoreCase));
                        if (existing != null && string.Equals(uploaded, existing.Value?.Trim(), StringComparison.Ordinal))
                        {
                            continue; // already the stored value => idempotent no-op
                        }

                        var from = existing?.Value ?? "-";
                        if (existing != null)
                        {
                            existing.Value = uploaded;
                            _translationContext.SportsbookStatic.Update(existing);
                        }
                        else
                        {
                            var newRow = new SportsbookStatic { LanguageCode = language, Name = key, Value = uploaded };
                            _translationContext.SportsbookStatic.Add(newRow);
                            existingRows.Add(newRow);
                        }

                        activityLogs.Add(BuildStaticTranslationActivityLog(requesterInfo, existing != null ? Activity.Update : Activity.Create, $"{language}.{key}", from, uploaded));
                        changedLanguages.Add(language);
                        rowChanged = true;
                    }

                    if (!keyExists)
                    {
                        result.CreatedCount++;
                    }
                    else if (rowChanged)
                    {
                        result.UpdatedCount++;
                    }
                    else
                    {
                        result.UnchangedCount++;
                    }
                }

                if (changedLanguages.Count > 0)
                {
                    await _translationContext.SaveChangesAsync();
                }

                if (activityLogs.Count > 0)
                {
                    await AddActivityLogList(new ActivityLogListRequest { ActivityLogs = activityLogs });
                }

                foreach (var language in changedLanguages)
                {
                    await PublishStaticTranslationChange(language);
                }

                return new BaseResponse<ImportSportsbookTranslationResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ImportSportsbookTranslationResponse> { StatusCode = StatusCode.Error, Message = ex.Message };
            }
        }

        private static ActivityLogModel BuildStaticTranslationActivityLog(RequesterInfo requesterInfo, Activity activity, string payload, string from, string to) => new ActivityLogModel
        {
            Page = requesterInfo.Page,
            UserId = requesterInfo.UserId,
            UserName = requesterInfo.UserName,
            Activity = activity.ToString(),
            Payload = payload,
            From = from,
            To = to,
            TimeStamp = DateTime.UtcNow
        };

        public async Task<BaseResponse<SportsbookGlossaryListResponse>> GetSportsbookGlossary(GetSportsbookGlossaryRequest request)
        {
            try
            {
                var metaData = request.MetaData ?? new MetaData();
                var targetLanguage = string.IsNullOrWhiteSpace(request.TargetLanguage) ? "EN" : request.TargetLanguage.ToUpper();

                var query = _translationContext.TranslationCache2.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SearchText))
                {
                    query = request.ExactMatch
                        ? query.Where(g => g.StringId == request.SearchText || g.SourceText == request.SearchText)
                        : query.Where(g => g.StringId.Contains(request.SearchText) || g.SourceText.Contains(request.SearchText));
                }

                if (request.UntranslatedOnly && targetLanguage != "EN")
                {
                    query = ApplyUntranslatedFilter(query, targetLanguage);
                }

                metaData.TotalCount = await query.CountAsync();
                metaData.TotalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(metaData.TotalCount) / Convert.ToDecimal(metaData.PageSize)));

                var skip = (metaData.CurrentPage - 1) * metaData.PageSize;

                var page = await query
                    .OrderBy(g => g.StringId)
                    .Skip(skip)
                    .Take(metaData.PageSize)
                    .ToListAsync();

                var glossary = page.Select(g => new SportsbookGlossaryResponse
                {
                    Id = g.Id,
                    StringId = g.StringId,
                    SourceLanguage = g.SourceLanguage,
                    SourceText = g.SourceText,
                    HasBetPlacement = g.HasBetPlacement,
                    Processed = g.Processed,
                    UpdatedAt = g.UpdatedAt,
                    TargetLanguage = targetLanguage,
                    TargetText = targetLanguage == "EN" ? g.SourceText : GetTargetText(g, targetLanguage)
                }).ToList();

                return new BaseResponse<SportsbookGlossaryListResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new SportsbookGlossaryListResponse { Glossary = glossary, MetaData = metaData }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<SportsbookGlossaryListResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message
                };
            }
        }

        public async Task<BaseResponse<TranslationStatsResponse>> GetSportsbookGlossaryStats()
        {
            try
            {
                var totalCount = await _translationContext.TranslationCache2.CountAsync();

                // Letterless source texts (e.g. "5+") read the same in every language, so an
                // untranslated term is one that is blank/EN-equal AND actually requires translation.
                var zhTranslated = totalCount - await _translationContext.TranslationCache2
                    .Where(g => string.IsNullOrEmpty(g.TargetTextZh) || g.TargetTextZh == g.SourceText)
                    .Where(SourceTextRequiresTranslation)
                    .CountAsync();
                var esTranslated = totalCount - await _translationContext.TranslationCache2
                    .Where(g => string.IsNullOrEmpty(g.TargetTextEs) || g.TargetTextEs == g.SourceText)
                    .Where(SourceTextRequiresTranslation)
                    .CountAsync();
                var frTranslated = totalCount - await _translationContext.TranslationCache2
                    .Where(g => string.IsNullOrEmpty(g.TargetTextFr) || g.TargetTextFr == g.SourceText)
                    .Where(SourceTextRequiresTranslation)
                    .CountAsync();

                return new BaseResponse<TranslationStatsResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TranslationStatsResponse
                    {
                        Languages = new List<LanguageTranslationStat>
                        {
                            new LanguageTranslationStat { LanguageCode = "ZH", TotalCount = totalCount, TranslatedCount = zhTranslated },
                            new LanguageTranslationStat { LanguageCode = "ES", TotalCount = totalCount, TranslatedCount = esTranslated },
                            new LanguageTranslationStat { LanguageCode = "FR", TotalCount = totalCount, TranslatedCount = frTranslated }
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<TranslationStatsResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message
                };
            }
        }

        public async Task<BaseResponse<bool>> AddOrUpdateSportsbookGlossary(AddOrUpdateSportsbookGlossaryRequest request, RequesterInfo requesterInfo)
        {
            try
            {
                var targetLanguage = string.IsNullOrWhiteSpace(request.TargetLanguage) ? null : request.TargetLanguage.ToUpper();
                if (targetLanguage != null && targetLanguage != "EN" && !IsSupportedTargetLanguage(targetLanguage))
                {
                    return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = $"Unsupported target language '{request.TargetLanguage}'.", Result = false };
                }

                var activity = new ActivityLogModel
                {
                    Page = requesterInfo.Page,
                    UserId = requesterInfo.UserId,
                    UserName = requesterInfo.UserName,
                    TimeStamp = DateTime.UtcNow
                };

                var translationChanges = new List<TranslationChangeItem>();
                string editedStringId;

                if (request.Id.HasValue && request.Id.Value > 0)
                {
                    var existing = await _translationContext.TranslationCache2.FirstOrDefaultAsync(g => g.Id == request.Id.Value);
                    if (existing == null)
                    {
                        return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = "Glossary entry not found.", Result = false };
                    }

                    editedStringId = existing.StringId;

                    activity.Activity = Activity.Update.ToString();
                    activity.Payload = existing.StringId;
                    activity.From = existing.SourceText;
                    activity.To = request.SourceText;

                    existing.SourceText = request.SourceText;
                    existing.UpdatedAt = DateTime.UtcNow;

                    if (targetLanguage != null && targetLanguage != "EN")
                    {
                        SetTargetText(existing, targetLanguage, request.TargetText ?? string.Empty);
                        translationChanges.Add(new TranslationChangeItem
                        {
                            StId = existing.StringId,
                            LaCo = targetLanguage,
                            Tx = request.TargetText ?? string.Empty,
                            SrcTx = existing.SourceText
                        });
                    }

                    _translationContext.TranslationCache2.Update(existing);
                }
                else
                {
                    var duplicate = await _translationContext.TranslationCache2.AnyAsync(g => g.StringId == request.StringId);
                    if (duplicate)
                    {
                        return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = "That String ID already exists.", Result = false };
                    }

                    editedStringId = request.StringId;

                    activity.Activity = Activity.Create.ToString();
                    activity.Payload = request.StringId;
                    activity.From = "-";
                    activity.To = request.SourceText;

                    var entity = new TranslationCache2
                    {
                        StringId = request.StringId,
                        SourceLanguage = string.IsNullOrWhiteSpace(request.SourceLanguage) ? "EN" : request.SourceLanguage.ToUpper(),
                        SourceText = request.SourceText,
                        TargetTextZh = string.Empty,
                        TargetTextEs = string.Empty,
                        TargetTextFr = string.Empty,
                        HasBetPlacement = false,
                        // New rows start on the external pipeline's worklist (PROCESSED = 0),
                        // matching the table default — the pipeline flips it to 1 once handled.
                        Processed = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (targetLanguage != null && targetLanguage != "EN")
                    {
                        SetTargetText(entity, targetLanguage, request.TargetText ?? string.Empty);
                        translationChanges.Add(new TranslationChangeItem
                        {
                            StId = request.StringId,
                            LaCo = targetLanguage,
                            Tx = request.TargetText ?? string.Empty,
                            SrcTx = request.SourceText
                        });
                    }

                    _translationContext.TranslationCache2.Add(entity);
                }

                var siblingActivityLogs = new List<ActivityLogModel>();

                if (targetLanguage != null && targetLanguage != "EN")
                {
                    await CascadeToSiblingGlossaryRowsAsync(
                        editedStringId, request.SourceText, targetLanguage, request.TargetText ?? string.Empty,
                        translationChanges, requesterInfo, siblingActivityLogs);
                }

                await _translationContext.SaveChangesAsync();
                await AddActivityLog(activity);

                if (siblingActivityLogs.Count > 0)
                {
                    await AddActivityLogList(new ActivityLogListRequest { ActivityLogs = siblingActivityLogs });
                }

                await PublishTranslationChange(translationChanges);

                return new BaseResponse<bool> { StatusCode = StatusCode.Success, Message = StatusCode.Success.ToString(), Result = true };
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = ex.Message, Result = false };
            }
        }

        // A glossary edit only ever updates the ONE StringId being edited, but the same English
        // source text can legitimately appear under other StringIds too (a duplicate/legacy row,
        // or two different real-world entities that just happen to share an English name — verified
        // live 2026-07: the only cross-prefix collision in TranslationCache2 today is SegId:/Te:
        // both named after the same country, e.g. "Argentina" as both a segment and several team
        // rows, which genuinely should share one translation). This propagates the SAME
        // target-language text to every OTHER row with an identical SourceText, regardless of
        // STRING_ID prefix. Each cascaded row is logged individually (siblingActivityLogs) so a
        // translator can see it happened.
        private async Task<int> CascadeToSiblingGlossaryRowsAsync(
            string editedStringId, string sourceText, string targetLanguage, string targetText,
            List<TranslationChangeItem> translationChanges, RequesterInfo requesterInfo, List<ActivityLogModel> activityLogs)
        {
            if (string.IsNullOrWhiteSpace(sourceText) || string.IsNullOrWhiteSpace(editedStringId))
                return 0;

            var siblings = await _translationContext.TranslationCache2
                .Where(g => g.SourceText == sourceText && g.StringId != editedStringId)
                .ToListAsync();

            var updatedCount = 0;

            foreach (var sibling in siblings)
            {
                var previous = GetTargetText(sibling, targetLanguage) ?? string.Empty;

                if (string.Equals(previous.Trim(), targetText.Trim(), StringComparison.Ordinal))
                    continue; // already the same value -- idempotent no-op, nothing to log or publish

                SetTargetText(sibling, targetLanguage, targetText);
                sibling.Processed = true;
                sibling.UpdatedAt = DateTime.UtcNow;
                _translationContext.TranslationCache2.Update(sibling);

                translationChanges.Add(new TranslationChangeItem { StId = sibling.StringId, LaCo = targetLanguage, Tx = targetText, SrcTx = sourceText });
                activityLogs.Add(BuildGlossaryActivityLog(requesterInfo, Activity.Update, sibling.StringId, previous, targetText));
                updatedCount++;
            }

            return updatedCount;
        }

        public async Task<BaseResponse<bool>> SetSportsbookGlossaryProcessed(SetSportsbookGlossaryProcessedRequest request, RequesterInfo requesterInfo)
        {
            try
            {
                var existing = await _translationContext.TranslationCache2.FirstOrDefaultAsync(g => g.Id == request.Id);
                if (existing == null)
                {
                    return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = "Glossary entry not found.", Result = false };
                }

                if (existing.Processed == request.Processed)
                {
                    return new BaseResponse<bool> { StatusCode = StatusCode.Success, Message = StatusCode.Success.ToString(), Result = true };
                }

                var previous = existing.Processed;
                existing.Processed = request.Processed;
                _translationContext.TranslationCache2.Update(existing);
                await _translationContext.SaveChangesAsync();

                await AddActivityLog(new ActivityLogModel
                {
                    Page = requesterInfo.Page,
                    UserId = requesterInfo.UserId,
                    UserName = requesterInfo.UserName,
                    Activity = Activity.Update.ToString(),
                    Payload = $"{existing.StringId}.Processed",
                    From = previous.ToString(),
                    To = request.Processed.ToString(),
                    TimeStamp = DateTime.UtcNow
                });

                return new BaseResponse<bool> { StatusCode = StatusCode.Success, Message = StatusCode.Success.ToString(), Result = true };
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = ex.Message, Result = false };
            }
        }

        public async Task<BaseResponse<bool>> DeleteSportsbookGlossary(DeleteSportsbookGlossaryRequest request, RequesterInfo requesterInfo)
        {
            try
            {
                var existing = await _translationContext.TranslationCache2.FirstOrDefaultAsync(g => g.Id == request.Id);
                if (existing == null)
                {
                    return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = "Glossary entry not found.", Result = false };
                }

                if (existing.HasBetPlacement)
                {
                    return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = "This entry has an active bet placement and cannot be deleted.", Result = false };
                }

                var translationChanges = new List<TranslationChangeItem>();
                if (!string.IsNullOrEmpty(existing.TargetTextZh))
                    translationChanges.Add(new TranslationChangeItem { StId = existing.StringId, LaCo = "ZH", Tx = string.Empty, SrcTx = existing.SourceText });
                if (!string.IsNullOrEmpty(existing.TargetTextEs))
                    translationChanges.Add(new TranslationChangeItem { StId = existing.StringId, LaCo = "ES", Tx = string.Empty, SrcTx = existing.SourceText });
                if (!string.IsNullOrEmpty(existing.TargetTextFr))
                    translationChanges.Add(new TranslationChangeItem { StId = existing.StringId, LaCo = "FR", Tx = string.Empty, SrcTx = existing.SourceText });

                _translationContext.TranslationCache2.Remove(existing);
                await _translationContext.SaveChangesAsync();

                await AddActivityLog(new ActivityLogModel
                {
                    Page = requesterInfo.Page,
                    UserId = requesterInfo.UserId,
                    UserName = requesterInfo.UserName,
                    Activity = Activity.Delete.ToString(),
                    Payload = existing.StringId,
                    From = existing.SourceText,
                    To = "-",
                    TimeStamp = DateTime.UtcNow
                });

                await PublishTranslationChange(translationChanges);

                return new BaseResponse<bool> { StatusCode = StatusCode.Success, Message = StatusCode.Success.ToString(), Result = true };
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = ex.Message, Result = false };
            }
        }

        // Returns every glossary row (no pagination) so the Web layer can build a full CSV
        // export/upload-template client-side, the same way Static Translation's CSV export
        // already works — this endpoint hands back data, not a formatted file.
        // All three language columns are always returned (the file doubles as the import
        // template); untranslatedOnly/targetLanguage only narrow which rows are included,
        // using the same filter as the Dynamic Translation grid.
        public async Task<BaseResponse<List<SportsbookGlossaryExportRow>>> ExportSportsbookGlossary(string searchText, string targetLanguage = null, bool untranslatedOnly = false, bool exactMatch = false)
        {
            try
            {
                var query = _translationContext.TranslationCache2.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = exactMatch
                        ? query.Where(g => g.StringId == searchText || g.SourceText == searchText)
                        : query.Where(g => g.StringId.Contains(searchText) || g.SourceText.Contains(searchText));
                }

                var normalizedTargetLanguage = string.IsNullOrWhiteSpace(targetLanguage) ? "EN" : targetLanguage.ToUpper();
                if (untranslatedOnly && normalizedTargetLanguage != "EN")
                {
                    query = ApplyUntranslatedFilter(query, normalizedTargetLanguage);
                }

                var rows = await query
                    .OrderBy(g => g.StringId)
                    .Select(g => new SportsbookGlossaryExportRow
                    {
                        StringId = g.StringId,
                        SourceText = g.SourceText,
                        TargetTextZh = g.TargetTextZh,
                        TargetTextEs = g.TargetTextEs,
                        TargetTextFr = g.TargetTextFr
                    })
                    .ToListAsync();

                return new BaseResponse<List<SportsbookGlossaryExportRow>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = rows
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<SportsbookGlossaryExportRow>>
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message
                };
            }
        }

        // Upserts glossary rows parsed from an uploaded export/template file (parsing happens
        // in Backoffice.Web; this only ever sees structured rows). Idempotent by design: a
        // target-language cell is skipped (no write, no activity log, no publish) when it's
        // blank (leave the existing translation untouched), equal to SourceText (treated as
        // "not yet translated"), or equal to the value already stored -- so re-uploading the
        // same file twice is a no-op the second time.
        public async Task<BaseResponse<ImportSportsbookGlossaryResponse>> ImportSportsbookGlossary(ImportSportsbookGlossaryRequest request, RequesterInfo requesterInfo)
        {
            try
            {
                if (request?.Rows == null || request.Rows.Count == 0)
                {
                    return new BaseResponse<ImportSportsbookGlossaryResponse> { StatusCode = StatusCode.Error, Message = "The uploaded file has no data rows." };
                }

                var existingByStringId = (await _translationContext.TranslationCache2.ToListAsync())
                    .ToDictionary(g => g.StringId, StringComparer.OrdinalIgnoreCase);

                var result = new ImportSportsbookGlossaryResponse();
                var translationChanges = new List<TranslationChangeItem>();
                var activityLogs = new List<ActivityLogModel>();
                var seenStringIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < request.Rows.Count; i++)
                {
                    var row = request.Rows[i];
                    var rowNumber = i + 2; // +1 for 1-based, +1 because row 1 is the header in the source file
                    var stringId = row.StringId?.Trim();

                    if (string.IsNullOrWhiteSpace(stringId))
                    {
                        continue;
                    }

                    if (!seenStringIds.Add(stringId))
                    {
                        result.SkippedCount++;
                        result.RowMessages.Add($"Row {rowNumber}: duplicate String ID '{stringId}' already processed earlier in this file — skipped.");
                        continue;
                    }

                    var uploadedSourceText = row.SourceText?.Trim() ?? string.Empty;

                    if (existingByStringId.TryGetValue(stringId, out var existing))
                    {
                        var changed = ApplyGlossaryTargetLanguageUpserts(existing, existing.SourceText, row.TargetTextZh, row.TargetTextEs, row.TargetTextFr, translationChanges);
                        if (changed)
                        {
                            existing.UpdatedAt = DateTime.UtcNow;
                            _translationContext.TranslationCache2.Update(existing);
                            activityLogs.Add(BuildGlossaryActivityLog(requesterInfo, Activity.Update, stringId, "(bulk import)", "(bulk import)"));
                            result.UpdatedCount++;
                        }
                        else
                        {
                            result.UnchangedCount++;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(uploadedSourceText))
                        {
                            result.SkippedCount++;
                            result.RowMessages.Add($"Row {rowNumber}: EN is required to create new String ID '{stringId}' — skipped.");
                            continue;
                        }

                        var entity = new TranslationCache2
                        {
                            StringId = stringId,
                            SourceLanguage = "EN",
                            SourceText = uploadedSourceText,
                            TargetTextZh = string.Empty,
                            TargetTextEs = string.Empty,
                            TargetTextFr = string.Empty,
                            HasBetPlacement = false,
                            Processed = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        ApplyGlossaryTargetLanguageUpserts(entity, uploadedSourceText, row.TargetTextZh, row.TargetTextEs, row.TargetTextFr, translationChanges);
                        _translationContext.TranslationCache2.Add(entity);
                        existingByStringId[stringId] = entity;
                        activityLogs.Add(BuildGlossaryActivityLog(requesterInfo, Activity.Create, stringId, "-", uploadedSourceText));
                        result.CreatedCount++;
                    }
                }

                if (result.CreatedCount > 0 || result.UpdatedCount > 0)
                {
                    await _translationContext.SaveChangesAsync();
                }

                if (activityLogs.Count > 0)
                {
                    await AddActivityLogList(new ActivityLogListRequest { ActivityLogs = activityLogs });
                }

                await PublishTranslationChange(translationChanges);

                return new BaseResponse<ImportSportsbookGlossaryResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ImportSportsbookGlossaryResponse> { StatusCode = StatusCode.Error, Message = ex.Message };
            }
        }
        #endregion

        #region Private Methods

        private static bool IsSupportedTargetLanguage(string targetLanguage) =>
            targetLanguage == "ZH" || targetLanguage == "ES" || targetLanguage == "FR";

        // Shared "untranslated" definition for the Dynamic Translation grid and its CSV
        // export: target text missing or still equal to the source, excluding letterless
        // source texts (numbers/punctuation only) that never need translating.
        private static IQueryable<TranslationCache2> ApplyUntranslatedFilter(IQueryable<TranslationCache2> query, string targetLanguage)
        {
            switch (targetLanguage)
            {
                case "ZH": query = query.Where(g => string.IsNullOrEmpty(g.TargetTextZh) || g.TargetTextZh == g.SourceText); break;
                case "ES": query = query.Where(g => string.IsNullOrEmpty(g.TargetTextEs) || g.TargetTextEs == g.SourceText); break;
                case "FR": query = query.Where(g => string.IsNullOrEmpty(g.TargetTextFr) || g.TargetTextFr == g.SourceText); break;
            }

            if (IsSupportedTargetLanguage(targetLanguage))
            {
                query = query.Where(SourceTextRequiresTranslation);
            }

            return query;
        }

        // Query-side equivalent of TranslationTextHelper.RequiresTranslation: char.IsLetter and
        // regex do not translate on EF Core 3.1, and LIKE '[A-Za-z]' ranges are SQL Server-only
        // (the InMemory provider treats them literally), so this builds
        // "SourceText != null && (LOWER(SourceText) contains 'a' || ... || contains 'z')".
        private static readonly Expression<Func<TranslationCache2, bool>> SourceTextRequiresTranslation = BuildSourceTextRequiresTranslation();

        private static Expression<Func<TranslationCache2, bool>> BuildSourceTextRequiresTranslation()
        {
            var parameter = Expression.Parameter(typeof(TranslationCache2), "g");
            var sourceText = Expression.Property(parameter, nameof(TranslationCache2.SourceText));
            var lowered = Expression.Call(sourceText, nameof(string.ToLower), Type.EmptyTypes);

            Expression containsAnyLetter = null;
            for (var letter = 'a'; letter <= 'z'; letter++)
            {
                var contains = Expression.Call(lowered, nameof(string.Contains), Type.EmptyTypes, Expression.Constant(letter.ToString()));
                containsAnyLetter = containsAnyLetter == null ? contains : (Expression)Expression.OrElse(containsAnyLetter, contains);
            }

            var notNull = Expression.NotEqual(sourceText, Expression.Constant(null, typeof(string)));
            return Expression.Lambda<Func<TranslationCache2, bool>>(Expression.AndAlso(notNull, containsAnyLetter), parameter);
        }

        private static string GetTargetText(TranslationCache2 entity, string targetLanguage) => targetLanguage switch
        {
            "ZH" => entity.TargetTextZh,
            "ES" => entity.TargetTextEs,
            "FR" => entity.TargetTextFr,
            _ => null
        };

        private static void SetTargetText(TranslationCache2 entity, string targetLanguage, string value)
        {
            switch (targetLanguage)
            {
                case "ZH": entity.TargetTextZh = value; break;
                case "ES": entity.TargetTextEs = value; break;
                case "FR": entity.TargetTextFr = value; break;
            }
        }

        // Applies the "not equal to source_text" upsert rule to all three target-language
        // columns for one glossary row. Returns true if any column actually changed.
        private static bool ApplyGlossaryTargetLanguageUpserts(TranslationCache2 entity, string sourceText, string zh, string es, string fr, List<TranslationChangeItem> translationChanges)
        {
            var changedZh = TryUpsertGlossaryTargetLanguage(entity.StringId, "ZH", entity.TargetTextZh, zh, sourceText, v => entity.TargetTextZh = v, translationChanges);
            var changedEs = TryUpsertGlossaryTargetLanguage(entity.StringId, "ES", entity.TargetTextEs, es, sourceText, v => entity.TargetTextEs = v, translationChanges);
            var changedFr = TryUpsertGlossaryTargetLanguage(entity.StringId, "FR", entity.TargetTextFr, fr, sourceText, v => entity.TargetTextFr = v, translationChanges);
            return changedZh || changedEs || changedFr;
        }

        private static bool TryUpsertGlossaryTargetLanguage(string stringId, string languageCode, string currentValue, string uploadedRaw, string sourceText, Action<string> setter, List<TranslationChangeItem> translationChanges)
        {
            if (string.IsNullOrWhiteSpace(uploadedRaw))
            {
                return false; // blank cell => leave the existing translation untouched
            }

            var uploaded = uploadedRaw.Trim();

            if (string.Equals(uploaded, sourceText?.Trim(), StringComparison.Ordinal))
            {
                return false; // matches source text => treat as not-yet-translated, skip
            }

            if (string.Equals(uploaded, currentValue?.Trim(), StringComparison.Ordinal))
            {
                return false; // already the stored value => idempotent no-op
            }

            setter(uploaded);
            translationChanges.Add(new TranslationChangeItem { StId = stringId, LaCo = languageCode, Tx = uploaded, SrcTx = sourceText });
            return true;
        }

        private static ActivityLogModel BuildGlossaryActivityLog(RequesterInfo requesterInfo, Activity activity, string stringId, string from, string to) => new ActivityLogModel
        {
            Page = requesterInfo.Page,
            UserId = requesterInfo.UserId,
            UserName = requesterInfo.UserName,
            Activity = activity.ToString(),
            Payload = stringId,
            From = from,
            To = to,
            TimeStamp = DateTime.UtcNow
        };

        // Emits the changed (StringId, TargetLanguage, Text) deltas after a glossary write so
        // Platform.CacheSync.Service purges the Sportsbook.API translation cache per key and
        // relays them to the OddsEngine (r.oe.ontranslationchange). Publish failures are caught
        // inside RabbitMqRepository.Publish and returned as an Error response, so a broker outage
        // cannot fail the glossary save that has already committed.
        private async Task PublishTranslationChange(List<TranslationChangeItem> translationChanges)
        {
            if (translationChanges == null || translationChanges.Count == 0)
                return;

            await _rabbitMqRepository.Publish(new MessageRequest
            {
                CorrelationId = Guid.NewGuid().ToString(),
                MessageType = "OnTranslationChange",
                RoutingKey = "r.oe.cachesync.ontranslationchange",
                Payload = JsonSerializer.Serialize(new OnTranslationChangeMessage { Tr = translationChanges })
            });
        }

        // Tells Platform.Sportsbook.API (via Platform.CacheSync.Service) to drop its
        // SportsbookStatic caches ("tr" + every "tr-{LANG}") so an edited UI label is served on
        // the next page load instead of waiting out the 24h TTL. The purge always clears every
        // language because EN rows are merged into all per-language results; CacheKey carries the
        // edited language code for traceability only.
        private async Task PublishStaticTranslationChange(string languageCode)
        {
            await _rabbitMqRepository.Publish(new MessageRequest
            {
                CorrelationId = Guid.NewGuid().ToString(),
                MessageType = "OnStaticTranslationChange",
                RoutingKey = "r.oe.cachesync.onstatictranslationchange",
                Payload = JsonSerializer.Serialize(new OnDeleteCacheMessage
                {
                    CacheType = CacheType.StaticTranslation,
                    CacheKey = languageCode ?? string.Empty
                })
            });
        }

        private static string GetMessageType(CacheType cacheType)
        {
            return cacheType switch
            {
                CacheType.MarketSortOrder => "OnMarketSortOrderChange",
                CacheType.SportSortOrder => "OnSportSortOrderChange",
                CacheType.UserFavorites => "OnUpdateUserFavorites",
                CacheType.Maintenance => "OnMaintenance",
                CacheType.ParlayBonus => "OnParlayBonusChange",
                CacheType.Client => "OnClientChange",
                CacheType.DynamicTranslation => "OnDynamicTranslationChange",
                CacheType.StaticTranslation => "OnStaticTranslationChange",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(cacheType),
                    cacheType,
                    "Unsupported cache type")
            };
        }
        #endregion
    }

}
