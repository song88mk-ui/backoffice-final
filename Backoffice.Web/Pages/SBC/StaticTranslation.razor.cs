using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Web.Models;
using Backoffice.Web.Pages.Base;
using Backoffice.Web.Services;
using Backoffice.Web.States;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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

        private bool _isExporting;
        private string _importResultMessage;
        private bool _importHasErrors;
        private List<string> _importRowMessages = new List<string>();

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
            GoToKeyPage(key);
            await LoadStats();
            await InvokeAsync(StateHasChanged);
            return true;
        }

        // The grid is sorted by key and paginated, so a saved key usually lands on another
        // page; jump there so the user sees the row they just saved. If the active filters
        // exclude it (e.g. untranslated-only and the row is now translated), stay put.
        private void GoToKeyPage(string key)
        {
            var index = FilteredRows.Select(r => r.Key).ToList().IndexOf(key);
            if (index >= 0)
            {
                CurrentPage = (index / PageSize) + 1;
            }
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

        // Downloads every static translation key with all supported target languages in one
        // file (Key, EN, ZH, JA, BR), mirroring the Dynamic Translation "Export to Excel" CSV
        // so the same file also works as the Upload CSV template.
        private async Task OnExportToExcel()
        {
            _isExporting = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                var response = await BackofficeService.ExportSportsbookTranslations();

                if (response == null || response.StatusCode != StatusCode.Success || response.Result == null)
                {
                    _errorMessage = response?.Message ?? "Failed to export the translations.";
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                _errorMessage = null;
                var csvText = StaticTranslationCsvHelper.BuildCsv(response.Result);
                var fileName = $"StaticTranslations-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

                await JSRuntime.InvokeVoidAsync("downloadFileFromText", fileName, csvText, "text/csv");
            }
            finally
            {
                _isExporting = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        // Parses the uploaded CSV client-side (header-validated, same as Dynamic Translation),
        // then posts the structured rows to the import endpoint and surfaces the per-row summary.
        private async Task OnImportFileSelected(InputFileChangeEventArgs e)
        {
            _importResultMessage = null;
            _importRowMessages = new List<string>();
            _importHasErrors = false;

            var file = e.File;
            if (file == null)
            {
                return;
            }

            string csvText;
            using (var stream = file.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                csvText = await reader.ReadToEndAsync();
            }

            var (success, parseError, rows) = StaticTranslationCsvHelper.ParseCsv(csvText);
            if (!success)
            {
                _importHasErrors = true;
                _importResultMessage = parseError;
                await InvokeAsync(StateHasChanged);
                return;
            }

            var response = await BackofficeService.ImportSportsbookTranslations(new ImportSportsbookTranslationRequest { Rows = rows });

            if (response == null || response.StatusCode != StatusCode.Success || response.Result == null)
            {
                _importHasErrors = true;
                _importResultMessage = response?.Message ?? "Failed to import the translation file.";
                await InvokeAsync(StateHasChanged);
                return;
            }

            var summary = response.Result;
            _importHasErrors = summary.SkippedCount > 0;
            _importResultMessage = $"Import complete: {summary.CreatedCount} created, {summary.UpdatedCount} updated, {summary.UnchangedCount} unchanged, {summary.SkippedCount} skipped.";
            _importRowMessages = summary.RowMessages ?? new List<string>();

            await LoadTranslations(SelectedLanguageCode);
            await LoadStats();
            await InvokeAsync(StateHasChanged);
        }
    }
}