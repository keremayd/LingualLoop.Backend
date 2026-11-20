using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class BlockUserRequest : IRequest<BlockUserResponse>
{
    public string UserId { get; set; } = string.Empty;
    public string BlockedUserId { get; set; } = string.Empty;

    /// false ise engel kaldırılır.
    public bool Block { get; set; } = true;
}
