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

    public async Task DeleteFolderAsync(
        string prefix,
        BucketType bucketType = BucketType.ProfilePhotos)
    {
        if (string.IsNullOrWhiteSpace(prefix) ||
            !prefix.EndsWith('/') ||
            prefix.Trim('/').Length == 0)
        {
            throw new ArgumentException("Önek '/' ile biten bir klasör olmalı.", nameof(prefix));
        }

        var bucket = _bucketFactory.GetBucketName(bucketType);
        var request = new ListObjectsV2Request { BucketName = bucket, Prefix = prefix };
        ListObjectsV2Response page;
        do
        {
            page = await _amazonClient.ListObjectsV2Async(request);
            // SDK 4'te boş sonuçta liste null geliyor.
            if (page.S3Objects is { Count: > 0 })
            {
                await _amazonClient.DeleteObjectsAsync(new DeleteObjectsRequest
                {
                    BucketName = bucket,
                    Objects = page.S3Objects
                        .Select(item => new KeyVersion { Key = item.Key })
                        .ToList(),
                });
            }

            request.ContinuationToken = page.NextContinuationToken;
        } while (page.IsTruncated == true);
    }
}