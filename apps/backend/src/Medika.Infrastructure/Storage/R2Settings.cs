namespace Medika.Infrastructure.Storage;

public class R2Settings
{
    public const string Section = "R2";
    public string AccountId { get; set; } = null!;
    public string AccessKeyId { get; set; } = null!;
    public string SecretAccessKey { get; set; } = null!;
    public string BucketName { get; set; } = null!;
    public string PublicUrl { get; set; } = null!;       // e.g. https://files.medika.app
}
