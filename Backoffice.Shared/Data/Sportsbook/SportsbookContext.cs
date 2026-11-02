
using Microsoft.EntityFrameworkCore;
using OE.Shared.Data.Sportsbook.SQLEntities;

namespace OE.Shared.Data.Sportsbook
{
    public class SportsbookContext : DbContext
    {
        public SportsbookContext(DbContextOptions<SportsbookContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClientSportOrder>().HasKey(k => new { k.ClientId, k.SportId });
            modelBuilder.Entity<ClientSegmentOrder>().HasKey(k => new { k.ClientId, k.SportId, k.SegmentId });
            modelBuilder.Entity<ClientLeagueOrder>().HasKey(k => new { k.ClientId, k.SportId, k.SegmentId, k.LeagueId });
            modelBuilder.Entity<ClientOddstableLeagueOrder>().HasKey(k => new { k.ClientId, k.SportId, k.SegmentId, k.LeagueId });
            modelBuilder.Entity<MarketTemplateGroup>().HasKey(k => new { k.SportId, k.FixtureStatusId, k.Id });
            // the groups the sportsbook site uses are in MarketTemplateGroup2; the table named MarketTemplateGroup is empty and unused (BO-142)
            modelBuilder.Entity<MarketTemplateGroup>().ToTable("MarketTemplateGroup2");
            modelBuilder.Entity<TierLeagueOrder>().HasKey(k => new { k.SportId, k.SegmentId, k.LeagueId });
            modelBuilder.Entity<SelectorMarketTemplate>().HasKey(k => new { k.SportId, k.MarketTemplateId });
            modelBuilder.Entity<ClientMarketTemplateOrder>().HasKey(k => new { k.ClientId, k.SportId, k.MarketTemplateId });
            modelBuilder.Entity<ClientSelectorMarketTemplate>().HasKey(k => new { k.ClientId, k.SportId, k.MarketTemplateId });
            modelBuilder.Entity<ClientMarketTemplateGroup>().HasKey(k => new { k.ClientId, k.SportId, k.FixtureStatusId, k.Id });
            modelBuilder.Entity<MarketTemplatePlayerProps>().HasKey(k => new { k.SportId, k.FixtureStatusId });
            modelBuilder.Entity<ClientSportOrderAudit>().HasKey(k => new { k.ClientCode, k.TimeStamp });
        }

        public DbSet<MarketTemplateGroup> MarketTemplateGroup { get; set; }
        public DbSet<ClientSportOrder> ClientSportOrder { get; set; }
        public DbSet<ClientSegmentOrder> ClientSegmentOrder { get; set; }
        public DbSet<ClientLeagueOrder> ClientLeagueOrder { get; set; }
        public DbSet<ClientOddstableLeagueOrder> ClientOddstableLeagueOrder { get; set; }
        public DbSet<TierLeagueOrder> TierLeagueOrder { get; set; }
        public DbSet<SelectorMarketTemplate> SelectorMarketTemplate { get; set; }
        public DbSet<ClientMarketTemplateOrder> ClientMarketTemplateOrder { get; set; }
        public DbSet<ClientSelectorMarketTemplate> ClientSelectorMarketTemplate { get; set; }
        public DbSet<ClientMarketTemplateGroup> ClientMarketTemplateGroup { get; set; }
        public DbSet<MarketTemplatePlayerProps> MarketTemplatePlayerProps { get; set; }
        public DbSet<ClientSportOrderAudit> ClientSportOrderAudit { get; set; }
    }
}
