using AwsService.Abstractions;
using AwsService.Options;
using Common.Enums;
using Common.Extensions;
using Microsoft.Extensions.Options;

namespace AwsService;

public class AwsBucketNameFactory : IAwsBucketNameFactory
{
    private readonly AwsOptions _options;

    public AwsBucketNameFactory(IOptions<AwsOptions> options)
    {
        _options = options.Value;
    }

    public string GetBucketName(BucketType type)
    {
        return type switch
        {
            BucketType.ProfilePhotos => _options.Buckets["ProfilePhotos"],
            BucketType.KartyAssets => _options.Buckets["KartyAssets"],
            _ => throw new ArgumentException("Invalid bucket type", nameof(type))
        };
    }
}
