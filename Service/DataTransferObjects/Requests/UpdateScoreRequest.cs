using MediatR;

namespace Service.DataTransferObjects.Requests;

public class UpdateScoreRequest: IRequest<bool>
{
    public int UserId { get; set; }
    public int Point { get; set; }
}