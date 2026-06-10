# Azure App Service Migration — Medika Backend

**Goal:** move the Medika API from Azure Container Apps to Azure App Service, aligned with eGestion's hosting (same platform, one set of skills to maintain).

**Environments (mirrors eGestion's eGestionAPI / dev-eGestion-Api):**

| Environment | App Service | Branch | Workflow | Database |
|---|---|---|---|---|
| Production | `medika-api` | `main` | `backend.yml` | `medik_prod` |
| Dev/staging | `dev-medika-api` | `dev` | `backend-dev.yml` | `medika_dev` |

Both apps share one B1 plan — a second app on the same plan costs nothing extra.

**Status:** workflows ready (`.github/workflows/backend.yml` + `backend-dev.yml`). The Azure resources and GitHub secrets below must be created once, manually. Estimated time: 45–60 min for both environments.

---

## 1. Create the App Service (one-time)

Run in a terminal where you're logged into Azure (`az login`):

```bash
# Variables — adjust names if taken
RG=rg-medika-prod
PLAN=plan-medika
APP=medika-api          # must match app-name in backend.yml
LOCATION=francecentral  # closest region to Algeria with App Service

az group create --name $RG --location $LOCATION

# B1 = cheapest always-on tier (~13€/mo). F1 free tier exists but sleeps.
az appservice plan create --name $PLAN --resource-group $RG --sku B1 --is-linux

az webapp create --name $APP --resource-group $RG --plan $PLAN --runtime "DOTNETCORE:10.0"

# Health check — uses the new /api/health endpoint
az webapp config set --name $APP --resource-group $RG --generic-configurations '{"healthCheckPath": "/api/health"}'

# HTTPS only
az webapp update --name $APP --resource-group $RG --https-only true

# ---- DEV environment: same plan, second app (mirrors dev-eGestion-Api) ----
APP_DEV=dev-medika-api

az webapp create --name $APP_DEV --resource-group $RG --plan $PLAN --runtime "DOTNETCORE:10.0"
az webapp config set --name $APP_DEV --resource-group $RG --generic-configurations '{"healthCheckPath": "/api/health"}'
az webapp update --name $APP_DEV --resource-group $RG --https-only true
```

## 2. Application settings (secrets live HERE, never in the repo)

Same rule as eGestion ADR-008: `Jwt:Secret` and `ApiSettings:ApiKey` exist only in Azure — never in Vercel, never in git.

```bash
az webapp config appsettings set --name $APP --resource-group $RG --settings \
  "MongoDB__ConnectionString=<prod Atlas connection string>" \
  "MongoDB__DatabaseName=medik_prod" \
  "Jwt__Secret=<openssl rand -base64 48>" \
  "ApiSettings__ApiKey=<openssl rand -base64 48>" \
  "Cors__AllowedOrigins=https://<your-vercel-domain>" \
  "R2__AccountId=<value>" \
  "R2__AccessKeyId=<value>" \
  "R2__SecretAccessKey=<value>" \
  "R2__BucketName=medika-files"
```

⚠️ Generate a **new** `ApiSettings__ApiKey` for prod — do not reuse the dev key from `appsettings.Development.json`.

Then the DEV app — same settings, dev values (separate DB, **separate** JWT secret and API key so a leaked dev key never opens prod):

```bash
az webapp config appsettings set --name $APP_DEV --resource-group $RG --settings \
  "MongoDB__ConnectionString=<dev Atlas connection string>" \
  "MongoDB__DatabaseName=medika_dev" \
  "Jwt__Secret=<openssl rand -base64 48 — different from prod>" \
  "ApiSettings__ApiKey=<openssl rand -base64 48 — different from prod>" \
  "Cors__AllowedOrigins=https://<your-vercel-preview-domain>" \
  "R2__AccountId=<value>" \
  "R2__AccessKeyId=<value>" \
  "R2__SecretAccessKey=<value>" \
  "R2__BucketName=medika-files-dev"
```

## 3. GitHub → Azure deployment credentials (OIDC)

Easiest path (this is how eGestion's workflow was generated):

1. Azure Portal → your App Service `medika-api` → **Deployment Center**
2. Source: **GitHub** → authorize → pick `akqira/Medika`, branch `main`
3. Authentication: **User-assigned identity** (recommended) → Save
4. Azure auto-creates three repo secrets (names will have hash suffixes). Either:
   - rename them in GitHub to `AZUREAPPSERVICE_CLIENTID`, `AZUREAPPSERVICE_TENANTID`, `AZUREAPPSERVICE_SUBSCRIPTIONID` (what `backend.yml` expects), **or**
   - update `backend.yml` to the generated names.
5. Deployment Center will also commit its own auto-generated workflow file — **delete it** and keep our `backend.yml`.

Repeat for the DEV app: `dev-medika-api` → Deployment Center → branch `dev` → its CLIENTID secret becomes `AZUREAPPSERVICE_CLIENTID_DEV` (what `backend-dev.yml` expects; TENANTID and SUBSCRIPTIONID are shared). Delete its auto-generated workflow too.

Create the dev branch first if it doesn't exist:

```powershell
git checkout -b dev
git push -u origin dev
```

## 4. Vercel (frontend) env vars

Vercel dashboard → Medika project → Settings → Environment Variables (Production):

| Name | Value | Vercel environment |
|---|---|---|
| `API_URL` | `https://medika-api.azurewebsites.net` | Production |
| `API_SECRET` | prod `ApiSettings__ApiKey` from step 2 | Production |
| `API_URL` | `https://dev-medika-api.azurewebsites.net` | Preview |
| `API_SECRET` | dev `ApiSettings__ApiKey` from step 2 | Preview |

All **without** `PUBLIC_` prefix — they must stay server-only.

With this split, every push to `dev` (or any PR branch) gets a Vercel **Preview** deployment automatically wired to the dev backend — that's your test environment end-to-end, like eGestion's dev setup.

## 5. Decommission Container Apps (after first successful App Service deploy)

```bash
# Verify the new deployment works first!
curl https://medika-api.azurewebsites.net/api/health

az containerapp delete --name <old-container-app-name> --resource-group <old-rg> --yes
# Optionally delete the ACR if nothing else uses it:
# az acr delete --name <registry-name> --yes
```

## 6. Local dev — nothing changes

- Backend reads `ApiSettings:ApiKey` from `appsettings.Development.json` (gitignored)
- Frontend reads `API_SECRET` from `apps/frontend/.env` (gitignored)
- Both already set to matching values

## Checklist

- [ ] Resource group + plan + `medika-api` + `dev-medika-api` created
- [ ] App settings configured on BOTH apps (separate ApiKey + JWT secret per env)
- [ ] `dev` branch created and pushed
- [ ] Deployment Center wired for both apps, secrets renamed (`..._CLIENTID`, `..._CLIENTID_DEV`), auto-workflows deleted
- [ ] Push to dev → `backend-dev.yml` green → `https://dev-medika-api.azurewebsites.net/api/health` ok
- [ ] Push to main → `backend.yml` green → `https://medika-api.azurewebsites.net/api/health` ok
- [ ] Vercel env vars set for Production AND Preview environments
- [ ] Login works end-to-end in both environments
- [ ] Container App deleted

## Day-to-day flow (same as eGestion)

`feature/*` branch → merge into `dev` → auto-deploys to staging → test → merge `dev` into `main` → auto-deploys to production.
