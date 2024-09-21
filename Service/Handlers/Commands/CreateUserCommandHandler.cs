using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Service.Handlers.Commands;

public class CreateUserCommandHandler : IRequestHandler<CreateUserRequest, CreateUserResponse>
{
    private readonly ILingualLoopGenericRepository<User> _genericRepository;
    private readonly LingualLoopContext _context;

    public CreateUserCommandHandler(ILingualLoopGenericRepository<User> genericRepository)
    {
        _genericRepository = genericRepository;
        _context = genericRepository.GetDbContext();
    }

    public async Task<CreateUserResponse> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var checkUser = await _genericRepository.FirstAsync(u => u.UserNickname.Equals(request.UserNickname), 
            user => new User() { UserId = user.UserId, UserNickname = user.UserNickname});
        if (checkUser == null || checkUser.UserId != 0)
        {
            throw new LingualLoopException(ErrorCode.TheUserNicknameWasRegistered.CreateMessage(checkUser!.UserId),
                ErrorCode.TheUserNicknameWasRegistered.GetDescription(checkUser.UserId), HttpStatusCode.Conflict);
        }
        
        var user = new User()
        {
            UserNickname = request.UserNickname,
            UserScore = {}
        };

        _genericRepository.Create(user);
        var response = await _genericRepository.SaveChangesAsync(cancellationToken);
        if (response == 0)
        {
            throw new LingualLoopException(ErrorCode.NoDataCreatedInUser.CreateMessage(request.UserNickname),
                ErrorCode.NoDataCreatedInUser.GetDescription(request.UserNickname), HttpStatusCode.InternalServerError);
        }

        return new CreateUserResponse()
        {
            UserNickname = request.UserNickname
        };
    }
}