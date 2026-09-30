using MediatR;
using Service.DataTransferObjects.Responses.Profile;

namespace Service.DataTransferObjects.Requests.Profile;

public class RecordDailyActivityRequest : IRequest<RecordDailyActivityResponse>
{
    public string UserId { get; set; } = string.Empty;
}
