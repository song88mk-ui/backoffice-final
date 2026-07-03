using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.Base;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Backoffice.Web.Pages.SBC
{
    public partial class StaticTranslation : BasePage
    {
        [Inject] public IBackofficeService BackofficeService { get; set; } = null!;
        [Inject] public IModalService ModalService { get; set; } = null!;
        [Inject] public TranslationState TranslationState { get; set; } = null!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

        private PageHeaderModel _pageHeaderModel = new PageHeaderModel()
        {
            Title = "Sportsbook Static Translation",
            IconClass = "ico ico-translations"
        };

        private Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
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
                    ResetPagination();
                }
            }
        }

        private string SelectedLanguageCode { get; set; } = "EN";

        private int CurrentPage { get; set; } = 1;
        private int PageSize { get; set; } = 10;

        private int TotalFilteredCount => FilteredTranslations.Count();
        private int TotalPages => (int)Math.Ceiling(TotalFilteredCount / (double)PageSize);

        private IEnumerable<int> PageNumbers
        {
            get
            {
                if (TotalPages <= 10)
                {
                    return Enumerable.Range(1, TotalPages);
                }

                var start = Math.Max(1, CurrentPage - 4);
                var end = Math.Min(TotalPages, start + 8);
                if (end - start < 8)
                {
                    start = Math.Max(1, end - 8);
                }

                return Enumerable.Range(start, end - start + 1);
            }
        }

        private IEnumerable<KeyValuePair<string, string>> FilteredTranslations =>
            Translations == null
                ? Enumerable.Empty<KeyValuePair<string, string>>()
                : string.IsNullOrWhiteSpace(SearchText)
                    ? Translations
                    : Translations.Where(t => t.Key.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                              t.Value.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        private IEnumerable<KeyValuePair<string, string>> PagedTranslations =>
            FilteredTranslations
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize);

        protected override async Task OnInitializedAsync()
        {
            BackofficeService.Page = SportsbookClientPermission.SBC_OE_Config.ToString().Replace("_", ".");

            await LoadTranslations(SelectedLanguageCode);
            await base.OnInitializedAsync();
        }

        private async Task LoadTranslations(string languageCode)
        {
            var response = await BackofficeService.GetSportsbookTranslations(languageCode);
            if (response != null && response.StatusCode == StatusCode.Success && response.Result != null)
            {
                Translations = response.Result.ToDictionary(t => t.Name, t => t.Value);
                _errorMessage = null;
            }
            else
            {
                Translations = new Dictionary<string, string>();
                _errorMessage = response?.Message ?? "Failed to load translations.";
            }

            ResetPagination();
        }

        private async Task OnLanguageChanged(ChangeEventArgs e)
        {
            if (e.Value != null)
            {
                SelectedLanguageCode = e.Value?.ToString() ?? SelectedLanguageCode;
                await LoadTranslations(SelectedLanguageCode);
            }
        }

        private async Task OnAddTranslation()
        {
            var parameters = new ModalParameters();
            parameters.Add("TranslationKey", string.Empty);
            parameters.Add("TranslationValue", string.Empty);
            parameters.Add("IsNew", true);
            parameters.Add("OnSaveCallback", (Func<string, string, Task<bool>>)(async (key, newValue) => await SaveTranslationValue(key, newValue)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<StaticTranslationEditModal>("Add Translation", parameters, options);
        }

        private async Task OnEditTranslation(KeyValuePair<string, string> translation)
        {
            var parameters = new ModalParameters();
            parameters.Add("TranslationKey", translation.Key);
            parameters.Add("TranslationValue", translation.Value);
            parameters.Add("IsNew", false);
            parameters.Add("OnSaveCallback", (Func<string, string, Task<bool>>)(async (key, newValue) => await SaveTranslationValue(key, newValue)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<StaticTranslationEditModal>("Edit Translation", parameters, options);
        }

        private async Task OnDeleteTranslation(KeyValuePair<string, string> translation)
        {
            var parameters = new ModalParameters();
            parameters.Add("TranslationKey", translation.Key);
            parameters.Add("OnDeleteCallback", (Func<Task<bool>>)(async () => await DeleteTranslationValue(translation.Key)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<StaticTranslationDeleteModal>("Delete Translation", parameters, options);
        }

        private async Task<bool> SaveTranslationValue(string key, string newValue)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var response = await BackofficeService.AddOrUpdateSportsbookTranslation(new AddOrUpdateSportsbookTranslationRequest
            {
                LanguageCode = SelectedLanguageCode,
                Name = key,
                Value = newValue
            });

            var success = response != null && response.StatusCode == StatusCode.Success;

            if (success)
            {
                Translations[key] = newValue;
                _errorMessage = null;
            }
            else
            {
                _errorMessage = response?.Message ?? "Failed to save translation.";
            }

            await InvokeAsync(StateHasChanged);
            return success;
        }

        private async Task<bool> DeleteTranslationValue(string key)
        {
            var response = await BackofficeService.DeleteSportsbookTranslation(new DeleteSportsbookTranslationRequest
            {
                LanguageCode = SelectedLanguageCode,
                Name = key
            });

            var success = response != null && response.StatusCode == StatusCode.Success;

            if (success)
            {
                if (Translations.ContainsKey(key))
                {
                    Translations.Remove(key);
                    EnsureValidCurrentPage();
                }

                _errorMessage = null;
            }
            else
            {
                _errorMessage = response?.Message ?? "Failed to delete translation.";
            }

            await InvokeAsync(StateHasChanged);
            return success;
        }

        private void ResetPagination()
        {
            CurrentPage = 1;
        }

        private void EnsureValidCurrentPage()
        {
            if (TotalPages == 0)
            {
                CurrentPage = 1;
                return;
            }

            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }

            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }
        }

        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }

        private void GoToPage(int page)
        {
            if (page < 1 || page > TotalPages)
            {
                return;
            }

            CurrentPage = page;
        }

        private void OnPageSizeChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var newSize) && newSize > 0)
            {
                PageSize = newSize;
                ResetPagination();
            }
        }

        private bool CanExportToExcel => FilteredTranslations.Any();

        private async Task OnExportToExcel()
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Key,Value");

            foreach (var translation in FilteredTranslations)
            {
                csvBuilder.AppendLine($"{EscapeCsv(translation.Key)},{EscapeCsv(translation.Value)}");
            }

            var fileName = $"StaticTranslations-{SelectedLanguageCode}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            var csvText = "\uFEFF" + csvBuilder.ToString();

            await JSRuntime.InvokeVoidAsync("downloadFileFromText", fileName, csvText, "text/csv");
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var escaped = value.Replace("\"", "\"\"");
            if (escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r'))
            {
                escaped = $"\"{escaped}\"";
            }

            return escaped;
        }
    }
}