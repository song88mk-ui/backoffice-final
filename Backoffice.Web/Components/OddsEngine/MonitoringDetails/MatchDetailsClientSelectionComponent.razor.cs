using Backoffice.Shared.Enums;
using Microsoft.AspNetCore.Components;
using static Backoffice.Shared.Models.Response.MarketFixtureResponse;

namespace Backoffice.Web.Components.OddsEngine.MonitoringDetails
{
    public partial class MatchDetailsClientSelectionComponent
    {
        [Parameter] public SelectionModel Selection { get; set; }
        [Parameter] public string CurrentClientCode { get; set; }
        [Parameter] public int MarketStatusId { get; set; }
        [Parameter] public bool IsBetStopGlobal { get; set; }
        [Parameter] public bool HasConfig { get; set; }

        private string _color = "odds-bg-default";
        private string _odds;
        private long _unixTime;

        private bool _shouldRender = true;
        private bool _init = true;
        private bool _fading = true;
        private string _selectionId;

        private CancellationTokenSource _cts;

        protected override async Task OnParametersSetAsync()
        {
            var color = GetColor();

            _shouldRender = ValidateParameters() || color != _color;

            if (_selectionId != Selection.SelectionId)
            {
                _selectionId = Selection.SelectionId;
            }

            if (_shouldRender)
            {
                _unixTime = Selection.UnixTime;

                if (_color != color)
                {
                    _color = color;

                    _cts?.Cancel();
                    if (_fading)
                    {
                        _cts = new CancellationTokenSource();
                        await Task.Delay(4000, _cts.Token);

                        _color = "odds-bg-default";
                        _shouldRender = true;
                    }
                }
            }

            _init = false;
        }
        protected override async Task OnAfterRenderAsync(bool firstRender) => _shouldRender = false;
        protected override bool ShouldRender() => _shouldRender;

        private bool ValidateParameters() => _init || _unixTime != Selection.UnixTime;
        private string GetColor()
        {
            _fading = true;
            var color = "odds-bg-default";

            if (IsBetStopGlobal || MarketStatusId != (int)MarketStatus.Active)
            {
                _fading = false;
                if (MarketStatusId == (int)MarketStatus.Inactive)
                {
                    color = "odds-bg-inactive";
                }
                else
                {
                    color = "odds-bg-betstop";
                }
            }
            else
            {
                if (_selectionId != Selection.SelectionId)
                {
                    _selectionId = Selection.SelectionId;
                    _odds = GetClientOdds();
                    return color;
                }

                if (CurrentClientCode == null)
                {
                    if (!string.IsNullOrEmpty(_odds))
                    {
                        if (Convert.ToDecimal(_odds) > Convert.ToDecimal(Selection.DecimalOdds))
                        {
                            color = "odds-bg-danger-active";
                        }

                        if (Convert.ToDecimal(_odds) < Convert.ToDecimal(Selection.DecimalOdds))
                        {
                            color = "odds-bg-success-active";
                        }
                    }
                }
                else
                {
                    if (Selection.ClientOdds == null)
                        return color;

                    var clientOdds = Selection.ClientOdds.FirstOrDefault(c => c.ClientCode == CurrentClientCode && c.SelectionTemplateId == Selection.SelectionTemplateId);
                    if (clientOdds != null)
                    {
                        if (!string.IsNullOrEmpty(_odds))
                        {
                            if (Convert.ToDecimal(_odds) > clientOdds.Odds)
                            {
                                color = "odds-bg-danger-active";
                            }

                            if (Convert.ToDecimal(_odds) < clientOdds.Odds)
                            {
                                color = "odds-bg-success-active";
                            }
                        }
                    }
                }
            }

            _odds = GetClientOdds();
            return color;
        }
        private string GetClientOdds()
        {
            if (Selection.ClientOdds != null && Selection.ClientOdds.Any(c => c.ClientCode == CurrentClientCode))
            {
                return Selection.ClientOdds.Where(c => c.ClientCode == CurrentClientCode && c.SelectionTemplateId == Selection.SelectionTemplateId).FirstOrDefault().Odds.ToString("0.00");
            }

            return Selection.DecimalOdds;
        }
    }
}
