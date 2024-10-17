using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class ValidateUserRequest : IRequest<bool>
{
    public string UserName { get; set; }
    public string Password { get; set; }
}