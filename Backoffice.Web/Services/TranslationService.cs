using Backoffice.Shared.Enums;
using Backoffice.Web.States;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.ComponentModel;

namespace Backoffice.Web.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly IBackofficeService _backofficeService;

        public TranslationService(IBackofficeService backofficeService)
        {
            _backofficeService = backofficeService;
        }

        public async Task<Dictionary<string, string>> GetTranslationsAsync(LanguageCode langCode)
        {
            var response = await _backofficeService.GetTranslation(langCode);

            if (response != null && response.StatusCode == StatusCode.Success)
            {
                return response.Result.Translations;
            }

            return new Dictionary<string, string>();
        }
    }
}
