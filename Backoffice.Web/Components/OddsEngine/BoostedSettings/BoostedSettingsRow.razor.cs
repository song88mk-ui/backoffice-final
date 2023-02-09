using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;


namespace Backoffice.Web.Components.OddsEngine.BoostedSettings
{
    public partial class BoostedSettingsRow
    {
        #region Properties

        [Inject] IModalService ModalService { get; set; }

        [Parameter] public BoostedSettingsModel BoostedSettings { get; set; }
        [Parameter] public Guid ClientId { get; set; }

        #endregion

        #region Methods

        public async Task OnEditSetting(BoostedSettingsModel client)
        {
            try
            {
                var parameters = new ModalParameters();

                parameters.Add("Client", client);

                var options = new ModalOptions();
                ModalService.Open<CreateOrUpdateBoostedSettings>("Edit % Cap of Current Odds from Boosted Odds", parameters, options);
            }
            catch (Exception ex)
            {

            }
        }

        public async Task OnViewLogs(BoostedSettingsModel client)
        {
            try
            {
                var parameters = new ModalParameters();

                parameters.Add("Client", client);
                parameters.Add("DialogStyle", "max-width: 80%; overflow-y: initial !important");

                var options = new ModalOptions();
                ModalService.Open<ViewLogsBoostedSettingModalBody>($"View Logs for {client.ClientName}", parameters, options);
            }
            catch (Exception ex)
            {

            }
        }

        #endregion
    }
}
