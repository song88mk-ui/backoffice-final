using Backoffice.Shared.Models;
using System.Security.Claims;

namespace Backoffice.Web.Models.Shared
{
    public class UserModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string MainUserId { get; set; }
        public string AccessToken { get; set; }
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public ClaimsPrincipal? ClaimsPrincipal { get; set; }
        //public List<string> ClientCodes { get; set; } = new List<string>();
        public List<ClientInformationModel> TradingClients { get; set; } = new List<ClientInformationModel>();
        public List<Claim> Claims { get; set; } = new List<Claim>();
        public List<Client> SportsbookClients { get; set; } = new List<Client>();
        public bool IsAuthenticated {  get; set; }
    }
}
