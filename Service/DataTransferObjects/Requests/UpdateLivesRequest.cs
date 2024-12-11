using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class UpdateLivesRequest : IRequest<UpdateLivesResponse>
{
    public string UserId { get; set; } = string.Empty;
}