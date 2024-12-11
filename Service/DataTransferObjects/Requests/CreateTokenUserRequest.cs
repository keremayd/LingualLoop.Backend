using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class CreateTokenRequest : IRequest<CreateTokenResponse>
{
    public string? Id { get; set; }
    public string? Password { get; set; }
    public bool PopulateExp { get; set; }
}