using Postgres.Models;

namespace Service.DataTransferObjects.Responses;

public class GetScoreByIdResponse
{
    public int UserId { get; set; }
    public string UserNickname { get; set; } = string.Empty;
    public UserScore UserScore { get; set; } = new UserScore();
}