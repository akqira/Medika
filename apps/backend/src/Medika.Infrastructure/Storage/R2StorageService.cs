using Amazon.S3;
using Amazon.S3.Model;
using Medika.Application.Common.Interfaces;

namespace Medika.Infrastructure.Storage;

public class R2StorageService(IAmazonS3 s3, R2Settings settings) : IStorageService
{
    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}/{fileName}";
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = settings.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true,
        }, ct);
        return key;
    }

    public async Task DeleteAsync(string fileKey, CancellationToken ct = default) =>
        await s3.DeleteObjectAsync(settings.BucketName, fileKey, ct);

    public string GetPublicUrl(string fileKey) => $"{settings.PublicUrl.TrimEnd('/')}/{fileKey}";
}
