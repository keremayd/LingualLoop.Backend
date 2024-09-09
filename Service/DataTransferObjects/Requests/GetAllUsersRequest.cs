using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class GetAllUsersRequest: IRequest<List<GetAllUsersResponse>>
{
    public int UserId { get; set; }
}