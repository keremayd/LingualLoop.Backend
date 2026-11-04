using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

public class GetKartyPathRequest : IRequest<GetKartyPathResponse>
{
    public string UserId { get; set; } = string.Empty;
}
