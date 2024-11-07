namespace Service.DataTransferObjects.Requests;

public class TokenDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}