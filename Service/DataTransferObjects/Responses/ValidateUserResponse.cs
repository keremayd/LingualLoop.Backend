using Postgres.Models;

namespace Service.DataTransferObjects.Responses;

public class ValidateUserResponse
{
    public User User { get; set; }
}