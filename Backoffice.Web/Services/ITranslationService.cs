using Backoffice.Shared.Enums;
using System.ComponentModel;

namespace Backoffice.Web.Services
{
    public interface ITranslationService
    {

        public Task<Dictionary<string, string>> GetTranslationsAsync(LanguageCode langCode);
    }
}
