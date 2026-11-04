using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.OutgoingEngineMessages;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Backoffice.Config.Service.Repository
{
    public class BackofficeRepository : IBackofficeRepository
    {
        // The repository is a singleton, so each message gets its own short-lived context from the factory. A shared
        // context was disposed by the first DeleteConfigTemplate and broke every later message (BO-138).
        private readonly IDbContextFactory<BackofficeContext> _contextFactory;
        private readonly ILogger<BackofficeRepository> _logger;

        private readonly string _prematchDefaultMargin;
        private readonly string _liveDefaultMargin;

        public BackofficeRepository(IDbContextFactory<BackofficeContext> contextFactory, ILogger<BackofficeRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;

            _prematchDefaultMargin = JsonSerializer.Serialize(new List<MarketMargin>() { new MarketMargin() { T = 691200, M = 101 } });
            _liveDefaultMargin = JsonSerializer.Serialize(new List<MarketMargin>() { new MarketMargin() { T = 0, M = 101 } });
        }

        public void UpdateClientMargin(long configTemplateId, IMessageBroker messageBroker)
        {
            _logger.LogInformation("processing update client margin {configTemplateId}", configTemplateId);

            // A failure is not caught here, so the worker can retry the message once and then report it (BO-138).
            using var context = _contextFactory.CreateDbContext();

            var clients = context.ConfigTemplateAssigned.AsNoTracking().Where(c => c.PrematchConfigTemplateId == configTemplateId || c.LiveConfigTemplateId == configTemplateId).ToList();

            var configTemplateIds = new List<long>(clients.SelectMany(client => new[] { client.PrematchConfigTemplateId, client.LiveConfigTemplateId }).Distinct().Where(id => id != -1));

            var configs = context.ConfigTemplateMargin.AsNoTracking().Where(c => configTemplateIds.Contains(c.ConfigTemplateId)).ToList();

            var leagues = clients.GroupBy(c => new { c.SportId, c.SegmentId, c.LeagueId, c.LiveConfigTemplateId, c.PrematchConfigTemplateId }).ToList();

            var leaguePayload = new List<ClientLeagueConfigModel2>();

            foreach (var league in leagues)
            {
                var clientConfig = configs.Where(c => c.SportId == league.Key.SportId && (c.ConfigTemplateId == league.Key.PrematchConfigTemplateId || c.ConfigTemplateId == league.Key.LiveConfigTemplateId))
                    .GroupBy(g => new
                    {
                        g.SportId,
                        g.MarketTemplateId,
                        g.FixtureTypeId
                    }).ToList();

                var newClientConfigs = new List<ClientMarketConfigModel2>();
                foreach (var client in league)
                {
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

                if (newClientConfigs.Any())
                {
                    var serMarketPayload = JsonSerializer.Serialize(new OnUpdateClientMarketConfigMessage()
                    {
                        ConfigSettings = new List<ClientMarketConfigModel2>(newClientConfigs)
                    });

                    messageBroker.Publish(serMarketPayload, "OnUpdateClientMarketConfig", "r.oe.web.config.OnUpdateClientMarketConfig", ConvertToUnixTimestamp(DateTime.UtcNow).ToString());
                }

                var clientLeaguePayload = newClientConfigs.GroupBy(g => new
                {
                    g.ClientId,
                    g.ClientCode,
                    g.SportId,
                    g.SegmentId,
                    g.LeagueId,
                }).Select(s => new ClientLeagueConfigModel2
                {
                    ClientId = s.Key.ClientId,
                    ClientCode = s.Key.ClientCode,
                    SportId = s.Key.SportId,
                    SegmentId = s.Key.SegmentId,
                    LeagueId = s.Key.LeagueId,
                    IsActive = s.Any(c => c.IsActive)
                }).ToList();

                if (clientLeaguePayload.Any())
                    leaguePayload.AddRange(clientLeaguePayload);
            }

            if (leaguePayload.Any())
                messageBroker.Publish(JsonSerializer.Serialize(new OnUpdateClientLeagueConfigMessage()
                {
                    ConfigSettings = leaguePayload
                }), "OnUpdateClientLeagueConfig", "r.oe.web.config.OnUpdateClientLeagueConfig", ConvertToUnixTimestamp(DateTime.UtcNow).ToString());
        }

        public void DeleteConfigTemplate(ConfigTemplate config)
        {
            // One SaveChanges on one context is already a single transaction, so no TransactionScope is needed.
            using var context = _contextFactory.CreateDbContext();

            var assignedLiveTemplates = context.ConfigTemplateAssigned.AsNoTracking().Where(c => c.LiveConfigTemplateId == config.Id).ToList();
            if (assignedLiveTemplates.Any())
            {
                foreach (var assignedTemplate in assignedLiveTemplates)
                {
                    if (assignedTemplate.PrematchConfigTemplateId > 0)
                    {
                        assignedTemplate.LiveConfigTemplateId = -1;
                        context.ConfigTemplateAssigned.Update(assignedTemplate);
                    }
                    else
                        context.ConfigTemplateAssigned.Remove(assignedTemplate);
                }
            }

            var assignedPrematchTemplates = context.ConfigTemplateAssigned.Where(c => c.PrematchConfigTemplateId == config.Id).ToList();
            if (assignedPrematchTemplates.Any())
            {
                foreach (var assignedTemplate in assignedPrematchTemplates)
                {
                    if (assignedTemplate.LiveConfigTemplateId > 0)
                    {
                        assignedTemplate.PrematchConfigTemplateId = -1;
                        context.ConfigTemplateAssigned.Update(assignedTemplate);
                    }
                    else
                        context.ConfigTemplateAssigned.Remove(assignedTemplate);
                }
            }

            context.SaveChanges();

            // Margins, to follow
        }

        private double ConvertToUnixTimestamp(DateTime date)
        {
            var diff = date.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return Math.Floor(diff.TotalSeconds);
        }
    }
}
