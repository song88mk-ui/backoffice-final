using System;
using System.Collections.Generic;
using System.Linq;
using Backoffice.Shared.Enums;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.Base;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;

namespace Backoffice.Web.Pages.SBC
{
    public partial class StaticTranslation : BasePage
    {
        [Inject] public IBackofficeService BackofficeService { get; set; }
        [Inject] public IModalService ModalService { get; set; }
        [Inject] public TranslationState TranslationState { get; set; }

        private PageHeaderModel _pageHeaderModel = new PageHeaderModel()
        {
            Title = "Sportsbook Static Translation",
            IconClass = "ico ico-translations"
        };

        private Dictionary<string, string> Translations { get; set; }

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
            var response = await BackofficeService.GetStaticTranslationSportsbook("EN");
            if (response != null && response.StatusCode == StatusCode.Success && response.Result != null)
            {
                Translations = response.Result.ToDictionary(t => t.Name, t => t.Value);
            }
            else
            {
                Translations = new Dictionary<string, string>();
            }

            ResetPagination();
            await base.OnInitializedAsync();
        }

        private async Task OnLanguageChanged(ChangeEventArgs e)
        {
            if (e.Value != null)
            {
                SelectedLanguageCode = e.Value.ToString();
                var response = await BackofficeService.GetStaticTranslationSportsbook(SelectedLanguageCode);
                if (response != null && response.StatusCode == StatusCode.Success && response.Result != null)
                {
                    Translations = response.Result.ToDictionary(t => t.Name, t => t.Value);
                }
                else
                {
                    Translations = new Dictionary<string, string>();
                }

                ResetPagination();
            }
        }

        private async Task OnAddTranslation()
        {
            var parameters = new ModalParameters();
            parameters.Add("TranslationKey", string.Empty);
            parameters.Add("TranslationValue", string.Empty);
            parameters.Add("IsNew", true);
            parameters.Add("OnSaveCallback", (Func<string, string, Task>)(async (key, newValue) => await SaveTranslationValue(key, newValue)));
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
            parameters.Add("OnSaveCallback", (Func<string, string, Task>)(async (key, newValue) => await SaveTranslationValue(key, newValue)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<StaticTranslationEditModal>("Edit Translation", parameters, options);
        }

        private async Task OnDeleteTranslation(KeyValuePair<string, string> translation)
        {
            var parameters = new ModalParameters();
            parameters.Add("TranslationKey", translation.Key);
            parameters.Add("OnDeleteCallback", (Func<Task>)(async () => await DeleteTranslationValue(translation.Key)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<StaticTranslationDeleteModal>("Delete Translation", parameters, options);
        }

        private Task SaveTranslationValue(string key, string newValue)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return Task.CompletedTask;
            }

            Translations[key] = newValue;
            return InvokeAsync(StateHasChanged);
        }

        private Task DeleteTranslationValue(string key)
        {
            if (Translations.ContainsKey(key))
            {
                Translations.Remove(key);
                EnsureValidCurrentPage();
            }

            return InvokeAsync(StateHasChanged);
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
    }
}