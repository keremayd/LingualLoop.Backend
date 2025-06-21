using Postgres.Models;

namespace Service.DataTransferObjects.Responses;

public class RegisterUserResponse
{
    public User? User { get; set; }
    public string SignedUrl { get; set; }
}