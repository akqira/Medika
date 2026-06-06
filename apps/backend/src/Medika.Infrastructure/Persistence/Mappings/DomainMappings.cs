using Medika.Domain.Finance;
using Medika.Domain.Identity;
using Medika.Domain.Medical;
using Medika.Domain.Patients;
using Medika.Domain.Scheduling;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Medika.Infrastructure.Persistence.Mappings;

public static class DomainMappings
{
    private static bool _registered;
    private static readonly Lock _lock = new();

    public static void Register()
    {
        lock (_lock)
        {
            if (_registered) return;

            // Global conventions
            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String),
            };
            ConventionRegistry.Register("MedikaConventions", pack, _ => true);

            // Strongly-typed ID serializers
            BsonSerializer.RegisterSerializer(new PatientIdSerializer());
            BsonSerializer.RegisterSerializer(new AppointmentIdSerializer());
            BsonSerializer.RegisterSerializer(new ConsultationIdSerializer());
            BsonSerializer.RegisterSerializer(new InvoiceIdSerializer());
            BsonSerializer.RegisterSerializer(new ChargeIdSerializer());
            BsonSerializer.RegisterSerializer(new UserIdSerializer());

            // BloodGroup value object
            BsonSerializer.RegisterSerializer(new BloodGroupSerializer());

            // DateOnly / TimeOnly
            BsonSerializer.RegisterSerializer(new DateOnlySerializer());
            BsonSerializer.RegisterSerializer(new TimeOnlySerializer());

            RegisterPatientMap();
            RegisterAppointmentMap();
            RegisterConsultationMap();
            RegisterInvoiceMap();
            RegisterChargeMap();
            RegisterUserMap();

            _registered = true;
        }
    }

    private static void RegisterPatientMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Patient))) return;
        BsonClassMap.RegisterClassMap<Patient>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
            cm.MapMember(p => p.Allergies).SetElementName("allergies");
            cm.MapMember(p => p.MedicalHistory).SetElementName("medicalHistory");
        });
    }

    private static void RegisterAppointmentMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Appointment))) return;
        BsonClassMap.RegisterClassMap<Appointment>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
    }

    private static void RegisterConsultationMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Consultation))) return;
        BsonClassMap.RegisterClassMap<Consultation>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
            cm.MapMember(c => c.Prescription).SetElementName("prescription");
        });
    }

    private static void RegisterInvoiceMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Invoice))) return;
        BsonClassMap.RegisterClassMap<Invoice>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
    }

    private static void RegisterChargeMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Charge))) return;
        BsonClassMap.RegisterClassMap<Charge>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
    }

    private static void RegisterUserMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(User))) return;
        BsonClassMap.RegisterClassMap<User>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
    }
}

// ── Custom serializers for strongly-typed IDs ──────────────────────────────

file sealed class PatientIdSerializer() : SerializerBase<PatientId>
{
    public override PatientId Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        => PatientId.From(ctx.Reader.ReadString());
    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, PatientId value)
        => ctx.Writer.WriteString(value.ToString());
}

file sealed class AppointmentIdSerializer() : SerializerBase<AppointmentId>
{
    public override AppointmentId Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        => AppointmentId.From(ctx.Reader.ReadString());
    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, AppointmentId value)
        => ctx.Writer.WriteString(value.ToString());
}

file sealed class ConsultationIdSerializer() : SerializerBase<ConsultationId>
{
    public override ConsultationId Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        => ConsultationId.From(ctx.Reader.ReadString());
    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, ConsultationId value)
        => ctx.Writer.WriteString(value.ToString());
}

file sealed class InvoiceIdSerializer() : SerializerBase<InvoiceId>
{
    public override InvoiceId Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        => InvoiceId.From(ctx.Reader.ReadString());
    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, InvoiceId value)
        => ctx.Writer.WriteString(value.ToString());
}

file sealed class ChargeIdSerializer() : SerializerBase<ChargeId>
{
    public override ChargeId Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        => ChargeId.From(ctx.Reader.ReadString());
    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, ChargeId value)
        => ctx.Writer.WriteString(value.ToString());
}

file sealed class UserIdSerializer() : SerializerBase<UserId>
{
    public override UserId Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        => UserId.From(ctx.Reader.ReadString());
    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, UserId value)
        => ctx.Writer.WriteString(value.ToString());
}

file sealed class BloodGroupSerializer() : SerializerBase<Domain.Patients.BloodGroup>
{
    public override Domain.Patients.BloodGroup Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        => Domain.Patients.BloodGroup.From(ctx.Reader.ReadString());
    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, Domain.Patients.BloodGroup value)
        => ctx.Writer.WriteString(value.ToString());
}

file sealed class DateOnlySerializer() : StructSerializerBase<DateOnly>
{
    public override DateOnly Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        => DateOnly.Parse(ctx.Reader.ReadString());
    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, DateOnly value)
        => ctx.Writer.WriteString(value.ToString("yyyy-MM-dd"));
}

file sealed class TimeOnlySerializer() : StructSerializerBase<TimeOnly>
{
    public override TimeOnly Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        => TimeOnly.Parse(ctx.Reader.ReadString());
    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, TimeOnly value)
        => ctx.Writer.WriteString(value.ToString("HH:mm"));
}
