using Backoffice.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class ClientTopFixtureMessageResponse
    {
        public StatusCode StatusCode { get; set; }
        public string Message { get; set; }
    }
}
