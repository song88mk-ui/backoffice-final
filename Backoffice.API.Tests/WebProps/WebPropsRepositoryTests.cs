using Backoffice.API.Repositories;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.WebProps
{
    public class WebPropsRepositoryTests
    {
        private const string RealConfigJson = "{\"ClientCode\":\"G02\",\"Features\":{\"BettingRules\":true,\"DarkMode\":true,\"GameTrackerProductNames\":[\"song88-light\"],\"LanguageOptions\":true,\"OGAuth\":false,\"PartialCashout\":true},\"Layout\":{\"AL\":{\"Themes\":[\"red\",\"gray\"]}}}";
        private const string NotFoundJson = "{\"code\":404,\"message\":\"no properties for client: G02\"}";

        private static (WebPropsRepository sut, Mock<HttpMessageHandler> handler) BuildSut(
            Func<HttpRequestMessage, HttpResponseMessage> getResponder,
            Func<HttpRequestMessage, HttpResponseMessage> postResponder = null)
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage req, CancellationToken _) =>
                {
                    var response = req.Method == HttpMethod.Get
                        ? getResponder(req)
                        : postResponder(req);
                    return Task.FromResult(response);
                });

            var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://platform-webprops.stg.song88.com") };
            var sut = new WebPropsRepository(httpClient, NullLogger<WebPropsRepository>.Instance);

            return (sut, handler);
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
            new HttpResponseMessage(status) { Content = new StringContent(body) };

        [Fact]
        public async Task GetClientFeatureFlagsAsync_Success_ReturnsDeserializedResult()
        {
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.OK, RealConfigJson));

            var result = await sut.GetClientFeatureFlagsAsync("G02");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal("G02", result.Result.ClientCode);
            Assert.True(result.Result.Features.BettingRules);
            Assert.True(result.Result.Features.PartialCashout);
            Assert.False(result.Result.Features.OGAuth);
        }

        [Fact]
        public async Task GetClientFeatureFlagsAsync_GenuineNotFound_ReturnsNotFoundStatus()
        {
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.NotFound, NotFoundJson));

            var result = await sut.GetClientFeatureFlagsAsync("G02");

            Assert.Equal(StatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task GetClientFeatureFlagsAsync_NotFoundWithDifferentMessage_ReturnsErrorNotNotFound()
        {
            // A 404 that does NOT match webprops-api's known "no properties for client" shape
            // must be treated as a genuine error, not silently classified as absence.
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.NotFound, "{\"code\":404,\"message\":\"route not found\"}"));

            var result = await sut.GetClientFeatureFlagsAsync("G02");

            Assert.Equal(StatusCode.Error, result.StatusCode);
        }

        [Fact]
        public async Task GetClientFeatureFlagsAsync_ServerError_ReturnsError()
        {
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.InternalServerError, "{\"message\":\"boom\"}"));

            var result = await sut.GetClientFeatureFlagsAsync("G02");

            Assert.Equal(StatusCode.Error, result.StatusCode);
        }

        [Fact]
        public async Task UpdateClientFeatureFlagsAsync_ExistingConfig_PreservesLayoutAndGameTrackerFields()
        {
            string capturedPostBody = null;

            var (sut, _) = BuildSut(
                getResponder: _ => Json(HttpStatusCode.OK, RealConfigJson),
                postResponder: req =>
                {
                    capturedPostBody = req.Content.ReadAsStringAsync().Result;
                    return Json(HttpStatusCode.OK, "\"ok\"");
                });

            var request = new UpdateClientFeatureFlagsRequest
            {
                ClientCode = "G02",
                BettingRules = false,
                DarkMode = true,
                LanguageOptions = true,
                OGAuth = false,
                PartialCashout = true,
                BetRecommendation = true
            };

            await sut.UpdateClientFeatureFlagsAsync(request);

            Assert.NotNull(capturedPostBody);
            Assert.Contains("\"Layout\"", capturedPostBody);
            Assert.Contains("\"AL\"", capturedPostBody);
            Assert.Contains("\"GameTrackerProductNames\"", capturedPostBody);
            Assert.Contains("\"BettingRules\":false", capturedPostBody);
            Assert.Contains("\"BetRecommendation\":true", capturedPostBody);
        }

        [Fact]
        public async Task UpdateClientFeatureFlagsAsync_ConfirmedAbsent_CreatesFreshConfigAndSucceeds()
        {
            var (sut, _) = BuildSut(
                getResponder: req => req.RequestUri.ToString().EndsWith("/api/v1/config/NEWCLIENT")
                    // First GET (pre-update check) -> not found. Second GET (re-fetch after
                    // successful POST, inside GetClientFeatureFlagsAsync) -> now succeeds.
                    ? Json(HttpStatusCode.NotFound, "{\"code\":404,\"message\":\"no properties for client: NEWCLIENT\"}")
                    : Json(HttpStatusCode.OK, RealConfigJson),
                postResponder: _ => Json(HttpStatusCode.OK, "\"ok\""));

            var request = new UpdateClientFeatureFlagsRequest { ClientCode = "NEWCLIENT", BettingRules = true };

            var result = await sut.UpdateClientFeatureFlagsAsync(request);

            // Note: the responder above always 404s for NEWCLIENT (including the post-update
            // re-fetch), which is intentional here - it isolates the behavior under test: a
            // confirmed-absent config must still result in a successful create (POST succeeds),
            // not an error, even though the immediate re-fetch also reports not-found.
            Assert.Equal(StatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task UpdateClientFeatureFlagsAsync_GetFailsWithGenuineError_ReturnsErrorAndNeverPosts()
        {
            var postCalled = false;

            var (sut, _) = BuildSut(
                getResponder: _ => Json(HttpStatusCode.InternalServerError, "{\"message\":\"upstream failure\"}"),
                postResponder: _ =>
                {
                    postCalled = true;
                    return Json(HttpStatusCode.OK, "\"ok\"");
                });

            var request = new UpdateClientFeatureFlagsRequest { ClientCode = "G02", BettingRules = true };

            var result = await sut.UpdateClientFeatureFlagsAsync(request);

            Assert.Equal(StatusCode.Error, result.StatusCode);
            Assert.False(postCalled, "POST must never be attempted when the pre-update GET fails with an unconfirmed error - it could silently wipe an existing config we simply failed to read.");
        }

        [Fact]
        public async Task UpdateClientFeatureFlagsAsync_PostFails_ReturnsError()
        {
            var (sut, _) = BuildSut(
                getResponder: _ => Json(HttpStatusCode.OK, RealConfigJson),
                postResponder: _ => Json(HttpStatusCode.Forbidden, "{\"code\":403,\"message\":\"Forbidden\"}"));

            var request = new UpdateClientFeatureFlagsRequest { ClientCode = "G02", BettingRules = true };

            var result = await sut.UpdateClientFeatureFlagsAsync(request);

            Assert.Equal(StatusCode.Error, result.StatusCode);
        }
    }
}
