using Common.Enums;

namespace AwsService.Abstractions;

public interface IAwsService
{
    Task UploadFileAsync(string key, Stream fileStream, string contentType);
    string GeneratePreSignedUrl(string key, BucketType bucketType, int expireMinutes = 120);
}