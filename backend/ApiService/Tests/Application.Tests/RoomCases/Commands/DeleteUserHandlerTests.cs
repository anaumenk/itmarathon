using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Queries;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using Epam.ItMarathon.ApiService.Domain.Entities.User;
using FluentAssertions;
using FluentValidation.Results;
using NSubstitute;

namespace Epam.ItMarathon.ApiService.Application.Tests.RoomCases.Commands
{
    public class DeleteUserHandlerTests
    {
        private readonly IUserReadOnlyRepository _userRepositoryMock;
        private readonly IRoomRepository _roomRepositoryMock;
        private readonly DeleteUserHandler _handler;

        public DeleteUserHandlerTests()
        {
            _userRepositoryMock = Substitute.For<IUserReadOnlyRepository>();
            _roomRepositoryMock = Substitute.For<IRoomRepository>();
            _handler = new DeleteUserHandler(_userRepositoryMock, _roomRepositoryMock);
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenUserCodeNotExists()
        {
            // Arrange
            var fakeUser = DataFakers.UserFaker.Generate(); 
            var fakeUserCode = "test-user-code";
            var request = new DeleteUserRequest(fakeUserCode, fakeUser.Id);
            
            _userRepositoryMock
                .GetByCodeAsync(fakeUserCode, Arg.Any<CancellationToken>(), true)
                .Returns(Result.Failure<User, ValidationResult>(
                    new NotFoundError([
                        new ValidationFailure("userCode", "User with such userCode is not found.")
                    ])
                ));

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Errors.Should().Contain(e => e.PropertyName == "userCode");
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenUserIdNotExists()
        {
            // Arrange
            var authUser = DataFakers.UserFaker.Generate(); 
            ulong fakeUserId = 1;
            var request = new DeleteUserRequest(authUser.AuthCode, fakeUserId);
            
            _userRepositoryMock
                    .GetByCodeAsync(authUser.AuthCode, Arg.Any<CancellationToken>(), true)
                    .Returns(authUser);
            
            _userRepositoryMock
                .GetByIdAsync(fakeUserId, Arg.Any<CancellationToken>(), false, false)
                .Returns(Result.Failure<User, ValidationResult>(
                    new NotFoundError([
                        new ValidationFailure("id", "User with such Id is not found.")
                    ])
                ));

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Errors.Should().Contain(e => e.PropertyName == "id");
        }
        
        [Fact]
        public async Task Handle_ShouldReturnNotAuthorized_WhenUsersInDifferentRooms()
        {
            // Arrange
            var adminUser = DataFakers.ValidUserBuilder
                .WithRoomId(1)
                .Build();
        
            var targetUser = DataFakers.ValidUserBuilder
                .WithRoomId(2)
                .Build();
        
            _userRepositoryMock.GetByCodeAsync(adminUser.AuthCode, Arg.Any<CancellationToken>(), true)
                .Returns(adminUser);
            _userRepositoryMock.GetByIdAsync(targetUser.Id, Arg.Any<CancellationToken>())
                .Returns(targetUser);
        
            var request = new DeleteUserRequest(adminUser.AuthCode, targetUser.Id);
        
            // Act
            var result = await _handler.Handle(request, CancellationToken.None);
        
            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotAuthorizedError>();
            result.Error.Errors.Should().Contain(e => e.PropertyName == "id");
        }
        
        [Fact]
        public async Task Handle_ShouldReturnBadRequest_WhenUserDeletesSelf()
        {
            // Arrange
            var fakeUser = DataFakers.UserFaker.Generate();
        
            _userRepositoryMock.GetByCodeAsync(fakeUser.AuthCode, Arg.Any<CancellationToken>(), true)
                .Returns(fakeUser);
            _userRepositoryMock.GetByIdAsync(fakeUser.Id, Arg.Any<CancellationToken>())
                .Returns(fakeUser);
        
            var request = new DeleteUserRequest(fakeUser.AuthCode, fakeUser.Id);
        
            // Act
            var result = await _handler.Handle(request, CancellationToken.None);
        
            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<BadRequestError>();
            result.Error.Errors.Should().Contain(e => e.PropertyName == "id");
        }
        
        [Fact]
        public async Task Handle_ShouldReturnForbidden_WhenAuthUserNotAdmin()
        {
            // Arrange
            var roomBuilder = DataFakers.ValidRoomBuilder;
            var authUserCode = Guid.NewGuid().ToString();
            roomBuilder.AddUser(u => u
                .WithAuthCode(authUserCode)
                .WithId(1)
                .WithIsAdmin(false)
                .WithFirstName("Auth")
                .WithLastName("Auth")
                .WithDeliveryInfo("Some info...")
                .WithPhone("+380000000000")
                .WithWishes([])
                .WithWantSurprise(true)
                .WithInterests("Some interests...")
            );
            ulong targetUserId = 2;
            roomBuilder.AddUser(u => u
                .WithId(targetUserId)
                .WithIsAdmin(false)
                .WithFirstName("Target")
                .WithLastName("Target")
                .WithDeliveryInfo("Some info...")
                .WithPhone("+380000000000")
                .WithInterests("Some interests...")
                .WithWishes([])
                .WithWantSurprise(true)
            );
            
            var room = roomBuilder.Build().Value;
            
            var authUser = room.Users.First(u => u.AuthCode == authUserCode);
            var targetUser = room.Users.First(u => u.Id == targetUserId);
        
            _userRepositoryMock.GetByCodeAsync(authUser.AuthCode, Arg.Any<CancellationToken>(), true).Returns(authUser);
            _userRepositoryMock.GetByIdAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(targetUser);
            _roomRepositoryMock.GetByUserCodeAsync(authUser.AuthCode, Arg.Any<CancellationToken>()).Returns(room);
        
            var request = new DeleteUserRequest(authUser.AuthCode, targetUser.Id);
        
            // Act
            var result = await _handler.Handle(request, CancellationToken.None);
        
            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            result.Error.Errors.Should().Contain(e => e.PropertyName == "userCode");
        }
        
        [Fact]
        public async Task Handle_ShouldDeleteUser_WhenValid()
        {
            var roomBuilder = DataFakers.ValidRoomBuilder;
            var authUserCode = Guid.NewGuid().ToString();
            roomBuilder.AddUser(u => u
                .WithAuthCode(authUserCode)
                .WithId(1)
                .WithIsAdmin(true)
                .WithFirstName("Auth")
                .WithLastName("Auth")
                .WithDeliveryInfo("Some info...")
                .WithPhone("+380000000000")
                .WithWishes([])
                .WithWantSurprise(true)
                .WithInterests("Some interests...")
            );
            ulong targetUserId = 2;
            roomBuilder.AddUser(u => u
                .WithId(targetUserId)
                .WithIsAdmin(false)
                .WithFirstName("Target")
                .WithLastName("Target")
                .WithDeliveryInfo("Some info...")
                .WithPhone("+380000000000")
                .WithInterests("Some interests...")
                .WithWishes([])
                .WithWantSurprise(true)
            );
            
            var room = roomBuilder.Build().Value;
            
            var authUser = room.Users.First(u => u.AuthCode == authUserCode);
            var targetUser = room.Users.First(u => u.Id == targetUserId);
        
            _userRepositoryMock.GetByCodeAsync(authUser.AuthCode, Arg.Any<CancellationToken>(), true).Returns(authUser);
            _userRepositoryMock.GetByIdAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(targetUser);
            _roomRepositoryMock.GetByUserCodeAsync(authUser.AuthCode, Arg.Any<CancellationToken>()).Returns(room);
        
            var request = new DeleteUserRequest(authUser.AuthCode, targetUser.Id);
        
            // Act
            var result = await _handler.Handle(request, CancellationToken.None);
        
            // Assert
            result.IsSuccess.Should().BeTrue();
            room.Users.Should().NotContain(u => u.Id == targetUser.Id);
        }
    }
}
