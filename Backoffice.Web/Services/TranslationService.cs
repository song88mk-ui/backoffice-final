using Backoffice.Shared.Enums;

namespace Backoffice.Web.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly IBackofficeService _backofficeService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TranslationService(IBackofficeService backofficeService, IHttpContextAccessor httpContextAccessor)
        {
            _backofficeService = backofficeService;
            _httpContextAccessor = httpContextAccessor;
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
