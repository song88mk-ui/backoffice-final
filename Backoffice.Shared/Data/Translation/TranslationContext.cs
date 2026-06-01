using Backoffice.Shared.Data.Translation.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backoffice.Shared.Data.Translation
{
    public sealed class TranslationContext : DbContext
    {
        public TranslationContext(DbContextOptions<TranslationContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BackofficeStatic>().HasKey(k => new { k.LanguageCode, k.Name });
        }
        public DbSet<BackofficeStatic> BackofficeStatic { get; set; }

    }
}
