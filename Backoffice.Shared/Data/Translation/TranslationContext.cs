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
            modelBuilder.Entity<SportsbookStatic>().HasKey(k => new { k.LanguageCode, k.Name });

            modelBuilder.Entity<SportsbookGlossary>(entity =>
            {
                entity.ToTable("SportsbookGlossary");

                entity.HasKey(e => e.Id)
                      .HasName("PK_SportsbookGlossary");

                entity.HasIndex(e => e.StringId)
                    .IsUnique();

                entity.HasIndex(e => e.Processed)
                    .HasFilter("[PROCESSED] = 0");

                entity.Property(e => e.SourceHash)
                    .ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<TranslationCache2>(entity =>
            {
                entity.ToTable("TranslationCache2");

                entity.HasKey(e => e.Id)
                      .HasName("PK_TranslationCache2");

                entity.HasIndex(e => new { e.StringId, e.TargetLanguage })
                    .IsUnique();
            });
        }
        public DbSet<BackofficeStatic> BackofficeStatic { get; set; }
        public DbSet<SportsbookStatic> SportsbookStatic { get; set; }
        public DbSet<SportsbookGlossary> SportsbookGlossary { get; set; }
        public DbSet<TranslationCache2> TranslationCache2 { get; set; }

    }
}
