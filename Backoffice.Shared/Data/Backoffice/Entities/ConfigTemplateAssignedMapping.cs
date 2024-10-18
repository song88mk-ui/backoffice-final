using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Backoffice.Shared.Data.Backoffice.Entities
{
    public class ConfigTemplateAssignedMapping
    {
        public long ConfigTemplateId { get; set; }
        public long ConfigTemplateAssignedId { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
    }
}
