using MediatR;
using Service.DataTransferObjects.Responses.Karty;

namespace Service.DataTransferObjects.Requests.Karty;

public class RecordWrongKartyRequest : IRequest<RecordWrongKartyResponse>
{
    public string UserId { get; set; } = string.Empty;
    public int KartyId { get; set; }
}
