# Credential leak — remediation guide

`apps/backend/src/Medika.Api/appsettings.Development.json` was committed and pushed to
`github.com/akqira/Medika`. It exposed **two** secrets:

| Secret | Leaked value (now burned) | Action |
|--------|---------------------------|--------|
| Mongo DB user `kaki` password | `#2_dq_pB97RWpz2` | Rotate in Atlas |
| JWT signing secret | `X7mQ9vK2pL8rT4nYc5Wz1Hj6Bd3Fs0Ua` | Replace everywhere |

Treat both as **public**. Anyone who saw the repo can connect to your cluster and forge
login tokens. Rotation is the only thing that actually fixes this — the steps below stop
the bleeding and prevent a repeat.

---

## 1. Rotate now (do this first)

### Mongo — separate user per project (recommended)
The same connection string is shared with another project (Azure dev + prod). A shared
credential means one leak compromises everything. Fix the structure while you rotate:

1. In Atlas → **Database Access**, create distinct users, e.g.
   - `medika_dev` (role: readWrite on `medika_dev`)
   - `medika_prod` (role: readWrite on `medik_prod`)
   - separate users for the *other* project's dev/prod
2. **Delete the old `kaki` user** (don't just reset it — it's the leaked identity).
3. Give each user a password with no characters that need URL-encoding (avoid
   `# / : @ ? &`) so connection strings stay clean. Atlas's "Autogenerate" is fine.
4. Update each project's config with its own new string (see §2/§3).

> Rotating breaks the other project until you update its config too — do them together.

### JWT secret
Anyone with the old secret can mint valid tokens. Replace it:

```
XFIr7/YaxdTtxpoZjgphJNRzD9qi9Bf7CbYADVPTLin51068aGUQooI9XFHzsFCV
```

(freshly generated for you; or run `openssl rand -base64 48` for your own). Set it in
local config and in Azure (§3). Existing logins are invalidated — users re-authenticate.

---

## 2. Local development — keep secrets off disk-in-repo

Your code already binds from config sections (`MongoDB`, `Jwt`, `R2`) — **no code change
needed**. Two good options:

**Option A — .NET User Secrets (best for local).** Secrets live in your user profile,
physically outside the repo, so they can never be committed:

```powershell
cd apps/backend/src/Medika.Api
dotnet user-secrets init
dotnet user-secrets set "MongoDB:ConnectionString" "<new-dev-connection-string>"
dotnet user-secrets set "Jwt:Secret" "<new-jwt-secret>"
```

**Option B — gitignored `appsettings.Development.json`.** Already done: the file is now
in `.gitignore` and untracked, with a committed `appsettings.Development.example.json`
showing the shape. Keep your real values in the local (ignored) file.

Use one or the other. User Secrets is the safer default.

---

## 3. Azure — App Service settings vs Key Vault

.NET maps config sections to env vars with a **double underscore** separator. So your
two settings become:

```
MongoDB__ConnectionString = <new-connection-string>
Jwt__Secret               = <new-jwt-secret>
```

Set these per environment (dev app and prod app get *different* values).

### Option A — App Service Application Settings (env vars)
In the Azure Portal → your App Service → **Settings → Environment variables** (formerly
"Configuration"), add `MongoDB__ConnectionString` and `Jwt__Secret`. These override
`appsettings.json` automatically — no code change. For the connection string
specifically you can also use the "Connection strings" blade, but App Settings with the
`__` names is simplest and consistent.

- **Pros:** zero setup, no new code, works immediately.
- **Cons:** secrets are visible in plaintext to anyone with portal/CLI access to the app;
  no versioning, rotation, or audit log; they sit in the app's config.

### Option B — Azure Key Vault + Managed Identity (most secure)
Store secrets in a Key Vault; the app reads them at startup using its **managed
identity** (no credentials in the app at all).

1. Create a Key Vault, add secrets named `MongoDB--ConnectionString` and `Jwt--Secret`
   (Key Vault uses `--`, which the config provider maps back to `:`).
2. Enable a **system-assigned managed identity** on the App Service.
3. Grant that identity **Key Vault Secrets User** on the vault.
4. Wire it up in `Program.cs`:

```csharp
// requires: Azure.Identity, Azure.Extensions.AspNetCore.Configuration.Secrets
if (!builder.Environment.IsDevelopment())
{
    var vaultUri = new Uri(builder.Configuration["KeyVault:Uri"]!);
    builder.Configuration.AddAzureKeyVault(vaultUri, new DefaultAzureCredential());
}
```

- **Pros:** secrets never live in app config; centralized rotation, versioning, access
  policies, and audit logging; no secret material in env vars or code.
- **Cons:** a bit more setup (vault, identity, two NuGet packages, a few lines of code).

### Recommendation
**Use Key Vault (Option B) for production.** For a healthcare app handling patient data,
the audit trail, access control, and rotation it gives you are worth the setup — App
Settings give none of that. A pragmatic path: App Service settings for **dev**, Key Vault
for **prod**. Either way, dev and prod must use **different** DB users and secrets.

---

## 3b. Other application secrets (R2, Brevo)

Beyond Mongo + JWT, the app binds two more secret-bearing config sections. They were
**not** part of the leak, but manage them the same way — never in `appsettings.json`
(which ships with empty values), always via User Secrets locally and App Service settings
/ Key Vault in prod:

| Section | Secret keys | Env-var form (Azure `__`) | Key Vault form (`--`) |
|---------|-------------|----------------------------|------------------------|
| `R2` (Cloudflare storage) | `AccessKeyId`, `SecretAccessKey` | `R2__AccessKeyId`, `R2__SecretAccessKey` | `R2--AccessKeyId`, `R2--SecretAccessKey` |
| `Brevo` (transactional email) | `ApiKey` | `Brevo__ApiKey` | `Brevo--ApiKey` |

### Brevo API key
The Brevo provider sends password-reset emails (see `CLAUDE.md` → *Transactional email*).
The key lives **only** in `Brevo__ApiKey`:

- **Local / CI:** leave `Brevo:ApiKey` empty. Sending is a deliberate no-op (the link is
  still logged), so nothing breaks and no real email is sent from dev.
- **Prod:** set `Brevo__ApiKey` (App Service env var or Key Vault `Brevo--ApiKey`). Also
  confirm `Brevo:FromEmail` is a **verified Brevo sender** or on an authenticated domain,
  or sends are rejected.
- **Rotation:** Brevo dashboard → **SMTP & API → API Keys** → generate a new v3 key,
  update `Brevo__ApiKey` in each prod app, then delete the old key. No code or redeploy
  needed — it's read from config at startup.
- If a key is ever exposed, treat it as public: a leaked Brevo key lets anyone send mail
  as your account (and burn your sending quota / reputation). Rotate immediately.

Set per environment — dev and prod use **different** keys, like every other secret here.

---

## 4. About git history

You chose "stop tracking + rotate" rather than rewriting history. That's reasonable
**because rotation makes the old values useless** — the secret strings still sit in old
commits, but they no longer unlock anything once rotated. If you later want them gone
from history entirely (e.g. before making the repo public), use `git filter-repo` or BFG
and force-push, coordinating with any collaborators.

---

## Checklist

- [ ] Atlas: create separate dev/prod users, delete old `kaki` user
- [ ] Update local config (User Secrets or ignored Development.json) with new Mongo string + JWT secret
- [ ] Update the **other** project's config with its own new credentials
- [ ] Azure dev app: set `MongoDB__ConnectionString` + `Jwt__Secret`
- [ ] Azure prod app: set its own `MongoDB__ConnectionString` + `Jwt__Secret`
- [ ] Azure prod app: set `Brevo__ApiKey` (+ verify `Brevo:FromEmail` is a verified Brevo sender); leave empty in dev/CI
- [ ] Commit the gitignore change + `appsettings.Development.example.json`
- [ ] (optional) Enable Atlas IP Access List to restrict who can connect
- [ ] (optional) Scrub git history before any public release
