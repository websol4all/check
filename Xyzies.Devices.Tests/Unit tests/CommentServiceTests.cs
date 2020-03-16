using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using Xyzies.Devices.Data.Repository.Behaviour;
using AutoFixture;
using Xyzies.Devices.Services.Service;
using Xyzies.Devices.Services.Service.Interfaces;
using Xyzies.Devices.Services.Models.User;
using System.Linq;
using System.Collections.Generic;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Data.Repository;
using Xyzies.Devices.Data.Common;

namespace Xyzies.Devices.Tests.Unit_tests
{
    public class CommentServiceTests : IClassFixture<BaseTest>
    {
        private readonly BaseTest _baseTest = null;

        private ILogger<CommentService> _loggerMock;
        private Mock<IHttpService> _httpServiceMock;
        private Mock<IDeviceRepository> _deviceRepositoryMock;

        private readonly ICommentService _commentService = null;
        private CommentRepository _commrepository;
        private DeviceRepository _devicerepository;

        public CommentServiceTests(BaseTest baseTest)
        {
            _baseTest = baseTest ?? throw new ArgumentNullException(nameof(baseTest));
            _baseTest.DbContext.ClearContext();

            _loggerMock = Mock.Of<ILogger<CommentService>>();
            _httpServiceMock = new Mock<IHttpService>();
            _commrepository = new CommentRepository(_baseTest.DbContext);
            _deviceRepositoryMock = new Mock<IDeviceRepository>();
            _deviceRepositoryMock.Setup(x => x.HasAsync(It.IsAny<Guid>())).ReturnsAsync(true);

            _commentService = new CommentService(_loggerMock, _httpServiceMock.Object, _commrepository, _deviceRepositoryMock.Object);
        }

        [Fact]
        public async Task ShouldReturnFailIfTokenNullWhenCreateComment()
        {
            //Arrange
            string comment = _baseTest.Fixture.Create<string>();
            Guid id = _baseTest.Fixture.Create<Guid>();

            //Act
            Func<Task> result = async () => await _commentService.CreateAsync(null, id, comment);

            //Assert
            await result.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ShouldReturnSuccessAndCreatedComment()
        {
            //Arrange
            string token = _baseTest.Fixture.Create<string>();
            Guid id = _baseTest.Fixture.Create<Guid>();
            string comment = _baseTest.Fixture.Create<string>();
            Guid userId = _baseTest.Fixture.Create<Guid>();
            string givenName = _baseTest.Fixture.Create<string>();
            string surName = _baseTest.Fixture.Create<string>();

            UserModel userStub = _baseTest.Fixture.Build<UserModel>()
                .With(x => x.CompanyId, 2222)
                .With(x => x.Surname, surName)
                .With(x => x.GivenName, givenName)
                .With(x => x.Id, userId)
                .Create();

            _httpServiceMock.Setup(x => x.GetCurrentUser(It.IsAny<string>())).ReturnsAsync(userStub);

            //Act
            var result = await _commentService.CreateAsync(token, id, comment);

            //Assert
            _baseTest.DbContext.Comments.Count().Should().Be(1);
            var newComment = _baseTest.DbContext.Comments.First();
            newComment.Id.Should().Be(result);
            newComment.IsDeleted.Should().Be(false);
            newComment.UserId.Should().Be(userId);
            newComment.UserName.Should().Be($"{givenName} {surName}");
            newComment.DeviceId.Should().Be(id);
            newComment.Message.Should().Be(comment);
        }

        [Fact]
        public async Task ShouldReturnCommentsFromSameUser()
        {
            //Arrange
            Guid deviceId = _baseTest.Fixture.Create<Guid>();
            string comment = _baseTest.Fixture.Create<string>();
            Guid userId = _baseTest.Fixture.Create<Guid>();

            UserModel userStub = _baseTest.Fixture.Build<UserModel>()
                .With(x => x.CompanyId, 2222)
                .With(x => x.DisplayName, "DDDD")
                .With(x => x.Id, userId)
                .Create();

            Comment commentDb = new Comment()
            {
                Message = comment,
                CreateOn = DateTime.UtcNow,
                DeviceId = deviceId,
                UserId = userId,
                UserName = "DDDD"
            };

            List<UserModel> userList = new List<UserModel>();
            userList.Add(userStub);

            _httpServiceMock.Setup(x => x.GetUsersByIdTrustedAsync(It.IsAny<string>())).ReturnsAsync(userList);
            Guid idComment = await _commrepository.AddAsync(commentDb);

            //Act
            var result = await _commentService.GetAllByDeviceIdAsync(It.IsAny<string>(), deviceId);

            //Assert
            _baseTest.DbContext.Comments.Count().Should().Be(1);
            var newComment = _baseTest.DbContext.Comments.First();
            newComment.Id.Should().Be(idComment);
            newComment.IsDeleted.Should().Be(false);
            newComment.UserId.Should().Be(userId);
            newComment.UserName.Should().Be("DDDD");
            newComment.DeviceId.Should().Be(deviceId);
            newComment.Message.Should().Be(comment);
        }

        [Fact]
        public async Task ShouldReturnCommentsFromDifferentUserName()
        {
            // Arrange
            Guid deviceId = _baseTest.Fixture.Create<Guid>();
            string comment = _baseTest.Fixture.Create<string>();
            Guid userId = _baseTest.Fixture.Create<Guid>();
            Guid userIdFromToken = _baseTest.Fixture.Create<Guid>();

            UserModel userStub = _baseTest.Fixture.Build<UserModel>()
                .With(x => x.CompanyId, 2222)
                .With(x => x.DisplayName, "AAAA")
                .With(x => x.Id, userId)
                .Create();

            Comment commentDb = new Comment()
            {
                Message = comment,
                CreateOn = DateTime.UtcNow,
                DeviceId = deviceId,
                UserId = userId,
                UserName = "DDDD"
            };

            List<UserModel> userList = new List<UserModel>();
            userList.Add(userStub);

            _httpServiceMock.Setup(x => x.GetUsersByIdTrustedAsync(It.IsAny<string>())).ReturnsAsync(userList);
            await _commrepository.AddAsync(commentDb);

            // Act
            var result = await _commentService.GetAllByDeviceIdAsync(It.IsAny<string>(), deviceId);

            //Assert
            _baseTest.DbContext.Comments.Count().Should().Be(1);
            var newComment = result.Result.First();
            newComment.Comment.Should().Be(comment);
            newComment.UserId.Should().Be(userId);
            newComment.UserName.Should().Be("AAAA");
        }

        [Fact]
        public async Task ShouldReturnCommentsFromDeletedUserName()
        {
            // Arrange
            Guid deviceId = _baseTest.Fixture.Create<Guid>();
            string comment = _baseTest.Fixture.Create<string>();
            Guid userId_1 = _baseTest.Fixture.Create<Guid>();
            Guid userId_2 = _baseTest.Fixture.Create<Guid>();
            Guid userIdFromToken = _baseTest.Fixture.Create<Guid>();

            UserModel userStub = _baseTest.Fixture.Build<UserModel>()
                .With(x => x.CompanyId, 2222)
                .With(x => x.DisplayName, "AAAA")
                .With(x => x.Id, userId_1)
                .Create();

            Comment commentDb_1 = new Comment()
            {
                Message = comment,
                CreateOn = DateTime.UtcNow,
                DeviceId = deviceId,
                UserId = userId_1,
                UserName = "DDDD"
            };

            Comment commentDb_2 = new Comment()
            {
                Message = comment,
                CreateOn = DateTime.UtcNow,
                DeviceId = deviceId,
                UserId = userId_2,
                UserName = "TTTT"
            };

            List<Comment> CommentListDb = new List<Comment>();
            CommentListDb.Add(commentDb_1);
            CommentListDb.Add(commentDb_2);
            _baseTest.DbContext.Comments.AddRange(CommentListDb);
            _baseTest.DbContext.SaveChanges();

            List<UserModel> userList = new List<UserModel>();
            userList.Add(userStub);

            _httpServiceMock.Setup(x => x.GetUsersByIdTrustedAsync(It.IsAny<string>())).ReturnsAsync(userList);
            
            // Act
            var result = await _commentService.GetAllByDeviceIdAsync(It.IsAny<string>(), deviceId);

            //Assert
            _baseTest.DbContext.Comments.Count().Should().Be(2);

            var newComment_1 = result.Result.First(x => x.UserId == userId_2);
            newComment_1.Comment.Should().Be(comment);
            newComment_1.UserId.Should().Be(userId_2);
            newComment_1.UserName.Should().Be("TTTT");

            var newComment_2 = result.Result.First(x => x.UserId == userId_1);
            newComment_2.Comment.Should().Be(comment);
            newComment_2.UserId.Should().Be(userId_1);
            newComment_2.UserName.Should().Be("AAAA");
        }

        [Fact]
        public async Task ShouldReturnCommentsLazyLoadResult()
        {
            // Arrange
            Guid deviceId = _baseTest.Fixture.Create<Guid>();
            string comment = _baseTest.Fixture.Create<string>();
            Guid userId = _baseTest.Fixture.Create<Guid>();
            string userName = _baseTest.Fixture.Create<string>();
            string userNameNewForAssert = _baseTest.Fixture.Create<string>();

            LazyLoadParameters filter = new LazyLoadParameters()
            {
                Limit = 1,
                Offset = 2
            };

            var commentList = _baseTest.Fixture.Build<Comment>()
                .With(x => x.Message, comment)
                .With(x => x.CreateOn, DateTime.UtcNow)
                .With(x => x.DeviceId, deviceId)
                .With(x => x.UserId, userId)
                .With(x => x.UserName, userName)
                .Without(x => x.Device)
                .CreateMany(4);

            var userStubList = _baseTest.Fixture.Build<UserModel>()
                .With(x => x.CompanyId, 2222)
                .With(x => x.DisplayName, userNameNewForAssert)
                .With(x => x.Id, userId)
                .CreateMany(1).ToList();

            await _commrepository.AddRangeAsync(commentList);

            _httpServiceMock.Setup(x => x.GetUsersByIdTrustedAsync(It.IsAny<string>())).ReturnsAsync(userStubList);

            // Act
            var result = await _commentService.GetAllByDeviceIdAsync(It.IsAny<string>(), deviceId, filter);

            //Assert
            var commentsresult = result.Result.FirstOrDefault();
            commentsresult.UserName.Should().Be(userNameNewForAssert);

            result.Result.Count().Should().Be(1);
            result.Total.Should().Be(4);
        }
    }
}
