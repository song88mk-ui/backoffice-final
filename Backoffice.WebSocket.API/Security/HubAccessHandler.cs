using Microsoft.AspNetCore.Authorization;

namespace Backoffice.WebSocket.API.Security
{
    /// <summary>
    /// Connect: any caller with a valid API key. Publish: only the Publisher key (WebSocket.Service).
    /// </summary>
    public class HubAccessRequirement : IAuthorizationRequirement
    {
        public HubAccessRequirement(string requiredRole = null)
        {
            RequiredRole = requiredRole;
        }

        public string RequiredRole { get; }
    }

    public class HubAccessHandler : AuthorizationHandler<HubAccessRequirement>
    {
        private readonly HubSecurityOptions _security;
        private readonly ILogger<HubAccessHandler> _logger;

        public HubAccessHandler(HubSecurityOptions security, ILogger<HubAccessHandler> logger)
        {
            _security = security;
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HubAccessRequirement requirement)
        {
            var allowed = context.User.Identity?.IsAuthenticated == true
                && (requirement.RequiredRole == null || context.User.IsInRole(requirement.RequiredRole));

            if (allowed)
            {
                context.Succeed(requirement);
            }
            else if (!_security.Enforce)
            {
                // Log-only mode: let it through so clients without a credential keep working during rollout.
                _logger.LogWarning("Hub caller would be refused once Security:Enforce is on (requires {Requirement})", requirement.RequiredRole ?? "an API key");
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
