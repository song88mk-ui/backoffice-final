using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
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

        private class TranslationRow
        {
            public string Key { get; set; }
            public string SourceText { get; set; }
            public string TargetText { get; set; }
        }

        private Dictionary<string, string> SourceTranslations { get; set; } = new Dictionary<string, string>();
        private Dictionary<string, string> TargetTranslations { get; set; } = new Dictionary<string, string>();
        private string _errorMessage;

        private List<LanguageTranslationStat> _stats = new List<LanguageTranslationStat>();

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

        private bool _exactMatch;
        private bool ExactMatch
        {
            get => _exactMatch;
            set
            {
                if (_exactMatch != value)
                {
                    _exactMatch = value;
                    ResetPagination();
                }
            }
        }

        private bool _showOnlyUntranslated;
        private bool ShowOnlyUntranslated
        {
            get => _showOnlyUntranslated;
            set
            {
                if (_showOnlyUntranslated != value)
                {
                    _showOnlyUntranslated = value;
                    ResetPagination();
                }
            }
        }

        private string SelectedLanguageCode { get; set; } = "ZH";

        private int CurrentPage { get; set; } = 1;
        private int PageSize { get; set; } = 10;

        private int TotalFilteredCount => FilteredRows.Count();
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

        // EN is the source language: rows join the EN dictionary (Source Text) with the
        // selected language's dictionary (Translation) by key, mirroring the Dynamic
        // Translation grid. GetSportsbookTranslations already returns every known key
        // (blank value for gaps), so both dictionaries carry the same key set.
        private IEnumerable<TranslationRow> Rows =>
            SourceTranslations.Keys.Union(TargetTranslations.Keys)
                .OrderBy(k => k)
                .Select(k => new TranslationRow
                {
                    Key = k,
                    SourceText = SourceTranslations.TryGetValue(k, out var source) ? source : string.Empty,
                    TargetText = TargetTranslations.TryGetValue(k, out var target) ? target : string.Empty
                });

        private IEnumerable<TranslationRow> FilteredRows
        {
            get
            {
                var result = Rows;

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    result = ExactMatch
                        ? result.Where(r => string.Equals(r.Key, SearchText, StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(r.SourceText, SearchText, StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(r.TargetText, SearchText, StringComparison.OrdinalIgnoreCase))
                        : result.Where(r => r.Key.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                            r.SourceText.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                            r.TargetText.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
                }

                if (ShowOnlyUntranslated)
                {
                    result = result.Where(r => string.IsNullOrWhiteSpace(r.TargetText));
                }

                return result;
            }
        }

        private IEnumerable<TranslationRow> PagedRows =>
            FilteredRows
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize);

        protected override async Task OnInitializedAsync()
        {
            BackofficeService.Page = TranslationsPermission.Translations_Static.ToString().Replace("_", ".");

            await LoadTranslations(SelectedLanguageCode);
            await LoadStats();
            await base.OnInitializedAsync();
        }

        private async Task LoadStats()
        {
            var response = await BackofficeService.GetSportsbookTranslationStats();
            _stats = response != null && response.StatusCode == StatusCode.Success && response.Result != null
                ? response.Result.Languages.Where(l => !string.Equals(l.LanguageCode, "EN", StringComparison.OrdinalIgnoreCase)).ToList()
                : new List<LanguageTranslationStat>();
        }

        private async Task LoadTranslations(string languageCode)
        {
            var sourceResponse = await BackofficeService.GetSportsbookTranslations("EN");
            var targetResponse = await BackofficeService.GetSportsbookTranslations(languageCode);

            if (sourceResponse != null && sourceResponse.StatusCode == StatusCode.Success && sourceResponse.Result != null &&
                targetResponse != null && targetResponse.StatusCode == StatusCode.Success && targetResponse.Result != null)
            {
                SourceTranslations = sourceResponse.Result.ToDictionary(t => t.Name, t => t.Value ?? string.Empty);
                TargetTranslations = targetResponse.Result.ToDictionary(t => t.Name, t => t.Value ?? string.Empty);
                _errorMessage = null;
            }
            else
            {
                SourceTranslations = new Dictionary<string, string>();
                TargetTranslations = new Dictionary<string, string>();
                _errorMessage = sourceResponse?.Message ?? targetResponse?.Message ?? "Failed to load translations.";
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
            parameters.Add("SourceText", string.Empty);
            parameters.Add("TargetText", string.Empty);
            parameters.Add("SelectedLanguageCode", SelectedLanguageCode);
            parameters.Add("IsNew", true);
            parameters.Add("OnSaveCallback", (Func<string, string, string, Task<bool>>)(async (key, sourceText, targetText) => await SaveTranslation(key, sourceText, targetText)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<StaticTranslationEditModal>("Add Translation", parameters, options);
        }

        private async Task OnEditTranslation(TranslationRow row)
        {
            var parameters = new ModalParameters();
            parameters.Add("TranslationKey", row.Key);
            parameters.Add("SourceText", row.SourceText);
            parameters.Add("TargetText", row.TargetText);
            parameters.Add("SelectedLanguageCode", SelectedLanguageCode);
            parameters.Add("IsNew", false);
            parameters.Add("OnSaveCallback", (Func<string, string, string, Task<bool>>)(async (key, sourceText, targetText) => await SaveTranslation(key, sourceText, targetText)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<StaticTranslationEditModal>("Edit Translation", parameters, options);
        }

        private async Task OnDeleteTranslation(TranslationRow row)
        {
            var parameters = new ModalParameters();
            parameters.Add("TranslationKey", row.Key);
            parameters.Add("OnDeleteCallback", (Func<Task<bool>>)(async () => await DeleteTranslationKey(row.Key)));
            parameters.Add("ModalClass", "modal-center modal-show");
            parameters.Add("ModalDialogClass", "modal-md");

            var options = new ModalOptions();
            ModalService.Open<StaticTranslationDeleteModal>("Delete Translation", parameters, options);
        }

        // Saves the EN source text and the selected language's translation, issuing one
        // AddOrUpdate call per language that actually changed (values are compared against
        // the loaded dictionaries, so an unchanged field costs no API call).
        private async Task<bool> SaveTranslation(string key, string sourceText, string targetText)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(sourceText))
            {
                return false;
            }

            var newTarget = targetText ?? string.Empty;
            var oldSource = SourceTranslations.TryGetValue(key, out var existingSource) ? existingSource : string.Empty;
            var oldTarget = TargetTranslations.TryGetValue(key, out var existingTarget) ? existingTarget : string.Empty;

            if (sourceText != oldSource)
            {
                var response = await BackofficeService.AddOrUpdateSportsbookTranslation(new AddOrUpdateSportsbookTranslationRequest
                {
                    LanguageCode = "EN",
                    Name = key,
                    Value = sourceText
                });

                if (response == null || response.StatusCode != StatusCode.Success)
                {
                    _errorMessage = response?.Message ?? "Failed to save the source text.";
                    await InvokeAsync(StateHasChanged);
                    return false;
                }

                SourceTranslations[key] = sourceText;
            }

            if (newTarget != oldTarget)
            {
                var response = await BackofficeService.AddOrUpdateSportsbookTranslation(new AddOrUpdateSportsbookTranslationRequest
                {
                    LanguageCode = SelectedLanguageCode,
                    Name = key,
                    Value = newTarget
                });

                if (response == null || response.StatusCode != StatusCode.Success)
                {
                    _errorMessage = response?.Message ?? "Failed to save translation.";
                    await InvokeAsync(StateHasChanged);
                    return false;
                }

                TargetTranslations[key] = newTarget;
            }

            if (!TargetTranslations.ContainsKey(key))
            {
                TargetTranslations[key] = string.Empty;
            }

            _errorMessage = null;
            await LoadStats();
            await InvokeAsync(StateHasChanged);
            return true;
        }

        private async Task<bool> DeleteTranslationKey(string key)
        {
            var response = await BackofficeService.DeleteSportsbookTranslationKey(new DeleteSportsbookTranslationKeyRequest
            {
                Name = key
            });

            var success = response != null && response.StatusCode == StatusCode.Success;

            if (success)
            {
                SourceTranslations.Remove(key);
                TargetTranslations.Remove(key);
                EnsureValidCurrentPage();

                _errorMessage = null;
                await LoadStats();
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

        private bool CanExportToExcel => FilteredRows.Any();

        private async Task OnExportToExcel()
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine($"Key,EN,{SelectedLanguageCode}");

            foreach (var row in FilteredRows)
            {
                csvBuilder.AppendLine($"{EscapeCsv(row.Key)},{EscapeCsv(row.SourceText)},{EscapeCsv(row.TargetText)}");
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