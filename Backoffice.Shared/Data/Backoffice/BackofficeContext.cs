using Backoffice.Shared.Data.Backoffice.Entities;
using Microsoft.EntityFrameworkCore;

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
        }

        public DbSet<ClientInformation> ClientInformation { get; set; }

        public DbSet<ClientBetSetting> ClientBetSetting { get; set; }
        public DbSet<ActivityLog> ActivityLog { get; set; }
        // Stored Procedure
        public DbSet<ActivityLog> ReadActivityLog { get; set; }
    }
}
