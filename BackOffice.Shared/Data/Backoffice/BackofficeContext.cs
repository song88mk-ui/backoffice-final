using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Models;
using Microsoft.EntityFrameworkCore;
using static Backoffice.Shared.Models.Response.ClientListResponse;

namespace Backoffice.Shared.Data.Backoffice
{
    public sealed class BackofficeContext : DbContext
    {
        public BackofficeContext(DbContextOptions<BackofficeContext> options) : base(options) { }
        public DbSet<ClientInformation> ClientInformation { get; set; }

        public DbSet<ClientBetSetting> ClientBetSetting { get; set; }
    }
}
