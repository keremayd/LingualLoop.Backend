using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null)
            throw new LingualLoopException(ErrorCode.NoDataInUsers.CreateMessage(),
                ErrorCode.NoDataInUsers.GetDescription(), HttpStatusCode.Unauthorized);

        var response = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!response)
            throw new LingualLoopException(ErrorCode.TheUserAuthenticatedFailed.CreateMessage(),
                ErrorCode.TheUserAuthenticatedFailed.GetDescription(), HttpStatusCode.Unauthorized);

        //TODO: UserScore Command'ı oluşturmak gerekir. Aşağıdaki işlemleri oraya aktar.
        
        var userScore = await _context.UserScores
            .Where(us => us.UserId == user.Id)
            .Select(us => us.Score)
            .FirstOrDefaultAsync(cancellationToken);
        
        var rank = await _context.UserScores
            .CountAsync(us => us.Score > userScore, cancellationToken);

        user.UserRank = rank + 1;
        
        return new ValidateUserResponse()
        {
            User = user
        };
    }
}