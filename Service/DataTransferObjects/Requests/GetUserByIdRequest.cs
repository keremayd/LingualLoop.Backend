using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class GetUserByIdRequest: IRequest<GetUserByIdResponse>
{
    public string UserId { get; set; } = string.Empty;
}