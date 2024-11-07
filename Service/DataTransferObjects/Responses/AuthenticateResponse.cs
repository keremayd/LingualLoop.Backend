namespace Service.DataTransferObjects.Responses;

public class AuthenticateResponse
{
    public string UserId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}