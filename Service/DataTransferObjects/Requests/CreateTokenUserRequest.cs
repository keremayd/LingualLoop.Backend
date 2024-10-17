using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class CreateTokenRequest : IRequest<string>
{
    public string UserName { get; set; }
    public string Password { get; set; }
}