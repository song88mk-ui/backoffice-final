using System;

namespace Backoffice.Shared.Data.Trading.SQLEntities
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string HashPassword { get; set; }
        public string SaltKey { get; set; }
    }
}
