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
- [ ] APIM gateway URL recorded here:
- [ ] APIM service name recorded here:

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
- [ ] Description: `Provides basic note keeper functionality with no attachment support.`
- [ ] Published = true, Requires Subscription = true, Requires Approval = false
- [ ] No subscription count limit, no legal terms
- [ ] Subscription added: display name `Basic Subscription`, name `BasicSubscription`
- [ ] Attachments operation removed from Note Keeper Basic API
- [ ] Primary Key recorded here:

### 4.2.7 Standard Product (HW6 §7)
- [ ] Product `Standard` (id `standard`) created
- [ ] Description: `Provides standard note keeper functionality with attachment support.`
- [ ] Published = true, Requires Subscription = true, Requires Approval = false
- [ ] No subscription count limit, no legal terms
- [ ] Subscription added: display name `Standard Subscription`, name `StandardSubscription`
- [ ] Primary Key recorded here:

### 4.2.8 Location Header Override (HW6 §8)
- [ ] `set-header` policy applied to all operations that return a `location` header, rewriting the host portion to the APIM gateway host:
  ```xml
  <set-header name="location" exists-action="override">
      <value>@(context.Response.Headers.GetValueOrDefault("location", "").Replace(context.Request.Url.Host, context.Request.OriginalUrl.Host))</value>
  </set-header>
  ```

### 4.2.9 Custom Response Headers (HW6 §9)
- [ ] `X-CourseName: CSCI-E94` added to all operations on both Basic and Standard
- [ ] `X-SubscriptionName` added to all operations on both Basic and Standard, dynamically populated from the subscription's name via APIM policy expression

### 4.2.10 HW6 Extra Credit Selected
*To be decided in coordination with the user. Options listed in HW6 instructions are:*
- *EC1 — Add cache support (Redis Basic 250 MB) for list-of-notes and list-of-attachments operations, 20 s TTL*
- *EC2 — Apply `json-to-xml` transformation policy to notes responses when `Accept: application/xml`*
- *EC3 — Add a `Free` product tier with rate-limit policy (5 calls/min) and `X-Free-NotesCalls-Remaining` / `-Limit` / `Retry-After` headers*

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

**HW6 API Management Gateway:** *(to be recorded once the APIM service is created — see §4.2.3)*

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
