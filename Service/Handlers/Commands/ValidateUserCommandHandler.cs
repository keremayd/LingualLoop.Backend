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
    private readonly IStelindaGenericRepository<User> _genericRepository;
    private readonly StelindaContext _context;
    private readonly UserManager<User> _userManager;

    public ValidateUserCommandHandler(IStelindaGenericRepository<User> genericRepository, UserManager<User> userManager)
    {
        _genericRepository = genericRepository;
        _userManager = userManager;
        _context = genericRepository.GetDbContext();
    }

    public async Task<ValidateUserResponse> Handle(ValidateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            throw new StelindaException(ErrorCode.NoDataInUsers.CreateMessage(),
                ErrorCode.NoDataInUsers.GetDescription(), HttpStatusCode.Unauthorized);

        var response = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!response)
            throw new StelindaException(ErrorCode.TheUserAuthenticatedFailed.CreateMessage(),
                ErrorCode.TheUserAuthenticatedFailed.GetDescription(), HttpStatusCode.Unauthorized);
        
        return new ValidateUserResponse()
        {
            User = user
        };
    }
}