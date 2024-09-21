using MediatR;
using Postgres.Models;
using Service.DataTransferObjects.Responses;

namespace Service.DataTransferObjects.Requests;

public class CreateUserRequest: IRequest<CreateUserResponse>
{
    public string UserNickname { get; set; } = string.Empty;
}