# ACMS – AWACS Central Management System

ASP.NET Core 8 implementation of the *ACMS Project Proposal*. It gives one web front end
to every AWACS server: engineers find equipment, change its attributes through a guided and
verified workflow, and every change lands in one central audit trail.

ACMS only uses the AWACS HTTP/XML interfaces documented by ITEC in "Urls for manipulating
workstations". It never touches the AWACS database and never invents endpoints:

| Purpose | AWACS URL |
|---|---|
| Read workstations | `/template/wsdata.xml?ws=WSID` (also `ws=*` and `ws=WS1,WS2`) |
| Change workstation attributes | `/template/wswoupdate.html?ws=WSID&setwsattr=WsId="WSID",attr="value",attr2="value2"` |

The second URL does the same as **Save changes** on the AWACS page
`/template/awacs_editws.html`.

| Proposal | Where it lives |
|---|---|
| Multi-server dashboard | `Pages/Index` – reachability and workstation count per server |
| Equipment search across servers | `Pages/Equipment/Index`, `Details` |
| Guided, verified edit | `Pages/Equipment/Edit` → `EquipmentChangeService` |
| Bulk update (one attribute, many machines) | `Pages/Equipment/Bulk` → `BulkUpdateService` |
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

## Bulk update (for example a new SPEED_SPEC list from IE)

When IE sends a new UPH ("Change T Speed") for many machines, there is no need to look up each
machine in MMSv3, open `awacs_editws.html` on its AWACS server and click **Save changes** one by one.

1. Open **Bulk update** (Engineer or Administrator).
2. Attribute to change: `SPEED_SPEC`.
3. Paste the machines and new values. You can copy the rows straight from the e-mail or Excel table:
   ACMS uses the **first column as the WSID** and the **last column as the new value**, so the columns in
   between (package, state, Prod%, old T speed...) are ignored. Rows with `N/A` or no value are skipped,
   and so is the header row.
4. **Preview**. ACMS reads every active AWACS server (`wsdata.xml?ws=A,B,C`) to find the machine and its
   current value. Nothing is changed yet. Each row shows *Will change*, *Already set*, *Skipped*,
   *Not found*, *On several servers* or *Unknown* (a server could not be read).
5. Untick any machine you want to leave out and click **Apply selected changes**.
   Each machine is then changed (one `setwsattr` call), re-read, verified and audited, exactly like a
   single edit. A machine whose value changed on AWACS after the preview is refused, not overwritten.

Running the same list again afterwards should show every machine as *Already set*, which is a quick
cross-check that all values are on AWACS.

Why not let a script fill in `awacs_editws.html` and click the button? The ITEC document says to stick
to the documented URLs, and a scripted browser breaks whenever the page layout changes. The
`setwsattr` URL is what the button does, and ACMS adds the verification and the audit trail.

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
| `Awacs:UpdateAttributeFormat` | `Quoted` (default, `attr="value"`, values may contain commas) or `Colon` (`attr:value`) |
| `Awacs:MaxIdsPerRequest` | Longest `ws=A,B,C` list in one read (default 50); longer lists are split |
| `Awacs:UpdateFailureMarkers` | Text that marks an update response as failed even with HTTP 200 |
| `Awacs:WorkstationElementNames` / `WorkstationIdNames` | How to find workstations and their id in `wsdata.xml` |
| `Awacs:UseDefaultCredentials` | Call AWACS as the app pool account (Windows auth) |
| `EquipmentRules:ReadOnlyAttributes` | Attributes ACMS never changes (`WSID`, and `UPDATED`, which AWACS stamps on save) |
| `EquipmentRules:KnownAttributes` | Attributes that can be filled in even when empty. AWACS leaves empty attributes out of `wsdata.xml`, so without this list e.g. an empty `AREA` could not be set |
| `EquipmentRules:AllowNewAttributes` | Allow any other attribute name (default off, so typos cannot create attributes) |

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

The read and update URLs and their formats come from the ITEC document. Still check, with
configuration only and no code changes:

- [ ] **One test change on a spare machine.** Change one attribute and check it on `awacs_editws.html`.
      Also try a value with a comma (for example `CONTROL = ATX18II,Flex`) to confirm AWACS keeps quoted
      values together. If it does not, ACMS's re-read reports the change as *Failed* in the audit trail.
- [ ] **Update error responses.** If AWACS answers HTTP 200 with an error page, put the error text in
      `Awacs:UpdateFailureMarkers`. The re-read catches it anyway, but the message is clearer.
- [ ] **AD group names** in `Acms:Security:RoleMappings`.
- [ ] **Read-only and known attributes** in `EquipmentRules` (taken from the AWACS edit workstation page;
      the `WO_*` attributes are left out of the known list on purpose).
- [ ] **AWACS servers.** Add every server (for example `http://myser01ms079.nws.nexperia.com/`) under
      **Servers**. The bulk update searches all active servers.

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
POST /api/bulk-update                                       (Engineer, Administrator)
     { "attribute": "SPEED_SPEC",
       "rows": [ { "wsId": "DB-AXF-012S", "value": "28000" } ],
       "dryRun": true }
     → preview, plus results when "dryRun": false
```

`dryRun` defaults to `true`, so a script only changes AWACS when it says so. Example from PowerShell
with your Windows login:

```powershell
$body = @{
  attribute = "SPEED_SPEC"
  dryRun    = $true            # look first, then run again with $false
  rows      = @(Import-Csv .\new-uph.csv | ForEach-Object { @{ wsId = $_.Ws; value = $_.'Change T Speed' } })
} | ConvertTo-Json -Depth 3

$r = Invoke-RestMethod https://acms.company.local/api/bulk-update -Method Post `
       -UseDefaultCredentials -ContentType "application/json" -Body $body
$r.preview.rows | Format-Table wsId, serverName, currentValue, newValue, status
```

## Project phases

| Phase | Proposal | Status in this code |
|---|---|---|
| 1 Discovery (2 wk) | Confirm AWACS interfaces | See *Confirm before go-live* |
| 2 Initial development (2 wk) | Read-only dashboard, search, audit | Done |
| 3 Controlled edit (3 wk) | Validated, verified, audited edits | Done |
| 4 Multi-server + UAT (2 wk) | Several servers, user testing | Multi-server done; UAT pending |
| 5 Production (1 wk) | IIS rollout | Steps above |
