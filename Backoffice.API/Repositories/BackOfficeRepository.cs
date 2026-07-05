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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using static Backoffice.Shared.Models.ManualBetDetailsModel;
using static Backoffice.Shared.Models.Requests.AddOrUpdateBetConfigRequest;
using static Backoffice.Shared.Models.Response.ClientListResponse;
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
            catch (Exception ex)
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
                        .FromSqlRaw($"GetActivityLogs '{request.Page}', '{request.Payload}', '{request.DateFrom.ToString("yyyyMMdd HH:mm:ss")}', '{request.DateTo.ToString("yyyyMMdd HH:mm:ss")}'")
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
                        .FromSqlRaw($"GetActivityLogs '{AdminPermission.Admin_Settlements_ManualBetSettlement.ToString().Replace('_', '.')}', '', '{request.ManualBetSettlementCallbackModel.DateFrom.ToString("yyyyMMdd HH:mm:ss")}', '{request.ManualBetSettlementCallbackModel.DateTo.ToString("yyyyMMdd HH:mm:ss")}'")
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
                        .FromSqlRaw($"GetActivityLogs '{request.Page}', '{request.Payload}', '{fromUTC}', '{toUTC}'")
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

                if (!string.IsNullOrEmpty(request.SearchText))
                {
                    activityLogs = activityLogs.Where(x => x.From.ToUpper().Contains(request.SearchText.ToUpper()) || x.To.ToUpper().Contains(request.SearchText.ToUpper()) || x.UserName.ToUpper().Contains(request.SearchText.ToUpper())).ToList();
                }

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

                var clientList = new List<ClientListResponse.Client>();
                foreach (var client in clients)
                {
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
                    var emailAddress = new MailAddress(request.Email);
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
                            ClientName = request.Name,
                            ClientCode = request.ClientCode,
                            IsActive = request.StatusId == (int)ClientStatus.Normal ? true : false,
                            ClientIdRef = (int)clientId,
                            //LimitId = string.Empty,
                            //EndPoint = string.Empty,
                            MonthlyFee = request.MonthlyFee,
                            CurrentCompanyShare = companyRevenueShare,
                            EffectiveDate = DateTime.UtcNow,
                        };

                        timestampStartedUpdate = DateTime.UtcNow;

                        var newPlatformResponse = await _platformRepository.AddOrUpdateClient(clientPlatform, request.Region, events);
                        if (newPlatformResponse.StatusCode == StatusCode.Success)
                        {
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


                    }
                    catch (Exception ex)
                    {
                    }

                    scope.Complete();


                    var properties = new Dictionary<string, string>
                    {
                        { "client-code", request.ClientCode },
                        { "events", JsonSerializer.Serialize(events) }
                    };

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
        public async Task<BaseResponse<bool>> SetConfigState(ConfigState configState, ConfigSetLeagueStateRequest request)
        {
            try
            {
                var prematchTemplateId = request.PrematchTemplateId;
                var liveTemplateId = request.LiveTemplateId;
                var leagueRequest = request.Leagues;
                var configs = leagueRequest.Select(x => new ConfigTemplateAssigned()
                {
                    Id = x.Id,
                    ClientId = Guid.Parse(x.ClientId),
                    ClientCode = x.ClientCode,
                    ClientName = x.ClientName,
                    SportId = x.SportId,
                    SportName = x.SportName,
                    SegmentId = x.SegmentId,
                    SegmentName = x.SegmentName,
                    LeagueId = x.LeagueId,
                    LeagueName = x.LeagueName,
                    PrematchConfigTemplateId = prematchTemplateId,
                    LiveConfigTemplateId = liveTemplateId
                }).ToList();

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                {
                    using (var context = _backofficeContext)
                    {
                        switch (configState)
                        {
                            case ConfigState.Assigned:
                                foreach (var config in configs)
                                {
                                    var configTemplateAssigned = _backofficeContext.ConfigTemplateAssigned.Where(x => x.Id == config.Id && x.SportId == config.SportId && x.SegmentId == config.SegmentId && x.LeagueId == config.LeagueId).AsNoTracking().FirstOrDefault();
                                    if (configTemplateAssigned == null)
                                    {
                                        context.ConfigTemplateAssigned.Add(new ConfigTemplateAssigned()
                                        {
                                            ClientId = config.ClientId,
                                            ClientCode = config.ClientCode,
                                            ClientName = config.ClientName,
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
                                    else
                                    {
                                        configTemplateAssigned.PrematchConfigTemplateId = config.PrematchConfigTemplateId;
                                        configTemplateAssigned.LiveConfigTemplateId = config.LiveConfigTemplateId;

                                        context.ConfigTemplateAssigned.Update(configTemplateAssigned);
                                    }
                                    context.SaveChanges();
                                }
                                break;
                            case ConfigState.UnAssigned:
                                context.ConfigTemplateAssigned.RemoveRange(configs);
                                context.SaveChanges();
                                break;
                        }
                        scope.Complete();
                    }
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

                var query = _backofficeContext.ConfigTemplateAssigned.AsNoTracking().Where(x => clientList.Select(cl => cl.ClientCode).Contains(x.ClientCode.ToUpper()));

                if (templateId != -1)
                    query = query.Where(x => x.PrematchConfigTemplateId == templateId || x.LiveConfigTemplateId == templateId);

                if (payload != null && payload.SportId != -1)
                {
                    query = query.Where(x => x.SportId == payload.SportId);
                }

                if (payload != null && !string.IsNullOrEmpty(payload.SearchText))
                {
                    query = query.Where(x => x.ClientName.ToLower().Contains(payload.SearchText.ToLower()) || x.SegmentName.ToLower().Contains(payload.SearchText.ToLower()) || x.LeagueName.ToLower().Contains(payload.SearchText.ToLower()));
                }

                if (query != null && query.Any())
                {
                    isCompleteClient = clientList.Count() == allClientsCount;
                }

                var sportsTranslationLookup = GetSportsLookup(langCode);

                switch (configState)
                {
                    case ConfigState.Assigned:
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
                        var leagueLookupResponse = await _tradingRepository.GetAllLeagueLookup(payload.SportId == -1 ? "" : string.Join(',', payload.SportId));
                        if (leagueLookupResponse.Result != null)
                        {
                            var leagueLookup = leagueLookupResponse.Result.LeagueLookup;

                            foreach (var client in clientList)
                            {
                                var userLeagues = query.Where(x => x.ClientCode == client.ClientCode).ToList();
                                var leagues = leagueLookup.Where(x => !userLeagues.Any(u => u.SportId == x.SportId && u.SegmentId == x.SegmentId && u.LeagueId == x.LeagueId)).ToList();

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
                        }

                        if (!string.IsNullOrEmpty(payload.SearchText))
                        {
                            clientConfigs = clientConfigs.Where(x => x.ClientName.ToLower().Contains(payload.SearchText.ToLower()) || x.LeagueName.ToLower().Contains(payload.SearchText.ToLower())).ToList();
                        }
                        break;

                }

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
                    var query = _backofficeContext.ConfigTemplateAssigned.Select(s => s.ClientCode);

                    if ((clientCodes.Count() == 1 && clientCodes.FirstOrDefault().ToUpper() == "ALL") == false)
                        query.Where(c => clientCodes.Contains(c));

                    count = query.Count();
                }
                else if (state.ToUpper() == ConfigState.UnAssigned.ToString().ToUpper())
                {
                    count = await GetUnAssignedCount(clientCodes);
                }
                else
                {
                    var configTemplate = await GetConfigTemplatesCount();
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
            var entity = _backofficeContext.ClientInformation.Update(client).Entity;
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

        private List<SportLookupModel>? GetSportsLookup(LanguageCode langCode)
        {
            if (langCode == LanguageCode.EN)
                return null;

            var cache = _memCache.GetCache($"sp-tr-{langCode.ToString().ToUpper()}", async () => await _tradingRepository.GetSportLookup(langCode.ToString().ToUpper()), 1);

            return cache.Result?.Result?.SportLookup ?? null;
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
                .Where(x => clientIds.Contains(JsonSerializer.Deserialize<ManualBetDetails>(x.To).ClientIdRef.ToString())).ToList();
            }

            if (callbackModel.Sports != null && callbackModel.Sports.Any())
            {
                sportIds = string.Join(",", callbackModel.Sports.Select(sport => sport.SportId)).Split(',');

                activityLogs = activityLogs
                .Where(x => sportIds.Contains(JsonSerializer.Deserialize<ManualBetDetails>(x.To).Selections.FirstOrDefault().SportId.ToString())).ToList();
            }

            if (callbackModel.Currencies != null && callbackModel.Currencies.Any())
            {
                currencyCodes = string.Join(",", callbackModel.Currencies.Select(currency => currency.CurrencyCode)).Split(',');

                activityLogs = activityLogs
                .Where(x => currencyCodes.Contains(JsonSerializer.Deserialize<ManualBetDetails>(x.To).CurrencyCode)).ToList();
            }

            if (callbackModel.FromBetStatuses.Any())
            {
                fromStatusesIds = string.Join(",", callbackModel.FromBetStatuses.Select(status => status.BetStatusId)).Split(',');

                activityLogs = activityLogs
                .Where(x => fromStatusesIds.Contains(JsonSerializer.Deserialize<ManualBetDetails>(x.To).BetStatusId.ToString())).ToList();
            }

            if (callbackModel.ToBetStatuses != null && callbackModel.ToBetStatuses.Any())
            {
                toStatusesIds = string.Join(",", callbackModel.ToBetStatuses.Select(status => status.BetStatusId)).Split(',');

                activityLogs = activityLogs
                .Where(x => toStatusesIds.Contains(JsonSerializer.Deserialize<ManualBetDetails>(x.To).NewBetStatusId.ToString())).ToList();
            }

            return activityLogs;
        }
        private List<BackofficeStatic> GetTranslations()
        {
            //return _memCache.GetCache("tr", () => _translationContext.BackofficeStatic.AsNoTracking().ToList(), 24);
            return _translationContext.BackofficeStatic.AsNoTracking().ToList();
        }
        private async Task<int> GetAssignedCount(List<string> clientCodes)
        {
            var prematchTask = _backofficeContext.ConfigTemplateAssigned.CountAsync(e => e.PrematchConfigTemplateId != -1);

            var liveTask = _backofficeContext.ConfigTemplateAssigned.CountAsync(e => e.LiveConfigTemplateId != -1);

            var result = 0;
            var assignedConfigTask = GetConfigState(clientCodes, null, ConfigState.Assigned);
            var leagueLookupResponseTask = _tradingRepository.GetAllLeagueLookup("");

            await Task.WhenAll(leagueLookupResponseTask, assignedConfigTask);

            var leagueLookupResponse = leagueLookupResponseTask.Result;
            var assignedConfig = assignedConfigTask.Result;

            if (leagueLookupResponse.StatusCode == StatusCode.Success)
            {
                var assignedLeagues = assignedConfig.Result.ClientConfigs.Count();
                var leagueLookup = leagueLookupResponse.Result.LeagueLookup;

                var clientList = (await _platformRepository.GetClients("HK")).Result;
                result = (leagueLookup.Count * clientList.Count) - assignedLeagues;
            }

            return result;
        }
        private async Task<int> GetUnAssignedCount(List<string> clientCodes)
        {
            var result = -1;

            var query = _backofficeContext.ConfigTemplateAssigned.Select(s => new { s.ClientCode, s.LeagueId }).Distinct();

            if ((clientCodes.Count() == 1 && clientCodes.FirstOrDefault().ToUpper() == "ALL") == false)
                query.Where(c => clientCodes.Contains(c.ClientCode));

            var leagueCount = query.Count();
            var leagueLookupResponse = await _tradingRepository.GetAllLeagueLookup("");

            var clientList = (await _platformRepository.GetClients("HK")).Result;
            if (leagueLookupResponse.StatusCode == StatusCode.Success)
                result = (leagueLookupResponse.Result.LeagueLookup.Count * clientList.Count) - leagueCount;

            return result;
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
                var translations = await _translationContext.SportsbookStatic
                    .AsNoTracking()
                    .Where(t => t.LanguageCode == languageCode)
                    .Select(t => new SportsbookTranslationResponse
                    {
                        LanguageCode = t.LanguageCode,
                        Name = t.Name,
                        Value = t.Value
                    })
                    .ToListAsync();

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

        public async Task<BaseResponse<SportsbookGlossaryListResponse>> GetSportsbookGlossary(GetSportsbookGlossaryRequest request)
        {
            try
            {
                var metaData = request.MetaData ?? new MetaData();
                var targetLanguage = string.IsNullOrWhiteSpace(request.TargetLanguage) ? "EN" : request.TargetLanguage.ToUpper();

                var query = _translationContext.TranslationCache2.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SearchText))
                {
                    query = query.Where(g => g.StringId.Contains(request.SearchText) || g.SourceText.Contains(request.SearchText));
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

                if (request.Id.HasValue && request.Id.Value > 0)
                {
                    var existing = await _translationContext.TranslationCache2.FirstOrDefaultAsync(g => g.Id == request.Id.Value);
                    if (existing == null)
                    {
                        return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = "Glossary entry not found.", Result = false };
                    }

                    activity.Activity = Activity.Update.ToString();
                    activity.Payload = existing.StringId;
                    activity.From = existing.SourceText;
                    activity.To = request.SourceText;

                    existing.SourceText = request.SourceText;
                    existing.UpdatedAt = DateTime.UtcNow;

                    if (targetLanguage != null && targetLanguage != "EN")
                    {
                        SetTargetText(existing, targetLanguage, request.TargetText ?? string.Empty);
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
                        GlossaryVersion = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (targetLanguage != null && targetLanguage != "EN")
                    {
                        SetTargetText(entity, targetLanguage, request.TargetText ?? string.Empty);
                    }

                    _translationContext.TranslationCache2.Add(entity);
                }

                await _translationContext.SaveChangesAsync();
                await AddActivityLog(activity);

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

                return new BaseResponse<bool> { StatusCode = StatusCode.Success, Message = StatusCode.Success.ToString(), Result = true };
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool> { StatusCode = StatusCode.Error, Message = ex.Message, Result = false };
            }
        }
        #endregion

        #region Private Methods

        private static bool IsSupportedTargetLanguage(string targetLanguage) =>
            targetLanguage == "ZH" || targetLanguage == "ES" || targetLanguage == "FR";

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
                _ => throw new ArgumentOutOfRangeException(
                    nameof(cacheType),
                    cacheType,
                    "Unsupported cache type")
            };
        }
        #endregion
    }

}
