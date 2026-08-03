using Backoffice.API.Repositories;
using Backoffice.API.Telemetry;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.Maintenance
{
    public class MaintenanceRepositoryTests
    {
        private static readonly Guid Client1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid Client2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private sealed class RecordedRequest
        {
            public HttpMethod Method { get; set; }
            public string Path { get; set; }
            public string Layout { get; set; }
            public string Body { get; set; }
        }

        private static Mock<IPlatformRepository> BuildPlatformRepository()
        {
            var platformRepository = new Mock<IPlatformRepository>();
            platformRepository
                .Setup(p => p.GetClients(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<List<PlatformClientResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Result = new List<PlatformClientResponse>
                    {
                        new PlatformClientResponse { ClientId = Client1Id, ClientCode = "G02", ClientName = "GIMLISTG" },
                        new PlatformClientResponse { ClientId = Client2Id, ClientCode = "OGE", ClientName = "OG Sports" }
                    }
                });
            return platformRepository;
        }

        private static (MaintenanceRepository sut, List<RecordedRequest> requests) BuildSut(
            Func<RecordedRequest, Task<HttpResponseMessage>> responder,
            Mock<IPlatformRepository> platformRepository = null,
            string apps = "el1, el2")
        {
            var requests = new List<RecordedRequest>();

            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage req, CancellationToken _) =>
                {
                    var recorded = new RecordedRequest
                    {
                        Method = req.Method,
                        Path = req.RequestUri.AbsolutePath,
                        Layout = req.Headers.TryGetValues("x-layout", out var values) ? values.FirstOrDefault() : null,
                        Body = req.Content?.ReadAsStringAsync().Result
                    };
                    requests.Add(recorded);
                    return responder(recorded);
                });

            var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://platform-webprops.stg.song88.com") };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["IsStaging"] = "true",
                    ["Maintenance:Apps"] = apps
                })
                .Build();

            var sut = new MaintenanceRepository(
                httpClient,
                configuration,
                (platformRepository ?? BuildPlatformRepository()).Object,
                new Instrumentation(),
                NullLogger<MaintenanceRepository>.Instance);

            return (sut, requests);
        }

        private static Task<HttpResponseMessage> Json(HttpStatusCode status, string body) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });

        [Fact]
        public async Task GetMaintenanceV2_ReturnsRealFlagsAndSeedsClientsWithoutConfig()
        {
            // EL1 has a real config object for G02 (keyed by client code); EL2 has none.
            var (sut, requests) = BuildSut(req =>
            {
                if (req.Layout == "EL1")
                    return Json(HttpStatusCode.OK, "[{\"clientId\":\"G02\",\"isMaintenance\":true,\"site\":\"site1\"}]");
                return Json(HttpStatusCode.OK, "[]");
            });

            var result = await sut.GetMaintenanceV2(null);

            Assert.Equal(StatusCode.Success, result.StatusCode);

            // one GET per configured app, path and x-layout header per webprops contract
            Assert.Equal(new[] { "EL1", "EL2" }, requests.Select(r => r.Layout).ToArray());
            Assert.All(requests, r => Assert.Equal("/api/v1/maintenance", r.Path));

            // 2 apps x 2 clients = 4 entries: G02@EL1 real, the rest seeded false
            Assert.Equal(4, result.Result.ClientConfig.Count);

            var real = result.Result.ClientConfig.Single(c => c.App == "EL1" && c.ClientCode == "G02");
            Assert.True(real.IsMaintenance);
            Assert.Equal("site1", real.Site);

            Assert.All(result.Result.ClientConfig.Where(c => c != real), c => Assert.False(c.IsMaintenance));
        }

        [Fact]
        public async Task GetMaintenanceV2_MatchesConfigKeyedByPlatformClientId()
        {
            var (sut, _) = BuildSut(req =>
            {
                if (req.Layout == "EL1")
                    return Json(HttpStatusCode.OK, $"[{{\"clientId\":\"{Client1Id}\",\"isMaintenance\":true,\"site\":\"\"}}]");
                return Json(HttpStatusCode.OK, "[]");
            });

            var result = await sut.GetMaintenanceV2(null);

            var real = result.Result.ClientConfig.Single(c => c.App == "EL1" && c.IsMaintenance);
            Assert.Equal("G02", real.ClientCode);
            Assert.Equal(Client1Id.ToString(), real.ClientId, ignoreCase: true);
        }

        [Fact]
        public async Task GetMaintenanceV2_OneAppFails_OtherAppResultsStillReturned()
        {
            var (sut, _) = BuildSut(req =>
            {
                if (req.Layout == "EL1")
                    return Json(HttpStatusCode.OK, "[{\"clientId\":\"G02\",\"isMaintenance\":true,\"site\":\"\"}]");
                return Task.FromException<HttpResponseMessage>(new HttpRequestException("el2 is down"));
            });

            var result = await sut.GetMaintenanceV2(null);

            Assert.Equal(StatusCode.Success, result.StatusCode);
            var real = result.Result.ClientConfig.Single(c => c.App == "EL1" && c.ClientCode == "G02");
            Assert.True(real.IsMaintenance);
        }

        [Fact]
        public async Task GetMaintenanceV2_PlatformClientsUnavailable_ReturnsUpstreamStatus()
        {
            var platformRepository = new Mock<IPlatformRepository>();
            platformRepository
                .Setup(p => p.GetClients(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<List<PlatformClientResponse>>
                {
                    StatusCode = StatusCode.Error,
                    Result = null
                });

            var (sut, _) = BuildSut(_ => Json(HttpStatusCode.OK, "[]"), platformRepository);

            var result = await sut.GetMaintenanceV2(null);

            Assert.Equal(StatusCode.Error, result.StatusCode);
        }

        [Fact]
        public async Task SetMaintenanceV2_MissingConfig_CreatesObjectThenSetsFlag()
        {
            var (sut, requests) = BuildSut(req =>
            {
                if (req.Method == HttpMethod.Post)
                    return Json(HttpStatusCode.OK, "\"ok\"");
                return Json(HttpStatusCode.OK, "[]"); // no config objects exist yet in any layout
            });

            var request = new SetMaintenanceRequest { ClientIds = "EL1:G02", IsMaintenance = true };

            var result = await sut.SetMaintenanceV2(request, null);

            Assert.Equal(StatusCode.Success, result.StatusCode);

            var create = requests.Single(r => r.Method == HttpMethod.Post && r.Path == "/api/v1/config/G02");
            Assert.Equal("EL1", create.Layout);
            Assert.Contains("\"isMaintenance\":false", create.Body);

            var set = requests.Single(r => r.Method == HttpMethod.Post && r.Path == "/api/v1/maintenance");
            Assert.Equal("EL1", set.Layout);
            Assert.Contains("\"ClientIds\":\"G02\"", set.Body);
            Assert.Contains("\"IsMaintenance\":true", set.Body);
        }

        [Fact]
        public async Task SetMaintenanceV2_ExistingConfig_SkipsCreate()
        {
            var (sut, requests) = BuildSut(req =>
            {
                if (req.Method == HttpMethod.Post)
                    return Json(HttpStatusCode.OK, "\"ok\"");
                if (req.Layout == "EL1")
                    return Json(HttpStatusCode.OK, "[{\"clientId\":\"G02\",\"isMaintenance\":false,\"site\":\"\"}]");
                return Json(HttpStatusCode.OK, "[]");
            });

            var request = new SetMaintenanceRequest { ClientIds = "EL1:G02", IsMaintenance = true };

            var result = await sut.SetMaintenanceV2(request, null);

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.DoesNotContain(requests, r => r.Path.StartsWith("/api/v1/config/"));

            var set = requests.Single(r => r.Method == HttpMethod.Post && r.Path == "/api/v1/maintenance");
            Assert.Equal("EL1", set.Layout);
            Assert.Contains("\"IsMaintenance\":true", set.Body);
        }
    }
}
