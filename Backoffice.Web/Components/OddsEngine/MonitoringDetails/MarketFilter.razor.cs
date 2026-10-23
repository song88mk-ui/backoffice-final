using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.MonitoringDetails
{
    public partial class MarketFilter : IDisposable
    {
        #region Properties

        [Parameter] public List<MarketNavigationModel2> MarketTemplates { get; set; }
        [Parameter] public EventCallback<MonitoringDetailsMarketTemplateSelectionModel> OnMarketClickCallback { get; set; }

        #endregion

        #region Fields

        private bool _init = true;
        private bool _shouldRender = true;
        private LanguageCode _languageCode = default;

        #endregion

        #region Methods

        protected override async Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
            {
                if (MarketTemplates != null && MarketTemplates.Any())
                {
                    if (!MarketTemplates.Any(c => c.IsSelected))
                    {
                        var model = MarketTemplates.FirstOrDefault();

                        model.IsSelected = true;

                        await OnMarketClickCallback.InvokeAsync(new MonitoringDetailsMarketTemplateSelectionModel() { MarketId = model.MarketTemplateId, MarketName = model.MarketName });
                    }
                }
            }
            _init = false;
        }
        protected override async Task OnAfterRenderAsync(bool firstRender) => _shouldRender = false;
        protected override bool ShouldRender() => _shouldRender;
        private bool ValidateParameters() => _init || _languageCode != _translationState.ActiveLanguageCode;
        public async Task OnMarketTemplateClickEvent(MarketNavigationModel2 model)
        {
            if (model.IsSelected)
                return;

            MarketTemplates.ForEach(s =>
            {
                s.IsSelected = false;
            });

            model.IsSelected = true;

            await OnMarketClickCallback.InvokeAsync(new MonitoringDetailsMarketTemplateSelectionModel() { MarketId = model.MarketTemplateId, MarketName = model.MarketName });
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
