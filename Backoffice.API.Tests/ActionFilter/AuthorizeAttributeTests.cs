using Backoffice.API.ActionFilter;
using Backoffice.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.ActionFilter
{
    public class AuthorizeAttributeTests
    {
        // Matches API:AuthenticationAPI:JWT:Issuer in Backoffice.API/appsettings.json, which the
        // attribute reads from the test output directory.
        private const string Issuer = "bo.auth.api";
        private const string SuccessJson = "{\"statusCode\":100}";
        private const string UnauthorizedJson = "{\"statusCode\":401,\"message\":\"Invalid token.\"}";

        // Builds the attribute with its HttpClient swapped for a stub Auth API that accepts only
        // validToken. The attribute has no injection seam, so the private _httpClient is set by reflection.
        private static AuthorizeAttribute BuildSut(string validToken, AuthorizeAttribute sut = null)
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage req, CancellationToken _) =>
                {
                    var response = req.Headers.Authorization?.Parameter == validToken
                        ? Json(HttpStatusCode.OK, SuccessJson)
                        : Json(HttpStatusCode.Unauthorized, UnauthorizedJson);
                    return Task.FromResult(response);
                });

            sut ??= new AuthorizeAttribute();
            typeof(AuthorizeAttribute)
                .GetProperty("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(sut, new HttpClient(handler.Object) { BaseAddress = new Uri("http://auth-api.test/") });

            return sut;
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
            new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

        // Unsigned token: passes the attribute's local issuer and expiry checks, so only the
        // Auth API call can reject it.
        private static string Token(string userId, params Claim[] permissions) =>
            new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                issuer: Issuer,
                claims: new[] { new Claim("UserId", userId) }.Concat(permissions),
                expires: DateTime.UtcNow.AddHours(1)));

        private static AuthorizationFilterContext Context(string token)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Authorization = "Bearer " + token;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor { EndpointMetadata = new List<object>() });

            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }

        [Fact]
        public void OnAuthorization_TokenAcceptedByAuthApi_IsAuthorized()
        {
            var validToken = Token("legit-user");
            var sut = BuildSut(validToken);
            var context = Context(validToken);

            sut.OnAuthorization(context);

            Assert.Null(context.Result);
            Assert.Equal("legit-user", context.HttpContext.Items["UserId"]);
        }

        [Fact]
        public void OnAuthorization_TokenRejectedByAuthApi_Returns401()
        {
            var sut = BuildSut(Token("legit-user"));
            var context = Context(Token("forged-user"));

            sut.OnAuthorization(context);

            var result = Assert.IsType<JsonResult>(context.Result);
            Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        }

        // BO-123: one attribute instance serves every request to its action. When the token was set on
        // the shared HttpClient's DefaultRequestHeaders, a forged token running alongside a valid one
        // was authorized in 20-45% of rounds, so 200 rounds catch a regression with near certainty.
        [Fact]
        public async Task OnAuthorization_ForgedTokenConcurrentWithValidToken_IsNeverAuthorized()
        {
            const int rounds = 200;
            var validToken = Token("legit-user");
            var forgedToken = Token("forged-user");
            var sut = BuildSut(validToken);
            var forgedContexts = new List<AuthorizationFilterContext>();

            for (var i = 0; i < rounds; i++)
            {
                var barrier = new Barrier(2);
                var forgedContext = Context(forgedToken);
                forgedContexts.Add(forgedContext);

                await Task.WhenAll(
                    Task.Run(() => { barrier.SignalAndWait(); sut.OnAuthorization(Context(validToken)); }),
                    Task.Run(() => { barrier.SignalAndWait(); sut.OnAuthorization(forgedContext); }));
            }

            var authorized = forgedContexts.Count(c => c.Result == null);
            Assert.Equal(0, authorized);
        }

        // BO-130: a permission claim on the attribute is checked against the token's own claims, with the
        // values the Auth API issues (1 = Edit, 2 = View, 0 = NoAccess).
        [Theory]
        [InlineData(null, (int)Access.Edit, false)]
        [InlineData("0", (int)Access.Edit, false)]
        [InlineData("2", (int)Access.Edit, false)]
        [InlineData("1", (int)Access.Edit, true)]
        [InlineData(null, (int)Access.View, false)]
        [InlineData("0", (int)Access.View, false)]
        [InlineData("2", (int)Access.View, true)]
        [InlineData("1", (int)Access.View, true)]
        public void OnAuthorization_RequiredClaim_IsCheckedAgainstToken(string tokenValue, int requiredAccess, bool expectAuthorized)
        {
            const string claimType = "Admin.CancelBets";
            var permissions = tokenValue == null ? Array.Empty<Claim>() : new[] { new Claim(claimType, tokenValue) };
            var token = Token("legit-user", permissions);
            var sut = BuildSut(token, new AuthorizeAttribute(claimType, requiredAccess));
            var context = Context(token);

            sut.OnAuthorization(context);

            if (expectAuthorized)
                Assert.Null(context.Result);
            else
                Assert.Equal(StatusCodes.Status401Unauthorized, Assert.IsType<JsonResult>(context.Result).StatusCode);
        }

        [Fact]
        public void OnAuthorization_RequiredClaim_OtherPermissionDoesNotCount()
        {
            var token = Token("legit-user", new Claim("Admin.Settlements.ResendSettlement", "1"));
            var sut = BuildSut(token, new AuthorizeAttribute("Admin.CancelBets", (int)Access.Edit));
            var context = Context(token);

            sut.OnAuthorization(context);

            Assert.Equal(StatusCodes.Status401Unauthorized, Assert.IsType<JsonResult>(context.Result).StatusCode);
        }
    }
}
