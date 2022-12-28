using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;


namespace Backoffice.Web.Components.OddsEngine.BoostedSettings
{
    public partial class BoostedSettingsRow
    {
        [Parameter] public BoostedSettingsModel BoostedSettings { get; set; }
        [Parameter] public Guid ClientId { get; set; }

        public async Task OnEditSetting(Guid marketTemplateGroupId)
        {
            try
            {
                var parameters = new ModalParameters();

                parameters.Add("ClientId", ClientId);
                parameters.Add("ClientCode", BoostedSettings.ClientCode);
                parameters.Add("ClientName", BoostedSettings.ClientName);
                parameters.Add("Cap", BoostedSettings.Cap);

                var options = new ModalOptions();
                //ModalService.Open<CreateOrUpdateBoostedSettings>("Edit % Cap of Current Odds from Boosted Odds", parameters, options);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
