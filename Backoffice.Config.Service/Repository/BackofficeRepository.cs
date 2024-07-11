using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backoffice.Utility.Repository
{
    public class BackofficeRepository : IBackofficeRepository
    {
        private readonly IConfiguration _configuration;
        private readonly BackofficeContext _backofficeContext;
        private readonly ILogger<BackofficeRepository> _logger;

        private readonly string _prematchDefaultMargin;
        private readonly string _liveDefaultMargin;

        public BackofficeRepository(IConfiguration configuration, ILogger<BackofficeRepository> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var optionsBuilder = new DbContextOptionsBuilder<BackofficeContext>().UseSqlServer(_configuration["ConnectionStrings:Backoffice"]);
            _backofficeContext = new BackofficeContext(optionsBuilder.Options);

            _prematchDefaultMargin = JsonSerializer.Serialize(new List<MarketMargin>() { new MarketMargin() { T = 691200, M = 101 } });
            _liveDefaultMargin = JsonSerializer.Serialize(new List<MarketMargin>() { new MarketMargin() { T = 0, M = 101 } });
        }

        public void UpdateClientMargin(long configTemplateId, IMessageBroker messageBroker)
        {
            _logger.LogInformation($"{configTemplateId} Processing...");
            var timer = new Stopwatch();
            timer.Start();
            try
            {
                var clients = _backofficeContext.ConfigTemplateAssigned.AsNoTracking().Where(c => c.PrematchConfigTemplateId == configTemplateId || c.LiveConfigTemplateId == configTemplateId).ToList();

                var configTemplateIds = new List<long>();

                foreach (var client in clients)
                {
                    if (client.PrematchConfigTemplateId != -1)
                        configTemplateIds.Add(client.PrematchConfigTemplateId);

                    if (client.LiveConfigTemplateId != -1)
                        configTemplateIds.Add(client.LiveConfigTemplateId);
                }

                var configs = _backofficeContext.ConfigTemplateMargin.AsNoTracking().Where(c => configTemplateIds.Contains(c.ConfigTemplateId)).ToList();

                var newClientConfigs = new List<ClientMarketConfigModel2>();
                foreach (var client in clients)
                {
                    var clientConfig = configs.Where(c => c.SportId == client.SportId && (c.ConfigTemplateId == client.PrematchConfigTemplateId || c.ConfigTemplateId == client.LiveConfigTemplateId))
                        .GroupBy(g => new
                        {
                            SportId = g.SportId,
                            MarketTemplateId = g.MarketTemplateId,
                            FixtureTypeId = g.FixtureTypeId
                        });

                    newClientConfigs.AddRange(clientConfig.Select(config => new ClientMarketConfigModel2
                    {
                        ClientId = client.ClientId,
                        ClientCode = client.ClientCode,
                        SportId = client.SportId,
                        SegmentId = client.SegmentId,
                        LeagueId = client.LeagueId,
                        MarketTemplateId = config.Key.MarketTemplateId,
                        FixtureTypeId = config.Key.FixtureTypeId,
                        IsActive = (config.FirstOrDefault(c => c.FixtureStatusId == (int)Scope.Prematch)?.IsActive ?? false) || (config.FirstOrDefault(c => c.FixtureStatusId == (int)Scope.Live)?.IsActive ?? false),
                        Prematch = config.FirstOrDefault(c => c.FixtureStatusId == (int)Scope.Prematch)?.Config ?? _prematchDefaultMargin,
                        Live = config.FirstOrDefault(c => c.FixtureStatusId == (int)Scope.Live)?.Config ?? _liveDefaultMargin
                    }).ToList());
                }

                var leagues = newClientConfigs.GroupBy(c => new { c.SportId, c.SegmentId, c.LeagueId }).ToList();

                Parallel.ForEach(leagues, league =>
                {
                    var configSetting = league.Select(s => s).ToList();

                    if (configSetting.Any())
                    {
                        var serMarketPayload = JsonSerializer.Serialize(new OnUpdateClientMarketConfigMessage()
                        {
                            ConfigSettings = configSetting
                        });

                        int payloadByteSize = ASCIIEncoding.Unicode.GetByteCount(serMarketPayload);

                        messageBroker.Publish(serMarketPayload, "OnUpdateClientMarketConfig", "r.oe.web.config.OnUpdateClientMarketConfig", ConvertToUnixTimestamp(DateTime.UtcNow).ToString());

                        _logger.LogInformation($"{leagues.IndexOf(league) + 1}/{leagues.Count()} {league.Key.LeagueId} is done! Size {configSetting.ToList().Count()} items with {payloadByteSize / (float)1048576} mb");

                        var leaguePayload = configSetting.GroupBy(g => new
                        {
                            ClientId = g.ClientId,
                            ClientCode = g.ClientCode,
                            SportId = g.SportId,
                            SegmentId = g.SegmentId,
                            LeagueId = g.LeagueId,
                        }).Select(s => new ClientLeagueConfigModel2
                        {
                            ClientId = s.Key.ClientId,
                            ClientCode = s.Key.ClientCode,
                            SportId = s.Key.SportId,
                            SegmentId = s.Key.SegmentId,
                            LeagueId = s.Key.LeagueId,
                            IsActive = s.Any(c => c.IsActive)
                        }).ToList();
                        messageBroker.Publish(JsonSerializer.Serialize(leaguePayload), "OnUpdateClientLeagueConfig", "r.oe.web.config.OnUpdateClientLeagueConfig", ConvertToUnixTimestamp(DateTime.UtcNow).ToString());
                    }
                });
                //var clientList = clients.GroupBy(s => s.ClientCode).ToList();

                //foreach (var client in clientList)
                //{
                //    var payload = new OnUpdateClientMarketConfigMessage();
                //    foreach (var leagues in client)
                //    {
                //        var clientConfig = configs.Where(c => c.SportId == leagues.SportId && (c.ConfigTemplateId == leagues.PrematchConfigTemplateId || c.ConfigTemplateId == leagues.LiveConfigTemplateId))
                //            .GroupBy(g => new
                //            {
                //                SportId = g.SportId,
                //                MarketTemplateId = g.MarketTemplateId,
                //                FixtureTypeId = g.FixtureTypeId
                //            });

                //        payload.ConfigSettings.AddRange(clientConfig.Select(config => new ClientMarketConfigModel2
                //        {
                //            ClientId = leagues.ClientId,
                //            ClientCode = leagues.ClientCode,
                //            SportId = leagues.SportId,
                //            SegmentId = leagues.SegmentId,
                //            LeagueId = leagues.LeagueId,
                //            MarketTemplateId = config.Key.MarketTemplateId,
                //            FixtureTypeId = config.Key.FixtureTypeId,
                //            IsActive = (config.FirstOrDefault(c => c.FixtureStatusId == (int)Scope.Prematch)?.IsActive ?? false) || (config.FirstOrDefault(c => c.FixtureStatusId == (int)Scope.Live)?.IsActive ?? false),
                //            Prematch = config.FirstOrDefault(c => c.FixtureStatusId == (int)Scope.Prematch)?.Config ?? _prematchDefaultMargin,
                //            Live = config.FirstOrDefault(c => c.FixtureStatusId == (int)Scope.Live)?.Config ?? _liveDefaultMargin
                //        }));

                //        //foreach (var config in clientConfig)
                //        //{
                //        //    var prematch = config.FirstOrDefault(c => c.FixtureStatusId == (int)Scope.Prematch);
                //        //    var live = config.FirstOrDefault(c => c.FixtureStatusId == (int)Scope.Live);

                //        //    payload.ConfigSettings.Add(new ClientMarketConfigModel2
                //        //    {
                //        //        ClientId = leagues.ClientId,
                //        //        ClientCode = leagues.ClientCode,
                //        //        SportId = leagues.SportId,
                //        //        SegmentId = leagues.SegmentId,
                //        //        LeagueId = leagues.LeagueId,
                //        //        MarketTemplateId = config.Key.MarketTemplateId,
                //        //        FixtureTypeId = config.Key.FixtureTypeId,
                //        //        IsActive = (prematch?.IsActive ?? false) || (live?.IsActive ?? false),
                //        //        Prematch = prematch?.Config ?? _prematchDefaultMargin,
                //        //        Live = live?.Config ?? _liveDefaultMargin
                //        //    });
                //        //}
                //    }

                //    //if (payload.ConfigSettings.Any())
                //    //{
                //    //    int payloadByteSize = ASCIIEncoding.Unicode.GetByteCount(JsonSerializer.Serialize(payload));

                //    //    messageBroker.Publish(JsonSerializer.Serialize(payload), "OnUpdateClientMarketConfig", "r.oe.web.config.OnUpdateClientMarketConfig", ConvertToUnixTimestamp(DateTime.UtcNow).ToString());

                //    //    allLeagues.AddRange(payload.ConfigSettings);

                //    //    _logger.LogInformation($"{clientList.IndexOf(client) + 1}/{clientList.Count()} {client.Key} is done! {client.ToList().Count()} items with {payloadByteSize / (float)1048576} mb");
                //    //}
                //}

                //if (allLeagues.Any())
                //{
                //    var leagues = allLeagues.GroupBy(s => new
                //    {
                //        s.ClientId,
                //        s.ClientCode,
                //        s.SportId,
                //        s.SegmentId,
                //        s.LeagueId
                //    }).ToList();

                //    var payload = new OnUpdateClientLeagueConfigMessage
                //    {
                //        ConfigSettings = leagues.Select(s => new ClientLeagueConfigModel2
                //        {
                //            ClientId = s.Key.ClientId,
                //            ClientCode = s.Key.ClientCode,
                //            SportId = s.Key.SportId,
                //            SegmentId = s.Key.SegmentId,
                //            LeagueId = s.Key.LeagueId,
                //            IsActive = s.Any(c => c.IsActive)
                //        }).ToList()
                //    };
                //    int payloadByteSize = System.Text.ASCIIEncoding.Unicode.GetByteCount(JsonSerializer.Serialize(payload));

                //    //messageBroker.Publish(JsonSerializer.Serialize(payload), "OnUpdateClientLeagueConfig", "r.oe.web.config.OnUpdateClientLeagueConfig", ConvertToUnixTimestamp(DateTime.UtcNow).ToString());
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _logger.LogInformation($"{configTemplateId} Finished!");
            }
        }


        private double ConvertToUnixTimestamp(DateTime date)
        {
            var diff = date.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return Math.Floor(diff.TotalSeconds);
        }
    }
}