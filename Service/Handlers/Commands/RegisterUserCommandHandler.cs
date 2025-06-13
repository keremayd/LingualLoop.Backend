using System.Net;
using Common.Enums;
using Common.Errors;
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

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserRequest, RegisterUserResponse>
{
    private readonly UserManager<User> _userManager;

    public RegisterUserCommandHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    private async Task CreateUserAsync(User user)
    {
        IdentityResult result;
        List<ErrorList> errorList;
        
        // Şifresiz geldiyse db'ye şifre göndermiyoruz. Örn: Google ile giriş
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
                return;
            
            errorList = result.Errors.Select(error => new ErrorList()
            {
                ErrorCode = error.Code,
                ErrorDescription = error.Description
            }).ToList();

            throw new LingualLoopException(ErrorCode.TheUserNotCreatedInDatabase.CreateMessage(user.UserName), errorList,
                ErrorCode.TheUserNotCreatedInDatabase.GetDescription(), HttpStatusCode.BadRequest);
        }
        
        result = await _userManager.CreateAsync(user, user.PasswordHash);
        if (result.Succeeded)
            return;
        
        errorList = result.Errors.Select(error => new ErrorList()
        {
            ErrorCode = error.Code,
            ErrorDescription = error.Description
        }).ToList();

        throw new LingualLoopException(ErrorCode.TheUserNotCreatedInDatabase.CreateMessage(user.UserName), errorList,
            ErrorCode.TheUserNotCreatedInDatabase.GetDescription(), HttpStatusCode.BadRequest);
    }

    public async Task<RegisterUserResponse> Handle(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var refreshToken = CreateTokenCommandHandler.GenerateRefreshToken();
        User u = new User()
        {
            Email = request.Email,
            UserName = request.UserName,
            UserNickname = request.UserName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PasswordHash = request.Password,
            PhoneNumber = request.PhoneNumber,
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow,
            UserScore = {}
        };
        
        await CreateUserAsync(u);

        await _userManager.AddToRolesAsync(u, request.Roles);

        return new RegisterUserResponse()
        {
            User = u
        };
    }
}