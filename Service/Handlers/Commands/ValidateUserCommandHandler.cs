using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Service.Handlers.Commands;

public class ValidateUserCommandHandler : IRequestHandler<ValidateUserRequest, ValidateUserResponse>
{
    private readonly ILingualLoopGenericRepository<User> _genericRepository;
    private readonly LingualLoopContext _context;
    private readonly UserManager<User> _userManager;

    public ValidateUserCommandHandler(ILingualLoopGenericRepository<User> genericRepository, UserManager<User> userManager)
    {
        _genericRepository = genericRepository;
        _userManager = userManager;
        _context = genericRepository.GetDbContext();
    }

    public async Task<ValidateUserResponse> Handle(ValidateUserRequest request, CancellationToken cancellationToken)
    {
        User u = new User()
        {
            UserName = request.UserName,
            PasswordHash = request.Password,
            UserScore = {}
        };
        
        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null)
            throw new LingualLoopException(ErrorCode.NoDataInUsers.CreateMessage(),
                ErrorCode.NoDataInUsers.GetDescription(), HttpStatusCode.Unauthorized);

        var response = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!response)
            throw new LingualLoopException(ErrorCode.TheUserAuthenticatedFailed.CreateMessage(),
                ErrorCode.TheUserAuthenticatedFailed.GetDescription(), HttpStatusCode.Unauthorized);

        return new ValidateUserResponse()
        {
            UserId = user.Id,
            UserNickname = user.UserNickname,
            UserName = user.UserName!
        };
    }
}