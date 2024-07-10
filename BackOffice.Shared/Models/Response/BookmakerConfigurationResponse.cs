using System;
using System.Collections.Generic;
using System.Text;

namespace Backoffice.Shared.Models.Response
{
    public class BookmakerConfigurationResponse
    {
        public BookmakerConfigurationResponse()
        {
            Bookmakers = new List<BookmakerConfiguration>();
        }

        public List<BookmakerConfiguration> Bookmakers { get; set; }

        #region Nested Class

        public class BookmakerConfiguration
        {
            public int BookmakerId { get; set; }
            public string BookmakerName { get; set; }
        }

        #endregion
    }
}
