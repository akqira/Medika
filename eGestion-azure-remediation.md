# eGestion (Azure) — connection string rotation + Key Vault

## What's actually running (verified in the portal, 2026-06-09)

Two App Services, both .NET 9, both from `github.com/akqira/eGestion`:

| App | Resource group | Role |
|-----|----------------|------|
| `eGestionAPI` | `eGestionRG` | prod |
| `dev-eGestion-Api` | `eGestion-dev-RG` | dev |

Findings, identical on both:
- **No managed identity** (System-assigned = Off, no User-assigned). A managed identity
  is *required* to read a Key Vault, so the app cannot be using `kv-egestion-prod`.
- Config comes from **App Service → Environment variables → App settings**
  (every row Source = "App Service" = literal values, not Key Vault references).
- The Mongo connection string is the app setting **`ConnectionStrings__Database…`**
  (the "Connection strings" tab is empty — it's not there).
- `kv-egestion-prod` exists with `ConnectionStrings--MongoDB` / `JwtSettings--SecretKey`,
  but **nothing reads it** — it's orphaned. Updating vault secrets would have no effect.

> So: the live Mongo credential to rotate is in App Service env vars, **not** Key Vault.

---

## Option A — Quick: update App Service settings (closes the leak fastest)

Do this for **both** apps (prod gets `eGestion_prod`, dev gets `eGestion_dev`).

1. Portal → App Service (`eGestionAPI`, then `dev-eGestion-Api`)
   → **Settings → Environment variables → App settings**.
2. Click the **`ConnectionStrings__Database…`** row to open it.
3. Replace the value with the new connection string:
   ```
   mongodb+srv://eGestion_prod:<password>@cluster0.pzuvl.mongodb.net/<db>?retryWrites=true&w=majority
   ```
   (use `eGestion_dev` for the dev app). URL-encode any special chars in the password.
4. **Apply** → confirm restart. The app reboots and picks up the new value.
5. Verify the app still reads/writes data (Browse the app, hit an endpoint).

Once **both** eGestion apps **and** Medika are confirmed on their new users, delete the
old `kaki` user in Atlas — that's what actually closes the leak.

---

## Option B — Secure: finish wiring Key Vault (recommended long-term)

This makes the vault you already have actually do its job. Per app:

1. **App Service → Identity → System assigned → Status = On → Save.** Copy the Object ID.
2. **Key Vault `kv-egestion-prod` → Access control (IAM) → Add role assignment**
   → role **Key Vault Secrets User** → assign to that app's managed identity.
   (Each app needs its own — or use a separate dev vault for the dev app.)
3. Make the app load the vault. Two ways:
   - **In code** (`Program.cs`), prod only:
     ```csharp
     // NuGet: Azure.Identity, Azure.Extensions.AspNetCore.Configuration.Secrets
     if (!builder.Environment.IsDevelopment())
         builder.Configuration.AddAzureKeyVault(
             new Uri("https://kv-egestion-prod.vault.azure.net/"),
             new DefaultAzureCredential());
     ```
   - **Or App Service references** (no code): set app setting
     `ConnectionStrings__MongoDB` =
     `@Microsoft.KeyVault(SecretUri=https://kv-egestion-prod.vault.azure.net/secrets/ConnectionStrings--MongoDB/)`
4. **Align the names.** Your code currently reads `ConnectionStrings:Database…`, but the
   vault secret is named `ConnectionStrings--MongoDB` (→ `ConnectionStrings:MongoDB`).
   Pick one key name and make code + vault agree, or the app won't find it.
5. Put the **new** `eGestion_prod` string into the vault secret (new version), restart, verify.

Trade-off: Option B is the secure end state (central rotation, audit, no secrets in app
config) but is several steps. Option A neutralizes the leak in minutes. A common path is
A now, B as a follow-up.

---

## Also check
- The **eGestion repo's own `appsettings.json`** may contain a committed connection string
  (same leak as Medika). Gitignore + rotate if so.
- `JwtSettings--SecretKey` in the vault is also unused — wherever eGestion's real JWT secret
  lives (App setting or appsettings.json), rotate it too if it was ever committed.

## Sequence
1. New Atlas users created ✓
2. Update Medika (local done) + Medika prod
3. Update `eGestionAPI` and `dev-eGestion-Api` (Option A or B)
4. Verify all apps connect
5. **Delete `kaki`** in Atlas → leak closed
