using Backoffice.API.Repositories;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
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
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LogInModel content)
        {
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString() ?? "";
            content.Provider = $"{(int)Application.Backoffice}";

            var result = await _authenticationRepository.Login(content, userAgent, GetRemoteIpAddress().Trim());

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
                result = await _authenticationRepository.Authenticate(token);
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
                result = await _authenticationRepository.Logout(token);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Create([FromBody] UserRegisterRequest request)
        {
            var result = new BaseResponse<long>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                result = await _authenticationRepository.Register(request, token);

            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("user/list")]
        public async Task<IActionResult> GetUserList([FromBody] UserGroupRequest request, [FromQuery] bool isShowSuperAdmin = false)
        {
            var result = new BaseResponse<UserListResponse>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                result = await _authenticationRepository.GetUserList(request, token, isShowSuperAdmin);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("user/modify")]
        public async Task<IActionResult> UpdateUser([FromBody] UserRegisterRequest request)
        {
            var result = new BaseResponse<bool>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                result = await _authenticationRepository.UpdateUser(request, token);

            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("user/modify/{userId}")]
        public async Task<IActionResult> UpdateUser(string userId,[FromBody] UserRegisterRequest request)
        {
            var result = new BaseResponse<bool>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                result = await _authenticationRepository.UpdateUserPassword(userId, request, token);

            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetCurrentUser(string userId)
        {
            var result = new BaseResponse<UserResponse>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                result = await _authenticationRepository.GetCurrentUser(userId, token);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("subaccount/list")]
        public async Task<IActionResult> GetSubAccounts([FromBody] SubAccountListRequest request)
        {
            var result = new BaseResponse<UserListResponse>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                result = await _authenticationRepository.GetSubAccounts(request, token);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("permission/list")]
        public async Task<IActionResult> GetPermissionList([FromQuery] bool isSubUser)
        {
            var result = new BaseResponse<PermissionResponse>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                result = await _authenticationRepository.GetPermissionList(isSubUser, token);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("usergroup/list")]
        public async Task<IActionResult> GetUserGroupList([FromBody] UserGroupListRequest request)
        {
            var result = new BaseResponse<UserGroupListResponse>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                result = await _authenticationRepository.GetUserGroupList(request, token);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("usergroup/user/list")]
        public async Task<IActionResult> GetUserGroupUsers([FromBody] UserGroupRequest request)
        {
            var result = new BaseResponse<UserListResponse>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                result = await _authenticationRepository.GetUserGroupUsers(request, token);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("usergroup/post")]
        public async Task<IActionResult> AddOrUpdateUserGroup([FromBody] UserGroupRequest request)
        {
            var result = new BaseResponse<UserGroupResponse>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                result = await _authenticationRepository.AddOrUpdateUserGroup(request, token);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("usergroup/delete")]
        public async Task<IActionResult> DeleteUserGroup([FromBody] UserGroupRequest request)
        {
            var result = new BaseResponse<bool>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                result = await _authenticationRepository.DeleteUserGroup(request, token);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("activitylog/list/{userId}")]
        public async Task<IActionResult> GetActivityLogList([FromRoute] string userId, [FromBody] MetaData metaData, [FromQuery] string? search = "")
        {
            var result = new BaseResponse<LogInActivityLogResponse>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                result = await _authenticationRepository.GetLogInActivityLogList(userId, search, metaData, token);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }


        [HttpPost("report/roles/url")]
        public async Task<IActionResult> GetReportRolesUrl([FromBody] ReportRolesUrlRequest request)
        {
            var result = new BaseResponse<ReportRolesUrlResponse>()
            {
                StatusCode = Shared.Enums.StatusCode.Unauthorized,
                Message = "Missing authorization"
            };

            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                result = await _authenticationRepository.GetReportRolesUrl(request, token);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        #region Utilities
        private Dictionary<string, string> Headers()
        {
            var headers = new Dictionary<string, string>
            {
                {"X-Forwarded-For", GetRemoteIpAddress()}
            };

            return headers;
        }
        private string GetRemoteIpAddress()
        {
            var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (string.IsNullOrEmpty(ip))
            {
                ip = HttpContext.Connection.RemoteIpAddress.ToString();
            }

            return ip.Split(':').FirstOrDefault();
        }
        #endregion
    }
}
