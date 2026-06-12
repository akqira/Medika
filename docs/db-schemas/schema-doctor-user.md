# Schema — Doctor User (`users` collection)

Reference schema for the `User` aggregate as persisted in MongoDB. Used when
provisioning a doctor account directly in the database (e.g. production bootstrap).

Source of truth: `Medika.Domain.Identity.User`, mapped by
`DomainMappings.RegisterUserMap()` with global conventions:
- `CamelCaseElementNameConvention` — C# `PascalCase` → Mongo `camelCase`
- `EnumRepresentationConvention(BsonType.String)` — enums stored as **strings**
- `IgnoreExtraElements(true)`
- `UserId` serialized as a **string** `_id` (Guid `"D"` form, with dashes)

## MongoDB document structure

```json
{
  "_id": "f3b9c1a2-1d4e-4c8b-9a7e-2c5d6e7f8a90",   // Guid "D" (with dashes)
  "cabinetId": "9f8e7d6c5b4a39281706f5e4d3c2b1a0",  // Guid "N" (32 hex, no dashes)
  "email": "kader.kebir@gmail.com",                  // stored lower-cased
  "passwordHash": "$2a$11$....",                      // BCrypt.Net (work factor 11)
  "firstName": "Abdelkader",
  "lastName": "Kebir",
  "role": "Doctor",                                   // enum as string
  "isActive": true,
  "createdAt": { "$date": "2026-06-11T00:00:00Z" },
  "updatedAt": { "$date": "2026-06-11T00:00:00Z" },
  "lastLoginAt": null,
  "specialty": "Médecine générale",                   // doctor-only, nullable
  "orderNumber": "RPPS-12345678",                     // doctor-only, nullable
  "cabinetName": null,
  "cabinetAddress": null,
  "cabinetCity": null,
  "cabinetWilaya": null,
  "cabinetPhone": null,
  "linkedPatientId": null                             // patient-only, nullable
}
```

### Field types

| Field            | BSON type | Notes |
|------------------|-----------|-------|
| `_id`            | string    | Guid `"D"` format; **not** ObjectId |
| `cabinetId`      | string    | Guid `"N"` format; multi-tenancy key |
| `email`          | string    | always `ToLowerInvariant()`; **unique** |
| `passwordHash`   | string    | BCrypt `$2a$11$…` — never plaintext |
| `firstName`/`lastName` | string |  |
| `role`           | string    | `Doctor` \| `Receptionist` \| `Patient` |
| `isActive`       | bool      |  |
| `createdAt`/`updatedAt` | date | UTC |
| `lastLoginAt`    | date/null |  |
| `specialty`/`orderNumber` | string/null | doctor-only |
| `cabinet*`       | string/null | doctor practice info |
| `linkedPatientId`| string/null | patient-only |

## Indexes

| Name      | Keys           | Options          | Defined in |
|-----------|----------------|------------------|------------|
| `email_1` | `{ email: 1 }` | **unique**       | `MongoDbInitializer.CreateUserIndexesAsync` |

> The unique `email` index means a doctor with `kader.kebir@gmail.com` can exist
> **only once**. `MongoDbInitializer.SeedAsync` seeds exactly this email on a fresh
> database with password `Doctor@123`. A second insert with the same email fails
> with `E11000 duplicate key`.

## Atlas free-tier notes

- `users` is tiny and bounded (one row per staff/patient account) — no storage concern.
- No unbounded arrays on `User`.
- No Atlas Search needed — lookups are exact-match on the unique `email` index.

## C# POCO skeleton

```csharp
// Medika.Domain.Identity.User (aggregate, private ctor)
public sealed class User : AggregateRoot<UserId>
{
    public string CabinetId { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Role   Role { get; private set; }
    public bool   IsActive { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? Specialty { get; private set; }
    public string? OrderNumber { get; private set; }
    public string? CabinetName { get; private set; }
    public string? CabinetAddress { get; private set; }
    public string? CabinetCity { get; private set; }
    public string? CabinetWilaya { get; private set; }
    public string? CabinetPhone { get; private set; }
    public string? LinkedPatientId { get; private set; }

    public static User Create(string email, string passwordHash,
        string firstName, string lastName, Role role,
        string? specialty = null, string? orderNumber = null,
        string? cabinetId = null);
}
```

## Provisioning a doctor directly (operational)

Prefer reusing domain logic (`User.Create` + `PasswordHasher`) over a raw insert so
hashing, `_id`/`cabinetId` generation and camelCase mapping stay consistent. If a raw
`mongosh` write is required, **upsert on `email`** to stay idempotent against the
seeded account and the unique index.
