using Backoffice.Shared.Data.Backoffice.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backoffice.Shared.Data.Backoffice
{
    public sealed class BackofficeContext : DbContext
    {
        public BackofficeContext(DbContextOptions<BackofficeContext> options) : base(options) { }
        public DbSet<ClientInformation> ClientInformation { get; set; }
    }
}
