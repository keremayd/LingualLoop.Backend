using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using AwsService.Abstractions;
using AwsService.Options;
using Microsoft.Extensions.Options;

namespace AwsService.Services;

public class AwsService : IAwsService
{
    private readonly AwsOptions _options;
    private readonly IAmazonS3 _amazonClient;

    public AwsService(IOptions<AwsOptions> options, IAmazonS3 amazonClient)
    {
        _options = options.Value;
        _amazonClient = amazonClient;
    }
    
    public async Task UploadFileAsync(string key, Stream fileStream, string contentType)
    {
        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = fileStream,
            Key = key,
            BucketName = _options.BucketName,
            ContentType = contentType
        };

        var transferUtility = new TransferUtility(_amazonClient);
        await transferUtility.UploadAsync(uploadRequest);
    }
    
    public string GeneratePreSignedUrl(string key, int expireMinutes = 15)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(expireMinutes)
        };

        return _amazonClient.GetPreSignedURL(request);
    }
}