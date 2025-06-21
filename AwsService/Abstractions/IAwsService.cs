namespace AwsService.Abstractions;

public interface IAwsService
{
    Task UploadFileAsync(string key, Stream fileStream, string contentType);
    string GeneratePreSignedUrl(string key, int expireMinutes = 120);
}