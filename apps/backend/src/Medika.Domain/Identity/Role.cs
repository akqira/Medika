namespace Medika.Domain.Identity;

public enum Role
{
    Doctor,
    // Front-desk staff (formerly "Receptionist"). Ordinal 1 is preserved so existing
    // int-serialized documents keep deserializing correctly.
    Secretary,
    Patient
}
