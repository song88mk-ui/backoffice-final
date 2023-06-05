using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using static Backoffice.Shared.Models.ManualBetDetailsModel;
using static Backoffice.Shared.Models.Requests.AddOrUpdateBetConfigRequest;
using static Backoffice.Shared.Models.Response.ClientListResponse;

namespace Backoffice.API.Repositories
{
    public class BackofficeRepository : IBackofficeRepository
    {
        #region Fields

        private readonly IConfiguration _configuration;
        private readonly BackofficeContext _backofficeContext;
        private readonly IPlatformRepository _platformRepository;
        private readonly ITradingRepository _tradingRepository;
        private readonly IMemCache _memCache;

        #endregion

        #region Ctor

        public BackofficeRepository(IConfiguration configuration, BackofficeContext backofficeContext, IPlatformRepository platformRepository, ITradingRepository tradingRepository, IMemCache memCache)
        {
            _configuration = configuration;
            _backofficeContext = backofficeContext;
            _platformRepository = platformRepository;
            _tradingRepository = tradingRepository;
            _memCache = memCache;
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
            }
            catch (Exception ex)
            {

            }

            return new BaseResponse<bool>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = true
            };
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
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponse<ActivityLogResponse>> GetActivityLogs(ActivityLogRequest request)
        {
            try
            {
                var metaData = request.MetaData;

                if (request.DateFrom == DateTime.MinValue)
                    request.DateFrom = DateTime.UtcNow.AddDays(-1).Date;
                else
                    request.DateFrom = request.DateFrom.ToUniversalTime();
                if (request.DateTo == DateTime.MinValue)
                    request.DateTo = DateTime.UtcNow.Date.AddTicks(-1);
                else
                    request.DateTo = request.DateTo.ToUniversalTime();

                if (request.TimeStamp == DateTime.MinValue)
                {
                    request.TimeStamp = DateTime.UtcNow;
                }

                var fromUTC = request.DateFrom.ToString("yyyy-MM-dd HH:mm:ss");
                var toUTC = request.DateTo.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss");
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
                var query = GetClientInformation().Where(x => (x.ApplicationId == (int)Application2.SBC || x.ApplicationId == (int)Application.Sportsbook));

                if (brands != null)
                {
                    query = query.Where(x => brands.Contains(x.Brand));
                }
                if (!string.IsNullOrEmpty(request.Search))
                    query = query.Where(c => c.Name.Contains(request.Search));

                // sort order
                if (request.SortBy == ClientSorting.SortByBusinessNameAsc)
                {
                    query = query.OrderBy(c => c.Name);
                }
                else if (request.SortBy == ClientSorting.SortByBusinessNameDesc)
                {
                    query = query.OrderByDescending(c => c.Name);
                }
                else if (request.SortBy == ClientSorting.SortByCreatedDateAsc)
                {
                    query = query.OrderBy(c => c.CreatedDate);
                }
                else if (request.SortBy == ClientSorting.SortByCreatedDateDesc)
                {
                    query = query.OrderByDescending(c => c.CreatedDate);
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
                    Result = GetClientInformation().Where(s => s.ClientId == clientId).Select(s => s.Region).FirstOrDefault().ToString()
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
                    Result = GetClientInformation().Where(s => s.Brand == clientCode).Select(s => s.ClientId).FirstOrDefault().ToString()
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

                    var hkResult = clientInformation.Where(s => clients.Contains(s.ClientId) && s.Region == "HK").Select(s => s.ClientId).ToList();
                    if (hkResult.Any())
                        result.Add("HK", string.Join(",", hkResult));

                    var saResult = clientInformation.Where(s => clients.Contains(s.ClientId) && s.Region == "SA").Select(s => s.ClientId).ToList();
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
        public async Task<BaseResponse<bool>> AddOrUpdateClient(ClientListResponse.Client request)
        {
            try
            {
                #region VALIDATION
                var userId = 0;

                long clientId = 0;
                if (GetClientInformation().Any(c => c.Name == request.Name && c.ClientId != request.ClientId))
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

                    if (GetClientInformation().Any(c => websites.Contains(c.Website) && c.ClientId != request.ClientId))
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

                if (GetClientInformation().Any(c => c.Brand == request.ClientCode && c.ClientId != request.ClientId))
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

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                {

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
                            ApplicationId = (int)Application.Sportsbook,
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
                            client.ApplicationId = (int)Application.Sportsbook;
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

                    var clientBetSettings = _backofficeContext.ClientBetSetting.Where(b => b.ClientId == clientId).ToList();

                    var clientBetSettingList = new List<ClientBetSetting>();

                    foreach (var betSetting in request.BetSettings)
                    {
                        int[] categories = { (int)BetSettingCategory.Soccer, (int)BetSettingCategory.OtherSports, (int)BetSettingCategory.Parlay, (int)BetSettingCategory.Cricket, (int)BetSettingCategory.Priceboost, (int)BetSettingCategory.BetBuilder };

                        decimal suggestedMinimumBet = 0;
                        suggestedMinimumBet = await GetSuggestedMinimumBet(betSetting.CurrencyCode);

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
                                    return new BaseResponse<bool>
                                    {
                                        StatusCode = StatusCode.Error,
                                        Message = "Suggested minimum bet not met.",
                                        Result = false
                                    };
                                }

                                if (minStake > maxStake)
                                {
                                    return new BaseResponse<bool>
                                    {
                                        StatusCode = StatusCode.Error,
                                        Message = "Max amount must be greater than minimum amount.",
                                        Result = false
                                    };
                                }
                            }

                            if (i == (int)BetSettingCategory.Parlay)
                            {
                                if (maxWinnings < 0.02m)
                                {
                                    return new BaseResponse<bool>
                                    {
                                        StatusCode = StatusCode.Error,
                                        Message = "Max Winnings Bonus must be at least 0.02",
                                        Result = false
                                    };
                                }
                            }

                            if (i == (int)BetSettingCategory.Priceboost || i == (int)BetSettingCategory.BetBuilder)
                            {
                                if (maxStake > 99999999999 || decimalPlaces > 12)
                                {
                                    return new BaseResponse<bool>
                                    {
                                        StatusCode = StatusCode.Error,
                                        Message = "Maximum stake must be lower or equal 99999999999.",
                                        Result = false
                                    };
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
                    }

                    if (clientBetSettings.Any())
                        _backofficeContext.ClientBetSetting.RemoveRange(clientBetSettings);

                    if (clientBetSettingList.Count > 0)
                        _backofficeContext.ClientBetSetting.AddRange(clientBetSettingList);

                    await _backofficeContext.SaveChangesAsync();


                    try
                    {
                        var betConfig = new List<BetConfig>();
                        foreach (var betSetting in request.BetSettings)
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
                        }

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

                        var newPlatformResponse = await _platformRepository.AddOrUpdateClient(clientPlatform, request.Region);
                        if (newPlatformResponse.StatusCode == StatusCode.Success)
                        {
                            var betConfigRequest = new AddOrUpdateBetConfigRequest
                            {
                                ClientId = newPlatformResponse.Result.ClientId,
                                BetConfigPayload = betConfig
                            };
                            int retry = 0;
                            while (retry < 3)
                            {
                                var newPlatformBettingResponse = await _platformRepository.ProcessBackOfficeClientBetConfig(betConfigRequest, request.Region);
                                if (newPlatformBettingResponse.StatusCode == StatusCode.Success)
                                    break;
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
        #endregion

        #region Config
        public async Task<BaseResponse<List<ConfigTemplateModel>>> GetConfigTemplate(string userId, int sportId = 0, int fixtureStatusId = -1)
        {
            try
            {
                List<ConfigTemplate> query = _backofficeContext.ConfigTemplates.Where(x => x.UserId == userId).AsNoTracking().ToList();                

                if (sportId != 0)
                {
                    //all sports
                    query = query.Where(x => x.UserId == userId && x.FixtureStatusId == fixtureStatusId).ToList();
                }

                if (fixtureStatusId != -1)
                {
                    //all scopes
                    query = query.Where(x => x.UserId == userId && x.FixtureStatusId == fixtureStatusId).ToList();
                }

                var result = query.Select(x => new ConfigTemplateModel()
                {
                    Id = x.Id.ToString(),
                    UserId = x.UserId,
                    Name = x.Name,
                    SportId = x.SportId,
                    FixtureStatusId = x.FixtureStatusId,
                    ClientCodes = !string.IsNullOrEmpty(x.ClientCodes) ? x.ClientCodes.Split(',').ToList() : new List<string>(),
                    LeagueIds = !string.IsNullOrEmpty(x.LeagueIds) ? x.LeagueIds.Split(',').Select(str => int.Parse(str)).ToList() : new List<int>(),
                    Timestamp = x.Timestamp
                }).ToList();

                return new BaseResponse<List<ConfigTemplateModel>>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ConfigTemplateModel>>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString()}: {ex.Message}",
                    Result = new List<ConfigTemplateModel>()
                };
            }
        }
        public async Task<BaseResponse<List<ConfigTemplateModel>>> AddOrUpdateConfigTemplate(string userId, ConfigTemplateModel request)
        {
            try
            {
                var config = _backofficeContext.ConfigTemplates.Where(x => x.UserId == userId && x.Id.ToString() == request.Id).AsNoTracking().FirstOrDefault();

                if (config != null)
                {
                    config.ClientCodes = string.Join(',', request.ClientCodes);
                    config.LeagueIds = string.Join(',', request.LeagueIds);

                    _backofficeContext.ConfigTemplates.Update(config);
                }
                else
                {
                    var isConfigExisting = _backofficeContext.ConfigTemplates.Any(e => e.Name == request.Name && e.UserId == request.UserId);

                    if (isConfigExisting)
                    {
                        return new BaseResponse<List<ConfigTemplateModel>>()
                        {
                            Message = "Sorry, that name already exists!",
                            StatusCode = StatusCode.Error,
                            Result = null
                        };
                    }

                    _backofficeContext.ConfigTemplates.Add(new ConfigTemplate()
                    {
                        Id = Guid.NewGuid(),
                        UserId = request.UserId,
                        Name = request.Name,
                        SportId = request.SportId,
                        FixtureStatusId = request.FixtureStatusId,
                        ClientCodes = string.Join(',', request.ClientCodes),
                        LeagueIds = string.Join(',', request.LeagueIds),
                        Timestamp = DateTime.UtcNow
                    });
                }

                await _backofficeContext.SaveChangesAsync();

                return await GetConfigTemplate(userId);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ConfigTemplateModel>>() { StatusCode = StatusCode.Error, Message = ex.Message, Result = null };
            }
        }
        public async Task<BaseResponse<List<ConfigTemplateModel>>> DeleteConfigTemplate(string userId, string id)
        {
            try
            {
                var config = _backofficeContext.ConfigTemplates.Where(x => x.UserId == userId && x.Id.ToString() == id).AsNoTracking().FirstOrDefault();

                if (config != null)
                {
                    _backofficeContext.ConfigTemplates.Remove(config);
                }

                await _backofficeContext.SaveChangesAsync();

                return await GetConfigTemplate(userId);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ConfigTemplateModel>>() { StatusCode = StatusCode.Error, Message = ex.Message, Result = null };
            }
        }
        public async Task<BaseResponse<List<ConfigClientModel>>> GetConfigClient(string userId, List<string> clientCodes, bool isAssigned)
        {
            try
            {
                var result = new List<ConfigClientModel>();
                var configTemplate = await GetConfigTemplate(userId);
                var templates = configTemplate.Result;
                var clientList = GetClientInformation();
                if (clientCodes.Count == 1)
                {
                    if (clientCodes.FirstOrDefault().ToUpper() == "ALL")
                    {
                        clientCodes = GetClientInformation().Select(x => x.Brand).ToList();
                    }
                }

                if (isAssigned)
                {
                    foreach (var template in templates)
                    {
                        foreach (var client in template.ClientCodes)
                        {
                            foreach (var league in template.LeagueIds)
                            {
                                result.Add(new ConfigClientModel()
                                {
                                    ClientCode = client.ToUpper(),
                                    ClientName = clientList.Where(x => x.Brand.ToUpper() == client).Select(x => x.Name).FirstOrDefault(),
                                    SportId = template.SportId,
                                    FixtureStatusId = template.FixtureStatusId,
                                    LeagueId = league,
                                    TemplateId = template.Id,
                                    TemplateName = template.Name
                                });
                            }
                        }
                    }
                }
                else
                {
                    var sportsLookup = await _tradingRepository.GetSportLookup();
                    var fixtureStatusIds = new List<int>() { (int)FixtureStatus.Prematch, (int)FixtureStatus.Live };

                    var option = 2;
                    if (option == 1)
                        for (var x = 0; x < clientCodes.Count(); x++)
                        {
                            var client = clientCodes[x];
                            for (var y = 0; y < sportsLookup.Result.SportLookup.Count(); y++)
                            {
                                var sport = sportsLookup.Result.SportLookup[y];
                                var segments = await _tradingRepository.GetSegmentLookup(sport.SportId);

                                for (var z = 0; z < segments.Result.SegmentLookup.Count; z++)
                                {
                                    var segment = segments.Result.SegmentLookup[z];
                                    var leagues = await _tradingRepository.GetLeagueLookup(sport.SportId, segment.SegmentId);

                                    for (var h = 0; h < leagues.Result.LeagueLookup.Count(); h++)
                                    {
                                        var league = leagues.Result.LeagueLookup[h];
                                        for (var i = 0; i < fixtureStatusIds.Count; i++)
                                        {
                                            var fixtureStatus = fixtureStatusIds[i];
                                            if (!templates.Any(x => x.ClientCodes.Contains(client) && x.SportId == sport.SportId && x.LeagueIds.Contains(league.LeagueId) && x.FixtureStatusId == fixtureStatus))
                                            {
                                                result.Add(new ConfigClientModel()
                                                {
                                                    ClientCode = client.ToUpper(),
                                                    ClientName = clientList.Where(x => x.Brand.ToUpper() == client).Select(x => x.Name).FirstOrDefault(),
                                                    SportId = sport.SportId,
                                                    FixtureStatusId = fixtureStatus,
                                                    LeagueId = league.LeagueId
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    else
                        foreach (var client in clientCodes)
                        {
                            foreach (var sport in sportsLookup.Result.SportLookup)
                            {
                                var segments = await _tradingRepository.GetSegmentLookup(sport.SportId);

                                foreach (var segment in segments.Result.SegmentLookup)
                                {
                                    var leagues = await _tradingRepository.GetLeagueLookup(sport.SportId, segment.SegmentId);

                                    foreach (var league in leagues.Result.LeagueLookup)
                                    {
                                        foreach (var fixtureStatus in fixtureStatusIds)
                                        {
                                            if (!templates.Any(x => x.ClientCodes.Contains(client) && x.SportId == sport.SportId && x.LeagueIds.Contains(league.LeagueId) && x.FixtureStatusId == fixtureStatus))
                                            {
                                                result.Add(new ConfigClientModel()
                                                {
                                                    ClientCode = client.ToUpper(),
                                                    ClientName = clientList.Where(x => x.Brand.ToUpper() == client).Select(x => x.Name).FirstOrDefault(),
                                                    SportId = sport.SportId,
                                                    FixtureStatusId = fixtureStatus,
                                                    LeagueId = league.LeagueId
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                }

                return new BaseResponse<List<ConfigClientModel>>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ConfigClientModel>>() { StatusCode = StatusCode.Error, Message = ex.Message, Result = null };
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
            return _memCache.GetCache("boci", () => _backofficeContext.ClientInformation.AsNoTracking().ToList(), 24) ?? new List<ClientInformation>();
        }
        private long InsertClientRecord(ClientInformation client)
        {
            _backofficeContext.ClientInformation.Add(client);
            _backofficeContext.SaveChanges();
            _memCache.AddOrUpdateCache<List<ClientInformation>>("boci", _backofficeContext.ClientInformation.AsNoTracking().ToList(), 24);

            return client.ClientId;
        }
        private long UpdateClientRecord(ClientInformation client)
        {
            var entity = _backofficeContext.ClientInformation.Update(client).Entity;
            _backofficeContext.SaveChanges();
            _memCache.AddOrUpdateCache<List<ClientInformation>>("boci", _backofficeContext.ClientInformation.AsNoTracking().ToList(), 24);

            return client.ClientId;
        }
        private async Task<decimal> GetSuggestedMinimumBet(string currencyCode)
        {
            var currency = (Currency)Enum.Parse(typeof(Currency), currencyCode);

            var suggestedMinimumBet = 1m;

            var highValueArr = new Currency[] { Currency.KRW, Currency.VND, Currency.KHR, Currency.IDR, Currency.MMK };
            var minValue100Arr = new Currency[] { Currency.CDF, Currency.TZS };
            var minValue1Arr = new Currency[] { Currency.PKR };

            if (highValueArr.Contains(currency))
            {
                suggestedMinimumBet = 1000m;
            }
            else if (minValue100Arr.Contains(currency))
            {
                suggestedMinimumBet = 100m;
            }
            else if (currency == Currency.BTC || currency == Currency.ETH)
            {
                //suggestedMinimumBet = 0.000000010000m;
                suggestedMinimumBet = 0.00010000m; // FCI-6803
            }
            else if (minValue1Arr.Contains(currency))
            {
                suggestedMinimumBet = 1m;
            }
            else
            {
                suggestedMinimumBet = 0.02m;
            }

            return suggestedMinimumBet;
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
        #endregion
    }
}