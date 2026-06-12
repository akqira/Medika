namespace Medika.Infrastructure.Persistence;

public class MongoSettings
{
    public const string Section = "MongoDB";
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = "medika";
}
