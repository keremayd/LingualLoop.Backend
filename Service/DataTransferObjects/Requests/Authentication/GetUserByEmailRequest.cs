using MediatR;
using Service.DataTransferObjects.Responses.Authentication;

namespace Service.DataTransferObjects.Requests.Authentication;

public class GetUserByEmailRequest : IRequest<GetUserByEmailResponse>
{
    public string Email { get; set; } = string.Empty;
}