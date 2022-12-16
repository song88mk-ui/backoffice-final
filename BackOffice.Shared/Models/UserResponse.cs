using System;

namespace Backoffice.Shared.Models
{
    public class UserResponse
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string NickName { get; set; }
        public string Status { get; set; }
        public string MainUserId { get; set; }
        public int AppId { get; set; }
        public string AppName { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
