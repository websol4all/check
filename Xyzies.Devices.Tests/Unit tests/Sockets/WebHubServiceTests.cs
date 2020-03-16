using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using AutoFixture;
using System;
using System.Threading.Tasks;
using Xunit;
using Xyzies.Devices.Services.Service;
using Xyzies.Devices.Services.Models.WebSocket;
using System.Collections.Generic;

namespace Xyzies.Devices.Tests.Unit_tests.Sockets
{
    public class WebHubServiceTests : IClassFixture<BaseTest>
    {
        private readonly BaseTest _baseTest = null;

        public WebHubServiceTests(BaseTest baseTest)
        {
            _baseTest = baseTest ?? throw new ArgumentNullException(nameof(baseTest));
            _baseTest.DbContext.ClearContext();
        }


        [Fact]
        public async Task ShouldSubscribeConnectionForDevicesUpdates()
        {
            //Arrange
            string contextId = _baseTest.Fixture.Create<string>();

            var logger = Mock.Of<ILogger<WebHubService>>();

            var context = new Mock<HubCallerContext>();
            context.Setup(x => x.ConnectionId).Returns(contextId);

            var groups = new Mock<IGroupManager>();
            groups.Setup(x => x.AddToGroupAsync(contextId, It.IsAny<string>(), new System.Threading.CancellationToken())).Returns(Task.CompletedTask);

            var webHubService = new WebHubService(logger);
            webHubService.Context = context.Object;
            webHubService.Groups = groups.Object;
            var udids = new List<string>()
            {
                "one","two","three"
            };

            //Action
            await webHubService.SubscribeDevicesUpdates(new SubscribeDevicesRequest
            {
                Udids = udids
            });

            //Assert
            WebHubService.ConnectionGroupNames.TryGetValue(contextId, out List<string> value).Should().BeTrue();
            value.Should().HaveSameCount(udids);
            foreach (var udid in udids)
            {
                value.Should().Contain(udid);
            }
        }

        [Fact]
        public async Task ShouldRemoveSubscribedConnectionsAndDisconnectSocket()
        {
            //Arrange
            string contextId = _baseTest.Fixture.Create<string>();

            var logger = Mock.Of<ILogger<WebHubService>>();

            var context = new Mock<HubCallerContext>();
            context.Setup(x => x.ConnectionId).Returns(contextId);

            var groups = new Mock<IGroupManager>();
            groups.Setup(x => x.AddToGroupAsync(contextId, It.IsAny<string>(), new System.Threading.CancellationToken())).Returns(Task.CompletedTask);

            var webHubService = new WebHubService(logger);
            webHubService.Context = context.Object;
            webHubService.Groups = groups.Object;
            var udids = new List<string>()
            {
                "one","two","three"
            };

            //Action
            await webHubService.SubscribeDevicesUpdates(new SubscribeDevicesRequest
            {
                Udids = udids
            });
            await webHubService.OnDisconnectedAsync(new Exception());

            //Assert
            WebHubService.ConnectionGroupNames.TryGetValue(contextId, out List<string> value).Should().BeFalse();
            value.Should().BeNull();
        }
    }
}
