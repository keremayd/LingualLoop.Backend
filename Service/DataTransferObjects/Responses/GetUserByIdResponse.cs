using Postgres.Models;

namespace Service.DataTransferObjects.Responses;

public class GetUserByIdResponse
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public UserScore UserScore { get; set; } = new UserScore();
}