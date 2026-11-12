using MediatR;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class DeleteAccountRequest : IRequest<DeleteAccountResponse>
{
    public string UserId { get; set; } = string.Empty;
}
