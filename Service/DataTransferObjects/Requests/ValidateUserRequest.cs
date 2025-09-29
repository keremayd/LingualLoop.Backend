using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class ValidateUserRequest : IRequest<ValidateUserResponse>
{
    public string Email { get; set; }
    public string Password { get; set; }
}