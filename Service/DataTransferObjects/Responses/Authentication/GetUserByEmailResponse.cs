using Postgres.Models;

namespace Service.DataTransferObjects.Responses.Authentication;

public class GetUserByEmailResponse
{
    public User? User { get; set; }
}