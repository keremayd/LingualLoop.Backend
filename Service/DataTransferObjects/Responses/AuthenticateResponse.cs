namespace Service.DataTransferObjects.Responses;

public class AuthenticateResponse
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ProfilePhotoUrl { get; set; } = string.Empty;
    public string UserNickname { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int UserRank { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}