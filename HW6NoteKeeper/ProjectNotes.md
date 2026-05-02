# Project Notes

## 1. Homework Assignment Title and Number
**Assignment #6 — Note Keeper (Azure API Management)**

This assignment builds on top of **Homework #4 (HW4NoteKeeper)**. The starting codebase for HW6 is an exact copy of the completed HW4 solution that has been renamed (`HW4*` → `HW6*`). All HW4 *core* functionality therefore carries forward unchanged; HW6 adds Azure API Management as a front-end gateway and the small set of code changes required to support a clean OpenAPI/swagger.json import.

> **Note on extra credit carry-over:** No HW4 extra credit options are being carried into HW6. Any HW4 extra-credit work that previously lived in this codebase has been intentionally removed from the project notes so the TA only sees what is in scope for HW6.

---

## 2. Student Information
**Name:** Paul Schwartzberg  
**Email:** paulschwartzberg@outlook.com

---

## 3. Attribution Regarding the Use of AI

### What I Used AI For (Initial Guidance and Debugging Only):
*(To be filled in as HW6 work progresses.)*

### What I Coded by Hand and/or Modified Considerably:
*(To be filled in as HW6 work progresses.)*

### Summary
*(To be filled in once HW6 is complete.)*

---

## 4. Notes for the TA

**No special installation or setup is required.** The application should run directly after:
1. Setting the Azure OpenAI configuration values in App Service Environment Variables (as documented below)
2. Restoring NuGet packages
3. Building the solution

The application uses standard ASP.NET Core 10.0 features and does not require any additional tools or dependencies beyond what is included in the `.csproj` file.

The HW6-specific configuration (API Management gateway URL, product subscription keys, etc.) is documented in **Section 4.2 — HW6 Implementation Status** below.

---

## 4.1. Inherited from HW4 (Already Implemented in the Copied Codebase)

The HW6 project starts as a clone of the HW4 project. The following functionality is **already in place** when HW6 begins; HW6 itself does not add or modify any of it (apart from the small swagger.json compatibility tweaks listed in §4.2):

### 4.1.1 Note Management (carried from HW2/HW3/HW4)
- `GET /NoteKeeper` — Retrieve all notes (optional `tagName` query parameter for filtering)
- `GET /NoteKeeper/{noteId}` — Retrieve a specific note by ID
- `POST /NoteKeeper` — Create a new note with AI-generated tags
  - Returns 201 (Created) on success
  - Returns 400 (Bad Request) if validation fails
  - Returns 403 (Forbidden) if the `MaxNotes` limit has been reached
  - Returns 500 (Internal Server Error) on unexpected errors
- `PATCH /NoteKeeper/{noteId}` — Update an existing note (regenerates tags if details change)
- `DELETE /NoteKeeper/{noteId}` — Delete a note (DB row only; storage cleanup is performed by the enhanced delete endpoint below)

### 4.1.2 Attachment Management (carried from HW3)
- `PUT /notes/{noteId}/attachments/{attachmentId}` — Upload or update an attachment
  - 201 (Created) with `Location` header for new attachments
  - 204 (No Content) for updates to existing attachments
  - 400 for invalid GUID format
  - 403 when `MaxAttachments` limit reached (new uploads only)
  - Works purely with blob storage (no database check)
- `GET /notes/{noteId}/attachments/{attachmentId}` — Retrieve a single attachment (200 + stream / 400 / 404)
- `GET /notes/{noteId}/attachments` — Retrieve all attachment metadata for a note (200 / 400 / 404)
- `DELETE /notes/{noteId}/attachments/{attachmentId}` — Delete an attachment (204 idempotent / 400 / 404 / 500)

### 4.1.3 Zip-Attachment Workflow (carried from HW4)
A controller (`HW6NoteKeeper.Controllers.NoteKeeperZipAttachmentController`) plus an Azure Function (`HW6AzureFunctions.AttachmentZipFunction`) implement asynchronous zip creation:
- `POST /notes/{noteId}/attachmentzipfiles` — enqueues a zip request, returns 202 + `Location`
- `DELETE /notes/{noteId}/attachmentzipfiles/{zipFileId}` — deletes a zip blob (idempotent)
- `GET /notes/{noteId}/attachmentzipfiles/{zipFileId}` — downloads a zip blob
- `GET /notes/{noteId}/attachmentzipfiles` — lists all zip blobs for a note
- `DELETE /notes/{noteId}` — enhanced delete: removes attachment container, zip container, and all DB rows

> **HW6 IMPORTANT:** Per the HW6 instructions, the API Management products **must not expose** the `attachmentzipfiles` resource. The zip endpoints remain in the App Service for legacy/internal use but are excluded from both the Basic and Standard API Management products.

### 4.1.4 Background Infrastructure (carried over)
- **Azure SQL Database** with EF Core 9.0 (Note + Tag tables, code-first with migrations)
- **Azure Blob Storage** — one container per note (`{noteId}`) plus a zip container per note (`{noteId}-zip`)
- **Azure Storage Queues** — `attachment-zip-requests` (+ `-poison`) for the function trigger
- **Application Insights** — TrackTrace / TrackException / TrackEvent telemetry
- **DefaultAzureCredential / Managed Identity** — passwordless auth to SQL, Blob, Queue, and Functions
- **Database/Storage seeding** at app startup: 4 default notes with attachments are reseeded on each boot

### 4.1.5 Note Limits
- `NoteLimits:MaxNotes` (default 10) — max notes; POST returns 403 when full
- `NoteLimits:MaxAttachments` (default 3) — max attachments per note; PUT returns 403 when full (only counts new uploads)

### 4.1.6 Tag Generation
The application uses Azure OpenAI's GPT-5-mini model to automatically generate any number of tags (no max set) for each note based on its details. Tags are concise (1–2 words), lowercase, and regenerated when note details change.

### 4.1.7 Technology Stack
- ASP.NET Core 10.0 Web API
- Azure OpenAI (GPT-5-mini deployment)
- Entity Framework Core 9.0 with Azure SQL Database
- Azure Blob Storage (note attachments + zip outputs)
- Azure Storage Queues + Azure Functions Isolated Worker v4 (`net8.0`)
- Application Insights (telemetry + Live Metrics)
- Managed Identity / DefaultAzureCredential (passwordless authentication)
- Swashbuckle/Swagger for API documentation
- `System.IO.Compression.ZipArchive` for in-memory zip creation
- `Azure.Storage.Queues` for queue interaction

---

## 4.2. HW6 Implementation Status

This section tracks the **HW6-specific** work — Azure API Management front-end and the small set of code changes the swagger.json import requires. It is currently a checklist; entries will be filled in as each step is completed in coordination with the user.

### 4.2.1 Required Code Changes (HW6 §1)
- [ ] Ensure unique `OperationId` for all API actions (Swashbuckle `CustomOperationIds` or attribute-based)
- [ ] Verify all controller method names are unique across controllers (no `GetById` duplicates)
- [ ] Add host-name handling so the swagger.json reflects the public host
- [ ] Add CORS support (`AddCors` / `UseCors`)

### 4.2.2 Azure App Service for the REST API (HW6 §2)
- [ ] New App Service on the existing Linux B2 App Service Plan, named per Azure abbreviation conventions
- [ ] Application Insights instance with matching name (`appi-…`)
- [ ] App Service Plan tier confirmed Basic
- [ ] Application deployed
- [ ] App Service set to TLS 1.2
- [ ] Deployed REST API URL recorded here:
- [ ] App Service name recorded here:

### 4.2.3 Azure API Management Service (HW6 §3)
- [ ] APIM service created in Consumption tier
- [ ] Organization name: `CSCIE-94 Paul Schwartzberg`
- [ ] Administrator email: `paulschwartzberg@outlook.com`
- [ ] Application Insights linked to the same AI instance from §4.2.2
- [ ] Protocol settings configured per HW6 §3.7
- [x] APIM gateway URL recorded here: `https://apim-cscie-94-hw6.azure-api.net`
- [x] APIM service name recorded here: `apim-CSCIE-94-HW6` (resource group: `rg_hw6`)

### 4.2.4 Connecting App Service ↔ APIM (HW6 §4)
- [ ] API Management definition (full URL to swagger.json) added to App Service
- [ ] App Service linked to APIM via the API Management option
- [ ] OpenAPI Specification import enabled
- [ ] Application Insights enabled on import
- [ ] All REST APIs imported
- [ ] No URL suffix, no versioning (defaults)

### 4.2.5 API Cloning (HW6 §5)
- [ ] Display name of imported API set to **Note Keeper Basic**
- [ ] API cloned and renamed to **Note Keeper Standard**
- [ ] Standard API URL suffix set to `standard`
- [ ] Backend Service URL of the cloned (Standard) API set to the App Service URL

### 4.2.6 Basic Product (HW6 §6)
- [ ] Product `Basic` (id `basic`) created
- [x] Description: `Provides basic note keeper functionality with no attachment support.`
- [x] Published = true, Requires Subscription = true, Requires Approval = false
- [x] No subscription count limit, no legal terms
- [x] Subscription added: display name `Basic Subscription`, name `BasicSubscription`
- [x] Attachments operation removed from Note Keeper Basic API
- [x] Primary Key recorded here: `b45b315cc0c54eb5a4ff690d7c09d164`

### 4.2.7 Standard Product (HW6 §7)
- [x] Product `Standard` (id `standard`) created
- [x] Description: `Provides standard note keeper functionality with attachment support.`
- [x] Published = true, Requires Subscription = true, Requires Approval = false
- [x] No subscription count limit, no legal terms
- [x] Subscription added: display name `Standard Subscription`, name `StandardSubscription`
- [x] Primary Key recorded here: `60686b7bb4cc4e3cb49ff6af5be3c09d`

### 4.2.8 Location Header Override (HW6 §8)
- [x] `set-header` policy applied to All Operations outbound on both Note Keeper Basic and Note Keeper Standard, rewriting the host portion to the APIM gateway host:
  ```xml
  <set-header name="location" exists-action="override">
      <value>@(context.Response.Headers.GetValueOrDefault("location", "").Replace(context.Request.Url.Host, context.Request.OriginalUrl.Host))</value>
  </set-header>
  ```

### 4.2.9 Custom Response Headers (HW6 §9)
- [x] `X-CourseName: CSCI-E94` added to All Operations on both Note Keeper Basic and Note Keeper Standard
- [x] `X-SubscriptionName` added to All Operations on both APIs, dynamically populated via `@(context.Subscription.Name)` — verified returning correct subscription name (e.g., `Basic Subscription` when called with Basic key)

### 4.2.10 HW6 Extra Credit Selected

### 4.2.10 HW6 Extra Credit Selected

#### XC1 — Redis Cache ✅

**Redis Cache Instance:** `redis-apimgmtcache-cscie94` (Basic C0 250MB, Sweden Central, `rg_hw6`)  
**Linked to APIM:** External cache → Default region → Access Key auth connection string

**Cache policies applied to (operation level):**
- **Note Keeper Standard** — `GET /NoteKeeper` (list of notes, `?tagName=` filter supported via `<vary-by-query-parameter>`)
- **Note Keeper Basic** — `GET /NoteKeeper` (list of notes, same policy)
- **Note Keeper Standard** — `GET /notes/{noteId}/attachments` (list of attachments)

**Inbound policy (on each cached operation):**
```xml
<set-header name="Cache-Control" exists-action="delete" />
<cache-lookup vary-by-developer="false" vary-by-developer-groups="false"
    allow-private-response-caching="false" must-revalidate="false"
    downstream-caching-type="none" caching-type="external">
    <vary-by-query-parameter>tagName</vary-by-query-parameter>
</cache-lookup>
```
*(The `set-header` delete strips the browser's `Cache-Control: no-cache, no-store` before cache-lookup — without it, every request is treated as a cache-miss. `caching-type="external"` explicitly targets the linked Redis instance.)*

**Outbound policy (on each cached operation):**
```xml
<cache-store duration="120" cache-response="true" />
```

**Note on Trace:** The APIM portal Trace button creates a new temporary authorization token per click — this token's sequential ID becomes part of the cache key, so Trace will always show cache-miss. Testing must be done via direct HTTP calls (PowerShell or curl).

**Testing — PowerShell:**
```powershell
$headers = @{ "Ocp-Apim-Subscription-Key" = "60686b7bb4cc4e3cb49ff6af5be3c09d" }
$url = "https://apim-cscie-94-hw6.azure-api.net/standard/NoteKeeper"
1..5 | ForEach-Object {
    $t = Measure-Command { Invoke-RestMethod $url -Headers $headers }
    Write-Host "Call $_`: $([math]::Round($t.TotalMilliseconds))ms"
}
```

**Testing — curl:**
```bash
for i in 1 2 3 4 5; do
  time curl -s -H "Ocp-Apim-Subscription-Key: 60686b7bb4cc4e3cb49ff6af5be3c09d" \
    https://apim-cscie-94-hw6.azure-api.net/standard/NoteKeeper > /dev/null
done
```

**Actual test results (client machine, Denmark → Sweden Central):**
- Call 1: 348ms — cache-miss (backend call)
- Call 2: 82ms — cache-hit ✅
- Call 3: 47ms — cache-hit ✅
- Call 4: 48ms — cache-hit ✅
- Call 5: 42ms — cache-hit ✅

**~8× speedup** (348ms → 42ms) confirms Redis caching is fully operational.

---

#### XC2 — JSON-to-XML Transformation ✅

**Applied to:** Both **Note Keeper Standard** and **Note Keeper Basic** — All Operations outbound policy.  
Transformation is conditional: only activates when the caller sends `Accept: application/xml`. All other requests continue to receive JSON responses unchanged.

**Outbound policy additions (appended to existing All Operations outbound):**
```xml
<json-to-xml apply="always" consider-accept-header="true" parse-date="false" />
<!-- Wrap converted XML in root element (workaround required as of 2026/04/28) -->
<choose>
    <when condition="@(context.Response.Headers.GetValueOrDefault("Content-Type", "").StartsWith("application/xml", StringComparison.OrdinalIgnoreCase))">
        <set-body>@{
            string xmlContent = context.Response.Body.As<string>(preserveContent: true);
            return "<document>" + xmlContent + "</document>";
        }</set-body>
        <set-header name="Content-Type" exists-action="override">
            <value>application/xml</value>
        </set-header>
    </when>
</choose>
```

*(The `<document>` wrapper is required because `json-to-xml` on a JSON array produces multiple XML elements with no root, which is invalid XML. The `<choose>` only fires after `json-to-xml` has run and set Content-Type to `application/xml`.)*

**Testing — PowerShell (XML response):**
```powershell
$headers = @{
    "Ocp-Apim-Subscription-Key" = "60686b7bb4cc4e3cb49ff6af5be3c09d"
    "Accept" = "application/xml"
}
Invoke-WebRequest "https://apim-cscie-94-hw6.azure-api.net/standard/NoteKeeper" `
    -Headers $headers | Select-Object -ExpandProperty Content
```

**Testing — PowerShell (JSON response — no Accept header):**
```powershell
$headers = @{ "Ocp-Apim-Subscription-Key" = "60686b7bb4cc4e3cb49ff6af5be3c09d" }
Invoke-RestMethod "https://apim-cscie-94-hw6.azure-api.net/standard/NoteKeeper" -Headers $headers
```

**Testing — curl:**
```bash
# XML response
curl -s -H "Ocp-Apim-Subscription-Key: 60686b7bb4cc4e3cb49ff6af5be3c09d" \
     -H "Accept: application/xml" \
     https://apim-cscie-94-hw6.azure-api.net/standard/NoteKeeper

# JSON response (no Accept header)
curl -s -H "Ocp-Apim-Subscription-Key: 60686b7bb4cc4e3cb49ff6af5be3c09d" \
     https://apim-cscie-94-hw6.azure-api.net/standard/NoteKeeper
```

**Expected XML response format:**
```xml
<document>
  <item><noteId>...</noteId><title>...</title>...</item>
  <item><noteId>...</noteId><title>...</title>...</item>
</document>
```

**Manually tested and confirmed working ✅**

---

#### XC3 — Free Product with Rate Limiting ✅

**Applied to:** **Free** product (product-level policy).  
The Free product contains the same **Note Keeper Basic** API as the Basic product. Rate limit: 5 calls per 60-second window per subscription.

**Free product details:**
- Display name: `Free` | Product ID: `free`
- API: Note Keeper Basic (shared, no clone needed)
- Subscription key (Free-Subscription primary): `f5914deacee04cd6a8f0d4deea5caafe`

**Product-level policy (Free → Policies):**
```xml
<policies>
    <inbound>
        <base />
        <rate-limit calls="5" renewal-period="60"
            remaining-calls-variable-name="remainingCallsCount" />
    </inbound>
    <backend>
        <base />
    </backend>
    <outbound>
        <base />
        <set-header name="X-Free-NotesCalls-Limit" exists-action="override">
            <value>5</value>
        </set-header>
        <set-header name="X-Free-NotesCalls-Remaining" exists-action="override">
            <value>@(context.Variables.GetValueOrDefault<int>("remainingCallsCount", 0).ToString())</value>
        </set-header>
    </outbound>
    <on-error>
        <base />
        <set-header name="X-Free-NotesCalls-Limit" exists-action="override">
            <value>5</value>
        </set-header>
        <choose>
            <when condition="@(context.Response.StatusCode == 429)">
                <set-header name="Retry-After" exists-action="override">
                    <value>60</value>
                </set-header>
            </when>
        </choose>
    </on-error>
</policies>
```

**Header behaviour:**
| Header | On 200 | On 429 |
|--------|--------|--------|
| `X-Free-NotesCalls-Limit` | ✅ returned (value: 5) | ✅ returned (value: 5) |
| `X-Free-NotesCalls-Remaining` | ✅ returned (counts down 4→3→2→1→0) | ❌ absent |
| `Retry-After` | ❌ absent | ✅ returned (value: 60) |

**Testing — PowerShell (run 6 calls, 5th triggers 429):**
```powershell
$key = "f5914deacee04cd6a8f0d4deea5caafe"
$url = "https://apim-cscie-94-hw6.azure-api.net/NoteKeeper"

1..6 | ForEach-Object {
    Write-Host "Call $_" -ForegroundColor Cyan
    try {
        $r = Invoke-WebRequest -Uri $url -Headers @{"Ocp-Apim-Subscription-Key"=$key}
        Write-Host "  Status: $($r.StatusCode)"
        Write-Host "  X-Free-NotesCalls-Limit:     $($r.Headers['X-Free-NotesCalls-Limit'])"
        Write-Host "  X-Free-NotesCalls-Remaining: $($r.Headers['X-Free-NotesCalls-Remaining'])"
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        Write-Host "  Status: $code" -ForegroundColor Red
        $hdrs = $_.Exception.Response.Headers
        Write-Host "  X-Free-NotesCalls-Limit: $($hdrs['X-Free-NotesCalls-Limit'])"
        Write-Host "  Retry-After: $($hdrs['Retry-After'])"
    }
    Start-Sleep -Milliseconds 200
}
```

**Manually tested and confirmed working ✅**  
Calls 1–4: HTTP 200, headers present, Remaining counts down to 0.  
Call 5: HTTP 429, `X-Free-NotesCalls-Limit: 5`, `Retry-After: 60`, no Remaining header.

---

## 5. Microsoft Foundry and Project Name

**Microsoft Foundry Name:** `ai-csscie94-foundry`  
**Foundry Project Name:** `note-keeper`

**Azure OpenAI Endpoint:** `https://ai-csscie94-foundry.openai.azure.com/`

---

## 6. Custom Azure Resource Abbreviations

**No custom Azure resource abbreviations were used in this project.**

All resources follow standard Azure naming conventions per the Cloud Adoption Framework.

---

## 7. Azure App Service Website URL

**HW6 REST API (App Service):** *(to be recorded once the new HW6 App Service is created — see §4.2.2)*

**HW6 API Management Gateway:** `https://apim-cscie-94-hw6.azure-api.net`

> The Swagger UI is configured to load at the root path (`/`) of the App Service, so navigating to the base URL displays the interactive API documentation.

---

## 8. Target URI for Azure OpenAI Service

**Azure OpenAI Service Endpoint:**  
`https://ai-csscie94-foundry.openai.azure.com/`

**Deployment Model Name:** `gpt-5-mini`

**Configuration Note:**  
The application expects the following environment variables to be set in Azure App Service (and equivalents in user secrets for local dev):

- `AzureOpenAI__DeploymentUri` = `https://ai-csscie94-foundry.openai.azure.com/`
- `AzureOpenAI__ApiKey` = *(API key — not included per requirement #9)*
- `AzureOpenAI__DeploymentModelName` = `gpt-5-mini`
- `AzureOpenAI__Temperature` = `1.0`
- `AzureOpenAI__TopP` = `1.0`
- `AzureOpenAI__MaxOutputTokens` = `500`
- `NoteLimits__MaxNotes` = `10`
- `NoteLimits__MaxAttachments` = `3`
- `ApplicationInsights__AuthenticationApiKey` = *(SDK control-channel key for Live Metrics)*
- `StorageAccountSettings__Url` = `https://<your-storage-account>.blob.core.windows.net/`
- `StorageAccountSettings__TenantId` = `<your-tenant-id>` *(local dev only)*
- `StorageAccountSettings__AccountName` = `<your-storage-account-name>`

### How to Configure Settings in Azure App Service

1. **Via Azure Portal:** App Service → **Settings** → **Environment variables** → **App settings** → **+ Add**, then **Apply** + **Confirm**, then restart.
2. **Via Azure CLI:**
   ```bash
   az webapp config appsettings set --name <app-name> --resource-group <rg> --settings KEY=VALUE
   ```
3. **Via Azure PowerShell:**
   ```powershell
   Set-AzWebApp -ResourceGroupName <rg> -Name <app-name> -AppSettings @{ "KEY"="VALUE" }
   ```

> **Note:** The double underscore (`__`) is used to represent nested configuration sections in Azure App Service environment variables. `KEY__SUB` maps to `KEY:SUB` in `appsettings.json`. Colons (`:`) are not valid in Linux App Service env-var names.

---

## 9. Security Note

**No credentials for Azure resources are included in this repository or documentation**, as specified by the requirements.

All sensitive configuration values are stored in:
- **Local Development:** User Secrets (`secrets.json`)
- **Production:** Azure App Service Environment Variables

---

## 10. Application Insights Instance

**HW6 Application Insights instance name:** *(to be recorded once created — see §4.2.2)*

**Telemetry features carried over from prior assignments:**
- Validation errors logged via `TrackTrace` (`SeverityLevel.Warning`) with `InputPayload`
- Exceptions logged via `TrackException` with `InputPayload`
- `TrackEvent` calls on note CRUD operations for usage metrics
- Live Metrics with SDK Control Channel authentication (`ApplicationInsights__AuthenticationApiKey`)

> 404 NotFound on a missing note is treated as a valid business outcome — it is **not** logged as a validation error or exception.

---

## 11. Managed Identity Implementation

**This application uses Microsoft Entra ID (Azure AD) authentication via Managed Identity for passwordless database access.**

**Connection string format:**
```
Server=tcp:<sql-server>.database.windows.net,1433;Initial Catalog=<sql-db>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=120;Authentication=Active Directory Default;
```

- **No credentials** in the connection string — `Authentication=Active Directory Default`
- **Local Development** — authenticates via Azure CLI / Visual Studio credentials through `DefaultAzureCredential`
- **Azure Deployment** — uses the App Service's system-assigned or user-assigned managed identity
- The identity must hold the appropriate roles on the SQL DB (`db_datareader`, `db_datawriter`, `db_ddladmin`, or `db_owner` for the function app's identity)

**Important — local dev:** The IP of your dev box must be added to the Azure SQL Server firewall (Networking → Firewall rules). Changes propagate within ~5 minutes.

**HW6 SQL information** *(to be recorded once a HW6-specific database, if any, is established):*
- Azure SQL Server name:
- Azure SQL Database name:
- Full URL:
- SQL Authentication username/password (if used in addition to managed identity):

---

## 12. Azure Blob Storage Configuration

**Storage Account:** As configured in user secrets / environment variables (`StorageAccountSettings__Url`, `StorageAccountSettings__AccountName`).

**Authentication:** `DefaultAzureCredential` everywhere — no account keys or connection strings in code or config.

**Container Naming:**
- Each note has a private blob container named after its lowercase GUID (`{noteId}`)
- Each note has a paired zip container `{noteId}-zip`
- Access level: Private (no anonymous access)

**Blob Metadata:** Every attachment blob carries a `noteid` metadata key set to the note's ID (case-insensitive per Azure spec).

**Retry Logic:** Container creation retries Azure's "ContainerBeingDeleted" transient error up to 10 times with 4 s delays.

**Protected Containers (never deleted during seeding):**
- `app-package-func-<funcapp-name>` — Azure Function deployment package
- `azure-webjobs-hosts` — Functions runtime metadata
- `azure-webjobs-secrets` — Functions secrets
- All `$`-prefixed containers (Azure system containers)

---

## 13. Azure Function App

**HW6 Azure Function name:** *(carried from HW4 — see HW4 deployment; will be re-recorded here once verified for HW6.)*

The function `AttachmentZipFunction` triggers on the `attachment-zip-requests` queue and writes zip files to the `{noteId}-zip` container. See §4.1.3 for the public-facing endpoints that surface this work.

> **HW6 reminder:** API Management does **not** expose the `attachmentzipfiles` operations to clients. They remain on the App Service for internal/legacy use only.

---
