using AwsService.Options;
using Common.Enums;

namespace AwsService.Abstractions;

public interface IAwsBucketNameFactory
{
    string GetBucketName(BucketType type);
}