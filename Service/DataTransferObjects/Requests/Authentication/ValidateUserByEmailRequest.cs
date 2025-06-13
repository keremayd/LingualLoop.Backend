namespace Service.DataTransferObjects.Requests.Authentication;

public class ValidateUserByEmailRequest
{
    public string IdToken { get; set; } = string.Empty;
}