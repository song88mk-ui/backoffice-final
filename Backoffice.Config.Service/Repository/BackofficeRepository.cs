using Backoffice.Config.Service;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
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
        private readonly ILogger<Worker> _logger;

        private readonly string _prematchDefaultMargin;
        private readonly string _liveDefaultMargin;
        public BackofficeRepository(IConfiguration configuration, ILogger<Worker> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var optionsBuilder = new DbContextOptionsBuilder<BackofficeContext>().UseSqlServer(_configuration["ConnectionStrings:Backoffice"]);
            _backofficeContext = new BackofficeContext(optionsBuilder.Options);

            _prematchDefaultMargin = JsonSerializer.Serialize(new { T = 691200, M = 101 });
            _liveDefaultMargin = JsonSerializer.Serialize(new { T = 0, M = 101 });
        }

        public async Task UpdateClientMargin(long configTemplateId, IMessageBroker messageBroker)
        {
            _logger.LogInformation("Processing Client Margin...");
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
                var allLeagues = new List<ClientMarketConfigModel2>();

                foreach (var client in clients.GroupBy(s => s.ClientCode))
                {
                    var payload = new OnUpdateClientMarketConfigMessage();
                    foreach (var leagues in client)
                    {
                        var clientConfig = configs.Where(c => c.SportId == leagues.SportId && (c.ConfigTemplateId == leagues.PrematchConfigTemplateId || c.ConfigTemplateId == leagues.LiveConfigTemplateId))
                            .GroupBy(g => new
                            {
                                SportId = g.SportId,
                                MarketTemplateId = g.MarketTemplateId,
                                FixtureTypeId = g.FixtureTypeId
                            });

                        foreach (var config in clientConfig)
                        {
                            var prematch = config.FirstOrDefault(c => c.FixtureStatusId == (int)Scope.Prematch);
                            var live = config.FirstOrDefault(c => c.FixtureStatusId == (int)Scope.Live);

                            payload.ConfigSettings.Add(new ClientMarketConfigModel2
                            {
                                ClientId = leagues.ClientId,
                                ClientCode = leagues.ClientCode,
                                SportId = leagues.SportId,
                                SegmentId = leagues.SegmentId,
                                LeagueId = leagues.LeagueId,
                                MarketTemplateId = config.Key.MarketTemplateId,
                                FixtureTypeId = config.Key.FixtureTypeId,
                                IsActive = (prematch?.IsActive ?? false) || (live?.IsActive ?? false),
                                Prematch = prematch?.Config ?? _prematchDefaultMargin,
                                Live = live?.Config ?? _liveDefaultMargin
                            });
                        }
                    }

                    if (payload.ConfigSettings.Any())
                    {
                        int payloadByteSize = ASCIIEncoding.Unicode.GetByteCount(JsonSerializer.Serialize(payload));

                        messageBroker.Publish(JsonSerializer.Serialize(payload), "OnUpdateClientMarketConfig", "r.oe.web.config.OnUpdateClientMarketConfig", ConvertToUnixTimestamp(DateTime.UtcNow).ToString());

                        allLeagues.AddRange(payload.ConfigSettings);
                    }
                }

                if(allLeagues.Any())
                {
                    var leagues = allLeagues.GroupBy(s => new
                    {
                        s.ClientId,
                        s.ClientCode,
                        s.SportId,
                        s.SegmentId,
                        s.LeagueId
                    }).ToList();

                    var payload = leagues.Select(s => new
                    {
                        s.Key.ClientId,
                        s.Key.ClientCode,
                        s.Key.SportId,
                        s.Key.SegmentId,
                        s.Key.LeagueId,
                        IsActive = s.Any(c => c.IsActive)
                    });
                    int payloadByteSize = System.Text.ASCIIEncoding.Unicode.GetByteCount(JsonSerializer.Serialize(allLeagues));

                    messageBroker.Publish(JsonSerializer.Serialize(allLeagues), "OnUpdateClientLeagueConfig", "r.oe.web.config.OnUpdateClientLeagueConfig", ConvertToUnixTimestamp(DateTime.UtcNow).ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _logger.LogInformation("Finished Client Margin");
            }
        }


        private double ConvertToUnixTimestamp(DateTime date)
        {
            var diff = date.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return Math.Floor(diff.TotalSeconds);
        }
    }
}