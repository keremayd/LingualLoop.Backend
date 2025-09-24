namespace AwsService.Options;

public class AwsOptions
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public Dictionary<string, string> Buckets { get; set; } = new Dictionary<string, string>();
}