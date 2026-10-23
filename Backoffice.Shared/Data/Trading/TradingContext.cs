
using Microsoft.EntityFrameworkCore;
using Backoffice.Shared.Data.Trading.SQLEntities;

namespace Backoffice.Shared.Data.Trading
{
    public class TradingContext : DbContext
    {
        public TradingContext(DbContextOptions<TradingContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActivityLog>().HasKey(k => new { k.Id });
            modelBuilder.Entity<BoostedSettings>().HasKey(k => new { k.ClientId });
            modelBuilder.Entity<ClientLeagueConfig>().HasKey(k => new { k.ClientCode, k.SportId, k.SegmentId, k.LeagueId });
            modelBuilder.Entity<ClientMarketConfig>().HasKey(k => new { k.ClientCode, k.SportId, k.SegmentId, k.LeagueId, k.MarketTemplateId, k.FixtureTypeId });
            modelBuilder.Entity<PriceBoostConfig>().HasKey(k => new { k.ClientCode, k.SportId, k.SegmentId, k.LeagueId, k.FixtureTypeId, k.ProviderFixtureId, k.MarketTemplateId, k.MarketSpecifier });
            modelBuilder.Entity<PriceBoostConfigAudit>().HasKey(k => new { k.ClientCode, k.SportId, k.SegmentId, k.LeagueId, k.FixtureTypeId, k.ProviderFixtureId, k.MarketTemplateId, k.TimeStamp });
            modelBuilder.Entity<ManualMarketSettlementAudit>().HasKey(k => new { k.Id });
            modelBuilder.Entity<User>().HasKey(k => new { k.UserId });
            modelBuilder.Entity<UserGroup>().HasKey(k => new { k.UserGroupId });
            modelBuilder.Entity<UserGroupTemplate>().HasKey(k => new { k.UserGroupTemplateId });
            modelBuilder.Entity<UserPolicy>().HasKey(k => new { k.UserGroupId, k.UserId });
            modelBuilder.Entity<Announcement>().HasKey(k => new { k.AnnouncementId });
            modelBuilder.Entity<AspNetRoles>().HasKey(k => new { k.Id });
            modelBuilder.Entity<AspNetRoleClaims>().HasKey(k => new { k.Id });
            modelBuilder.Entity<AspNetUserRoles>().HasKey(k => new { k.UserId });
            modelBuilder.Entity<AspNetUsers>().HasKey(k => new { k.Id });
            modelBuilder.Entity<Permission>().HasKey(k => new { k.PermissionId });
            modelBuilder.Entity<UserGroupClients>().HasKey(k => new { k.RoleId });
            modelBuilder.Entity<ClientSportOrderAudit>().HasKey(k => new { k.ClientCode, k.TimeStamp });
            modelBuilder.Entity<ConfigAudit>().HasKey(k => new { k.Id });
            modelBuilder.Entity<ManualMessageSettlementAudit>().HasKey(k => new { k.Id });
            modelBuilder.Entity<BookmakerConfiguration>().HasKey(k => new { k.BookmakerId });
            modelBuilder.Entity<Notifications>().HasKey(k => new { k.Id });
            modelBuilder.Entity<NotificationsStatus>().HasKey(k => new { k.Id, k.UserId });
            modelBuilder.Entity<ClientOddsFeedConfiguration>().HasKey(k => new { k.ClientId, k.SportId });
            modelBuilder.Entity<ClientOddsFeedLiveFixture>().HasKey(k => new { k.ClientId, k.SportId, k.FixtureSpecifier });
            modelBuilder.Entity<SportMapping>().HasKey(k => new { k.SpId });
            modelBuilder.Entity<SegmentMapping>().HasKey(k => new { k.SegId });
            modelBuilder.Entity<LeagueMapping>().HasKey(k => new { k.LeId });
            modelBuilder.Entity<FixtureMapping>().HasKey(k => new { k.FiId });
            modelBuilder.Entity<MarketMapping>().HasKey(k => new { k.MaTeId });
            modelBuilder.Entity<SelectionMapping>().HasKey(k => new { k.SeTeId });
        }

        public DbSet<ActivityLog> ActivityLog { get; set; }
        public DbSet<ClientLeagueConfig> ClientLeagueConfig { get; set; }
        public DbSet<ClientMarketConfig> ClientMarketConfig { get; set; }
        public DbSet<PriceBoostConfig> PriceBoostConfig { get; set; }
        public DbSet<BoostedSettings> BoostedSettings { get; set; }
        public DbSet<PriceBoostConfigAudit> PriceBoostConfigAudit { get; set; }
        public DbSet<ManualMarketSettlementAudit> ManualMarketSettlementAudit { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<UserGroup> UserGroup { get; set; }
        public DbSet<UserGroupTemplate> UserGroupTemplate { get; set; }
        public DbSet<UserPolicy> UserPolicy { get; set; }
        public DbSet<Announcement> Announcement { get; set; }
        public DbSet<AspNetRoles> AspNetRoles { get; set; }
        public DbSet<AspNetRoleClaims> AspNetRoleClaims { get; set; }
        public DbSet<AspNetUserRoles> AspNetUserRoles { get; set; }
        public DbSet<AspNetUsers> AspNetUsers { get; set; }
        public DbSet<Permission> Permission { get; set; }
        public DbSet<UserGroupClients> UserGroupClients { get; set; }
        public DbSet<ClientSportOrderAudit> ClientSportOrderAudit { get; set; }
        public DbSet<ConfigAudit> ConfigAudit { get; set; }
        public DbSet<ManualMessageSettlementAudit> ManualMessageSettlementAudit { get; set; }
        public DbSet<BookmakerConfiguration> BookmakerConfiguration { get; set; }
        public DbSet<Notifications> Notifications { get; set; }
        public DbSet<NotificationsStatus> NotificationsStatus { get; set; }
        public DbSet<ClientOddsFeedConfiguration> ClientOddsFeedConfiguration { get; set; }
        public DbSet<ClientOddsFeedLiveFixture> ClientOddsFeedLiveFixture { get; set; }
        public DbSet<SportMapping> SportMapping { get; set; }
        public DbSet<SegmentMapping> SegmentMapping { get; set; }
        public DbSet<LeagueMapping> LeagueMapping { get; set; }
        public DbSet<FixtureMapping> FixtureMapping { get; set; }
        public DbSet<MarketMapping> MarketMapping { get; set; }
        public DbSet<SelectionMapping> SelectionMapping { get; set; }

        // Stored Procedure
        public DbSet<ActivityLog> ReadActivityLog { get; set; }
        public DbSet<Notifications> ReadNotification { get; set; }
    }
}