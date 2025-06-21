using Postgres.Models;

namespace Service.DataTransferObjects.Responses;

public class GetUserByIdResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserNickname { get; set; } = string.Empty;
    public string? ProfilePhoto { get; set; } = string.Empty;
    public UserScore UserScore { get; set; } = new UserScore();
}