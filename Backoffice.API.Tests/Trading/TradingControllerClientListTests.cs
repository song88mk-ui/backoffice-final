using Backoffice.API.Controllers.Trading;
using Backoffice.API.Repositories;
using Backoffice.API.Telemetry;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.Trading
{
    // BO-141: GET api/trading/client was commented out when the OddsEngine Trading API was retired, and its route
    // attribute ended up on GetClientConfig, so the Config page's client filter always came back empty (HTTP 415).
    // The action now answers from the Platform client list, filtered to the caller's client codes.
    public class TradingControllerClientListTests
    {
        private static readonly Guid IdS88 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid IdOg = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid IdW88 = Guid.Parse("33333333-3333-3333-3333-333333333333");

        private static (TradingController Controller, Mock<IBackofficeRepository> Backoffice) CreateController(string clientCodes)
        {
            var backoffice = new Mock<IBackofficeRepository>();
            var httpContext = new DefaultHttpContext();
            if (clientCodes != null)
                httpContext.Items["ClientCodes"] = clientCodes;

            var controller = new TradingController(new Mock<ITradingRepository>().Object, backoffice.Object, new Instrumentation())
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            return (controller, backoffice);
        }

        private static BaseResponse<List<PlatformClientResponse>> ThreePlatformClients() => new()
        {
            StatusCode = StatusCode.Success,
            Message = "Success",
            Result = new List<PlatformClientResponse>
            {
                new() { ClientId = IdS88, ClientCode = "S88", ClientName = "Song88" },
                new() { ClientId = IdOg, ClientCode = "OG", ClientName = "OG Sports" },
                new() { ClientId = IdW88, ClientCode = "W88", ClientName = "W88" }
            }
        };

        private static List<ClientInformationModel> ClientsFrom(IActionResult result)
        {
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<BaseResponse<List<ClientInformationModel>>>(ok.Value);
            Assert.Equal(StatusCode.Success, body.StatusCode);
            return body.Result;
        }

        [Fact]
        public async Task AllClientsCode_ReturnsEveryPlatformClient_WithItsCodeIdAndName()
        {
            var (controller, backoffice) = CreateController("ALL");
            backoffice.Setup(b => b.GetPlatformClients()).ReturnsAsync(ThreePlatformClients());

            var clients = ClientsFrom(await controller.GetClientList());

            Assert.Equal(new[] { "S88", "OG", "W88" }, clients.Select(c => c.ClientCode));
            var og = clients.Single(c => c.ClientCode == "OG");
            Assert.Equal(IdOg, og.ClientId);
            Assert.Equal("OG Sports", og.ClientName);
        }

        [Fact]
        public async Task UserClientCodes_KeepOnlyThoseClients_IgnoringCase()
        {
            var (controller, backoffice) = CreateController("s88,OG");
            backoffice.Setup(b => b.GetPlatformClients()).ReturnsAsync(ThreePlatformClients());

            var clients = ClientsFrom(await controller.GetClientList());

            Assert.Equal(new[] { "S88", "OG" }, clients.Select(c => c.ClientCode));
        }

        [Fact]
        public async Task NoClientCodesForTheCaller_ReturnsNoClients_NotEveryClient()
        {
            var (controller, backoffice) = CreateController(clientCodes: null);
            backoffice.Setup(b => b.GetPlatformClients()).ReturnsAsync(ThreePlatformClients());

            var clients = ClientsFrom(await controller.GetClientList());

            Assert.Empty(clients);
        }

        [Fact]
        public async Task APlatformListWithoutAResult_IsAnEmptyList()
        {
            var (controller, backoffice) = CreateController("ALL");
            backoffice
                .Setup(b => b.GetPlatformClients())
                .ReturnsAsync(new BaseResponse<List<PlatformClientResponse>> { StatusCode = StatusCode.Success, Result = null });

            var clients = ClientsFrom(await controller.GetClientList());

            Assert.Empty(clients);
        }

        [Fact]
        public async Task APlatformFailure_IsABadRequest_CarryingTheStatusAndTheMessage()
        {
            var (controller, backoffice) = CreateController("ALL");
            backoffice
                .Setup(b => b.GetPlatformClients())
                .ReturnsAsync(new BaseResponse<List<PlatformClientResponse>> { StatusCode = StatusCode.Error, Message = "Platform API is down" });

            var result = await controller.GetClientList();

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var body = Assert.IsType<BaseResponse<List<ClientInformationModel>>>(bad.Value);
            Assert.Equal(StatusCode.Error, body.StatusCode);
            Assert.Equal("Platform API is down", body.Message);
            Assert.Null(body.Result);
        }

        [Fact]
        public void TheClientListIsAGetRoute_AndTheClientConfigActionIsPostOnly()
        {
            // The dangling [HttpGet("client")] used to sit on GetClientConfig, which expects a JSON body: HTTP 415.
            var list = typeof(TradingController).GetMethod(nameof(TradingController.GetClientList))!;
            Assert.Equal("client", Assert.Single(list.GetCustomAttributes<HttpGetAttribute>()).Template);

            var config = typeof(TradingController).GetMethod(nameof(TradingController.GetClientConfig))!;
            Assert.Empty(config.GetCustomAttributes<HttpGetAttribute>());
            Assert.Equal("client/config/all", Assert.Single(config.GetCustomAttributes<HttpPostAttribute>()).Template);
        }
    }
}
