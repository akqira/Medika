using Medika.Domain.Finance;
using Medika.Domain.Identity;
using Medika.Domain.Medical;
using Medika.Domain.Patients;
using Medika.Domain.Scheduling;
using Medika.Infrastructure.Audit;
using MongoDB.Driver;

namespace Medika.Infrastructure.Persistence;

public class MongoContext
{
    private readonly IMongoDatabase _db;

    public MongoContext(IMongoClient client, MongoSettings settings)
    {
        _db = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<Patient> Patients => _db.GetCollection<Patient>("patients");
    public IMongoCollection<Appointment> Appointments => _db.GetCollection<Appointment>("appointments");
    public IMongoCollection<Consultation> Consultations => _db.GetCollection<Consultation>("consultations");
    public IMongoCollection<Invoice> Invoices => _db.GetCollection<Invoice>("invoices");
    public IMongoCollection<Charge> Charges => _db.GetCollection<Charge>("charges");
    public IMongoCollection<Act> Acts => _db.GetCollection<Act>("acts");
    public IMongoCollection<User> Users => _db.GetCollection<User>("users");
    public IMongoCollection<AuditLog> AuditLogs => _db.GetCollection<AuditLog>("audit_logs");
}
