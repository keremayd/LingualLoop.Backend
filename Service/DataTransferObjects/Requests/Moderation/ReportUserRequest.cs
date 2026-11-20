using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class ReportUserRequest : IRequest<ReportUserResponse>
{
    public string UserId { get; set; } = string.Empty;
    public string ReportedUserId { get; set; } = string.Empty;

    /// name, photo ya da other.
    public string Reason { get; set; } = string.Empty;

    /// Bildirimin yapıldığı yer; şimdilik yalnız league.
    public string Context { get; set; } = "league";
}
