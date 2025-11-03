using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Queries;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentValidation.Results;
using MediatR;
using RoomAggregate = Epam.ItMarathon.ApiService.Domain.Aggregate.Room.Room;

namespace Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers
{
    /// <summary>
    /// Handler for deleting a user from the room.
    /// </summary>
    /// <param name="userRepository">Read-only repository for accessing user data.</param>
    /// <param name="roomRepository">Repository for accessing and modifying room data.</param>
    public class DeleteUserHandler(IUserReadOnlyRepository userRepository, IRoomRepository roomRepository)
        : IRequestHandler<DeleteUserRequest, Result<RoomAggregate, ValidationResult>>
    {
        ///<inheritdoc/>
        public async Task<Result<RoomAggregate, ValidationResult>> Handle(DeleteUserRequest request,
            CancellationToken cancellationToken)
        {
            // Check user with UserCode exist
            var authUserResult = await userRepository.GetByCodeAsync(
                request.UserCode, cancellationToken, includeRoom: true
            );
            if (authUserResult.IsFailure)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(
                    new NotFoundError([
                        new ValidationFailure("userCode", "User with such userCode is not found.")
                    ])
                );
            }
            
            // Check user with id exist
            var requestedUserResult = await userRepository.GetByIdAsync(
                request.UserId.Value, cancellationToken
            );
            if (requestedUserResult.IsFailure)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(
                    new NotFoundError([
                        new ValidationFailure("id", "User with such Id is not found.")
                    ])
                );
            }
            
            // Check if users belong to the same room
            if (requestedUserResult.Value.RoomId != authUserResult.Value.RoomId)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new NotAuthorizedError([
                    new ValidationFailure("id", "User with userCode and user with Id belongs to different rooms.")
                ]));
            }
            
            // Check if users are not the same person
            if (requestedUserResult.Value.Id == authUserResult.Value.Id)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(
                    new BadRequestError([
                        new ValidationFailure("id", "User cannot delete themselves.")
                    ])
                );
            }
            
            // Get room by user code
            var roomResult = await roomRepository.GetByUserCodeAsync(request.UserCode, cancellationToken);
            if (roomResult.IsFailure)
            {
                return roomResult;
            }
            
            // Check if user with userCode is admin
            var authUser = roomResult.Value.Users.First(user => user.AuthCode.Equals(request.UserCode));
            if (!authUser.IsAdmin)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new ForbiddenError([
                    new ValidationFailure("userCode", "Only admin can remove users")
                ]));
            }
            
            // Delete user by id in room
            var room = roomResult.Value;
            var deleteResult = room.DeleteUser(request.UserId);
            if (deleteResult.IsFailure)
            {
                return deleteResult;
            }
            
            // Update room in repository
            var updateResult = await roomRepository.UpdateAsync(room, cancellationToken);
            if (updateResult.IsFailure)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new BadRequestError([
                    new ValidationFailure(string.Empty, updateResult.Error)
                ]));
            }
            
            // Get updated room
            var updatedRoomResult = await roomRepository.GetByUserCodeAsync(request.UserCode, cancellationToken);
            return updatedRoomResult;
        }
    }
}