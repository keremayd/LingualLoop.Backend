using Postgres.Models;

namespace Service.DataTransferObjects.Responses;

public class GetWatchedVideosByUserResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserNickname { get; set; } = string.Empty;
    public ICollection<UserVideoHistory>? UserVideoHistories { get; set; }
}