using Medika.Domain.Finance;
using Medika.Domain.Identity;
using Medika.Domain.Medical;
using Medika.Domain.Patients;
using Medika.Domain.Scheduling;
using Medika.Infrastructure.Audit;
using MongoDB.Bson;
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
        await CreateActIndexesAsync(ctx);
        await CreateUserIndexesAsync(ctx);
        await CreateAuditLogIndexesAsync(ctx);
        await BackfillCabinetIdAsync(ctx);
        await BackfillRoleRenameAsync(ctx);
    }

    /// <summary>
    /// Idempotent rename of the staff role string "Receptionist" → "Secretary" (issue #24).
    /// Enums serialize as strings, so any pre-rename document would otherwise fail to deserialize.
    /// Safe to run on every startup — matched count is 0 once migrated.
    /// </summary>
    private static async Task BackfillRoleRenameAsync(MongoContext ctx)
    {
        var result = await ctx.Users.UpdateManyAsync(
            new BsonDocument("role", "Receptionist"),
            new BsonDocument("$set", new BsonDocument("role", "Secretary")));
        if (result.ModifiedCount > 0)
            Console.WriteLine($"[RoleRename] Renamed Receptionist → Secretary on {result.ModifiedCount} user(s).");
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
            orderNumber: "RPPS-12345678",
            cabinetId: Guid.NewGuid().ToString("N"));

        await ctx.Users.InsertOneAsync(doctor);
    }

    private static async Task CreatePatientIndexesAsync(MongoContext ctx)
    {
        var col = ctx.Patients;
        var builders = Builders<Patient>.IndexKeys;

        // NSS uniqueness is scoped PER CABINET (multi-tenancy doctrine) and only enforced
        // when an NSS is actually provided. A plain or *sparse* unique index is not enough:
        // sparse skips only MISSING fields, not explicit nulls, and the Patient document
        // serializes Nss as an explicit null — so two patients without an NSS collide
        // (E11000 dup key { nss: null }). Use a PARTIAL filter on nss being a string so the
        // constraint applies only to real NSS values. Drop legacy single-field nss indexes.
        try { await col.Indexes.DropOneAsync("nss_1"); } catch { }

        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Patient>(builders.Combine(
                builders.Ascending(p => p.LastName),
                builders.Ascending(p => p.FirstName))),
            // cabinetId is ALWAYS the first field in compound indexes (multi-tenancy rule)
            new CreateIndexModel<Patient>(builders.Combine(
                builders.Ascending(p => p.CabinetId),
                builders.Ascending(p => p.LastName),
                builders.Ascending(p => p.FirstName))),
            new CreateIndexModel<Patient>(
                builders.Combine(
                    builders.Ascending(p => p.CabinetId),
                    builders.Ascending(p => p.Nss)),
                new CreateIndexOptions<Patient>
                {
                    Unique = true,
                    PartialFilterExpression = new BsonDocument("nss", new BsonDocument("$type", "string"))
                }),
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
            // cabinetId-first compound indexes (multi-tenancy rule)
            new CreateIndexModel<Appointment>(builders.Combine(
                builders.Ascending(a => a.CabinetId),
                builders.Ascending(a => a.DoctorId),
                builders.Ascending(a => a.Date),
                builders.Ascending(a => a.Time))),
            new CreateIndexModel<Appointment>(builders.Combine(
                builders.Ascending(a => a.CabinetId),
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
            // cabinetId-first compound index (multi-tenancy rule)
            new CreateIndexModel<Consultation>(builders.Combine(
                builders.Ascending(c => c.CabinetId),
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

        // Invoice numbers are now unique PER CABINET — drop the legacy global unique index on number
        try { await col.Indexes.DropOneAsync("number_1"); } catch { /* index may not exist */ }

        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Invoice>(builders.Ascending(i => i.PatientId)),
            new CreateIndexModel<Invoice>(builders.Combine(
                builders.Ascending(i => i.DoctorId),
                builders.Ascending(i => i.Status))),
            new CreateIndexModel<Invoice>(builders.Combine(
                builders.Ascending(i => i.DoctorId),
                builders.Descending(i => i.IssuedAt))),
            // cabinetId-first compound indexes (multi-tenancy rule)
            new CreateIndexModel<Invoice>(builders.Combine(
                builders.Ascending(i => i.CabinetId),
                builders.Ascending(i => i.DoctorId),
                builders.Ascending(i => i.Status))),
            new CreateIndexModel<Invoice>(builders.Combine(
                builders.Ascending(i => i.CabinetId),
                builders.Ascending(i => i.DoctorId),
                builders.Descending(i => i.IssuedAt))),
            new CreateIndexModel<Invoice>(builders.Combine(
                builders.Ascending(i => i.CabinetId),
                builders.Ascending(i => i.Number)),
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
            // cabinetId-first compound index (multi-tenancy rule)
            new CreateIndexModel<Charge>(builders.Combine(
                builders.Ascending(c => c.CabinetId),
                builders.Ascending(c => c.DoctorId),
                builders.Descending(c => c.Date))),
            new CreateIndexModel<Charge>(builders.Ascending(c => c.Category)),
        ]);
    }

    private static async Task CreateActIndexesAsync(MongoContext ctx)
    {
        var col = ctx.Acts;
        var builders = Builders<Act>.IndexKeys;
        await col.Indexes.CreateManyAsync([
            // cabinetId-first (multi-tenancy rule); name for a stable sorted catalogue.
            new CreateIndexModel<Act>(builders.Combine(
                builders.Ascending(a => a.CabinetId),
                builders.Ascending(a => a.Name))),
        ]);
    }

    private static async Task CreateUserIndexesAsync(MongoContext ctx)
    {
        var col = ctx.Users;
        await col.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true }));
    }

    /// <summary>
    /// Idempotent backfill: assigns the first doctor's cabinetId to every document
    /// that predates cabinet scoping (missing/null/empty cabinetId field).
    /// Safe to run on every startup — matched count is 0 once backfilled.
    /// </summary>
    private static async Task BackfillCabinetIdAsync(MongoContext ctx)
    {
        var doctor = await ctx.Users
            .Find(u => u.Role == Role.Doctor)
            .SortBy(u => u.CreatedAt)
            .FirstOrDefaultAsync();

        if (doctor is null) return; // fresh database — seed will create the doctor with a cabinetId

        var cabinetId = doctor.CabinetId;
        if (string.IsNullOrEmpty(cabinetId))
        {
            cabinetId = Guid.NewGuid().ToString("N");
            await ctx.Users.UpdateOneAsync(
                new BsonDocument("_id", doctor.Id.ToString()),
                new BsonDocument("$set", new BsonDocument("cabinetId", cabinetId)));
            Console.WriteLine($"[CabinetBackfill] Assigned cabinetId {cabinetId} to doctor {doctor.Email}.");
        }

        // Matches documents where cabinetId is missing, null, or empty
        var missingFilter = new BsonDocument("$or", new BsonArray
        {
            new BsonDocument("cabinetId", new BsonDocument("$exists", false)),
            new BsonDocument("cabinetId", BsonNull.Value),
            new BsonDocument("cabinetId", ""),
        });
        var update = new BsonDocument("$set", new BsonDocument("cabinetId", cabinetId));

        var patients = await ctx.Patients.UpdateManyAsync(missingFilter, update);
        var appointments = await ctx.Appointments.UpdateManyAsync(missingFilter, update);
        var consultations = await ctx.Consultations.UpdateManyAsync(missingFilter, update);
        var invoices = await ctx.Invoices.UpdateManyAsync(missingFilter, update);
        var charges = await ctx.Charges.UpdateManyAsync(missingFilter, update);
        var users = await ctx.Users.UpdateManyAsync(missingFilter, update);

        var total = patients.ModifiedCount + appointments.ModifiedCount + consultations.ModifiedCount
                  + invoices.ModifiedCount + charges.ModifiedCount + users.ModifiedCount;
        if (total > 0)
        {
            Console.WriteLine(
                $"[CabinetBackfill] cabinetId={cabinetId} — patients: {patients.ModifiedCount}, " +
                $"appointments: {appointments.ModifiedCount}, consultations: {consultations.ModifiedCount}, " +
                $"invoices: {invoices.ModifiedCount}, charges: {charges.ModifiedCount}, users: {users.ModifiedCount}.");
        }
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
