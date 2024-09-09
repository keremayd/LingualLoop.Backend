using MediatR;
using Postgres.Models;

namespace Service.DataTransferObjects.Requests;

public class CreateUserRequest: IRequest<bool>
{
    public string UserName { get; set; } = string.Empty;
}