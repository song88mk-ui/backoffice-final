using Backoffice.Web.Models.Validation;
using System.ComponentModel.DataAnnotations;

namespace Backoffice.Web.Models.OddsEngine.Mapping
{
    public class MappingSetupItemModel
    {
        public string Id { get; set; }

        [MinLength(1, ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [MustHaveOneElement(ErrorMessage = "At least one provider is required.")]
        public List<MappingProviderItemModel> Providers { get; set; }
    }
}