using Postgres.Models;

namespace Service.DataTransferObjects.Responses;

public class AppleLoginResponse
{
    public User User { get; set; } = null!;
}
