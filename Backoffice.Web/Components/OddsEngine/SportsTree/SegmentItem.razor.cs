using Backoffice.Web.Models.OddsEngine.Monitoring;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.SportsTree
{
    public partial class SegmentItem
    {
        [Parameter] public SegmentItemModel Segment { get; set; }
        [Parameter] public EventCallback<int> OnSegmentClickCallback { get; set; }

        private bool _init = true;
        private bool _shouldRender = true;

        private bool _isSelected;
        private string _cssArrowIcon;

        protected override Task OnParametersSetAsync()
        {
            _shouldRender = ValidateParameter();

            if (_shouldRender)
            {
                _isSelected = Segment.IsSelected;

                _cssArrowIcon = _isSelected ? "fa-chevron-up" : "fa-chevron-down";
            }

            return Task.CompletedTask;
        }

        private bool ValidateParameter() => _init || _isSelected != Segment.IsSelected || Segment.ShouldRender;
        private async Task HandleArrowClick()
        {
            _isSelected = !_isSelected;
            Segment.IsSelected = _isSelected;

            if (_isSelected)
                await OnSegmentClickCallback.InvokeAsync(Segment.Id);

            _shouldRender = true;
        }
    }
}
