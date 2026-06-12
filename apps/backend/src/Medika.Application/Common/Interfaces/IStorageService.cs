namespace Medika.Application.Common.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string fileKey, CancellationToken ct = default);
    string GetPublicUrl(string fileKey);
}
