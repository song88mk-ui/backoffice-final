using Backoffice.API.Repositories;
using Backoffice.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Backoffice.API.Controllers.Authentication
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private IAuthenticationRepository _authenticationRepository { get; set; }
        public AuthenticationController(IAuthenticationRepository authenticationRepository)
        {
            _authenticationRepository = authenticationRepository;
        }
        // POST api/<AuthenticationController>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LogInModel content)
        {
            var result = await this._authenticationRepository.Login(content);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpGet("authenticate")]
        public async Task<IActionResult> Authenticate()
        {
            var result = new BaseResponse<TokenModel>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                result = await this._authenticationRepository.Authenticate(token);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            var result = new BaseResponse<bool>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                result = await this._authenticationRepository.Logout(token);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
    }
}
