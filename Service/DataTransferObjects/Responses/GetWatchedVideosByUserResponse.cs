using Postgres.Models;

namespace Service.DataTransferObjects.Responses;

public class GetWatchedVideosByUserResponse
{
    public int UserId { get; set; }
    public string UserNickname { get; set; } = string.Empty;
    public ICollection<UserVideoHistory>? UserVideoHistories { get; set; }
}