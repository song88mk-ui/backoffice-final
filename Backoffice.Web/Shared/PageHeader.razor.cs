using Backoffice.Shared.Enums;
using Backoffice.Web.Interfaces.Pages;
using Backoffice.Web.Models;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Shared
{
    public partial class PageHeader : IDisposable, IRenderComponent
    {
        #region Properties

        [Parameter] public PageHeaderModel PageHeaderModel { get; set; }
        [Parameter] public string PageHeaderClass { get; set; }

        #endregion

        #region Fields

        private LanguageCode _activeLanguageCode;

        private string _title = "";
        private string _iconClass = "";

        private bool _showLanguageDropdown;
        private bool _init = true;
        private bool _shouldRender;

        #endregion

        #region Methods

        protected override void OnParametersSet()
        {
            _shouldRender = ValidateParameters();

            if (_shouldRender)
            {
                _activeLanguageCode = _translationState.ActiveLanguageCode;
                _title = PageHeaderModel.Title;
                _iconClass = PageHeaderModel.IconClass;
                _showLanguageDropdown = PageHeaderModel.ShowLanguageDropDown;
            }

            _init = false;
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            _shouldRender = false;

            return base.OnAfterRenderAsync(firstRender);
        }

        protected override bool ShouldRender() => _shouldRender;

        public bool ValidateParameters() => _init || PageHeaderModel.Title != _title || PageHeaderModel.IconClass != _iconClass || PageHeaderModel.ShowLanguageDropDown != _showLanguageDropdown || _translationState.ActiveLanguageCode != _activeLanguageCode;

        public void Dispose()
        {
            PageHeaderModel = null;
        }

        #endregion
    }
}