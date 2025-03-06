using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Components.Web;

namespace Backoffice.Web.Models.OddsEngine.Mapping
{
    public enum ProviderType
    {
        SportsRadar = 1,
        BetGenius = 2
    }
    public class ProviderItemModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ProviderType Provider { get; set; }
    }
}