using Backoffice.API.Repositories;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
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
            var rabbitMqRepository = new Mock<IRabbitMqRepository>();
            rabbitMqRepository
                .Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<MessageResponse>() { StatusCode = StatusCode.Success });
            var sut = new WebPropsRepository(httpClient, NullLogger<WebPropsRepository>.Instance, rabbitMqRepository.Object);

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
            Assert.Equal("G02", result.Result.ClientId);
            Assert.True(result.Result.Features.BettingRules);
            Assert.True(result.Result.Features.PartialCashout);
            Assert.False(result.Result.Features.OGAuth);
        }

        [Fact]
        public async Task GetClientFeatureFlagsAsync_MissingFullCashoutKey_DefaultsToFalse()
        {
            // RealConfigJson predates FullCashout, so this also covers every existing
            // client's stored config until it is next saved with the new field. GM-863
            // considered defaulting this to true instead, decided against it (2026-09-28) -
            // same false-when-absent behavior as every other Features flag here.
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.OK, RealConfigJson));

            var result = await sut.GetClientFeatureFlagsAsync("G02");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.False(result.Result.Features.FullCashout);
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
                ClientId = "G02",
                BettingRules = false,
                DarkMode = true,
                LanguageOptions = true,
                OGAuth = false,
                FullCashout = false,
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
            Assert.Contains("\"FullCashout\":false", capturedPostBody);
        }

        [Fact]
        public async Task UpdateClientFeatureFlagsAsync_PartialWithoutFullCashout_StoresPartialCashoutDisabled()
        {
            string capturedPostBody = null;

            var (sut, _) = BuildSut(
                getResponder: _ => Json(HttpStatusCode.OK, RealConfigJson),
                postResponder: req =>
                {
                    capturedPostBody = req.Content.ReadAsStringAsync().Result;
                    return Json(HttpStatusCode.OK, "\"ok\"");
                });

            // Full-before-partial dependency: the UI never sends this combination, but a
            // direct API call could - the stored config must not enable partial without full.
            var request = new UpdateClientFeatureFlagsRequest
            {
                ClientId = "G02",
                FullCashout = false,
                PartialCashout = true
            };

            await sut.UpdateClientFeatureFlagsAsync(request);

            Assert.NotNull(capturedPostBody);
            Assert.Contains("\"FullCashout\":false", capturedPostBody);
            Assert.Contains("\"PartialCashout\":false", capturedPostBody);
        }

        [Fact]
        public async Task UpdateClientFeatureFlagsAsync_PartialWithFullCashout_StoresPartialCashoutEnabled()
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
                ClientId = "G02",
                FullCashout = true,
                PartialCashout = true
            };

            await sut.UpdateClientFeatureFlagsAsync(request);

            Assert.NotNull(capturedPostBody);
            Assert.Contains("\"FullCashout\":true", capturedPostBody);
            Assert.Contains("\"PartialCashout\":true", capturedPostBody);
        }

        // BO-108: FullCashout was a plain bool, so a request that omitted it wrote false
        // and silently disabled the feature. It must behave like GameTrackerProductNames/
        // LayoutThemes below - null means "not edited", stored value survives untouched -
        // and the full-before-partial coercion must use the stored value in that case too,
        // not force PartialCashout off just because the caller didn't resend FullCashout.
        [Fact]
        public async Task UpdateClientFeatureFlagsAsync_FullCashoutOmitted_LeavesStoredValueUntouched()
        {
            const string StoredFullCashoutEnabledJson =
                "{\"ClientCode\":\"G02\",\"Features\":{\"BettingRules\":true,\"FullCashout\":true,\"PartialCashout\":true}}";
            string capturedPostBody = null;

            var (sut, _) = BuildSut(
                getResponder: _ => Json(HttpStatusCode.OK, StoredFullCashoutEnabledJson),
                postResponder: req =>
                {
                    capturedPostBody = req.Content.ReadAsStringAsync().Result;
                    return Json(HttpStatusCode.OK, "\"ok\"");
                });

            var request = new UpdateClientFeatureFlagsRequest
            {
                ClientId = "G02",
                BettingRules = true,
                PartialCashout = true
            };

            await sut.UpdateClientFeatureFlagsAsync(request);

            Assert.NotNull(capturedPostBody);
            Assert.Contains("\"FullCashout\":true", capturedPostBody);
            Assert.Contains("\"PartialCashout\":true", capturedPostBody);
        }

        [Fact]
        public async Task UpdateClientFeatureFlagsAsync_NullOptionalFields_LeavesStoredValuesUntouched()
        {
            string capturedPostBody = null;

            var (sut, _) = BuildSut(
                getResponder: _ => Json(HttpStatusCode.OK, RealConfigJson),
                postResponder: req =>
                {
                    capturedPostBody = req.Content.ReadAsStringAsync().Result;
                    return Json(HttpStatusCode.OK, "\"ok\"");
                });

            // GameTrackerProductNames and LayoutThemes left null = "not edited".
            var request = new UpdateClientFeatureFlagsRequest { ClientId = "G02", BettingRules = true };

            await sut.UpdateClientFeatureFlagsAsync(request);

            Assert.NotNull(capturedPostBody);
            // Stored values from RealConfigJson must survive verbatim.
            Assert.Contains("\"song88-light\"", capturedPostBody);
            Assert.Contains("\"red\"", capturedPostBody);
            Assert.Contains("\"gray\"", capturedPostBody);
        }

        [Fact]
        public async Task UpdateClientFeatureFlagsAsync_ProvidedOptionalFields_OverwriteStoredValues()
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
                ClientId = "G02",
                IsEuroLayout = true,
                GameTrackerProductNames = new System.Collections.Generic.List<string> { "song88-dark" },
                LayoutThemes = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>
                {
                    ["AL"] = new System.Collections.Generic.List<string> { "blue", "orange-light" }
                }
            };

            await sut.UpdateClientFeatureFlagsAsync(request);

            Assert.NotNull(capturedPostBody);
            Assert.Contains("\"IsEuroLayout\":true", capturedPostBody);
            Assert.Contains("\"song88-dark\"", capturedPostBody);
            Assert.DoesNotContain("\"song88-light\"", capturedPostBody);
            Assert.Contains("\"blue\"", capturedPostBody);
            Assert.Contains("\"orange-light\"", capturedPostBody);
            Assert.DoesNotContain("\"gray\"", capturedPostBody);
        }

        [Fact]
        public async Task GetClientFeatureFlagsAsync_IsEuroLayout_RoundTrips()
        {
            var json = RealConfigJson.Replace("\"OGAuth\":false", "\"OGAuth\":false,\"IsEuroLayout\":true");
            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.OK, json));

            var result = await sut.GetClientFeatureFlagsAsync("G02");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.True(result.Result.Features.IsEuroLayout);
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

            var request = new UpdateClientFeatureFlagsRequest { ClientId = "NEWCLIENT", BettingRules = true };

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

            var request = new UpdateClientFeatureFlagsRequest { ClientId = "G02", BettingRules = true };

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

            var request = new UpdateClientFeatureFlagsRequest { ClientId = "G02", BettingRules = true };

            var result = await sut.UpdateClientFeatureFlagsAsync(request);

            Assert.Equal(StatusCode.Error, result.StatusCode);
        }
    }
}
