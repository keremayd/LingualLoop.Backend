namespace Service.DataTransferObjects.Responses;

public class CreateTokenResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}