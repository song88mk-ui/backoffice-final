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

        [MinLength(1, ErrorMessage = "ID is required.")]
        public string Id { get; set; }

        [MinLength(1, ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [MustHaveOneElement(ErrorMessage = "At least one provider is required.")]
        public List<MappingProviderItemModel> Providers { get; set; }
    }
}