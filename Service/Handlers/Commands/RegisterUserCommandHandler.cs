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

    private Dictionary<string, string> AddErrorInDictionary(IEnumerable<IdentityError> identityErrors)
    {
        var errorDictionary = new Dictionary<string, string>();

        foreach (var error in identityErrors)
        {
            switch (error.Code)
            {
                case "DuplicateUserName":
                    errorDictionary["UserName"] = "Username is already taken.";
                    break;
                case "DuplicateEmail":
                    errorDictionary["Email"] = "Email is already registered.";
                    break;
                case "DuplicateUserNickname":
                    errorDictionary["UserNickname"] = "User nickname is already taken.";
                    break;

                default:
                    errorDictionary["General"] = error.Description;
                    break;
            }
        }

        return errorDictionary;
    }

    public async Task<RegisterUserResponse> Handle(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        User u = new User()
        {
            Email = request.Email,
            UserName = request.UserName,
            UserNickname = request.UserNickname,
            PasswordHash = request.Password,
            PhoneNumber = request.PhoneNumber,
            UserScore = {}
        };
        
        var result = await _userManager.CreateAsync(u, request.Password);
        if (!result.Succeeded)
        {
            var errorList = result.Errors.Select(error => new ErrorList()
            {
                ErrorCode = error.Code,
                ErrorDescription = error.Description
            }).ToList();

            throw new LingualLoopException(ErrorCode.TheUserNotCreatedInDatabase.CreateMessage(u.UserName), errorList,
                ErrorCode.TheUserNotCreatedInDatabase.GetDescription(), HttpStatusCode.BadRequest);
        }

        await _userManager.AddToRolesAsync(u, request.Roles);

        return new RegisterUserResponse()
        {
            User = u
        };
    }
}