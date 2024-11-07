using MediatR;
using Postgres.Models;

namespace Service.DataTransferObjects.Requests;

public class RefreshTokenRequest : IRequest<User>
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}