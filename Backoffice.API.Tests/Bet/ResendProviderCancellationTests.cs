using Backoffice.API.Controllers.Backoffice;
using Backoffice.API.Repositories;
using Backoffice.API.Telemetry;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.Bet
{
    // BO-125: the Resend Settlement page can now send a Cancelled bet's status to the provider (Bet Genius /
    // GTS). PlatformRepository builds its HttpClient inline, so these tests stand a loopback listener in for
    // the Platform API and check what actually goes over the wire: the route, the body, the x-api-key
    // header and how each kind of answer comes back. The controller tests check the status mapping and that
    // the x-region header reaches the repository.
    public class ResendProviderCancellationTests
    {
        private const string Route = "/v1/bet/resendprovidercancellation";

        private sealed class LoopbackPlatformApi : IDisposable
        {
            private readonly HttpListener _listener = new HttpListener();

            public string BaseUrl { get; }
            public string Path { get; private set; }
            public string Method { get; private set; }
            public string Body { get; private set; }
            public string ApiKey { get; private set; }
            public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
            public string ResponseBody { get; set; } = "{}";

            public LoopbackPlatformApi()
            {
                BaseUrl = $"http://127.0.0.1:{FreePort()}/";
                _listener.Prefixes.Add(BaseUrl);
                _listener.Start();
                _ = Task.Run(Serve);
            }

            public static int FreePort()
            {
                var probe = new TcpListener(IPAddress.Loopback, 0);
                probe.Start();
                var port = ((IPEndPoint)probe.LocalEndpoint).Port;
                probe.Stop();
                return port;
            }

            private async Task Serve()
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await _listener.GetContextAsync();
                    }
                    catch
                    {
                        return;
                    }

                    using (var reader = new System.IO.StreamReader(context.Request.InputStream, Encoding.UTF8))
                        Body = await reader.ReadToEndAsync();

                    Path = context.Request.Url.AbsolutePath;
                    Method = context.Request.HttpMethod;
                    ApiKey = context.Request.Headers["x-api-key"];

                    var bytes = Encoding.UTF8.GetBytes(ResponseBody);
                    context.Response.StatusCode = (int)Status;
                    context.Response.ContentType = "application/json";
                    await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                    context.Response.Close();
                }
            }

            public void Dispose() => _listener.Close();
        }

        private static PlatformRepository CreateRepository(string hkUri, string hkApiKey = "hk-key", string saUri = null, string saApiKey = "sa-key")
        {
            var values = new Dictionary<string, string>
            {
                ["API:PlatformApi:HK:Uri"] = hkUri,
                ["API:PlatformApi:HK:APIKey"] = hkApiKey,
                ["API:PlatformApi:SA:Uri"] = saUri ?? hkUri,
                ["API:PlatformApi:SA:APIKey"] = saApiKey
            };

            return new PlatformRepository(
                new ConfigurationBuilder().AddInMemoryCollection(values).Build(),
                activityLogRepository: null,
                rabbitMqRepository: null,
                new Instrumentation());
        }

        private static string Json(BaseResponse<ResendSettlementResponse> response) => JsonSerializer.Serialize(response);

        private static BaseResponse<ResendSettlementResponse> Answer(StatusCode statusCode, params (Guid BetId, StatusCode StatusCode, string Message)[] bets) =>
            new BaseResponse<ResendSettlementResponse>
            {
                StatusCode = statusCode,
                Message = statusCode.ToString().Replace('_', ' '),
                Result = new ResendSettlementResponse
                {
                    Data = bets.Select(b => new ResendSettlementResponse.ResendSettlementResponseData { BetId = b.BetId, StatusCode = b.StatusCode, Message = b.Message }).ToList()
                }
            };

        private static ResendProviderCancellationRequest RequestFor(params Guid[] betIds) =>
            new ResendProviderCancellationRequest { BetIds = betIds.Select(b => b.ToString()).ToList() };

        [Fact]
        public async Task PostsTheBetIdsToTheProviderCancellationRoute_WithTheRegionApiKey()
        {
            var betId = Guid.NewGuid();
            using var platformApi = new LoopbackPlatformApi
            {
                ResponseBody = Json(Answer(StatusCode.Success, (betId, StatusCode.Success, "Success")))
            };
            var repository = CreateRepository(platformApi.BaseUrl, hkApiKey: "hk-key");

            var response = await repository.ResendProviderCancellation(RequestFor(betId), "HK");

            Assert.Equal("POST", platformApi.Method);
            Assert.Equal(Route, platformApi.Path);
            Assert.Equal("hk-key", platformApi.ApiKey);
            var sent = JsonSerializer.Deserialize<ResendProviderCancellationRequest>(platformApi.Body);
            Assert.Equal(new[] { betId.ToString() }, sent.BetIds);

            Assert.Equal(StatusCode.Success, response.StatusCode);
            var result = Assert.Single(response.Result.Data);
            Assert.Equal(betId, result.BetId);
            Assert.Equal(StatusCode.Success, result.StatusCode);
        }

        [Fact]
        public async Task TheSaRegion_UsesTheSaPlatformApiAndKey()
        {
            using var platformApi = new LoopbackPlatformApi { ResponseBody = Json(Answer(StatusCode.Success)) };
            var repository = CreateRepository(hkUri: $"http://127.0.0.1:{LoopbackPlatformApi.FreePort()}/", saUri: platformApi.BaseUrl, saApiKey: "sa-key");

            await repository.ResendProviderCancellation(RequestFor(Guid.NewGuid()), "sa");

            Assert.Equal(Route, platformApi.Path);
            Assert.Equal("sa-key", platformApi.ApiKey);
        }

        [Fact]
        public async Task ABadRequestWithPerBetResults_IsPassedThrough_SoThePageCanShowEachFailure()
        {
            var cancelledId = Guid.NewGuid();
            var wonId = Guid.NewGuid();
            using var platformApi = new LoopbackPlatformApi
            {
                Status = HttpStatusCode.BadRequest,
                ResponseBody = Json(Answer(StatusCode.Error,
                    (cancelledId, StatusCode.Success, "Success"),
                    (wonId, StatusCode.Error, "Bet is not cancelled (current status: Won)")))
            };
            var repository = CreateRepository(platformApi.BaseUrl);

            var response = await repository.ResendProviderCancellation(RequestFor(cancelledId, wonId), "HK");

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal(2, response.Result.Data.Count);
            var failed = response.Result.Data.Single(d => d.BetId == wonId);
            Assert.Equal(StatusCode.Error, failed.StatusCode);
            Assert.Equal("Bet is not cancelled (current status: Won)", failed.Message);
        }

        [Fact]
        public async Task AServerError_BecomesAnErrorWithTheReasonPhrase_AndNoData()
        {
            using var platformApi = new LoopbackPlatformApi { Status = HttpStatusCode.InternalServerError, ResponseBody = "boom" };
            var repository = CreateRepository(platformApi.BaseUrl);

            var response = await repository.ResendProviderCancellation(RequestFor(Guid.NewGuid()), "HK");

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Contains("Internal Server Error", response.Message);
            Assert.Null(response.Result);
        }

        [Fact]
        public async Task AnUnreachablePlatformApi_BecomesAnError_InsteadOfThrowing()
        {
            var repository = CreateRepository($"http://127.0.0.1:{LoopbackPlatformApi.FreePort()}/");

            var response = await repository.ResendProviderCancellation(RequestFor(Guid.NewGuid()), "HK");

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(response.Message));
        }

        private static (BetController Controller, Mock<IPlatformRepository> Repository) CreateController(string regionHeader = null)
        {
            var repository = new Mock<IPlatformRepository>();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["IsStaging"] = "false" })
                .Build();

            var httpContext = new DefaultHttpContext();
            if (regionHeader != null)
                httpContext.Request.Headers["x-region"] = regionHeader;

            var controller = new BetController(repository.Object, configuration)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            return (controller, repository);
        }

        [Fact]
        public async Task Controller_Success_IsOk_AndTheRegionHeaderIsForwarded()
        {
            var (controller, repository) = CreateController("SA");
            var request = RequestFor(Guid.NewGuid());
            repository
                .Setup(r => r.ResendProviderCancellation(request, "SA"))
                .ReturnsAsync(Answer(StatusCode.Success));

            var result = await controller.ResendProviderCancellation(request);

            Assert.IsType<OkObjectResult>(result);
            repository.Verify(r => r.ResendProviderCancellation(request, "SA"), Times.Once);
        }

        [Fact]
        public async Task Controller_WithoutARegionHeader_ForwardsAnEmptyRegion()
        {
            var (controller, repository) = CreateController();
            var request = RequestFor(Guid.NewGuid());
            repository
                .Setup(r => r.ResendProviderCancellation(request, string.Empty))
                .ReturnsAsync(Answer(StatusCode.Success));

            await controller.ResendProviderCancellation(request);

            repository.Verify(r => r.ResendProviderCancellation(request, string.Empty), Times.Once);
        }

        [Fact]
        public async Task Controller_Failure_IsABadRequest_CarryingThePerBetResults()
        {
            var (controller, repository) = CreateController("HK");
            var request = RequestFor(Guid.NewGuid());
            var answer = Answer(StatusCode.Error, (Guid.NewGuid(), StatusCode.Error, "Bet not found"));
            repository
                .Setup(r => r.ResendProviderCancellation(request, "HK"))
                .ReturnsAsync(answer);

            var result = await controller.ResendProviderCancellation(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Same(answer, badRequest.Value);
        }
    }
}
