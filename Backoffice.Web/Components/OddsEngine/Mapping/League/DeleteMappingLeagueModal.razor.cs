using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Backoffice.Web.Services;
using Backoffice.Web.Components.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Backoffice.Web.Components.OddsEngine.Mapping.League
{
    public partial class DeleteMappingLeagueModal : IDisposable
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [CascadingParameter] public ModalParameters ModalParameters { get; set; }
        [CascadingParameter] private CustomModal Modal { get; set; }
        private bool _isPreLoader = true;
        private bool _isSportMappingFound = false;
        private AlertModel _alertModel;
        private string _leagueId;
        private List<MappingProviderItemModel> _providers = new List<MappingProviderItemModel>();
        private Dictionary<int, string> providerIdToName = new Dictionary<int, string>
        {
            {
                0, "Sports Radar"
            },
            {
                1, "Bet Genius"
            },
        };

        private string _leagueName;

        protected override async Task OnInitializedAsync()
        {
            _leagueName = ModalParameters.TryGet<string>("LeNa");
            var mappedResponse = await _providerMappingService.GetLeagueMappingByLeId(ModalParameters.TryGet<int>("LeId"));
            var leagueMapping = mappedResponse.Result;

            if (leagueMapping == null)
            {
                _alertModel = new AlertModel()
                {
                    Status = AlertStatus.Error,
                    Message = "Sorry! Unable to find mapping for this sport.",
                    Opacity = 1.0m,
                };
                return;
            }
            _leagueId = leagueMapping.LeId.ToString();

            var providerMappings = new[]
            {
                new { Info = leagueMapping.Info.SrId, ChannelType = ChannelType.SR },
                new { Info = leagueMapping.Info.BgId, ChannelType = ChannelType.BG }
            };
            foreach (var mapping in providerMappings)
            {
                // if (mapping.Info != null && mapping.Info.Id > 0)
                // {
                // 	// _providers.Add(new MappingProviderItemModel()
                // 	// {
                // 	// 	SpId = mapping.Info.Id.ToString(),
                // 	// 	SpNa = mapping.Info.Name,
                // 	// 	ChTyId = (int)mapping.ChannelType,
                // 	// 	OtPrLi = mapping.Info.OtPrLi,
                // 	// });
                // }
            }

            _isPreLoader = false;
        }
        private async Task HandleDeleteMappingClick()
        {
            var response = await _providerMappingService.DeleteLeagueMapping(_leagueId, _userState.Model.Id);

            if (response != null && response.StatusCode == StatusCode.Success)
            {
                ModalService.Cancel();

                await Modal.OnSaveClickCallback.InvokeAsync();
            }
            else
            {
                _alertModel = new AlertModel()
                {
                    Status = AlertStatus.Error,
                    Message = response?.Message ?? "Something went wrong. Please try again!",
                    Opacity = 1.0m,
                };
            }
        }
        public void OnCloseClickEvent()
        {
            ModalService.Cancel();
        }

        public void OnDeleteClickEvent()
        {
            ModalService.Cancel();
        }
        public void Dispose()
        {
        }
    }
}