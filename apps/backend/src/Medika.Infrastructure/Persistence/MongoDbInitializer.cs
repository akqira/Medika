using Medika.Domain.Finance;
using Medika.Domain.Identity;
using Medika.Domain.Medical;
using Medika.Domain.Patients;
using Medika.Domain.Scheduling;
using Medika.Infrastructure.Audit;
using MongoDB.Driver;

namespace Medika.Infrastructure.Persistence;

public static class MongoDbInitializer
{
    public static async Task InitializeAsync(MongoContext ctx)
    {
        await CreatePatientIndexesAsync(ctx);
        await CreateAppointmentIndexesAsync(ctx);
        await CreateConsultationIndexesAsync(ctx);
        await CreateInvoiceIndexesAsync(ctx);
        await CreateChargeIndexesAsync(ctx);
        await CreateUserIndexesAsync(ctx);
        await CreateAuditLogIndexesAsync(ctx);
    }

    public static async Task SeedAsync(MongoContext ctx)
    {
        var anyDoctor = await ctx.Users
            .Find(u => u.Role == Role.Doctor)
            .AnyAsync();

        if (anyDoctor) return;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Doctor@123");
        var doctor = User.Create(
            email: "kader.kebir@gmail.com",
            passwordHash: passwordHash,
            firstName: "Abdelkader",
            lastName: "Kebir",
            role: Role.Doctor,
            specialty: "Médecine générale",
            orderNumber: "RPPS-12345678");

        await ctx.Users.InsertOneAsync(doctor);
    }

    private static async Task CreatePatientIndexesAsync(MongoContext ctx)
    {
        var col = ctx.Patients;
        var builders = Builders<Patient>.IndexKeys;

        // Ensure the nss index is sparse — drop the old non-sparse version if it exists
        try { await col.Indexes.DropOneAsync("nss_1"); } catch { }

        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Patient>(builders.Combine(
                builders.Ascending(p => p.LastName),
                builders.Ascending(p => p.FirstName))),
            new CreateIndexModel<Patient>(
                builders.Ascending(p => p.Nss),
                new CreateIndexOptions { Unique = true, Sparse = true }),
        ]);
    }

    private static async Task CreateAppointmentIndexesAsync(MongoContext ctx)
    {
        var col = ctx.Appointments;
        var builders = Builders<Appointment>.IndexKeys;
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Appointment>(builders.Ascending(a => a.Date)),
            new CreateIndexModel<Appointment>(builders.Combine(
                builders.Ascending(a => a.DoctorId),
                builders.Ascending(a => a.Date),
                builders.Ascending(a => a.Time))),
            new CreateIndexModel<Appointment>(builders.Combine(
                builders.Ascending(a => a.PatientId),
                builders.Ascending(a => a.Date))),
        ]);
    }

    private static async Task CreateConsultationIndexesAsync(MongoContext ctx)
    {
        var col = ctx.Consultations;
        var builders = Builders<Consultation>.IndexKeys;
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Consultation>(builders.Combine(
                builders.Ascending(c => c.PatientId),
                builders.Descending(c => c.Date))),
            new CreateIndexModel<Consultation>(
                builders.Ascending(c => c.AppointmentId),
                new CreateIndexOptions { Sparse = true }),
        ]);
    }

    private static async Task CreateInvoiceIndexesAsync(MongoContext ctx)
    {
        var col = ctx.Invoices;
        var builders = Builders<Invoice>.IndexKeys;
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Invoice>(builders.Ascending(i => i.PatientId)),
            new CreateIndexModel<Invoice>(builders.Combine(
                builders.Ascending(i => i.DoctorId),
                builders.Ascending(i => i.Status))),
            new CreateIndexModel<Invoice>(builders.Combine(
                builders.Ascending(i => i.DoctorId),
                builders.Descending(i => i.IssuedAt))),
            new CreateIndexModel<Invoice>(
                builders.Ascending(i => i.Number),
                new CreateIndexOptions { Unique = true }),
        ]);
    }

    private static async Task CreateChargeIndexesAsync(MongoContext ctx)
    {
        var col = ctx.Charges;
        var builders = Builders<Charge>.IndexKeys;
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Charge>(builders.Combine(
                builders.Ascending(c => c.DoctorId),
                builders.Descending(c => c.Date))),
            new CreateIndexModel<Charge>(builders.Ascending(c => c.Category)),
        ]);
    }

    private static async Task CreateUserIndexesAsync(MongoContext ctx)
    {
        var col = ctx.Users;
        await col.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true }));
    }

    private static async Task CreateAuditLogIndexesAsync(MongoContext ctx)
    {
        var col = ctx.AuditLogs;
        var builders = Builders<AuditLog>.IndexKeys;
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<AuditLog>(builders.Combine(
                builders.Ascending(a => a.EntityType),
                builders.Ascending(a => a.EntityId))),
            new CreateIndexModel<AuditLog>(builders.Ascending(a => a.UserId)),
            new CreateIndexModel<AuditLog>(builders.Descending(a => a.Timestamp)),
            // TTL: keep audit logs for 2 years
            new CreateIndexModel<AuditLog>(
                builders.Ascending(a => a.Timestamp),
                new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(730) }),
        ]);
    }
}
