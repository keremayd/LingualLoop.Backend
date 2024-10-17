namespace Common.Options;

public class JwtOptions
{
    public string ValidIssuer { get; set; } = string.Empty;
    public string ValidAudience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int Expires { get; set; }
}