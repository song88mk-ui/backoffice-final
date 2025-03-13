using System;
using System.ComponentModel.DataAnnotations;
using Backoffice.Web.Models.Validation;
using Backoffice.Shared.Models.Response;

namespace Backoffice.Web.Models.OddsEngine.Mapping
{
    public class MappingSetupItemModel
    {
        public MappingSetupItemModel()
        {
            Id = Guid.NewGuid().ToString();
        }

        [Required(ErrorMessage = "ID is required.")]
        public string Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [MustHaveOneElement(ErrorMessage = "At least one provider is required.")]
        public List<UnmappedModel> Providers { get; set; }
    }
}