using Postgres.Models;

namespace Service.DataTransferObjects.Responses;

public class GetScoreWithLivesByIdResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserNickname { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Lives { get; set; }
}