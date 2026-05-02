# My Prompts Used During Development

This document contains all text-based prompts used with GitHub Copilot to aid in the implementation of the HW6NoteKeeper assignment.

---

## 1. Locate `ApplicationInsights:AuthenticationApiKey` in the Azure Portal

**Prompt:**
```
where do i find this enviorment setting value (from my secrets.json) ApplicationInsights:AuthenticationApiKey in the azure portal?  Remember to update the existing myprompts.md file in the hw6 solution - first reseting it because the prompts from HW4 schould not be there.
```

**Context:**
The `secrets.json` file for the HW6NoteKeeper project contains a key named `ApplicationInsights:AuthenticationApiKey`. The codebase uses this in `Program.cs` to configure `QuickPulseTelemetryModule.AuthenticationApiKey`, which authenticates the SDK control channel for Application Insights **Live Metrics**. The user needs to know where in the Azure Portal this key is generated. Additionally, MyPrompts.md was carried over from the HW4 copy and contained 974 lines of HW4-specific prompts that no longer apply to HW6 — it needed to be reset.

**Resolution:**
- **Azure Portal location**: Application Insights resource → **Configure** section → **API Access** → **+ Create API key** → check **"Authenticate SDK control channel"** → **Generate key** → copy the key (shown only once) and store it as `ApplicationInsights__AuthenticationApiKey` in user secrets or App Service configuration.
- This key is **distinct** from `APPLICATIONINSIGHTS_CONNECTION_STRING` (found on the Application Insights *Overview* blade), which authorizes telemetry ingestion rather than Live Metrics filtering.
- Reset `MyPrompts.md` by deleting the old 974-line HW4 file and recreating it fresh with this prompt as entry #1.

**Key Learning:**
- Application Insights uses **two separate credentials**: the Connection String (write/ingest path) and an API Key with the *"Authenticate SDK control channel"* permission (read/Live Metrics filtering path).
- When copying a project to a new homework iteration, `MyPrompts.md` should be reset so the prompt log accurately reflects work done on the new assignment, not the previous one.
- The `QuickPulseTelemetryModule` is what powers the **Live Metrics** experience in the portal; without `AuthenticationApiKey` it still works in unauthenticated mode but cannot apply server-side filters.

---

## 2. Setting `ApplicationInsights:AuthenticationApiKey` as an App Service Application Setting

**Prompt:**
```
this key / value is from my secrets.json tell me how to set it up in my web app (see screen shot .... in this screen shot (towards the end of this sentence) you see i am trying to add an application setting ... what name and value should i put in
```

**Context:**
The user opened the **Add/Edit application setting** blade in the Azure Portal for `app-notekeeper-cscie94-ps-hw6` (App Service → Settings → Environment variables → App settings → + Add) and asked what `Name` and `Value` to enter to surface the local `secrets.json` value `ApplicationInsights:AuthenticationApiKey` to the deployed web app.

**Resolution:**
- **Name**: `ApplicationInsights__AuthenticationApiKey` (double underscore, not colon)
- **Value**: the API key value copied from `secrets.json`
- **Deployment slot setting**: leave unchecked
- Click **Apply** then **Save** — the App Service will restart and `QuickPulseTelemetryModule.AuthenticationApiKey` will be populated.

**Key Learning:**
- Azure App Service (especially on Linux) does not allow `:` in environment variable names. .NET's configuration provider automatically translates `__` (double underscore) into the `:` hierarchy separator, so `ApplicationInsights__AuthenticationApiKey` binds correctly to `ApplicationInsightsSettings.AuthenticationApiKey`.
- This convention is portable across Windows and Linux App Service plans and matches how the rest of the project's nested settings (e.g., `StorageAccountSettings__Url`, `AzureOpenAI__ApiKey`) are configured in production.
- Using App Settings instead of `appsettings.json` keeps secrets out of source control and lets each environment (dev/test/prod) hold its own values.

---

## 3. Always Log Every Prompt to MyPrompts.md

**Prompt:**
```
always log every prompt
```

**Context:**
After answering prompt #2, the assistant asked whether to log it. The user established a standing instruction: every prompt — without needing to ask first — must be appended to `MyPrompts.md` as part of the workflow.

**Resolution:**
- Recorded prompt #2 (App Service environment variable setup) as the previous entry.
- Recorded this directive as prompt #3.
- Going forward, every user prompt will automatically be appended to `MyPrompts.md` with its number, the verbatim prompt text, context, resolution, and key learning — no confirmation required.

**Key Learning:**
- Standing instructions like this remove friction by eliminating one round-trip per prompt and ensure the prompt log is always complete.
- This restores the same MANDATORY logging protocol that was used in earlier homework iterations.

---

## 4. Begin HW6 Implementation — Reset ProjectNotes.md and Establish Step-by-Step Workflow

**Prompt:**
```
We will now implement HW6 - it is described in the pdf and docx files (name is "HW06 Instructions") at this location ... you will do the code (step by step and only in coordination with me - at every step i must first give the "go" -- and you will make no asumptions and ask me first every time -- you will edit the code and I will do the changes on azure (unless we coordinate otherwise) ... Please update ProjectNotes.md accordingly to the fact that HW6 is based on Hw4 -- but i will not do the extracredit from HW4 that can be removed from ProjectNotes.md ... you may now edit projectnotes.md (it exists - do not create it) ...
```

**Context:**
The HW6NoteKeeper folder began life as a verbatim copy of HW4NoteKeeper (renamed via the `dotnet-project-renamer` skill earlier in this session). Its `ProjectNotes.md` therefore still described HW4 in full — including HW4's extra credit options (Managed Identities for Azure Storage Queues, Custom Application Insights Telemetry for attachments, and the `HW6NoteKeeperEx1` Jobs-table tracking). The user will **not** be carrying any HW4 extra credit forward into HW6, so those sections must be removed before HW6 implementation begins.

The user also established the working protocol for HW6:
- **Step-by-step:** assistant proposes one step at a time and waits for an explicit "go" before acting.
- **No assumptions:** every ambiguity must be confirmed via a question before any code is written.
- **Division of labor:** assistant edits the .NET code; the user performs Azure Portal changes (unless agreed otherwise).
- **Source of truth for the assignment spec:** `HW06 Instructions.docx` and `HW06 Instructions.pdf` in the assignment folder.

**Resolution:**
- Read the full `HW06 Instructions.docx` to understand HW6 scope (primarily an Azure API Management exercise with four small code changes: unique `OperationId`, unique controller method names, host-name handling, and CORS).
- Deleted the inherited 888-line HW4-flavored `ProjectNotes.md` and replaced it with a fresh HW6 version that:
  - Re-titles the assignment as **"Assignment #6 — Note Keeper (Azure API Management)"** and explicitly states it is built on HW4.
  - Removes all HW4 extra-credit content (Managed Identities for Storage Queues, Custom App Insights Telemetry for attachments, sections 4.2.14–4.2.16 about `HW6NoteKeeperEx1` / Jobs table / `ex1` queue).
  - Reframes the surviving HW4 functionality as **§4.1 "Inherited from HW4 (Already Implemented in the Copied Codebase)"** so the TA can clearly see what came in for free vs. what HW6 added.
  - Adds **§4.2 "HW6 Implementation Status"** as a checklist mirroring the HW6 instructions (§§1–9 plus the three HW6 extra-credit options) — entries to be filled in as each step is completed.
  - Calls out the HW6-specific rule that API Management products **must not** expose the `attachmentzipfiles` resource.
  - Leaves placeholders for HW6 resources (App Service URL, APIM gateway URL, App Insights name) to be recorded as they are created.

**Key Learning:**
- When iterating from one homework to the next via a project copy, ProjectNotes.md should be aggressively pruned so the TA only sees the new assignment's scope. Carrying forward stale extra-credit content creates noise and may hurt grading clarity.
- Establishing a "wait for go" cadence up front avoids costly rework on a multi-step assignment with both code edits and Azure Portal changes — particularly when the two halves must stay in sync (e.g., `OperationId` uniqueness must be in place *before* APIM imports the swagger.json).
- HW6's actual code surface is small; the real complexity is the API Management configuration (clone, products, subscriptions, policies, headers).

---

## 5. Diagnose `app-notekeeper-cscie94-ps-hw6` Startup Failure After Publish + Add 180-Second Rule

**Prompt:**
```
ok -- the HW6NoteKeeper WAS PUBLISHED but it sets up the database and other things when it is published please check the logstream what went wrong and how to fix it.  do not loop ... if you are doinug Anything and it is looping more than 180 seconds STOP -- tell me where you are at - and ask me if you should proceed.  UnDERSTOOND?  please update copilot-instructions.md with htis
```

**Context:**
A zip-deploy publish of `HW6NoteKeeper` to App Service `app-notekeeper-cscie94-ps-hw6` (resource group `rg_hw6`, Linux DOTNETCORE|10.0, Sweden Central) reported success in the IDE/CLI, but the App Service does not respond. The user noted that the app performs first-boot work (DB seeding, blob container provisioning, etc.), so a startup-time failure is the suspected cause. The user also imposed a hard rule: any tool/command/loop running longer than 180 seconds must be aborted with a status report instead of allowed to continue, and asked for that rule to be persisted in `copilot-instructions.md`.

**Resolution:**
- Re-authenticated `az` to tenant `d607a394-7dc0-4c7d-8b9e-a9ed69c728b9` as `paulschwartzberg@outlook.com`. Located the App Service in resource group `rg_hw6`.
- Used a **bounded** `az webapp log download` (one-shot zip) instead of `az webapp log tail` (unbounded stream) so the 180-second rule could not be violated.
- Reviewed the deployment record: `status = Success`, completed 09:03:24 — the **publish itself was fine**.
- Reviewed the docker container log: container repeatedly **exits with code 134** roughly 21 seconds after start, which is `SIGABRT` — i.e., a .NET unhandled startup exception or `FailFast`. After three rapid failures the App Service applied a 1-then-2-minute cold-start backoff (`Site is blocked due to multiple, consecutive cold start failures`).
- The orchestrator log does not contain the actual .NET stdout/stderr because **Application logging (Filesystem)** is not enabled on this App Service. The `ContainerTimeout` and the 230-second site warm-up message are downstream symptoms, not the root cause.
- Provided the user a bounded next-step plan (no Azure changes made by the assistant): turn on Application logging in the portal, restart, let it crash again, re-pull logs to see the actual stack trace; in parallel, audit App Service environment variables for missing/incorrect values pointing at SQL/Blob/OpenAI/App Insights, since the project rename (HW4→HW6) may have left the running app pointing at resources that don't exist or that managed identity has no role on.
- Created `.github/copilot-instructions.md` codifying: step-by-step coordination, the 180-second loop-abort rule, mandatory prompt logging, ProjectNotes.md hygiene, Azure resource defaults for this assignment, log-handling preferences (bounded over streaming), and the working-environment conventions.

**Key Learning:**
- **Exit code 134 = SIGABRT** on Linux App Service for .NET means an unhandled startup exception aborted the process. The container-orchestrator's `ContainerTimeout` message is misleading — it is the *consequence* of the crash loop, not the cause.
- App Service Linux **does not surface `Console.Error`/`ILogger` startup output in the docker.log by default**. Application logging (Filesystem) at Verbose must be enabled before the docker_container log will include the .NET stack trace; without it, root-causing a startup crash is essentially impossible from the downloadable logs alone.
- After a project rename (HW4→HW6) it is easy to overlook that **App Service environment variables still hold whatever the deployer set them to** — they are independent of the codebase. Mismatched DB/storage/OpenAI/App Insights values are the single most common cause of post-rename startup crashes.
- Use **bounded** log retrieval (`az webapp log download`) by default in any diagnostic flow; reserve `tail`/`stream` for cases where the user is actively repro-ing a live failure and explicitly asks for streaming.
- Standing rules like "stop after 180 s" belong in `.github/copilot-instructions.md` so they survive across sessions and turns rather than living only in chat memory.

---

## 6. Approval to Re-Pull Logs After Enabling Application Logging

**Prompt:**
```
go
```

**Context:**
Following prompt #5, the user was asked to enable **Application logging (Filesystem) → Verbose** on the App Service `app-notekeeper-cscie94-ps-hw6`, restart it, and let it crash again so the real .NET stack trace would be persisted alongside the docker_container.log. "go" is the user's explicit consent for the assistant to now re-download the logs.

**Resolution:**
- Re-ran `az webapp log download` (bounded, one-shot) into a new temp location and inspected the resulting log files for the actual unhandled exception. (Findings continue in subsequent prompt entries.)

**Key Learning:**
- A single-word "go" from the user is the green light defined by the HW6 standing rules; no further confirmation is required for the action that was just proposed (and only that action).
- Re-pulling logs after enabling Application logging is the standard recovery sequence for an exit-code-134 startup crash on App Service Linux.

---

## 7. Real Stack Trace — `DefaultAzureCredential` Cannot Get a Token

**Prompt:**
```
please se the program.cs in HW6 ... when it starts it is running into an error ... you can see screen shots
```
*(Two screenshots attached showing the application startup log captured from the live Log Stream window.)*

**Context:**
The user enabled Log Stream / filesystem application logging in the portal and captured the actual .NET startup output. The screenshots show successful early steps (`AISettings loaded successfully`, `StorageOperationalSettings loaded`, `NoteLimits loaded: MaxNotes = 10, MaxAttachments = 3`, `Starting database and storage initialization...`, `HW6NoteKeeper.Data.DbInitializer[0] Ensuring database exists and applying migrations...`) and then:
```
crit: Program[0]
An error occurred during initialization. Application startup will be aborted.
Microsoft.Data.SqlClient.SqlException (0x80131904):
DefaultAzureCredential failed to retrieve a token from the included credentials.
```
followed by the full credential-chain breakdown:
- `EnvironmentCredential authentication unavailable. Environment variables are not fully configured.`
- `WorkloadIdentityCredential authentication unavailable. The workload options are not fully configured.`
- **`ManagedIdentityCredential authentication unavailable. No response received from the managed identity endpoint.`**
- All interactive credentials (`VisualStudio`, `VSCode`, `AzureCli`, `AzurePowerShell`, `AzureDeveloperCli`, `Broker`) fail because they are unsupported on Linux App Service.
- Inner exception #2 ends in `System.Net.Sockets.SocketException (111): Connection refused (169.254.169.254:80)`.

The crash is exit code 134 (SIGABRT) because the unhandled `SqlException` inside the seeding/migration `Task` propagates back to `Program.Main` and aborts the process before Kestrel ever binds.

**Resolution (proposed — awaiting "go"):**
The 169.254.169.254:80 connection-refused message is the giveaway: that is the IMDS endpoint that `ManagedIdentityCredential` falls back to when **no managed identity is assigned to the App Service**. The connection string uses `Authentication=Active Directory Default`, which means `DefaultAzureCredential` is being asked to retrieve a token, but the chain has no working credential source on a Linux App Service without a managed identity. The fix is two-fold:

1. **Enable a managed identity on the App Service** (system-assigned is simplest):
   - Portal → App Service → **Settings → Identity → System assigned → Status = On → Save**, OR
   - User-assigned: assign the existing `id-dbadmin` managed identity (and add `AZURE_CLIENT_ID=<id-dbadmin clientId>` to App Settings so `DefaultAzureCredential` selects it).
2. **Grant that identity database access**. As an Entra admin on the SQL server, run against the target DB:
   ```sql
   CREATE USER [app-notekeeper-cscie94-ps-hw6] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_datareader ADD MEMBER [app-notekeeper-cscie94-ps-hw6];
   ALTER ROLE db_datawriter ADD MEMBER [app-notekeeper-cscie94-ps-hw6];
   ALTER ROLE db_ddladmin  ADD MEMBER [app-notekeeper-cscie94-ps-hw6];
   ```
   (For a user-assigned identity, replace the principal name with the identity's display name, e.g. `id-dbadmin`.)
3. **Grant the same identity** `Storage Blob Data Contributor` and `Storage Queue Data Contributor` on the storage account, since the next thing the seeding code does is provision blob containers via the same `DefaultAzureCredential`.

**Key Learning:**
- On **App Service Linux without a managed identity assigned**, `DefaultAzureCredential` fails fast at the `ManagedIdentityCredential` step with `Connection refused (169.254.169.254:80)`. That IP is the Azure-VM IMDS endpoint; App Service uses a different mechanism (`IDENTITY_ENDPOINT` + `IDENTITY_HEADER` env vars, populated only when an identity is assigned). When those env vars are missing, the SDK falls back to the VM IMDS IP and gets a TCP refusal — a very recognizable signature of "no managed identity assigned".
- The `Authentication=Active Directory Default` connection-string keyword triggers `DefaultAzureCredential` regardless of how you configured the rest of the SDK. So a missing identity affects **every** Azure-SDK or token-aware library in the app, not just the SQL connection.
- **Always** enable Application logging (Filesystem) at Verbose **before** the first publish to a brand-new App Service. Without it, exit-code-134 crashes are essentially invisible from the Azure CLI logs.
- Project-rename workflows (HW4→HW6) carry forward source code but **not** the App Service's managed-identity configuration, RBAC role assignments, or SQL `CREATE USER FROM EXTERNAL PROVIDER` grants — those have to be re-established for each new App Service / SQL DB.

---
