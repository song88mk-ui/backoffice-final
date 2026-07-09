using Backoffice.API.ActionFilter;
using Backoffice.API.Models;
using Backoffice.API.Repositories;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using AuthorizeAttribute = Backoffice.API.ActionFilter.AuthorizeAttribute;
using BoStatusCode = Backoffice.Shared.Enums.StatusCode;

namespace Backoffice.API.Controllers.Backoffice
{
    // No class-level [Authorize] here on purpose: each action below is gated on its own
    // Translations.Static/Translations.Dynamic claim. These endpoints used to live on
    // ConfigController, which carries a class-level [Authorize("SBC.OE.Config", View)] --
    // since the custom AuthorizeAttribute is a plain IAuthorizationFilter (not ASP.NET
    // Core's policy-merging [Authorize]), a controller-level instance and a method-level
    // instance both run independently, and the controller-scoped one executes first. A
    // user without SBC.OE.Config was rejected by the class-level check before their own,
    // correctly-satisfied Translations.* method-level check ever ran. Moving these actions
    // to their own controller with no restrictive class-level default avoids that entirely.
    [Route("api/[controller]")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class TranslationController : ControllerBase
    {
        private readonly IBackofficeRepository _backofficeRepository;
        public TranslationController(IBackofficeRepository backofficeRepository)
        {
            _backofficeRepository = backofficeRepository;
        }

        [HttpGet("sportsbook/translations")]
        [Authorize("Translations.Static", ((int)Access.View))]
        public async Task<IActionResult> GetSportsbookTranslations([FromQuery] string languageCode = "EN")
        {
            var result = await _backofficeRepository.GetSportsbookTranslations(languageCode);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("sportsbook/translations")]
        [Authorize("Translations.Static", ((int)Access.Edit))]
        public async Task<IActionResult> AddOrUpdateSportsbookTranslation([FromBody] AddOrUpdateSportsbookTranslationRequest request)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            var userName = HttpContext.Items["UserName"]?.ToString();

            var requester = new RequesterInfo()
            {
                UserId = userId,
                UserName = userName,
                Page = HttpContext.Request.Headers["x-page"].FirstOrDefault()
            };

            var result = await _backofficeRepository.AddOrUpdateSportsbookTranslation(request, requester);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("sportsbook/translations/delete")]
        [Authorize("Translations.Static", ((int)Access.Edit))]
        public async Task<IActionResult> DeleteSportsbookTranslation([FromBody] DeleteSportsbookTranslationRequest request)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            var userName = HttpContext.Items["UserName"]?.ToString();

            var requester = new RequesterInfo()
            {
                UserId = userId,
                UserName = userName,
                Page = HttpContext.Request.Headers["x-page"].FirstOrDefault()
            };

            var result = await _backofficeRepository.DeleteSportsbookTranslation(request, requester);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("sportsbook/glossary/list")]
        [Authorize("Translations.Dynamic", ((int)Access.View))]
        public async Task<IActionResult> GetSportsbookGlossary([FromBody] GetSportsbookGlossaryRequest request)
        {
            var result = await _backofficeRepository.GetSportsbookGlossary(request);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("sportsbook/glossary")]
        [Authorize("Translations.Dynamic", ((int)Access.Edit))]
        public async Task<IActionResult> AddOrUpdateSportsbookGlossary([FromBody] AddOrUpdateSportsbookGlossaryRequest request)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            var userName = HttpContext.Items["UserName"]?.ToString();

            var requester = new RequesterInfo()
            {
                UserId = userId,
                UserName = userName,
                Page = HttpContext.Request.Headers["x-page"].FirstOrDefault()
            };

            var result = await _backofficeRepository.AddOrUpdateSportsbookGlossary(request, requester);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        // Sets TranslationCache2.PROCESSED — the external translation pipeline's worklist flag
        // (false = queued for the pipeline's next pass, true = handled/skipped).
        [HttpPost("sportsbook/glossary/processed")]
        [Authorize("Translations.Dynamic", ((int)Access.Edit))]
        public async Task<IActionResult> SetSportsbookGlossaryProcessed([FromBody] SetSportsbookGlossaryProcessedRequest request)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            var userName = HttpContext.Items["UserName"]?.ToString();

            var requester = new RequesterInfo()
            {
                UserId = userId,
                UserName = userName,
                Page = HttpContext.Request.Headers["x-page"].FirstOrDefault()
            };

            var result = await _backofficeRepository.SetSportsbookGlossaryProcessed(request, requester);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("sportsbook/glossary/delete")]
        [Authorize("Translations.Dynamic", ((int)Access.Edit))]
        public async Task<IActionResult> DeleteSportsbookGlossary([FromBody] DeleteSportsbookGlossaryRequest request)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            var userName = HttpContext.Items["UserName"]?.ToString();

            var requester = new RequesterInfo()
            {
                UserId = userId,
                UserName = userName,
                Page = HttpContext.Request.Headers["x-page"].FirstOrDefault()
            };

            var result = await _backofficeRepository.DeleteSportsbookGlossary(request, requester);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        // Returns every glossary row as JSON (no pagination) -- Backoffice.Web builds the
        // downloadable CSV from this, the same way Static Translation's CSV export already
        // works entirely client-side.
        [HttpGet("sportsbook/glossary/export")]
        [Authorize("Translations.Dynamic", ((int)Access.View))]
        public async Task<IActionResult> ExportSportsbookGlossary([FromQuery] string searchText = null)
        {
            var result = await _backofficeRepository.ExportSportsbookGlossary(searchText);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        // Accepts glossary rows already parsed (and header-validated) client-side from the
        // uploaded export/template file -- this endpoint only ever deals with structured data.
        [HttpPost("sportsbook/glossary/import")]
        [Authorize("Translations.Dynamic", ((int)Access.Edit))]
        public async Task<IActionResult> ImportSportsbookGlossary([FromBody] ImportSportsbookGlossaryRequest request)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            var userName = HttpContext.Items["UserName"]?.ToString();

            var requester = new RequesterInfo()
            {
                UserId = userId,
                UserName = userName,
                Page = HttpContext.Request.Headers["x-page"].FirstOrDefault()
            };

            var result = await _backofficeRepository.ImportSportsbookGlossary(request, requester);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
    }
}
