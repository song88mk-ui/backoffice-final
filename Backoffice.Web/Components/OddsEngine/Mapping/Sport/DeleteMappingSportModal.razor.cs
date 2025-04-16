using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Models.OddsEngine.Mapping;
using Backoffice.Web.Services;
using Backoffice.Web.Components.Common;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Components.OddsEngine.Mapping.Sport
{
    public partial class DeleteMappingSportModal: IDisposable
    {
		[Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IModalService ModalService { get; set; }
		[CascadingParameter] public ModalParameters ModalParameters { get; set; }
		[CascadingParameter] private CustomModal Modal { get; set; }
		private bool _isPreLoader = true;
		private bool _isSportMappingFound = false;
		private AlertModel _alertModel;
		private string _sportId;
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
		protected override async Task OnInitializedAsync()
        {
            var mappedResponse = await _providerMappingService.GetSportMappingBySpId(ModalParameters.TryGet<int>("SpId"));
            var sportMapping = mappedResponse.Result;

            if (sportMapping == null)
            {
                _alertModel = new AlertModel()
                {
                    Status = AlertStatus.Error,
                    Message = "Sorry! Unable to find mapping for this sport.",
                    Opacity = 1.0m,
                };
                _isPreLoader = false;
                return;
			}
			_sportId = sportMapping.SpId.ToString();

			var providerMappings = new[]
			{
				new { Info = sportMapping.Info.SrId, ChannelType = ChannelType.SR },
				new { Info = sportMapping.Info.BgId, ChannelType = ChannelType.BG }
			};
			
			foreach (var mapping in providerMappings)
			{
				if (mapping.Info != null && mapping.Info.Id > 0)
				{
					_providers.Add(new MappingProviderItemModel()
					{
						SpId = mapping.Info.Id.ToString(),
						SpNa = mapping.Info.Name,
						ChTyId = (int)mapping.ChannelType,
						OtPrLi = mapping.Info.OtPrLi,
					});
				}
			}

			_isPreLoader = false;
            _isSportMappingFound = true;
        }
		private async Task HandleDeleteMappingClick()
        {
			var response = await _providerMappingService.DeleteSportMapping(_sportId, _userState.Model.Id);
            
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
		public void Dispose()
		{
		}
    }
}