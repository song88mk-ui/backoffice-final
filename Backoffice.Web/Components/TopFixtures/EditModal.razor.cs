using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.TopFixtures
{
    public partial class EditModal : IDisposable
    {
        #region Properties
        [CascadingParameter] private ModalParameters ModalParameters { get; set; }
        public int ClientId { get; set; }
        #endregion
        #region Methods
        protected override async Task OnInitializedAsync()
        {
            try
            {
                ClientId = ModalParameters.TryGet<int>("ClientId");
            }
            catch (Exception)
            {

            }
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
