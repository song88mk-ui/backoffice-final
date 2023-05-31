using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Backoffice.Shared.Data.Backoffice.Entities
{
    public class ConfigTemplate
    {
        [Key]
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public string ClientCodes { get; set; }
        public string LeagueIds { get; set; }
    }
}
