using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class PermissionResponse
    {
        public PermissionResponse()
        {
            Permissions = new List<PermissionModel>();
        }

        public List<PermissionModel> Permissions { get; set; }
    }
}
