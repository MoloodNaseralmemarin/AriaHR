# AriaHR — Work Location, QR Code, GPS Geofencing & Attendance Backend Report

**Status:** Backend complete and building; ready for the separate Angular phase.
**Date:** 2026-11-15
**Branch:** work performed on the current `main`-derived working branch, backend only. No Angular file was created or modified.
**Target framework:** .NET 10 / ASP.NET Core / EF Core 10 (SQL Server)

---

## 1. Architecture Audit

### 1.1 What was inspected

| Area | Files / components inspected |
|---|---|
| Modular structure | `src/Modules/{Organization,Attendance,Scheduling,Identity,Requests,Notification,Reporting,Payroll}` with `Domain → Application → Infrastructure → API` layers |
| Work Location | `Organization/Domain/Entities/WorkLocation.cs`, `QRCode.cs`, `WorkLocationsController`, `CreateWorkLocationUseCase`, `GenerateQrCodeUseCase`, `WorkLocationRepository`, `QrCodeRepository`, `WorkLocationConfiguration`, `QRCodeConfiguration` |
| Center model | `Organization` entity is the AriaHR "Center"; `CenterManager` is linked to a Center through the `organization_id` JWT claim (`CurrentUserService`) |
| Employee | `Employee` entity (`UserId`, `OrganizationId`, `IsActive`), `EmployeeLookupService` |
| Identity / Auth | `AuthController`, `JwtTokenService`, JWT bearer configuration in `Program.cs`, `CurrentUserService`, existing role names (`SystemAdmin`, `CenterManager`, `Employee`) |
| Attendance | `AttendanceRecord`, `CheckInUseCase`, `CheckOutUseCase`, `AttendanceController`, `AttendanceRecordRepository`, `AttendanceDbContext`, index configuration, **no migrations existed** for this module |
| Scheduling | `Shift`, `ShiftAssignment`, `ShiftAssignmentRepository`, `AssignShiftUseCase` (`Status = "Assigned"`) |
| Shared infrastructure | `BaseEntity` (soft delete + audit fields), `ICurrentUserService`, lookup-service contracts (`IEmployeeLookupService`, `ILeaveLookupService`, `IWorkLocationLookupService`), `TimezoneHelper`, `UtcDateTimeJsonConverter` |
| Validation / errors | `ProblemDetails` conventions, `LeaveRestrictionException`, per-controller try/catch, Scalar/OpenAPI configuration |

### 1.2 Problems discovered

1. **No work-location name.** `WorkLocation` only stored coordinates, so a center could not distinguish its sites. An existing test even asserted that `Name` must not exist — an outdated assumption that conflicted with the product requirement.
2. **Only one work location per center.** `CreateWorkLocationUseCase` called `HasWorkLocationAsync` and rejected a second location, contradicting "multiple locations per center, each with its own radius".
3. **Radius and coordinates had no database precision.** `Latitude`, `Longitude`, `RadiusInMeters` were `double` mapped to SQL `float` with no Fluent API precision, so stored coordinates were not guaranteed to match the map pin and no radius limits were enforced.
4. **QR system could not support Static or Rotating modes.** It wrote a new `QRCodes` row per generation, deactivated all previous rows, had no signing/authenticity check beyond a database lookup, no expiry beyond `ValidTo`, no per-location configuration, and rotation required a database write per rotation.
5. **QR validation bypassed geofencing.** `CheckInUseCase` set `IsWithinGeofence = true` whenever a QR token was supplied — no coordinates, no distance, no radius. A QR photo from any distance counted as physical presence.
6. **No QR management API.** There was no endpoint for settings, mode change, rotation interval, enable/disable, current token, or revoke.
7. **No GPS validation.** No accuracy threshold, no freshness check, no coordinate range check on the scan path, and no distance stored for audit.
8. **No idempotency / replay protection.** A retried HTTP request created a second attendance record; nothing in the schema prevented duplicate check-ins at the database level.
9. **Shift assignments were ignored when registering attendance.** Late/early information was impossible to derive.
10. **Attendance module had no EF migration at all**, so the module schema was created outside migrations (only history table `__EFMigrationsHistory_Attendance` was configured).
11. **Resource ownership on work locations existed only for create/generate**; update/list/read/manage did not exist at all.
12. **No consistent error contract for attendance**, so a frontend could not reliably map "outside radius" vs "expired QR" vs "duplicate".

### 1.3 Corrections made

- Added `Name`, precise `decimal(9,6)` coordinates and `decimal(8,1)` `AllowedRadiusInMeters` (renamed from `RadiusInMeters`, data preserved by column rename), with range validation (`-90..90`, `-180..180`, `1..1000 m` configurable).
- Removed the one-location restriction; a Center may own many work locations, each with independent coordinates, radius and QR configuration.
- Replaced the DB-row-per-token QR with **stateless HMAC-signed QR tokens** derived from per-location configuration, so rotation causes **zero database writes** and multiple employees can scan the same displayed code.
- Added full QR management endpoints (generate/current/settings/update/enable/disable/revoke) with Center ownership enforcement.
- Added `POST /api/attendance/scan` implementing the complete validation pipeline (JWT identity → QR → location configuration → authorization → GPS → geofence → shift → duplicate/idempotency → persist).
- Hardened the legacy `POST /api/attendance/check-in` QR path: it no longer claims presence without a server-side distance check.
- Added attendance scan audit fields and idempotency keys, plus filtered unique indexes.
- Added an explicit, guarded attendance migration and a non-destructive organization migration.
- Added 60+ automated tests covering work locations, QR modes, GPS and the scan workflow.

---

## 2. Database Changes

### 2.1 Entities modified

**`WorkLocation`** (`Organization` module, inherits `BaseEntity`)

| Column | Type (SQL Server) | Notes |
|---|---|---|
| `Name` | `nvarchar(200)` NOT NULL | required |
| `Latitude` | `decimal(9,6)` | 6 decimals ≈ 0.11 m resolution |
| `Longitude` | `decimal(9,6)` | 6 decimals |
| `AllowedRadiusInMeters` | `decimal(8,1)` | renamed from `RadiusInMeters`; per-location |
| `IsActive` | `bit` | existing |
| `QrCodeMode` | `int` NOT NULL | `1 = Static`, `2 = Dynamic` |
| `QrCodeIdentifier` | `nvarchar(64)` NOT NULL | opaque random identifier, unique |
| `QrCodeEnabled` | `bit` NOT NULL | enable/disable flag |
| `QrRotationIntervalMinutes` | `int` NOT NULL | dynamic rotation window |
| `QrCodeLastGeneratedAtUtc` | `datetime2` NULL | last explicit generate/revoke |

Indexes: `IX_WorkLocations_OrganizationId`, unique `IX_WorkLocations_QrCodeIdentifier` filtered on `[QrCodeIdentifier] <> ''`.

**`QRCode`** (legacy table, kept for compatibility): `Code` and `Title` limited to `nvarchar(128)` / `nvarchar(200)`, `IX_QRCodes_Code`, `IX_QRCodes_WorkLocationId_IsActive`. New attendance QR tokens are **not** stored in this table.

**`AttendanceRecord`** (`Attendance` module, inherits `BaseEntity`)

| New column | Type | Purpose |
|---|---|---|
| `CheckInDistanceInMeters` | `float` NULL | server-computed distance at check-in |
| `CheckOutDistanceInMeters` | `float` NULL | server-computed distance at check-out |
| `CheckInLocationTimestampUtc` | `datetime2` NULL | GPS reading timestamp used at check-in |
| `CheckOutLocationTimestampUtc` | `datetime2` NULL | GPS reading timestamp used at check-out |
| `CheckInIdempotencyKey` | `nvarchar(100)` NULL | replay protection |
| `CheckOutIdempotencyKey` | `nvarchar(100)` NULL | replay protection |

Indexes: unique filtered `IX_AttendanceRecords_EmployeeId_CheckInIdempotencyKey` and `..._CheckOutIdempotencyKey` (`WHERE [key] IS NOT NULL`), plus the existing organization/employee/time indexes.

### 2.2 Entities created

None. QR configuration lives on `WorkLocation` because it is one-to-one with the site, avoids a new entity/layer, and avoids signing-secret storage on the domain entity. No signing secret is stored in the database at all.

### 2.3 Migrations

| Migration | Context | Notes |
|---|---|---|
| `20261115103740_ExtendWorkLocationQrAndGeofencing` | `OrganizationDbContext` | `sp_rename` of `RadiusInMeters` → `AllowedRadiusInMeters`, `ALTER COLUMN` float → decimal, new columns, indexes, and SQL backfill for existing rows (`Name = N'محل کار اصلی'`, per-row random `QrCodeIdentifier`, `QrCodeMode = 2`, `QrCodeEnabled = IsActive`, interval `5`) |
| `20261115103934_AddAttendanceScanGeofenceAndIdempotency` | `AttendanceDbContext` | The Attendance module had no baseline migration, so this migration is guarded: it creates `AttendanceRecords` only if absent, then adds the new columns/indexes only if missing. Safe for both fresh databases and deployments where the table was created manually. |

Migration SQL was generated with `dotnet ef migrations script` for both contexts and inspected; the organization migration contains `sp_rename` (no `DROP COLUMN`), so **no work-location radius data is lost**. No table is dropped or recreated.

### 2.4 Applying migrations (deployment step, not executed here)

```bash
dotnet ef database update --project src/Modules/Organization/Infrastructure \
  --startup-project src/AriaHR.API --context OrganizationDbContext

dotnet ef database update --project src/Modules/Attendance/Infrastructure \
  --startup-project src/AriaHR.API --context AttendanceDbContext
```

---

## 3. Backend Changes

### 3.1 New files

**Shared**
- `src/Shared/AriaHR.Shared/Services/IShiftLookupService.cs` (cross-module scheduling contract)

**Organization / Domain**
- `Domain/Enums/QrCodeMode.cs`

**Organization / Application**
- `Application/Services/IQrCodeTokenService.cs`
- `Application/Exceptions/QrCodeOperationException.cs`
- `Application/Validators/WorkLocationInputValidator.cs`
- `Application/DTOs/UpdateWorkLocationRequest.cs`
- `Application/DTOs/QrCodeSettingsDto.cs`
- `Application/DTOs/UpdateQrCodeSettingsRequest.cs`
- `Application/UseCases/CreateWorkLocation/WorkLocationDtoMapper.cs`
- `Application/UseCases/UpdateWorkLocation/IUpdateWorkLocationUseCase.cs`
- `Application/UseCases/UpdateWorkLocation/UpdateWorkLocationUseCase.cs`
- `Application/UseCases/GetWorkLocations/IGetWorkLocationsUseCase.cs`
- `Application/UseCases/GetWorkLocations/GetWorkLocationsUseCase.cs`
- `Application/UseCases/QrCode/IQrCodeManagementUseCase.cs`
- `Application/UseCases/QrCode/QrCodeManagementUseCase.cs`

**Organization / Infrastructure**
- `Infrastructure/Security/QrCodeTokenService.cs`
- `Infrastructure/Persistence/Migrations/20261115103740_ExtendWorkLocationQrAndGeofencing.cs` (+ Designer)

**Attendance / Application**
- `Application/Options/GpsValidationOptions.cs`
- `Application/Options/AttendancePolicyOptions.cs`
- `Application/Geofencing/GeoDistanceCalculator.cs`
- `Application/AttendanceErrorCodes.cs`
- `Application/Exceptions/AttendanceScanException.cs`
- `Application/DTOs/ScanAttendanceRequest.cs`
- `Application/DTOs/AttendanceScanResponse.cs`
- `Application/Shifts/ShiftTimeValidator.cs`
- `Application/UseCases/IScanAttendanceUseCase.cs`
- `Application/UseCases/ScanAttendanceUseCase.cs`

**Attendance / Infrastructure**
- `Infrastructure/Migrations/20261115103934_AddAttendanceScanGeofenceAndIdempotency.cs` (+ Designer, `AttendanceDbContextModelSnapshot`)

**Scheduling / Infrastructure**
- `Infrastructure/Services/ShiftLookupService.cs`

**Tests**
- `tests/AriaHR.Modules.Attendance.Tests/ScanAttendanceTests.cs`
- Rewritten: `tests/AriaHR.Modules.Organization.Tests/WorkLocationCreateTests.cs`, `DynamicQrCodeTests.cs`

**Docs**
- `docs/QR-GEOFENCING-ATTENDANCE-API-REPORT.md` (this file)

### 3.2 Modified files

- `WorkLocation.cs`, `WorkLocationConfiguration.cs`, `QRCodeConfiguration.cs`
- `IWorkLocationLookupService.cs` (Shared), `WorkLocationLookupService.cs`
- `CreateWorkLocationRequest.cs`, `CreateWorkLocationUseCase.cs`, `WorkLocationDto.cs`, `QrCodeResponse.cs`, `QrCodeOptions.cs`
- `IWorkLocationRepository.cs`, `WorkLocationRepository.cs`
- `WorkLocationsController.cs` (full rewrite)
- `Organization/Infrastructure/DependencyInjection.cs`
- `AttendanceRecord.cs`, `AttendanceRecordDto.cs`, `IAttendanceRecordRepository.cs`, `AttendanceRecordRepository.cs`, `AttendanceRecordConfiguration.cs`
- `CheckInUseCase.cs` (radius property, QR geofence hardening, DTO mapping), `AttendanceController.cs`, `Attendance/Infrastructure/DependencyInjection.cs`
- `Scheduling/Infrastructure/DependencyInjection.cs`
- `src/AriaHR.API/appsettings.json` (QR rotation/radius limits, GPS and shift policy)
- Attendance test fakes for the extended `IWorkLocationLookupService`

### 3.3 Services and business logic

- **`QrCodeTokenService`** — stateless HMAC-SHA256 issue/parse. Key material: `QrCode:SigningKey`, falling back to `Jwt:SecretKey` (documented; production must set a dedicated key). Signatures are compared with `CryptographicOperations.FixedTimeEquals`.
- **`QrCodeManagementUseCase`** — mode/interval settings, enable/disable, revoke (new identifier), generate and current token, with organization scoping. Accepts an optional `TimeProvider` (defaults to `TimeProvider.System`) so rotation windows are deterministic in tests.
- **`WorkLocationLookupService`** — validates signed tokens against database configuration (existence, active, enabled, identifier, mode, expiry, organization) and keeps the legacy `QRCodes` row validation as a fallback.
- **`GeoDistanceCalculator`** — Haversine distance on a 6 371 000 m sphere.
- **`ShiftTimeValidator`** — shift window evaluation with overnight shift support and configurable tolerances.
- **`ScanAttendanceUseCase`** — the complete workflow described in §5 and §6.

### 3.4 Controllers and routes

See §6 for the full reference. Summary:

- `WorkLocationsController` — `api/WorkLocations/work-locations` CRUD + `qrcode/*`
- `AttendanceController` — added `POST api/attendance/scan`
- Existing `ShiftAssignmentsController`, `ShiftsController`, auth and other modules are unchanged.

---

## 4. QR Code Documentation

### 4.1 Static QR

- `QrCodeMode = Static (1)`.
- Token payload has **no expiry** and stays valid while all of these remain unchanged: `QrCodeIdentifier`, `QrCodeMode`, `QrCodeEnabled`, work location active state.
- Managers may revoke/regenerate: `POST .../qrcode/revoke` rotates `QrCodeIdentifier`, immediately invalidating the old printed/displayed code. `POST .../qrcode/disable` suspends validation without changing the identifier; re-enabling restores the same token because the signed payload is derived from the identifier.
- Payload contains only: version, work location id (GUID `N`), opaque identifier, mode, expiry (0 for static). No employee, organization name, coordinates or database identifiers beyond the location GUID are embedded.

### 4.2 Dynamic QR

- `QrCodeMode = Dynamic (2)`.
- Rotation window: `expiresAtUtc = floor(now / intervalMinutes × 60) × interval + interval`, i.e. windows are aligned to absolute UTC boundaries. Recommended default 5 minutes, allowed range 1–60 minutes.
- The server issues the token for the current window; every employee scanning the **same displayed token** in that window is validated successfully (no one-time use).
- `GET .../qrcode/current` performs **no database write**; the token is computed from the stored identifier, mode and interval.
- The display client should fetch a new token at `refreshAfterUtc` (equal to `expiresAtUtc`); see the Angular guide.

### 4.3 Token format and validation

```
token     = base64url(payload) "." base64url(HMAC_SHA256(key, payload))
payload   = "v1|{workLocationId:N}|{qrCodeIdentifier}|{s|d}|{expiresAtUnixSeconds}"
key       = SHA256("v1:AriaHR.QrCode:" + (QrCode:SigningKey ?? Jwt:SecretKey))
```

Server validation order for a signed token:
1. base64url and signature verification (`QR_CODE_INVALID` on failure)
2. work location exists and is not soft-deleted (`WORK_LOCATION_NOT_FOUND`)
3. work location active (`WORK_LOCATION_INACTIVE`)
4. QR enabled (`QR_CODE_DISABLED`)
5. identifier equals the stored identifier (`QR_CODE_INVALID`, i.e. revoked)
6. token mode equals configured mode (`QR_CODE_INVALID`)
7. dynamic: `expiresAtUtc > serverUtcNow` (`QR_CODE_EXPIRED`); static: must have no expiry (`QR_CODE_INVALID`)
8. token organization equals the authenticated employee's organization (`EMPLOYEE_NOT_AUTHORIZED`)

Legacy tokens (64 hex characters created by the previous `POST .../qr` endpoint) are still resolved through the `QRCodes` table, so no previously issued code stops working.

Server UTC time is the only clock used for expiry. The client never validates or generates security tokens; it only displays the string returned by the API as a QR image.

---

## 5. Geofencing Documentation

- **Coordinate format:** WGS-84 decimal degrees, stored as `decimal(9,6)` (≈ 0.11 m). API accepts decimal numbers. Latitude `[-90, 90]`, longitude `[-180, 180]`; violations return `LOCATION_COORDINATES_INVALID`.
- **Radius:** `AllowedRadiusInMeters` per work location, `decimal(8,1)`, configurable limits `QrCode:MinAllowedRadiusMeters` (1) and `QrCode:MaxAllowedRadiusMeters` (1000).
- **Distance:** Haversine (`R = 6 371 000 m`) between the employee's reported position and the stored work-location position. The result is rounded to 0.1 m in responses and stored on the attendance record.
- **GPS accuracy:** `GpsAccuracyInMeters` is mandatory and must be `> 0` and `<= Attendance:Gps:MaxAccuracyMeters` (default 50 m). Otherwise `LOCATION_ACCURACY_INSUFFICIENT` is returned so the employee can retry.
- **Freshness:** `LocationTimestampUtc` is mandatory. It is rejected when it is more than `Attendance:Gps:MaxFutureSkewSeconds` (default 30 s) in the future (`LOCATION_TIMESTAMP_INVALID`) or older than `Attendance:Gps:MaxLocationAgeSeconds` (default 120 s) (`LOCATION_DATA_EXPIRED`).
- **Boundary:** the decision is `distance <= allowedRadius` (inclusive). Example: radius 5 m, distance 4.9 m → accepted; distance 15 m → `OUTSIDE_ALLOWED_RADIUS`.
- **Who decides:** only the backend. The request cannot carry work-location coordinates, a radius, an employee id or `isInsideRadius`.
- **Known limitations (important):**
  - Browser/device geolocation can be spoofed (developer tools, mock-location apps, rooted devices). GPS validation raises the effort but **does not prove** physical presence.
  - Web Geolocation accuracy is frequently 20–100 m indoors; a 5 m radius may be unusable inside buildings. Center managers should set a realistic radius and accuracy limit.
  - No device attestation, IP cross-check, BLE/NFC or Wi-Fi fingerprinting is implemented. These would be the next integrity layers; they require additional native capabilities and are out of scope.
  - `LOCATION_PERMISSION_DENIED` can only be produced by the browser; the API cannot detect denied permission. The Angular app must map its own geolocation error to that code.

---

## 6. Complete API Reference

All endpoints require `Authorization: Bearer <JWT>` (except auth endpoints). Error bodies are `ProblemDetails` (`application/problem+json`) unless stated otherwise; `ProblemDetails.Extensions["errorCode"]` is present for QR configuration failures.

Roles: `CenterManager` and `SystemAdmin` for work-location endpoints; any authenticated employee for `attendance/scan`.

Timestamps are ISO-8601 UTC (`yyyy-MM-ddTHH:mm:ss.FFFFFFFZ`). Field names are camelCase.

### 6.1 List work locations

`GET /api/WorkLocations/work-locations?organizationId={guid}`

- Purpose: list all work locations of a center.
- Auth: JWT. Roles: CenterManager, SystemAdmin.
- `organizationId` is required for SystemAdmin; a CenterManager may omit it (their own center is used) and receives `403` if it targets another center.
- Success `200`:

```json
[
  {
    "id": "2b0d8e0f-3c5a-4c1b-9c1e-3a0f7f7a1a11",
    "organizationId": "9b1f0a52-6f2b-4c40-8a53-2c11c3a4b001",
    "name": "Main Site",
    "latitude": 35.689200,
    "longitude": 51.389000,
    "allowedRadiusInMeters": 20.0,
    "isActive": true,
    "qrCodeMode": "Dynamic",
    "qrCodeEnabled": true,
    "qrRotationIntervalMinutes": 5,
    "qrCodeLastGeneratedAtUtc": "2026-11-15T09:12:33.4218750Z",
    "createdAtUtc": "2026-11-10T08:00:00.0000000Z",
    "createdByUserId": "0f5c2a6e-7f3a-4c2e-9d4b-2a6f7c8e9d10",
    "updatedAtUtc": null,
    "updatedByUserId": null
  }
]
```

- Errors: `400` missing organizationId, `401`, `403`.

### 6.2 Get one work location

`GET /api/WorkLocations/work-locations/{workLocationId}`

- Success `200`: a single object as above.
- Errors: `400` invalid id, `401`, `403` other center, `404` not found.

### 6.3 Create work location

`POST /api/WorkLocations/work-locations`

```json
{
  "organizationId": "9b1f0a52-6f2b-4c40-8a53-2c11c3a4b001",
  "name": "Main Site",
  "latitude": 35.6892,
  "longitude": 51.389,
  "allowedRadiusInMeters": 20,
  "isActive": true,
  "qrCodeMode": 2,
  "qrCodeEnabled": true,
  "qrRotationIntervalMinutes": 5
}
```

- `organizationId` is optional for CenterManager (taken from JWT, mismatch → `403`) and optional for SystemAdmin if the admin token has an organization claim.
- `qrCodeMode`: **numeric enum** `1 = Static`, `2 = Dynamic`. Default `1`.
- `qrRotationIntervalMinutes`: optional; defaults to `QrCode:DefaultRotationIntervalMinutes` (5), validated 1–60. Ignored when the mode is Static (still stored).
- `name` is required (max 200). Coordinates and radius are validated as described in §5. Multiple locations per center are allowed.
- Success `201`:

```json
{
  "id": "2b0d8e0f-3c5a-4c1b-9c1e-3a0f7f7a1a11",
  "organizationId": "9b1f0a52-6f2b-4c40-8a53-2c11c3a4b001",
  "name": "Main Site",
  "latitude": 35.6892,
  "longitude": 51.389,
  "allowedRadiusInMeters": 20.0,
  "isActive": true,
  "qrCodeMode": "Dynamic",
  "qrCodeEnabled": true,
  "qrRotationIntervalMinutes": 5,
  "qrCodeLastGeneratedAtUtc": "2026-11-15T09:12:33.4218750Z",
  "createdAtUtc": "2026-11-15T09:12:33.4218750Z",
  "createdByUserId": "0f5c2a6e-7f3a-4c2e-9d4b-2a6f7c8e9d10",
  "updatedAtUtc": null,
  "updatedByUserId": null
}
```

- Errors: `400` validation, `401`, `403`, `404` organization not found is reported as `400`/`404` depending on path (create returns `400` with "سازمان مورد نظر یافت نشد").

### 6.4 Update work location

`PUT /api/WorkLocations/work-locations/{workLocationId}`

```json
{
  "name": "Main Site - North Wing",
  "latitude": 35.68925,
  "longitude": 51.38904,
  "allowedRadiusInMeters": 5,
  "isActive": true
}
```

- Updates only geographic/business configuration; **never** touches QR fields. Deactivating a location also disables its QR.
- Success `200`: updated object.
- Errors: `400`, `401`, `403` (other center), `404`.

### 6.5 QR settings

`GET /api/WorkLocations/work-locations/{workLocationId}/qrcode/settings`

```json
{
  "workLocationId": "2b0d8e0f-3c5a-4c1b-9c1e-3a0f7f7a1a11",
  "qrCodeMode": "Dynamic",
  "qrCodeEnabled": true,
  "rotationIntervalMinutes": 5,
  "minRotationIntervalMinutes": 1,
  "maxRotationIntervalMinutes": 60,
  "defaultRotationIntervalMinutes": 5,
  "lastGeneratedAtUtc": "2026-11-15T09:12:33.4218750Z",
  "isDynamic": true
}
```

`PUT /api/WorkLocations/work-locations/{workLocationId}/qrcode/settings`

```json
{ "qrCodeMode": 2, "qrRotationIntervalMinutes": 10 }
```

- `qrCodeMode`: numeric enum (`1` Static / `2` Dynamic). Interval validated 1–60.
- Success `200`: settings object (as above).
- Errors: `400` invalid mode/interval, `401`, `403`, `404` (`errorCode: WORK_LOCATION_NOT_FOUND`), `400` `WORK_LOCATION_INACTIVE`.

### 6.6 Current QR token

`GET /api/WorkLocations/work-locations/{workLocationId}/qrcode/current`

- Returns the token currently valid for this location. No database write.
- Success `200`:

```json
{
  "workLocationId": "2b0d8e0f-3c5a-4c1b-9c1e-3a0f7f7a1a11",
  "qrCodeMode": "Dynamic",
  "token": "djF8MmIwZDhlMGYzYzVhNGMxYjljMWUzYTBmN2Y3YTFhMTF8MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWZ8ZHwxNzYzMjg3NjAw.kJ3n0a…",
  "expiresAtUtc": "2026-11-15T09:15:00.0000000Z",
  "refreshAfterUtc": "2026-11-15T09:15:00.0000000Z",
  "generatedAtUtc": "2026-11-15T09:13:41.1200000Z",
  "qrCodeEnabled": true,
  "rotationIntervalMinutes": 5
}
```

- Static mode: `expiresAtUtc = null`, `refreshAfterUtc = null`.
- Errors: `400` `QR_CODE_DISABLED`, `400` `QR_CODE_NOT_GENERATED`, `400` `WORK_LOCATION_INACTIVE`, `401`, `403`, `404`.

### 6.7 Generate QR

`POST /api/WorkLocations/work-locations/{workLocationId}/qrcode/generate`

- Marks the QR as generated (`qrCodeLastGeneratedAtUtc`) and returns the current token. Requires an enabled QR.
- Success `200`: same shape as 6.6.
- Errors: `400` (`QR_CODE_DISABLED`, `WORK_LOCATION_INACTIVE`), `401`, `403`, `404`.
- Legacy alias: `POST /api/WorkLocations/work-locations/{workLocationId}/qr` (same behaviour).

### 6.8 Enable / disable QR

`POST /api/WorkLocations/work-locations/{workLocationId}/qrcode/enable`
`POST /api/WorkLocations/work-locations/{workLocationId}/qrcode/disable`

- Enable requires an active work location; a fresh identifier is generated if missing.
- Success `200`: settings object (§6.5).
- Errors: `400` (`WORK_LOCATION_INACTIVE`), `401`, `403`, `404`.

### 6.9 Revoke / regenerate QR

`POST /api/WorkLocations/work-locations/{workLocationId}/qrcode/revoke`

- Rotates `QrCodeIdentifier` immediately (old tokens become invalid) and deactivates legacy `QRCodes` rows for the location.
- Success `200`: new token object (§6.6).
- Errors: `400`, `401`, `403`, `404`.

### 6.10 Attendance scan (primary attendance endpoint)

`POST /api/attendance/scan`

Request:

```json
{
  "qrCodeToken": "djF8MmIwZDhlMGYzYzVhNGMxYjljMWUzYTBmN2Y3YTFhMTF8…",
  "latitude": 35.689215,
  "longitude": 51.389012,
  "gpsAccuracyInMeters": 3.5,
  "locationTimestampUtc": "2026-11-15T09:13:55.000Z",
  "attendanceAction": "CheckIn",
  "idempotencyKey": "6a0e2c92-1b6c-4b2d-9ac9-2a3b1f9f5f10",
  "deviceInfo": "Chrome/Web/Android",
  "notes": null
}
```

| Field | Required | Notes |
|---|---|---|
| `qrCodeToken` | yes | string returned by the QR API |
| `latitude`, `longitude` | yes | WGS-84 decimal |
| `gpsAccuracyInMeters` | yes | metres, > 0 |
| `locationTimestampUtc` | yes | when the reading was taken |
| `attendanceAction` | no | `"CheckIn"` / `"CheckOut"` (case-insensitive); omitted → server decides from the active record |
| `idempotencyKey` | strongly recommended | ≤ 100 chars, unique per employee/action |
| `deviceInfo` | no | ≤ 500 chars |
| `notes` | no | ≤ 1000 chars |

Employee identity is always resolved from the JWT (`ClaimTypes.NameIdentifier` / `sub`); no employee id is accepted in the body.

Success `200`:

```json
{
  "success": true,
  "code": "ATTENDANCE_REGISTERED",
  "message": "حضور شما با موفقیت ثبت شد",
  "data": {
    "attendanceId": "e2f7f0f3-4f6b-4f53-b9f4-9d3c9b7c9a21",
    "workLocationId": "2b0d8e0f-3c5a-4c1b-9c1e-3a0f7f7a1a11",
    "workLocationName": "Main Site",
    "attendanceAction": "CheckIn",
    "distanceInMeters": 3.2,
    "allowedRadiusInMeters": 5.0,
    "gpsAccuracyInMeters": 3.5,
    "registeredAtUtc": "2026-11-15T09:13:56.1200000Z",
    "attendanceStatus": "Present",
    "isLate": false,
    "isEarlyDeparture": false,
    "shiftName": "Morning",
    "isReplay": false
  }
}
```

Check-out returns `"attendanceAction": "CheckOut"`, `"attendanceStatus": "Completed"` and message `"خروج شما با موفقیت ثبت شد"`.

Rejected response (HTTP status varies, see table):

```json
{
  "success": false,
  "code": "OUTSIDE_ALLOWED_RADIUS",
  "message": "شما خارج از محدوده مجاز محل کار هستید.",
  "data": {
    "attendanceId": null,
    "workLocationId": "2b0d8e0f-3c5a-4c1b-9c1e-3a0f7f7a1a11",
    "workLocationName": "Main Site",
    "attendanceAction": null,
    "distanceInMeters": 15.4,
    "allowedRadiusInMeters": 5.0,
    "gpsAccuracyInMeters": 3.0,
    "registeredAtUtc": null,
    "attendanceStatus": null,
    "isLate": null,
    "isEarlyDeparture": null,
    "shiftName": null,
    "isReplay": false
  }
}
```

Error codes:

| `code` | HTTP | Meaning |
|---|---|---|
| `INVALID_REQUEST` | 400 | null body, oversized strings |
| `EMPLOYEE_NOT_FOUND` | 403 | JWT user has no active employee record |
| `EMPLOYEE_NOT_AUTHORIZED` | 403 | employee belongs to another center |
| `QR_CODE_INVALID` | 400 | unknown/tampered/revoked token, mode mismatch, legacy row missing |
| `QR_CODE_EXPIRED` | 400 | dynamic token window ended |
| `QR_CODE_DISABLED` | 400 | QR disabled by manager |
| `WORK_LOCATION_NOT_FOUND` | 404 | token points to a missing/deleted location |
| `WORK_LOCATION_INACTIVE` | 400 | location deactivated |
| `LOCATION_UNAVAILABLE` | 400 | coordinates missing |
| `LOCATION_COORDINATES_INVALID` | 400 | out-of-range coordinates |
| `LOCATION_ACCURACY_INSUFFICIENT` | 400 | accuracy missing, ≤ 0, or above the limit (retry allowed) |
| `LOCATION_TIMESTAMP_INVALID` | 400 | timestamp missing or in the future |
| `LOCATION_DATA_EXPIRED` | 400 | timestamp older than the freshness limit |
| `OUTSIDE_ALLOWED_RADIUS` | 400 | distance > allowed radius |
| `SHIFT_VALIDATION_FAILED` | 400 | shift inactive or outside the permitted window |
| `DUPLICATE_ATTENDANCE` | 409 | active check-in already exists |
| `INVALID_ATTENDANCE_ACTION` | 400 | unknown action, or check-out without active check-in |
| `ATTENDANCE_BLOCKED_BY_APPROVED_LEAVE` | 400 | employee is on approved leave (existing rule) |

Idempotency: when `idempotencyKey` repeats for the same employee (check-in or check-out), the stored result is returned with `success = true`, `code = ATTENDANCE_REGISTERED`, `data.isReplay = true`, message `"این درخواست قبلاً ثبت شده است و مجدداً ثبت نشد."` and **no new record**. Filtered unique indexes provide a database-level safety net.

The request is fully validated before any write; a rejected scan creates no attendance record.

### 6.11 Existing attendance endpoints (unchanged contract)

- `POST /api/attendance/check-in` (`CheckInRequest`) — `201`, returns `AttendanceRecordDto`. Legacy; still supported, QR path now also evaluates the geofence when coordinates are present.
- `POST /api/attendance/check-out` (`CheckOutRequest`) — `200`.
- `GET /api/attendance/current` — `200` active record or `204`.
- `GET /api/attendance/my?startDate=&endDate=` — `200` list.
- `GET /api/attendance?organizationId=&startDate=&endDate=&employeeId=` — `200` list, roles CenterManager/SystemAdmin.
- `PATCH /api/attendance/{id}/status` — `200`, roles CenterManager/SystemAdmin.
- `AttendanceRecordDto` now also exposes `checkInDistanceInMeters`, `checkOutDistanceInMeters`, `checkInLocationTimestampUtc`, `checkOutLocationTimestampUtc`.

---

## 7. Angular Integration Guide

> All snippets reflect the implemented contracts. The Angular project is intentionally **not** modified in this phase.

### 7.1 Common setup

```ts
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errorCode?: string;
}

export type QrCodeMode = 1 | 2; // 1 = Static, 2 = Dynamic (request enum)
export type QrCodeModeName = 'Static' | 'Dynamic'; // response string
```

Send `Authorization: Bearer <accessToken>` on every request. A `403` means "authenticated but not allowed" (other center / wrong role); a `401` means the token is missing or invalid — redirect to login.

### 7.2 Work location DTOs

```ts
export interface WorkLocationDto {
  id: string;
  organizationId: string;
  name: string;
  latitude: number;
  longitude: number;
  allowedRadiusInMeters: number;
  isActive: boolean;
  qrCodeMode: QrCodeModeName;
  qrCodeEnabled: boolean;
  qrRotationIntervalMinutes: number;
  qrCodeLastGeneratedAtUtc: string | null;
  createdAtUtc: string;
  createdByUserId: string | null;
  updatedAtUtc: string | null;
  updatedByUserId: string | null;
}

export interface CreateWorkLocationRequest {
  organizationId?: string;
  name: string;
  latitude: number;
  longitude: number;
  allowedRadiusInMeters: number;
  isActive: boolean;
  qrCodeMode: QrCodeMode;
  qrCodeEnabled: boolean;
  qrRotationIntervalMinutes?: number;
}

export interface UpdateWorkLocationRequest {
  name: string;
  latitude: number;
  longitude: number;
  allowedRadiusInMeters: number;
  isActive: boolean;
}
```

Create:

```ts
this.http.post<WorkLocationDto>(`${api}/api/WorkLocations/work-locations`, body);
```

Edit (map pin + radius form):

```ts
this.http.put<WorkLocationDto>(`${api}/api/WorkLocations/work-locations/${id}`, {
  name, latitude, longitude, allowedRadiusInMeters, isActive
});
```

Coordinates must come from the map widget as decimal degrees; send `Number(value.toFixed(6))`. Radius is in metres; the UI should enforce 1–1000 (or read the limits from the QR settings endpoint's range fields for dynamic, and treat radius limits as configuration).

### 7.3 QR settings and display

```ts
export interface QrCodeSettingsDto {
  workLocationId: string;
  qrCodeMode: QrCodeModeName;
  qrCodeEnabled: boolean;
  rotationIntervalMinutes: number;
  minRotationIntervalMinutes: number;
  maxRotationIntervalMinutes: number;
  defaultRotationIntervalMinutes: number;
  lastGeneratedAtUtc: string | null;
  isDynamic: boolean;
}

export interface QrCodeResponse {
  workLocationId: string;
  qrCodeMode: QrCodeModeName;
  token: string;
  expiresAtUtc: string | null;
  refreshAfterUtc: string | null;
  generatedAtUtc: string;
  qrCodeEnabled: boolean;
  rotationIntervalMinutes: number;
}

// Read settings
this.http.get<QrCodeSettingsDto>(`${api}/api/WorkLocations/work-locations/${id}/qrcode/settings`);

// Switch to dynamic with a 10-minute rotation
this.http.put<QrCodeSettingsDto>(
  `${api}/api/WorkLocations/work-locations/${id}/qrcode/settings`,
  { qrCodeMode: 2, qrRotationIntervalMinutes: 10 });

// Enable / disable / revoke / generate
this.http.post<QrCodeSettingsDto>(`${api}/api/WorkLocations/work-locations/${id}/qrcode/enable`, {});
this.http.post<QrCodeSettingsDto>(`${api}/api/WorkLocations/work-locations/${id}/qrcode/disable`, {});
this.http.post<QrCodeResponse>(`${api}/api/WorkLocations/work-locations/${id}/qrcode/revoke`, {});
this.http.post<QrCodeResponse>(`${api}/api/WorkLocations/work-locations/${id}/qrcode/generate`, {});
```

Displaying the code on a fixed monitor/tablet:

```ts
loadCurrent(): Observable<QrCodeResponse> {
  return this.http.get<QrCodeResponse>(
    `${api}/api/WorkLocations/work-locations/${id}/qrcode/current`);
}

// Poll: refresh at the server-provided boundary, or every 15 s as a fallback.
startDisplay() {
  this.loadCurrent().subscribe(res => {
    this.token = res.token;
    this.mode = res.qrCodeMode;
    const refreshIn = res.refreshAfterUtc
      ? Math.max(1000, new Date(res.refreshAfterUtc).getTime() - Date.now() + 500)
      : 60_000;                    // static tokens do not change
    this.timer = setTimeout(() => this.startDisplay(), refreshIn);
  });
}
```

Render `token` with any QR library (e.g. `angularx-qrcode`). Do **not** wrap the token in JSON or add the location id; the raw string is the payload. Static tokens never rotate; re-fetch only after the manager revokes/regenerates.

### 7.4 Employee scan flow

```ts
export interface ScanAttendanceRequest {
  qrCodeToken: string;
  latitude: number;
  longitude: number;
  gpsAccuracyInMeters: number;
  locationTimestampUtc: string;
  attendanceAction?: 'CheckIn' | 'CheckOut';
  idempotencyKey?: string;
  deviceInfo?: string;
  notes?: string;
}

export interface AttendanceScanData {
  attendanceId: string | null;
  workLocationId: string | null;
  workLocationName: string | null;
  attendanceAction: string | null;
  distanceInMeters: number | null;
  allowedRadiusInMeters: number | null;
  gpsAccuracyInMeters: number | null;
  registeredAtUtc: string | null;
  attendanceStatus: string | null;
  isLate: boolean | null;
  isEarlyDeparture: boolean | null;
  shiftName: string | null;
  isReplay: boolean;
}

export interface AttendanceScanResponse {
  success: boolean;
  code: string;
  message: string;
  data: AttendanceScanData | null;
}
```

Geolocation helper:

```ts
getPosition(): Promise<GeolocationPosition> {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) {
      reject({ code: 'LOCATION_UNAVAILABLE' });
      return;
    }
    navigator.geolocation.getCurrentPosition(resolve, err => {
      const code = err.code === err.PERMISSION_DENIED ? 'LOCATION_PERMISSION_DENIED'
        : err.code === err.POSITION_UNAVAILABLE ? 'LOCATION_UNAVAILABLE'
        : 'LOCATION_TIMEOUT';
      reject({ code });
    }, { enableHighAccuracy: true, timeout: 15_000, maximumAge: 0 });
  });
}
```

Submit (read the QR string with the device camera, then immediately read GPS):

```ts
const pos = await this.getPosition();
const body: ScanAttendanceRequest = {
  qrCodeToken: scannedText,
  latitude: pos.coords.latitude,
  longitude: pos.coords.longitude,
  gpsAccuracyInMeters: pos.coords.accuracy ?? -1,
  locationTimestampUtc: new Date(pos.timestamp).toISOString(),
  idempotencyKey: crypto.randomUUID(),
  deviceInfo: navigator.userAgent
};

this.http.post<AttendanceScanResponse>(`${api}/api/attendance/scan`, body)
  .subscribe({
    next: res => this.handleScanResult(res),
    error: err => this.handleHttpError(err)
  });
```

- Keep the same `idempotencyKey` when retrying the **same** action after a network timeout; generate a new key for a new scan.
- Omit `attendanceAction` to let the server decide (auto check-out when an active check-in exists). Pass `'CheckOut'` explicitly if the UI has separate buttons.

### 7.5 Handling results

| `code` | UI behaviour |
|---|---|
| `ATTENDANCE_REGISTERED` (`isReplay=false`) | Success screen; show time, location, distance. |
| `ATTENDANCE_REGISTERED` + `isReplay=true` | Treat as success ("already registered"). |
| `QR_CODE_INVALID` | "کد نامعتبر است" — rescan; if it persists, the code may be revoked. |
| `QR_CODE_EXPIRED` | Ask the employee to scan the newly displayed dynamic code (show a live refresh hint). |
| `QR_CODE_DISABLED` | Inform the employee that QR attendance is disabled; contact the manager. |
| `LOCATION_PERMISSION_DENIED` (client-side) | Explain how to enable location permission in the browser/OS; offer retry. |
| `LOCATION_UNAVAILABLE` / `LOCATION_TIMEOUT` (client-side) | Retry with a clear "move to an open area" message. |
| `LOCATION_ACCURACY_INSUFFICIENT` | Show "دقت موقعیت مکانی کافی نیست"; retry button; optionally show `data.gpsAccuracyInMeters`. |
| `LOCATION_DATA_EXPIRED` / `LOCATION_TIMESTAMP_INVALID` | Read GPS again and resubmit with the same idempotency key only if the action did not change. |
| `OUTSIDE_ALLOWED_RADIUS` | Show distance vs allowed radius (`data.distanceInMeters`, `data.allowedRadiusInMeters`). No automatic retry. |
| `DUPLICATE_ATTENDANCE` | Show that a check-in already exists; offer check-out. |
| `INVALID_ATTENDANCE_ACTION` | Refresh current status (`GET /api/attendance/current`) and retry the correct action. |
| `SHIFT_VALIDATION_FAILED` | Show the server message (shift name and window). |
| `EMPLOYEE_NOT_AUTHORIZED` | Explain that the employee is not assigned to this center; do not retry. |
| `WORK_LOCATION_NOT_FOUND` / `WORK_LOCATION_INACTIVE` | Contact the manager; do not retry. |
| `ATTENDANCE_BLOCKED_BY_APPROVED_LEAVE` | Show that attendance is blocked while on approved leave. |
| HTTP 401 | Re-authenticate. |
| HTTP 403 | Show "not allowed"; no retry loop. |
| HTTP 5xx | Generic error with a retry using the same idempotency key. |

Additional rules for the UI:

- Never compute distance, radius, "isInside" or QR validity locally; display server values only.
- Do not cache or reuse a dynamic token beyond `expiresAtUtc`.
- Use `attendance/current` to decide whether to show "Check-in" or "Check-out" before scanning.

---

## 8. Testing Report

### Executed

```bash
dotnet build AriaHR.sln
dotnet test AriaHR.sln
dotnet ef migrations script     # both contexts
dotnet ef migrations has-pending-model-changes   # both contexts
```

### Results

| Project | Result |
|---|---|
| `AriaHR.Modules.Organization.Tests` | 158 passed, 0 failed |
| `AriaHR.Modules.Attendance.Tests` | 34 passed, 0 failed |
| `AriaHR.Modules.Identity.Tests` | 48 passed, 0 failed |
| `AriaHR.Modules.Requests.Tests` | 88 passed, 0 failed |
| `AriaHR.Modules.Scheduling.Tests` | 17 passed, 0 failed |
| `AriaHR.Modules.Notification.Tests` | 7 passed, 0 failed |
| **Total** | **352 passed, 0 failed, 0 skipped** |

Both contexts report *"No changes have been made to the model since the last migration."*

### Coverage of the new behaviour

**Work location** (`WorkLocationCreateTests`)
- create with valid coordinates/radius/name and verify persistence
- two independent locations per center with different radii
- invalid latitude, invalid longitude, invalid radius (0, negative, above maximum), missing name
- non-existent organization
- update coordinates/radius/name without touching the QR identifier
- center manager updating another center's location → `UnauthorizedAccessException` / `403`
- controller creation with organization taken from JWT; cross-center create/read → `Forbid`
- controller role attribute check

**QR** (`DynamicQrCodeTests`)
- static token is stable, has no expiry and validates
- dynamic token stable within a window, expiry aligned to the interval, and rotation at the window boundary invalidates the previous token (deterministic clock)
- expired dynamic token rejected (`QR_CODE_EXPIRED`)
- tampered signature rejected (`QR_CODE_INVALID`)
- token from another work location rejected (`EMPLOYEE_NOT_AUTHORIZED` / `QR_CODE_INVALID`)
- disabled QR rejected (`QR_CODE_DISABLED`) and `current` refuses to issue
- revoke invalidates the previous static token and issues a working new one
- multiple validations of the same dynamic token (multiple employees)
- settings update persists mode/interval; invalid interval rejected
- cross-organization manager rejected
- controller generate/route metadata

**GPS + attendance** (`ScanAttendanceTests`)
- valid check-in within radius, distance/location/geofence persisted
- outside radius rejected with distance/radius data and **no record written**
- boundary (inside) accepted
- insufficient accuracy rejected with retry context
- invalid coordinate ranges (4 cases)
- outdated timestamp, future timestamp, missing timestamp, missing GPS
- duplicate explicit check-in (`409`)
- check-out without active check-in
- valid check-out completes the record and stores checkout distance
- invalid action string
- employee from another organization
- expired QR
- idempotent replay returns the same attendance id and keeps a single record
- inactive shift and too-early shift rejected; no assignment does not block existing behaviour
- employee without employee record rejected with `403`

### Known limitations / not tested

- **No live SQL Server** in this environment: migrations were generated and scripted but **not applied** to a database, and no HTTP-level end-to-end test was executed against a running API (the API requires a SQL Server connection and performs identity seeding at startup).
- Scalar/OpenAPI was therefore **not opened at runtime**. OpenAPI metadata is complete through `[Authorize]`, `[ProducesResponseType]` and the existing document/operation transformers; the routes and response types compile into the controllers.
- No real device GPS or QR camera test; distance math is covered with computing offsets in unit tests.
- No load/performance or rate-limiting tests.
- No concurrency test for two simultaneous scans of the same employee; the filtered unique idempotency indexes and duplicate checks cover the normal path, but a simultaneous double-check-in without idempotency keys may still race (documented as a remaining hardening item).

---

## 9. Final Build Status

| Item | Status |
|---|---|
| Solution build (`dotnet build AriaHR.sln`) | **Passed**, 0 errors (only pre-existing NuGet vulnerability warnings for `System.Security.Cryptography.Pkcs` 9.0.0 / SQLitePCLRaw) |
| Test build/run | **Passed** — 352/352 |
| EF migrations created | `20261115103740_ExtendWorkLocationQrAndGeofencing` (Organization), `20261115103934_AddAttendanceScanGeofenceAndIdempotency` (Attendance) |
| Migration scripts generated | **Yes**, inspected: column rename + precision alteration + backfill; no destructive drops |
| Migrations applied to a database | **No** — no SQL Server available in this environment |
| Swagger/Scalar runtime check | **Not performed** — API startup needs SQL Server; endpoint metadata is configured for OpenAPI |
| Authorization rules | Verified through controller/use-case tests (ownership, cross-center 403, role attributes) |
| Angular changes | **None** (out of scope) |

### Deployment checklist

1. Set a dedicated `QrCode:SigningKey` (≥ 32 random bytes) in production configuration; do not rely on the JWT secret fallback. Never commit it.
2. Apply both migrations (`dotnet ef database update` per context, or the generated SQL scripts).
3. Optionally tune `QrCode:MinAllowedRadiusMeters` / `MaxAllowedRadiusMeters`, `Attendance:Gps:*`, `Attendance:Policy:*`.
4. Verify `/scalar` and `/openapi/v1.json` in Development on a machine with SQL Server.
5. Proceed with the Angular phase using §7.

### Remaining tasks / recommended follow-ups

- Angular implementation (separate phase) per §7.
- Apply migrations to the target database and run an end-to-end smoke test of `scan` with a real JWT and real coordinates.
- Consider per-user rate limiting on `POST /api/attendance/scan` and `POST /api/WorkLocations/work-locations/{id}/qrcode/generate`.
- Optional integrity layers: device attestation, Wi-Fi/BLE checks, IP allow-listing, anomaly reporting for impossible travel.
- Optional read model for QR audit history (current design intentionally avoids per-rotation rows).
