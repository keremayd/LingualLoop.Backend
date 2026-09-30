using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using AwsService.Abstractions;
using BucketType = Common.Enums.BucketType;

namespace AwsService.Services;

public class AwsService : IAwsService
{
    private readonly IAmazonS3 _amazonClient;
    private readonly IAwsBucketNameFactory _bucketFactory;

    public AwsService(IAmazonS3 amazonClient, IAwsBucketNameFactory bucketFactory)
    {
        _amazonClient = amazonClient;
        _bucketFactory = bucketFactory;
    }
    
    public async Task UploadFileAsync(
        string key,
        Stream fileStream,
        string contentType,
        BucketType bucketType = BucketType.ProfilePhotos)
    {
        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = fileStream,
            Key = key,
            // Kova adı artık sözlükten elle okunmuyor, fabrikadan geliyor;
            // eşleme tek yerde kalsın (`AwsBucketNameFactory`).
            BucketName = _bucketFactory.GetBucketName(bucketType),
            ContentType = contentType
        };

        var transferUtility = new TransferUtility(_amazonClient);
        await transferUtility.UploadAsync(uploadRequest);
    }
    
    public string GeneratePreSignedUrl(string key, BucketType bucketType, int expireMinutes = 15)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketFactory.GetBucketName(bucketType),
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(expireMinutes)
        };

        return _amazonClient.GetPreSignedURL(request);
    }
}