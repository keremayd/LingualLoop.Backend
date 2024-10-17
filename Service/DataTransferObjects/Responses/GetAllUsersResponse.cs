namespace Service.DataTransferObjects.Responses;

public class GetAllUsersResponse
{
    public string UserId { get; set; } = string.Empty;
    public string? UserNickname { get; set; }
}