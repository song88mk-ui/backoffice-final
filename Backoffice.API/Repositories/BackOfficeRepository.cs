using Backoffice.API.Models;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

namespace Backoffice.API.Repositories
{
    public class BackofficeRepository : IBackofficeRepository
    {
        #region Fields
        private readonly BackofficeContext _backofficeContext;
        private readonly IPlatformRepository _platformRepository;
        private readonly ITradingRepository _tradingRepository;
        private readonly IRabbitMqRepository _rabbitMqRepository;
        private readonly IMemCache _memCache;
        private string[] cryptoCurrencies = new string[] { Currency.BTC.ToString(), Currency.ETH.ToString() };

        #endregion

        #region Ctor

        public BackofficeRepository(BackofficeContext backofficeContext, IPlatformRepository platformRepository, ITradingRepository tradingRepository, IMemCache memCache, IRabbitMqRepository rabbitMqRepository)
        {
            _backofficeContext = backofficeContext;
            _platformRepository = platformRepository;
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
                    .Where(c => c.IsDeleted == false && c.StatusId == (int)ClientStatus.Normal)
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
                var query = GetClientInformation().Where(c => c.IsDeleted == false && (c.ApplicationId == (int)Application2.SBC || c.ApplicationId == (int)Application.Sportsbook));

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
                #region VALIDATION
                var userId = 0;

                long clientId = 0;
                if (GetClientInformation().Any(c => c.Brand == request.ClientCode && c.ClientId != request.ClientId))
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "Brand ID is already in use. Please add another ID.",
                        Result = false
                    };
                }
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
                                        Message = $"Suggested minimum bet not met. ({betSetting.CurrencyCode})",
                                        Result = false
                                    };
                                }

                                if (minStake > maxStake)
                                {
                                    return new BaseResponse<bool>
                                    {
                                        StatusCode = StatusCode.Error,
                                        Message = $"Max amount must be greater than minimum amount. ({betSetting.CurrencyCode})",
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
                                        Message = $"Max Winnings Bonus must be at least 0.02. ({betSetting.CurrencyCode})",
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
                                        Message = $"Maximum stake must be lower or equal 99999999999. ({betSetting.CurrencyCode})",
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
                                var newPlatformBettingResponse = await _platformRepository.ProcessBackofficeClientBetConfig(betConfigRequest, request.Region);
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
        #endregion

        #region Config
        public async Task<BaseResponse<List<ConfigTemplateModel>>> GetConfigTemplates(string searchText, List<int> sportIds = null, List<int> scopeIds = null)
        {
            try
            {
                var query = _backofficeContext.ConfigTemplate.AsNoTracking();


                if (!string.IsNullOrEmpty(searchText))
                    query = query.Where(x => x.Name.ToLower().Contains(searchText.ToLower()));

                if (sportIds != null && sportIds.Count() > 0)
                    query = query.Where(x => sportIds.Contains(x.SportId));

                if (scopeIds != null && scopeIds.Count() > 0)
                    query = query.Where(x => scopeIds.Contains(x.FixtureStatusId));

                var configTemplates = await query.ToListAsync();

                var tempsSportIds = query.Select(s => s.SportId).ToList();
                var configTemplateAssigned = _backofficeContext.ConfigTemplateAssigned.Where(s => tempsSportIds.Contains(s.SportId)).ToList();

                var result = configTemplates.Select(x =>
                {
                    var leagues = configTemplateAssigned.Where(p => p.SportId == x.SportId && (p.PrematchConfigTemplateId == x.Id || p.LiveConfigTemplateId == x.Id)).ToList();

                    return new ConfigTemplateModel()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        SportId = x.SportId,
                        FixtureTypeId = x.FixtureTypeId,
                        FixtureStatusId = x.FixtureStatusId,
                        Timestamp = x.Timestamp,
                        ClientsCount = leagues.Select(x => x.ClientCode).Distinct().Count(),
                        LeaguesCount = leagues.Select(x => x.LeagueId).Distinct().Count(),
                    };
                }).OrderBy(x => x.Name).ToList();

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
                    Activity = Activity.Delete.ToString(),
                    UserId = requesterInfo.UserId,
                    UserName = requesterInfo.UserName,
                    TimeStamp = DateTime.UtcNow
                };

                var config = await _backofficeContext.ConfigTemplate.FindAsync(id);

                activity.Payload = $"{config.SportId}.{config.FixtureStatusId}.{config.Name}.T".Replace(" ", "_");
                activity.From = JsonSerializer.Serialize(config);

                if (config != null)
                {
                    _backofficeContext.ConfigTemplate.Remove(config);
                }

                await _backofficeContext.SaveChangesAsync();
                await AddActivityLog(activity);

                return await GetConfigTemplates("");
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<ConfigTemplateModel>>() { StatusCode = StatusCode.Error, Message = ex.Message, Result = null };
            }
        }
        public async Task<BaseResponse<ConfigTemplateLeagueResponse>> GetConfigState(List<string> clientCodes, ConfigTemplateLeagueRequest payload, ConfigState configState, int templateId = -1)
        {
            try
            {
                var clientList = GetFilteredClientList(clientCodes, payload);
                var query = BuildQuery(clientList, payload, templateId);

                var isCompleteClient = clientList.Count == GetTradingClientInformation().Count;
                var clientConfigs = await GetClientConfigs(query, configState, payload, clientList);

                if (payload?.MetaData != null)
                {
                    ApplyPagination(clientConfigs, payload.MetaData);
                }

                return new BaseResponse<ConfigTemplateLeagueResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ConfigTemplateLeagueResponse
                    {
                        IsComplete = isCompleteClient,
                        ClientConfigs = clientConfigs,
                        MetaData = payload.MetaData
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
                    var assignedConfig = await GetConfigState(clientCodes, null, ConfigState.Assigned);
                    count = assignedConfig.Result.ClientConfigs.Count();
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
                var existingConfig = _backofficeContext.ConfigTemplateMargin.Where(x => x.ConfigTemplateId == request.ConfigTemplateMargins.FirstOrDefault().ConfigTemplateId).AsNoTracking();

                if (existingConfig != null)
                {
                    _backofficeContext.ConfigTemplateMargin.RemoveRange(existingConfig);
                }

                _backofficeContext.ConfigTemplateMargin.AddRange(request.ConfigTemplateMargins.Select(config => new Shared.Data.Backoffice.Entities.ConfigTemplateMargin()
                {
                    ConfigTemplateId = config.ConfigTemplateId,
                    FixtureTypeId = config.FixtureTypeId,
                    FixtureStatusId = config.FixtureStatusId,
                    MarketTemplateId = config.MarketTemplateId,
                    SportId = config.SportId,
                    Config = config.Config,
                    IsActive = config.IsActive
                }).ToArray());

                await _backofficeContext.SaveChangesAsync();

                var configTemplateIds = request.ConfigTemplateMargins.Select(s => s.ConfigTemplateId).Distinct().ToList();

                foreach (var configTemplateId in configTemplateIds)
                {
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

            //result = englishTranslations.GroupJoin(refTranslations,
            //    en => en.Name,
            //    la => la.Name,
            //    (english, language) => new { english, language })
            //    .SelectMany(s => s.language.DefaultIfEmpty(),
            //    (left, right) => new
            //    {
            //        Name = left.english.Name,
            //        Value = right != null ? right.Value : left.english.Value
            //    }).ToDictionary(s => s.Name, s => s.Value);

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
            }; ;
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
            //return _memCache.GetCache("boci", () => _backofficeContext.ClientInformation.AsNoTracking().ToList(), 24) ?? new List<ClientInformation>();
            return _backofficeContext.ClientInformation.AsNoTracking().ToList() ?? new List<ClientInformation>();
        }
        private List<ClientInformationModel> GetTradingClientInformation()
        {
            return _tradingRepository.GetClientList("").Result?.Result ?? new List<ClientInformationModel>();
        }
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
        private List<Translation> GetTranslations()
        {
            //return _memCache.GetCache("tr", () => _backofficeContext.Translation.AsNoTracking().ToList(), 24);
            return _backofficeContext.Translation.AsNoTracking().ToList();
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

                result = (leagueLookup.Count * GetTradingClientInformation().Count) - assignedLeagues;
            }

            return result;
        }
        private async Task<int> GetUnAssignedCount(List<string> clientCodes)
        {
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

                result = (leagueLookup.Count * GetTradingClientInformation().Count) - assignedLeagues;
            }

            return result;
        }
        private List<ClientInformationModel> GetFilteredClientList(List<string> clientCodes, ConfigTemplateLeagueRequest payload)
        {
            var clientList = GetTradingClientInformation();

            if (clientCodes.Count == 1 && clientCodes.FirstOrDefault().ToUpper() != "ALL")
            {
                return clientList.Where(c => c.ClientCode == clientCodes.First()).ToList();
            }

            if (payload?.ClientCodes != null)
            {
                return clientList.Where(c => payload.ClientCodes.Contains(c.ClientCode)).ToList();
            }

            return clientList;
        }
        private IQueryable<ConfigTemplateAssigned> BuildQuery(List<ClientInformationModel> clientList, ConfigTemplateLeagueRequest payload, int templateId)
        {
            var query = _backofficeContext.ConfigTemplateAssigned.AsNoTracking()
                .Where(x => clientList.Select(cl => cl.ClientCode.ToUpper()).Contains(x.ClientCode.ToUpper()));

            if (templateId != -1)
            {
                query = query.Where(x => x.PrematchConfigTemplateId == templateId || x.LiveConfigTemplateId == templateId);
            }

            if (payload != null && payload.SportId != -1)
            {
                query = query.Where(x => x.SportId == payload.SportId);
            }

            if (!string.IsNullOrEmpty(payload?.SearchText))
            {
                var searchText = payload.SearchText.ToLower();
                query = query.Where(x => x.ClientName.ToLower().Contains(searchText) || x.LeagueName.ToLower().Contains(searchText));
            }

            return query;
        }
        private async Task<List<ConfigClientModel>> GetClientConfigs(IQueryable<ConfigTemplateAssigned> query, ConfigState configState, ConfigTemplateLeagueRequest payload, List<ClientInformationModel> clientList)
        {
            switch (configState)
            {
                case ConfigState.Assigned:
                    return await GetAssignedClientConfigs(query);
                case ConfigState.UnAssigned:
                    return await GetUnassignedClientConfigs(clientList, query, payload);
                default:
                    return new List<ConfigClientModel>();
            }
        }
        private async Task<List<ConfigClientModel>> GetAssignedClientConfigs(IQueryable<ConfigTemplateAssigned> query)
        {
            var configList = query
                .Select(x => new
                {
                    x.Id,
                    x.ClientId,
                    x.ClientCode,
                    x.ClientName,
                    x.SportId,
                    x.SportName,
                    x.SegmentId,
                    x.SegmentName,
                    x.LeagueId,
                    x.LeagueName,
                    x.PrematchConfigTemplateId,
                    x.LiveConfigTemplateId
                })
                .ToList();

            var prematchTemplateIds = configList.Select(c => c.PrematchConfigTemplateId).Distinct().ToList();
            var liveTemplateIds = configList.Select(c => c.LiveConfigTemplateId).Distinct().ToList();

            var templateNames = await _backofficeContext.ConfigTemplate
                .Where(c => prematchTemplateIds.Contains(c.Id) || liveTemplateIds.Contains(c.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

            var mappedConfigList = configList.Select(x => new ConfigClientModel
            {
                Id = x.Id,
                ClientId = x.ClientId.ToString(),
                ClientCode = x.ClientCode,
                ClientName = x.ClientName,
                SportId = x.SportId,
                SportName = x.SportName,
                SegmentId = x.SegmentId,
                SegmentName = x.SegmentName,
                LeagueId = x.LeagueId,
                LeagueName = x.LeagueName,
                PrematchTemplateId = x.PrematchConfigTemplateId,
                LiveTemplateId = x.LiveConfigTemplateId,
                PrematchTemplateName = templateNames.GetValueOrDefault(x.PrematchConfigTemplateId),
                LiveTemplateName = templateNames.GetValueOrDefault(x.LiveConfigTemplateId)
            })
            .OrderBy(x => x.ClientCode)
            .ThenBy(x => x.SportName)
            .ThenBy(x => x.LeagueName)
            .ToList();

            return mappedConfigList;

        }
        private async Task<List<ConfigClientModel>> GetUnassignedClientConfigs(List<ClientInformationModel> clientList, IQueryable<ConfigTemplateAssigned> query, ConfigTemplateLeagueRequest payload)
        {
            var leagueLookupResponse = await _tradingRepository.GetAllLeagueLookup(payload?.SportId == -1 ? "" : string.Join(',', payload.SportId));

            if (leagueLookupResponse.StatusCode != StatusCode.Success)
                return new List<ConfigClientModel>();

            var leagueLookup = leagueLookupResponse.Result.LeagueLookup;
            var clientConfigs = new List<ConfigClientModel>();

            foreach (var client in clientList)
            {
                var userLeagues = query.Where(x => x.ClientCode == client.ClientCode).ToList();
                var leagues = leagueLookup.Where(x => !userLeagues.Any(u => u.SportId == x.SportId && u.SegmentId == x.SegmentId && u.LeagueId == x.LeagueId)).ToList();

                clientConfigs.AddRange(leagues.Select(x => new ConfigClientModel
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

            return clientConfigs;
        }
        private void ApplyPagination<T>(List<T> contents, MetaData metaData)
        {
            metaData.TotalCount = contents.Count;
            metaData.TotalPages = (int)Math.Ceiling((decimal)metaData.TotalCount / metaData.PageSize);

            var skip = (metaData.CurrentPage - 1) * metaData.PageSize;
            metaData.PageSize = metaData.PageSize == -1 ? metaData.TotalCount : metaData.PageSize;

            contents = contents.Skip(Math.Max(0, skip)).Take(metaData.PageSize).ToList();
        }

        #endregion
    }
}