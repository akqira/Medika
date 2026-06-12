using Amazon.Runtime;
using Amazon.S3;
using Medika.Application.Common.Interfaces;
using Medika.Domain.Finance;
using Medika.Domain.Identity;
using Medika.Domain.Medical;
using Medika.Domain.Patients;
using Medika.Domain.Scheduling;
using Medika.Infrastructure.Audit;
using Medika.Infrastructure.Auth;
using Medika.Infrastructure.Persistence;
using Medika.Infrastructure.Pdf;
using Medika.Infrastructure.Persistence.Mappings;
using Medika.Infrastructure.Persistence.Repositories;
using Medika.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using QuestPDF.Infrastructure;

namespace Medika.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        DomainMappings.Register();

        // QuestPDF Community licence — free for companies under $1M annual revenue.
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddMongoDB(config);
        services.AddAuth(config);
        services.AddStorage(config);
        services.AddAudit();
        services.AddScoped<IPrescriptionPdfGenerator, PrescriptionPdfGenerator>();

        return services;
    }

    private static void AddMongoDB(this IServiceCollection services, IConfiguration config)
    {
        var settings = config.GetSection(MongoSettings.Section).Get<MongoSettings>()!;
        services.AddSingleton(settings);
        services.AddSingleton<IMongoClient>(_ => new MongoClient(settings.ConnectionString));
        services.AddSingleton<MongoContext>();

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IChargeRepository, ChargeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
    }

    private static void AddAuth(this IServiceCollection services, IConfiguration config)
    {
        var jwtSettings = config.GetSection(JwtSettings.Section).Get<JwtSettings>()!;
        services.AddSingleton(jwtSettings);
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
    }

    private static void AddStorage(this IServiceCollection services, IConfiguration config)
    {
        var r2 = config.GetSection(R2Settings.Section).Get<R2Settings>()!;
        services.AddSingleton(r2);
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
            new BasicAWSCredentials(r2.AccessKeyId, r2.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = $"https://{r2.AccountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true,
            }));
        services.AddScoped<IStorageService, R2StorageService>();
    }

    private static void AddAudit(this IServiceCollection services)
    {
        services.AddScoped<IAuditService, AuditService>();
    }
}
