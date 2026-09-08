using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.ManageBoost
{
    public partial class EditBoostedOddsModalRow : IDisposable
    {
        #region Properties
        [Parameter] public List<BoostedSelectionTranslation> BoostedSelectionTranslationModel { get; set; }
        [Parameter] public LanguagesModel LanguagesModel { get; set; }
        [Parameter] public EventCallback<Tuple<int, Language>> OnSelectLanguageCallback { get; set; }
        [Parameter] public EventCallback OnAddSelectionCallback { get; set; }
        [Parameter] public EventCallback<BoostedSelectionTranslation> OnDeleteSelectionCallback { get; set; }
        #endregion

        #region Methods
        public async Task OnSelectLanguage(int index, Language language)
        {
            await OnSelectLanguageCallback.InvokeAsync(new Tuple<int, Language>(index, language));
        }

        public async Task OnAddSelection()
        {
            await OnAddSelectionCallback.InvokeAsync();
        }

        public async Task OnDeleteSelection(BoostedSelectionTranslation selection)
        {
            await OnDeleteSelectionCallback.InvokeAsync(selection);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Utilities
        public string FormatLanguageClass(Language language)
        {
            return $"ico-flag flag-{language.LanguageCode.ToLower()}";
        }
        #endregion
    }
}
