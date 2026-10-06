# ACMS – AWACS Central Management System

ASP.NET Core 8 implementation of the *ACMS Project Proposal*. It gives one web front end
to every AWACS server: engineers find equipment, change its attributes through a guided and
verified workflow, and every change lands in one central audit trail.

ACMS only uses the documented AWACS HTTP/XML interfaces. It never touches the AWACS
database and never invents endpoints.

| Proposal | Where it lives |
|---|---|
| Multi-server dashboard | `Pages/Index` – reachability and workstation count per server |
| Equipment search across servers | `Pages/Equipment/Index`, `Details` |
| Guided, verified edit | `Pages/Equipment/Edit` → `EquipmentChangeService` |
| Central audit trail | `Pages/Audit`, table `AuditEntries` |
| Server configuration | `Pages/Admin/Servers` (Administrator only) |
| Windows Auth + AD groups → roles | `Security/AdGroupClaimsTransformation` |
| REST API | `Api/ServersController`, `Api/AuditController` |

## Solution layout

```
ACMS/
├─ Acms.sln
├─ database/001_create_schema.sql   SQL Server schema (production)
├─ src/
│  ├─ Acms.Core            domain, rules, change workflow (no web or database code)
│  ├─ Acms.Infrastructure  EF Core (SQL Server / SQLite), AWACS HTTP/XML client
│  └─ Acms.Web             Razor Pages UI, Web API, Windows auth, role mapping
└─ tests/Acms.Tests        xUnit tests
```

## Change workflow (slide "System Logic Diagram")

`EquipmentChangeService.EditAsync` runs these steps for every edit, from the UI or the API:

1. **Access control** – only Engineer and Administrator can edit (`CanEditEquipment` policy).
2. **Validate** – WSID and attribute names (`A-Z a-z 0-9 _ - .`), read-only attributes,
   maximum length, no control characters. Unknown attributes are refused unless
   `AllowNewAttributes` is on.
3. **Resolve server** – must exist and be active.
4. **Capture before-state** – `wsdata.xml?ws=WSID`.
5. **Apply only differences** – unchanged values are never sent. If someone changed a value
   on AWACS after the form was opened, the edit is refused (HTTP 409 in the API).
6. **Re-read and verify** – the workstation is read again and every changed value compared.
7. **Audit** – user, time, server, WSID, requested / before / after values, outcome
   (`Success`, `NoChange`, `Rejected`, `Failed`) and a reference id that also appears in the logs.

Once the update has been sent, steps 6 and 7 always finish, even if the browser disconnects.
Updates are never retried automatically; the re-read decides what really happened.

## Run it locally

You need the .NET 8 SDK. Development mode uses SQLite, a fake AWACS with demo
workstations, and a fake signed-in user (`DEV\developer`, Administrator), so neither AWACS
nor Active Directory is required.

```bash
cd ACMS
dotnet test
dotnet run --project src/Acms.Web
```

Open the URL printed in the console. To try other roles, change
`Acms:Security:DevAuthentication:Roles` in `src/Acms.Web/appsettings.Development.json`
to `["Engineer"]` or `["Viewer"]`. Delete `src/Acms.Web/acms-dev.db` to start from empty data.

From Visual Studio, open `Acms.sln` and run the `Acms.Web` profile.

## Configuration

All settings are in `src/Acms.Web/appsettings.json`. On the server, override them in
`appsettings.Production.json` or with environment variables
(`ConnectionStrings__Acms`, `Awacs__TimeoutSeconds`, ...).

| Setting | Meaning |
|---|---|
| `ConnectionStrings:Acms` | ACMS database (server list and audit trail) |
| `Database:Provider` | `SqlServer` (production) or `Sqlite` (development) |
| `Acms:Security:RoleMappings` | AD groups (`DOMAIN\Group` or SID) granting each role |
| `Acms:Ui:SummaryAttributes` | Columns shown on the equipment list |
| `Awacs:WorkstationDataPath` | Read interface, default `template/wsdata.xml` |
| `Awacs:UpdatePath` | Update interface, default `template/wswoupdate.html` |
| `Awacs:UpdateQueryTemplate` | Query sent per changed attribute. Tokens `{ws}`, `{name}`, `{value}` |
| `Awacs:UpdateFailureMarkers` | Text that marks an update response as failed even with HTTP 200 |
| `Awacs:WorkstationElementNames` / `WorkstationIdNames` | How to find workstations and their id in `wsdata.xml` |
| `Awacs:UseDefaultCredentials` | Call AWACS as the app pool account (Windows auth) |
| `EquipmentRules:ReadOnlyAttributes` | Attributes ACMS never changes (default `WSID`) |
| `EquipmentRules:AllowNewAttributes` | Allow creating attributes that do not exist yet (default off) |

Roles:

| Role | Can |
|---|---|
| Viewer | Dashboard, equipment, audit trail (read-only) |
| Engineer | Viewer + edit equipment |
| Administrator | Engineer + manage AWACS servers |

Users in none of the mapped groups get "Access denied".

## Deploy to IIS (Windows Server)

1. Install the **ASP.NET Core 8 Hosting Bundle** on the server.
2. Create the database and run `database/001_create_schema.sql`. Grant the app pool account
   the permissions at the end of the script (the audit table is insert-only).
3. Publish: `dotnet publish src/Acms.Web -c Release -o C:\inetpub\acms`.
4. In IIS create a site or application pointing to that folder, with an app pool set to
   *No Managed Code* and running as a domain service account (for example `COMPANY\svc-acms`).
   That account must be allowed to call the AWACS servers and to use the ACMS database.
5. Under **Authentication**, enable *Windows Authentication* and disable
   *Anonymous Authentication*.
6. Put production settings in `appsettings.Production.json`: connection string, real AD
   group names, and the AWACS settings confirmed below.
7. Sign in as an Administrator, open **Servers** and add each AWACS server. Use
   **Test connection** to check reachability.

## Confirm before go-live

These parts depend on AWACS details that were not in the proposal. They are configuration
only, with no code changes needed:

- [ ] **Update query format.** `Awacs:UpdateQueryTemplate` is currently an assumption
      (`ws={ws}&setwsattr={name}&value={value}`). Match it to *AWACS API RESTful.pdf*.
- [ ] **`wsdata.xml` layout.** The parser handles `<ws id="..">` elements, repeated record
      elements, and a single flat record (see `AwacsXmlParserTests`). Check it against a real
      response and adjust `WorkstationElementNames` / `WorkstationIdNames` if needed.
- [ ] **Update error responses.** If AWACS answers HTTP 200 with an error page, put the
      error text in `UpdateFailureMarkers`. The re-read catches it anyway, but the message is clearer.
- [ ] **AD group names** in `Acms:Security:RoleMappings`.
- [ ] **Read-only attributes**: which attributes engineers must never change.

## Not implemented on purpose

- **Add / delete equipment.** AWACS does not document create or delete interfaces
  (pending ITEC). The buttons are visible but disabled. When ITEC documents them, add them
  to `IAwacsClient` and follow the same validate → apply → verify → audit pattern.
- **Optional `wswodata.xml`** (work order data) is not used yet.

## REST API

All endpoints use the same Windows sign-in and roles as the UI.

```
GET  /api/servers
GET  /api/servers/{id}/workstations?ws=WS1,WS2
GET  /api/servers/{id}/workstations/{wsId}
PUT  /api/servers/{id}/workstations/{wsId}/attributes      (Engineer, Administrator)
     { "values": { "RECIPELOAD": "RCP_02" },
       "expectedOriginal": { "RECIPELOAD": "RCP_01" } }
     → 200 Success/NoChange, 400 Rejected, 409 conflict, 502 AWACS failure
GET  /api/audit?serverId=&wsId=&userName=&action=&outcome=&fromUtc=&toUtc=&page=&pageSize=
```

## Project phases

| Phase | Proposal | Status in this code |
|---|---|---|
| 1 Discovery (2 wk) | Confirm AWACS interfaces | See *Confirm before go-live* |
| 2 Initial development (2 wk) | Read-only dashboard, search, audit | Done |
| 3 Controlled edit (3 wk) | Validated, verified, audited edits | Done |
| 4 Multi-server + UAT (2 wk) | Several servers, user testing | Multi-server done; UAT pending |
| 5 Production (1 wk) | IIS rollout | Steps above |
