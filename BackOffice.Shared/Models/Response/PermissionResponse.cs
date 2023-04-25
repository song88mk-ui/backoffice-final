using Backoffice.Shared.Enums;
using System.Collections.Generic;

namespace Backoffice.Shared.Models.Response
{
    public class PermissionResponse
    {
        public PermissionResponse()
        {
            Permissions = new List<PermissionModel>();
        }
        public StatusCode StatusCode { get; set; }
        public List<PermissionModel> Permissions { get; set; }
    }
}
