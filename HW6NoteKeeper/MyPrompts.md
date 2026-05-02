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
