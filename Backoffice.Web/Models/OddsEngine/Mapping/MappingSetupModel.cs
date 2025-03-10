using System;
using System.ComponentModel.DataAnnotations;
using Backoffice.Web.Models.Validation;

namespace Backoffice.Web.Models.OddsEngine.Mapping
{
    public class MappingSetupModel
    {
        public MappingSetupModel()
        {
            MappedId = Guid.NewGuid().ToString();
        }

        [Required(ErrorMessage = "Mapped ID is required.")]
        public string MappedId { get; set; }

        [Required(ErrorMessage = "New Fixture Name is required.")]
        public string NewFixtureName { get; set; }

        [MustHaveOneElement(ErrorMessage = "At least one provider is required.")]
        public List<ProviderItemModel> Providers { get; set; }
    }
}