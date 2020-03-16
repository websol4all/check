using FluentAssertions;
using AutoFixture;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xyzies.Devices.API.Controllers;
using Xyzies.Devices.Services.Helpers;
using Xyzies.Devices.Tests.Models.User;
using System.Linq;
using Xyzies.Devices.Services.Models.User;
using Xyzies.Devices.Tests.IntegrationTests.Services;
using Xyzies.Devices.Services.Models.Comment;
using Xyzies.Devices.Data.Common;
using IdentityServiceClient;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.API.Models;

namespace Xyzies.Devices.Tests.IntegrationTests
{
    public class DeviceCommentsControllerTests : IClassFixture<BaseIntegrationTest>
    {
        private readonly BaseIntegrationTest _baseTest = null;

        public DeviceCommentsControllerTests(BaseIntegrationTest baseTest)
        {
            _baseTest = baseTest ?? throw new ArgumentNullException(nameof(baseTest));
            _baseTest.DbContext.ClearContext();
        }

        #region GetComments

        [Fact]
        public async Task ShouldReturUnauthorizedIFUserHasNotTokenWhenGetComments()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            string uriGetComments = $"devices/{deviceId}/comments";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = null;
            var response = await _baseTest.HttpClient.GetAsync(uriGetComments);

            response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Fact]
        public async Task ShouldReturForbidResultIFUserHasNotAccessWhenGetComments()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            string uriGetComments = $"devices/{deviceId}/comments";
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.Manager.Email, Password = ConstTest.Password, Scope = ConstTest.DefaultScopeHasNotAccess };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uriGetComments);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task ShouldReturEmptyResultIFDeviceNotExistWhenGetComments()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            string uriGetComments = $"devices/{deviceId}/comments";
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Comment.Read };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uriGetComments);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var comments = JsonConvert.DeserializeObject<LazyLoadedResult<CommentModel>>(responseString);

            //Assert
            comments.Total.Should().Be(0);
            comments.Result.Should().BeEmpty();
        }

        [Theory]
        [InlineData(ConstTest.Role.AccountAdmin)]
        [InlineData(ConstTest.Role.OperationAdmin)]
        [InlineData(ConstTest.Role.SystemAdmin)]
        [InlineData(ConstTest.Role.Supervisor)]
        public async Task ShouldReturAllCommentsWhenGetComments(string roleName)
        {
            // Arrange
            var user = _baseTest.Users[roleName];
            var userLoginOptiomTest = new UserLoginOption { UserName = user.Email, Password = ConstTest.Password, Scope = Const.Permissions.Comment.Read };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            int commentCount = 10;
            var device = _baseTest.Fixture.Build<Device>().With(x => x.Id, Guid.NewGuid()).Without(x => x.DeviceHistory).Create();
            var commentsMockList = _baseTest.Fixture.Build<Comment>()
                                          .With(x => x.DeviceId, device.Id)
                                          .With(x => x.UserId, _baseTest.OperationAdmin.Id)
                                          .Without(x => x.Device)
                                          .CreateMany(commentCount);

            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.Comments.AddRange(commentsMockList);
            _baseTest.DbContext.SaveChanges();

            string uriGetComments = $"devices/{device.Id}/comments";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uriGetComments);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var comments = JsonConvert.DeserializeObject<LazyLoadedResult<CommentModel>>(responseString);

            //Assert
            comments.Total.Should().Be(commentCount);
            comments.Result.Count().Should().Be(_baseTest.DbContext.Comments.Count());
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(3, 3)]
        public async Task ShouldReturCommentsWithLazyLoadingWhenGetComments(int skip, int take)
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Comment.Read };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            int commentCount = 10;
            var device = _baseTest.Fixture.Build<Device>().With(x=>x.Id, Guid.NewGuid()).Without(x => x.DeviceHistory).Create();
            var commentsMockList = _baseTest.Fixture.Build<Comment>()
                                          .With(x => x.DeviceId, device.Id)
                                          .With(x => x.UserId, _baseTest.OperationAdmin.Id)
                                          .Without(x => x.Device)
                                          .CreateMany(commentCount);

            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.Comments.AddRange(commentsMockList);
            _baseTest.DbContext.SaveChanges();

            string uriGetComments = $"devices/{device.Id}/comments?{nameof(LazyLoadParameters.Offset)}={skip}&{nameof(LazyLoadParameters.Limit)}={take}";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uriGetComments);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var comments = JsonConvert.DeserializeObject<LazyLoadedResult<CommentModel>>(responseString);

            //Assert
            comments.Offset.Should().Be(skip);
            comments.Limit.Should().Be(take);
            comments.Total.Should().Be(commentCount);
            comments.Result.Count().Should().Be(_baseTest.DbContext.Comments.Skip(skip).Take(take).Count());
        }

        #endregion

        #region Post Comment

        [Fact]
        public async Task ShouldReturUnauthorizedIFUserHasNotTokenWhenPostComments()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            string uriGetComments = $"devices/{deviceId}/comments";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = null;
            var content = new StringContent(JsonConvert.SerializeObject(_baseTest.Fixture.Create<CommentRequestModel>()), Encoding.UTF8, "application/json");

            var response = await _baseTest.HttpClient.PostAsync(uriGetComments, content);

            response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Fact]
        public async Task ShouldReturForbidResultIFUserHasNotAccessWhenPostComments()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            string uriPostComments = $"devices/{deviceId}/comments";
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.Manager.Email, Password = ConstTest.Password, Scope = ConstTest.DefaultScopeHasNotAccess };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var content = new StringContent(JsonConvert.SerializeObject(_baseTest.Fixture.Create<CommentRequestModel>()), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PostAsync(uriPostComments, content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task ShouldReturBadRequestIFDeviceNotExistWhenPostComments()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            string uriPostComments = $"devices/{deviceId}/comments";
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Comment.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var content = new StringContent(JsonConvert.SerializeObject(_baseTest.Fixture.Create<CommentRequestModel>()), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PostAsync(uriPostComments, content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Theory]
        [InlineData(ConstTest.Role.AccountAdmin)]
        [InlineData(ConstTest.Role.OperationAdmin)]
        [InlineData(ConstTest.Role.SystemAdmin)]
        [InlineData(ConstTest.Role.Supervisor)]
        public async Task ShouldReturSuccessWhenPostComments(string roleName)
        {
            // Arrange
            var device = _baseTest.Fixture.Build<Device>().Without(x => x.DeviceHistory).Create();

            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            string uriPostComments = $"devices/{device.Id}/comments";
            var user = _baseTest.Users[roleName];
            var userLoginOptiomTest = new UserLoginOption { UserName = user.Email, Password = ConstTest.Password, Scope = Const.Permissions.Comment.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var commentRequestModel = _baseTest.Fixture.Create<CommentRequestModel>();
            var content = new StringContent(JsonConvert.SerializeObject(commentRequestModel), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PostAsync(uriPostComments, content);

            response.EnsureSuccessStatusCode();

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status201Created);
            _baseTest.DbContext.Comments.Count().Should().Be(1);
            _baseTest.DbContext.Comments.First().Message.Should().Be(commentRequestModel.Comment);
            _baseTest.DbContext.Comments.First().UserId.Should().Be(user.Id);
            _baseTest.DbContext.Comments.First().DeviceId.Should().Be(device.Id);
            _baseTest.DbContext.Comments.First().UserName.Should().Be($"{user.GivenName} {user.Surname}");
        }

        #endregion
    }
}
