using Backoffice.Shared.Data.Backoffice.Entities;
using Microsoft.EntityFrameworkCore;
using ConfigTemplateModel = Backoffice.Shared.Models.ConfigTemplateModel;

namespace Backoffice.Shared.Data.Backoffice
{
    public sealed class BackofficeContext : DbContext
    {
        public BackofficeContext(DbContextOptions<BackofficeContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClientInformation>().HasKey(k => new { k.ClientId });
            modelBuilder.Entity<ClientBetSetting>().HasKey(k => new { k.ClientBetSettingId });
            modelBuilder.Entity<ActivityLog>().HasKey(k => new { k.Id });
            modelBuilder.Entity<ConfigTemplate>().HasKey(k => new { k.Id });
            modelBuilder.Entity<ConfigTemplateMargin>().HasKey(k => new { k.ConfigTemplateId, k.SportId, k.FixtureTypeId, k.FixtureStatusId, k.MarketTemplateId });
            modelBuilder.Entity<ConfigTemplateAssigned>().HasKey(k => new { k.Id });
            modelBuilder.Entity<Translation>().HasKey(k => new { k.LanguageCode, k.Name });
            modelBuilder.Entity<ConfigTemplateAssignedMapping>().HasKey(k => new { k.ConfigTemplateAssignedId, k.ConfigTemplateId, k.FixtureStatusId });
        }

        public DbSet<ClientInformation> ClientInformation { get; set; }

        public DbSet<ClientBetSetting> ClientBetSetting { get; set; }
        public DbSet<ConfigTemplate> ConfigTemplate { get; set; }
        public DbSet<ConfigTemplateMargin> ConfigTemplateMargin { get; set; }
        public DbSet<ConfigTemplateAssigned> ConfigTemplateAssigned { get; set; }
        public DbSet<ConfigTemplateAssignedMapping> ConfigTemplateAssignedMapping { get; set; }
        public DbSet<ActivityLog> ActivityLog { get; set; }
        // Stored Procedure
        public DbSet<ActivityLog> ReadActivityLog { get; set; }
        public DbSet<Translation> Translation { get; set; }
        public DbSet<ConfigTemplateModel> ConfigTemplateModel { get; set; }
    }
}
