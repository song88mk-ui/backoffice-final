using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.Base;
using Backoffice.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class DynamicTranslation : BasePage
    {
        [Inject] public IBackofficeService BackofficeService { get; set; } = null!;
        [Inject] public IModalService ModalService { get; set; } = null!;

        private PageHeaderModel _pageHeaderModel = new PageHeaderModel()
        {
            Title = "Dynamic Translation",
            IconClass = "ico ico-translations"
        };

        private List<SportsbookGlossaryResponse> Glossary { get; set; } = new List<SportsbookGlossaryResponse>();
        private string _errorMessage;

        private string _searchText = string.Empty;
        private string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    _ = LoadGlossary(resetPage: true);
                }
            }
        }

        private string SelectedLanguageCode { get; set; } = "EN";

        private MetaData MetaData { get; set; } = new MetaData { CurrentPage = 1, PageSize = 10 };

        private IEnumerable<int> PageNumbers
        {
            get
            {
                if (MetaData.TotalPages <= 10)
                {
                    return Enumerable.Range(1, Math.Max(MetaData.TotalPages, 0));
                }

                var start = Math.Max(1, MetaData.CurrentPage - 4);
                var end = Math.Min(MetaData.TotalPages, start + 8);
                if (end - start < 8)
                {
                    start = Math.Max(1, end - 8);
                }

                return Enumerable.Range(start, end - start + 1);
            }
        }

        protected override async Task OnInitializedAsync()
        {
            BackofficeService.Page = TranslationsPermission.Translations_Dynamic.ToString().Replace("_", ".");

            await LoadGlossary(resetPage: true);
            await base.OnInitializedAsync();
        }

        private async Task LoadGlossary(bool resetPage)
        {
            if (resetPage)
            {
                MetaData.CurrentPage = 1;
            }

            var response = await BackofficeService.GetSportsbookGlossary(new GetSportsbookGlossaryRequest
            {
                SearchText = SearchText,
                TargetLanguage = SelectedLanguageCode,
                MetaData = MetaData
            });

            if (response != null && response.StatusCode == StatusCode.Success && response.Result != null)
            {
                Glossary = response.Result.Glossary ?? new List<SportsbookGlossaryResponse>();
                MetaData = response.Result.MetaData ?? MetaData;
                _errorMessage = null;
            }
            else
            {
                Glossary = new List<SportsbookGlossaryResponse>();
                _errorMessage = response?.Message ?? "Failed to load glossary.";
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task OnLanguageChanged(ChangeEventArgs e)
        {
            if (e.Value != null)
            {
                SelectedLanguageCode = e.Value?.ToString() ?? SelectedLanguageCode;
                await LoadGlossary(resetPage: true);
            }
        }

        private async Task OnAddGlossary()
        {
            var parameters = new ModalParameters();
            parameters.Add("StringId", string.Empty);
            parameters.Add("SourceText", string.Empty);
            parameters.Add("TargetText", string.Empty);
            parameters.Add("SelectedLanguageCode", SelectedLanguageCode);
            parameters.Add("IsNew", true);
            parameters.Add("OnSaveCallback", (Func<string, string, string, Task<bool>>)((stringId, sourceText, targetText) => SaveGlossary(null, stringId, sourceText, targetText)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<DynamicTranslationEditModal>("Add Glossary Term", parameters, options);
        }

        private async Task OnEditGlossary(SportsbookGlossaryResponse entry)
        {
            var parameters = new ModalParameters();
            parameters.Add("StringId", entry.StringId);
            parameters.Add("SourceText", entry.SourceText);
            parameters.Add("TargetText", SelectedLanguageCode == "EN" ? string.Empty : entry.TargetText ?? string.Empty);
            parameters.Add("SelectedLanguageCode", SelectedLanguageCode);
            parameters.Add("IsNew", false);
            parameters.Add("OnSaveCallback", (Func<string, string, string, Task<bool>>)((stringId, sourceText, targetText) => SaveGlossary(entry.Id, stringId, sourceText, targetText)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<DynamicTranslationEditModal>("Edit Glossary Term", parameters, options);
        }

        private async Task OnDeleteGlossary(SportsbookGlossaryResponse entry)
        {
            var parameters = new ModalParameters();
            parameters.Add("StringId", entry.StringId);
            parameters.Add("OnDeleteCallback", (Func<Task<bool>>)(() => DeleteGlossary(entry.Id)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<DynamicTranslationDeleteModal>("Delete Glossary Term", parameters, options);
        }

        private async Task<bool> SaveGlossary(long? id, string stringId, string sourceText, string targetText)
        {
            if (string.IsNullOrWhiteSpace(stringId) || string.IsNullOrWhiteSpace(sourceText))
            {
                return false;
            }

            var response = await BackofficeService.AddOrUpdateSportsbookGlossary(new AddOrUpdateSportsbookGlossaryRequest
            {
                Id = id,
                StringId = stringId,
                SourceLanguage = "EN",
                SourceText = sourceText,
                TargetLanguage = SelectedLanguageCode,
                TargetText = targetText
            });

            if (response == null || response.StatusCode != StatusCode.Success)
            {
                _errorMessage = response?.Message ?? "Failed to save glossary entry.";
                await InvokeAsync(StateHasChanged);
                return false;
            }

            _errorMessage = null;
            await LoadGlossary(resetPage: false);
            return true;
        }

        private void OnToggleProcessed(SportsbookGlossaryResponse entry)
        {
            var parameters = new ModalParameters();
            parameters.Add("StringId", entry.StringId);
            parameters.Add("Processed", entry.Processed);
            parameters.Add("OnConfirmCallback", (Func<Task<bool>>)(() => SetProcessed(entry, !entry.Processed)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<DynamicTranslationProcessedModal>(
                entry.Processed ? "Re-queue for Translation" : "Mark as Processed", parameters, options);
        }

        private async Task<bool> SetProcessed(SportsbookGlossaryResponse entry, bool processed)
        {
            var response = await BackofficeService.SetSportsbookGlossaryProcessed(new SetSportsbookGlossaryProcessedRequest
            {
                Id = entry.Id,
                Processed = processed
            });

            if (response == null || response.StatusCode != StatusCode.Success)
            {
                _errorMessage = response?.Message ?? "Failed to update the Processed flag.";
                await InvokeAsync(StateHasChanged);
                return false;
            }

            _errorMessage = null;
            await LoadGlossary(resetPage: false);
            return true;
        }

        private async Task<bool> DeleteGlossary(long id)
        {
            var response = await BackofficeService.DeleteSportsbookGlossary(new DeleteSportsbookGlossaryRequest { Id = id });

            if (response == null || response.StatusCode != StatusCode.Success)
            {
                _errorMessage = response?.Message ?? "Failed to delete glossary entry.";
                await InvokeAsync(StateHasChanged);
                return false;
            }

            _errorMessage = null;
            await LoadGlossary(resetPage: false);
            return true;
        }

        private async Task PreviousPage()
        {
            if (MetaData.HasPrevious)
            {
                MetaData.CurrentPage--;
                await LoadGlossary(resetPage: false);
            }
        }

        private async Task NextPage()
        {
            if (MetaData.HasNext)
            {
                MetaData.CurrentPage++;
                await LoadGlossary(resetPage: false);
            }
        }

        private async Task GoToPage(int page)
        {
            if (page < 1 || page > MetaData.TotalPages || page == MetaData.CurrentPage)
            {
                return;
            }

            MetaData.CurrentPage = page;
            await LoadGlossary(resetPage: false);
        }

        private async Task OnPageSizeChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var newSize) && newSize > 0)
            {
                MetaData.PageSize = newSize;
                await LoadGlossary(resetPage: true);
            }
        }
    }
}
