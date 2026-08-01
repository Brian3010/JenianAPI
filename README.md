# Jenian API

ASP.NET Core (.NET 9) backend API for **Jenian**, a practical workplace productivity application that supports shift management, estimated pay calculations, and structured end-of-day (night) report workflows for retail/pharmacy staff.

- **Live frontend:** https://jenian-client.vercel.app
- **Frontend repository:** not included in this repository (separate Next.js project)
- **API / Swagger URL:** not publicly documented in this repository — Swagger UI is enabled only in the Development environment (see [Swagger](#swagger))
- This repository contains **only the backend API**. UI, forms, and BFF (Backend-for-Frontend) logic live in the separate Next.js frontend repository.

## Overview

Jenian helps shift workers track their rosters, estimate what they should be paid under an award-based pay structure, and submit structured end-of-day operational reports (stock levels, night tasks, aisle facing, cleaning, and general checks) — some of which can be filled in from a photographed roster via OCR.

This API is responsible for:

- User authentication (including a temporary, isolated demo mode)
- Persisting shifts and pay-cycle settings
- Performing trusted, server-side pay calculations
- Storing and retrieving end-of-day reports
- Linking and processing Telegram bot interactions
- Extracting roster data from images (Azure Vision OCR + OpenAI)

It is consumed exclusively by a separate Next.js frontend, which is expected to call this API through its own Backend-for-Frontend (BFF) route handlers rather than directly from the browser.

## Main Features

Confirmed in the codebase:

- Email/username + password registration (gated by an invite token) and login
- JWT access tokens with a configurable/short lifetime
- Cookie-based session (`accessToken`, `refreshToken`, `deviceId` — all `HttpOnly`)
- Per-device refresh tokens, allowing multiple concurrent sessions/devices per user
- Logout and refresh-token revocation
- Isolated, self-expiring demo login (`demo-login` / `demo-logout`) backed by real, temporary user accounts
- Pay-cycle settings (weekly / fortnightly / monthly, anchored to a start date)
- Shift creation, update, and deletion via a single bulk "save" endpoint
- Pharmacy Award–based pay-rate calculation (ordinary/evening/Saturday/Sunday/public-holiday multipliers)
- Daily pay summaries aggregated per user and work date
- Structured end-of-day report submission (stock update, night tasks, aisle facing, cleaning, general checks)
- Image upload with Azure Blob Storage persistence and background job processing
- Telegram account linking (`/start <token>`) and a `/roster` command for OCR-based roster import
- Telegram webhook endpoint for receiving bot updates
- Azure AI Vision OCR for reading text from uploaded/Telegram images
- OpenAI-based text extraction/normalisation on top of OCR output
- Global rate limiting (sliding window) plus a stricter fixed-window policy on auth endpoints
- A lightweight, unauthenticated health-check endpoint
- Centralised exception handling using RFC 7807 `ProblemDetails` responses

## Tech Stack

Determined from the `.csproj` files and `global.json`:

- **.NET SDK 9.0.305** (`global.json`), all projects target **`net9.0`**
- **ASP.NET Core 9** (`Microsoft.NET.Sdk.Web`) for the API and Infrastructure projects
- **C#** with nullable reference types and implicit usings enabled
- **Entity Framework Core 9.0.4** (SQL Server provider) — two `DbContext`s (see [Database](#database))
- **ASP.NET Core Identity** (`Microsoft.AspNetCore.Identity.EntityFrameworkCore` 9.0.4) for user management
- **JWT Bearer authentication** (`Microsoft.AspNetCore.Authentication.JwtBearer` 9.0.4)
- **SQL Server** (local/dev via `Trusted_Connection`; production connects to an Azure SQL–compatible connection string)
- **xUnit 2.9.3** with `Microsoft.NET.Test.Sdk` 18.5.1 for testing
- **Docker** multi-stage build (SDK 9.0 → ASP.NET runtime 9.0, Debian-based)
- **Azure Container Registry** and **Azure Container Apps** as the deployment target (per the GitHub Actions workflow)
- **GitHub Actions** for CI/CD (`.github/workflows/azure-deploy.yml`)
- **OpenAI** SDK (`OpenAI` 2.4.0)
- **Azure AI Vision** (`Azure.AI.Vision.ImageAnalysis` 1.0.0) for OCR
- Telegram Bot API (consumed via a plain `HttpClient`, no third-party Telegram SDK)
- **Serilog** for console/file logging
- **OpenCvSharp4** / **SixLabors.ImageSharp** for image pre-processing prior to OCR
- **Azure.Storage.Blobs** + **Azure.Identity** for file storage

## Architecture

The solution follows a layered/Clean Architecture split into four projects plus one test project. Project references only point inward (`API` → `Application` + `Infrastructure`; `Infrastructure` → `Application` + `Domain`; `Application` → `Domain`), so `Domain` has no dependency on any other project and `Application` has no dependency on ASP.NET Core, EF Core, or any external SDK.

### Domain (`Jenian.Domain`)
Framework-independent entities and enums with no external dependencies:
- `UserShift`, `PayCycleSetting`, `UserDailyPaySummary`, `EodReport`, `DeliveryExtractionJob`
- Enums: `ShiftEntryType`, `EmploymentType`, `ShiftSource`, `PayCycleType`

### Application (`Jenian.Application`)
Use cases, orchestration, and abstractions — no direct dependency on EF Core, ASP.NET Core, or external SDKs:
- Feature folders: `Auth`, `Shifts`, `PaySummaries`, `Payroll`, `Telegram`
- Commands and DTOs per feature (e.g. `SaveShiftsCommand`, `LoginCommand`, `ShiftDto`)
- Service interfaces (`IShiftService`, `IAuthService`, `IPayCalculator`, `IAwardRateService`)
- Shift validation (`IShiftValidator`)
- Application-specific exceptions (`AppException`, `DuplicateShiftException`)

### Infrastructure (`Jenian.Infrastructure`)
Implementations of the abstractions defined in `Application`:
- EF Core `DbContext`s, entity configuration, and migrations
- ASP.NET Core Identity (`ApplicationUser`, `RefreshToken`)
- `AuthService`, `JwtTokenManager`, `DemoAccount` service
- `OpenAiService`, `AzureVisionParserService`, roster OCR/matching helpers
- `TelegramService`, `TelegramMessenger`, Telegram concurrency helpers (`LatestRequestRunner`, `RosterSessionManager`)
- Azure Blob Storage service and background job queue

### API (`Jenian.API`)
The ASP.NET Core host:
- Controllers (`AuthController`, `CWHController`, `TelegramController`, `HomeController`)
- `GlobalExceptionHandler` middleware
- Dependency injection wiring, Swagger, CORS, JWT, and rate-limiting configuration in `Program.cs`
- Request/response contracts (`Contracts/Auth`, `Contracts/Cwh`, `Contracts/Common`)

```
src/
├── Jenian.API/            # Controllers, middleware, DI/auth/rate-limit config, Program.cs
│   ├── Auth/               # Cookie names & settings
│   ├── Configurations/     # JwtBearerConfigurationOptions, Ollama options
│   ├── Contracts/          # Request/response DTOs (Auth, CWH, common ApiResponse<T>)
│   ├── Controllers/
│   └── Middleware/         # GlobalExceptionHandler
├── Jenian.Application/     # Use cases, DTOs, commands, service interfaces
│   ├── Abstractions/       # Auth, AI, Messaging, Persistence, Storage, DemoAccount, BackgroundJobs
│   ├── Common/Exceptions/
│   └── Features/           # Auth, Shifts, PaySummaries, Payroll, Telegram
├── Jenian.Infrastructure/  # EF Core, Identity, external service implementations
│   ├── Persistence/        # JenianDbContext, JenianAuthDbContext, Migrations
│   ├── Identity/            # ApplicationUser, RefreshToken, AuthService, DemoAccountStatus
│   ├── Services/            # Auth, AI (OCR/OpenAI), Telegram, Demo, storage
│   └── BackgroundJobs/
└── Jenian.Domain/          # Entities & enums only
tests/
└── Jenian.Application.Tests/   # xUnit tests (currently: pay-rate calculation logic)
```

## Request Flow

```
Next.js frontend
   → Next.js BFF route handler (server-side, holds/forwards cookies)
      → ASP.NET Core Controller (Jenian.API)
         → Application Service (Jenian.Application)
            → Infrastructure / EF Core / External APIs (Jenian.Infrastructure)
         ← Result / ServiceResult<T>
      ← ApiResponse<T> / ProblemDetails
   ← JSON response
```

The frontend is expected to talk to this API through its own Next.js BFF layer (e.g. Route Handlers) rather than calling it directly from client-side code, which keeps `HttpOnly` cookies scoped correctly and centralises error handling on the frontend side.

## Authentication and Session Management

**Authentication** (identifying the user) is handled with JWT access tokens plus a database-backed refresh-token mechanism; **authorisation** (deciding whether the authenticated user may access an endpoint or resource) is handled with `[Authorize]` on protected controller actions and manual ownership checks (endpoints filter data by the `sub` claim of the calling user).

- **Registration** (`POST /api/auth/register`) creates an ASP.NET Core Identity user via `UserManager`, and is gated by a shared invite token that must match a value configured via `Registration:InviteToken` (appsettings/environment/user-secrets, not hardcoded); registration is disabled by default (fails closed) if the token isn't configured.
- **Login** (`POST /api/auth/login`) validates credentials with `UserManager.CheckPasswordAsync`, then issues a JWT access token and upserts a per-device refresh token.
- **Access tokens** are signed with `HmacSha256` using a symmetric key from `jwt:Key`, and carry `sub`, `name`, `email`, and `IsDemoUser` claims. Standard login issues a 30-minute access token (`AuthService.LoginAsync`); the cookie-set access-token expiry for password logins is driven by the configurable `AuthCookies:AccessTokenMinutes` setting.
- **Refresh tokens** are opaque random values (`RandomNumberGenerator`, base64) stored in the `RefreshTokens` table together with a `DeviceId`, created/updated with a 7-day expiry (`JwtTokenManager`). Each browser/device gets its own tracked refresh-token row, so a user can have multiple active sessions simultaneously.
- **Refresh** (`POST /api/auth/refresh-token`) validates the refresh-token/device pair, checks expiry, and — if valid — extends the same refresh token's expiry and issues a new access token. A rotation TODO exists in the code (`AuthService.RefreshTokenAsync`), so the refresh token itself is currently reused rather than replaced on every refresh.
- **Cookies**: `accessToken`, `refreshToken`, and `deviceId` are all set as `HttpOnly`, `SameSite=Lax`, and unconditionally `Secure` — including on the demo-login path. Because of this, these cookies are not sent/stored by the browser when testing locally over plain HTTP (e.g. `http://localhost:5000`); use the HTTPS `launchSettings.json` profile (`https://localhost:7034`) for local auth testing.
- **Logout** (`DELETE /api/auth/logout`, `[Authorize]`) revokes the current device's refresh token and clears all three cookies.
- **Invalid/expired tokens**: `JwtBearerConfigurationOptions` validates issuer, audience, lifetime, and signing key on every request, and additionally checks (via `OnTokenValidated`) that the user still exists and — for demo users — that the account is not expired or pending deletion.
- `GET /api/auth/get-me` (`[Authorize]`) returns the current user's basic profile info and Telegram-linked status from JWT claims.

This description reflects the current implementation; it is not a claim of complete or certified security.

## Demo Account Design

- A visitor starts a demo session by calling `POST /api/auth/demo-login` (rate-limited under the `login` policy). This creates a **real, separate `ApplicationUser` row** (`IsDemoUser = true`) with a randomly generated username/email, sets `DemoStatus = Active`, and stamps `DemoExpiresAtUtc` to one hour from creation.
- Demo data is isolated by ownership: all shifts, pay summaries, and pay-cycle settings are scoped to that dedicated demo `UserId`, the same way as any other user.
- On `DemoLogout` (`DELETE /api/auth/demo-logout`), the account is marked `PendingDeletion`, its expiry is forced to "now", and its refresh token is revoked immediately.
- Expired or `PendingDeletion` demo accounts are cleaned up lazily: every new `demo-login` call first runs `DeleteExpiredDemoAccountAsync`, which removes up to 50 stale demo users (and cascades deletion of their refresh tokens, shifts, and pay-cycle settings) that expired more than 5 minutes ago.
- `JwtBearerConfigurationOptions` re-checks demo expiry/`PendingDeletion` status on every authenticated request, so an access token issued to a demo user stops working as soon as the session ends or expires, even before cleanup runs.

## Rate Limiting

Configured in `Program.cs` using `Microsoft.AspNetCore.RateLimiting`:

- **Global limiter** (applies to all endpoints by default): a **sliding-window** limiter partitioned by `user:{sub}` when authenticated, otherwise `ip:{RemoteIpAddress}`. Limit: **60 requests per minute**, split into 6 segments, no queueing (`QueueLimit = 0`).
- **`login` policy** (applied explicitly to `register`, `login`, and `demo-login`): a **fixed-window** limiter partitioned by client IP address. Limit: **30 requests per minute**, no queueing.
- The health-check endpoint explicitly disables rate limiting (`[DisableRateLimiting]`).
- Partition keys use `HttpContext.Connection.RemoteIpAddress` directly; no `Forwarded-For`/forwarded-headers middleware is configured in `Program.cs`, so the effective client IP behind a reverse proxy depends on the hosting platform terminating that connection correctly.
- On rejection, the API returns **HTTP 429**, sets a `Retry-After` header when available, and returns `{ "message": "Too many requests. Please try again later." }`.

This provides basic abuse mitigation; it is not a guarantee against all forms of denial-of-service or credential-stuffing traffic.

## Shift and Pay-Calculation Design

This is the most business-logic-heavy part of the API.

- **Pay-cycle settings** (`PayCycleSetting`) store an `AnchorStartDate` and a `PayCycleType` (`Weekly`, `Fortnightly`, `Monthly`) per user. The current cycle's date range is computed dynamically from that anchor via `IPayCalculator.CalculatePayCycleDateRange`, rather than being stored per cycle.
- **`UserShift`** is the source of truth for raw shift data: start/end time, time zone, paid/unpaid break minutes, entry type (`Worked`, `PaidNonWorked`, `Leave`), employment type (`FullTime`, `PartTime`, `Casual`), and source (`Manual`, `OCR`, `Telegram`, `CsvImport`, `ApiImport`).
- **`UserDailyPaySummary`** stores the calculated result per user/work-date: payable minutes, paid/unpaid break minutes, evening-penalty minutes, overtime minutes, the base rate used, and gross pay. A unique index on `(UserId, WorkDate)` enforces one summary per user per day.
- The frontend submits raw shift data (times, break minutes, entry/employment type); **the backend performs all pay calculations** — the frontend never sends computed pay figures that are trusted as-is. This keeps payroll-relevant business rules (award multipliers, public holidays, break handling) centralised and auditable on the server rather than duplicated or trusted from client-side code.
- **Bulk save** (`PUT /shifts/bulks`, query params `cycleStartDate`/`cycleEndDate`): shift DTOs **with an `Id`** are treated as updates to existing shifts; DTOs **without an `Id`** are created as new shifts; IDs listed in `DeletedShiftIds` are removed. All work dates touched by the change (including any other shifts on the same day) are tracked and recalculated, so untouched shifts sharing a day with an edited shift still get an up-to-date daily summary.
- Daily summaries are then aggregated per pay cycle when shifts are retrieved via `GET /shifts/by-cycle-date`.
- **Pharmacy Award rate logic** (`PharmacyAwardRateService`) is implemented per the comment header in that file and covers, for the Victoria (`VIC`) public-holiday calendar hardcoded for 2026:
  - Ordinary weekday hours vs. evening penalty hours (19:00–21:00) and late/early hours
  - Saturday and Sunday differentiated rate bands
  - Public-holiday multipliers (different rates for permanent vs. casual staff)
  - Separate multiplier tables for `FullTime`/`PartTime` ("permanent") vs. `Casual` employees
  - Unpaid break minutes deducted only from the 08:00–18:00 segment of a shift
  - A currently hardcoded base hourly rate (`27.81m` AUD) — the code notes this should later come from a user profile
  - An "overtime minutes" field exists on the daily summary, but the current implementation always calculates it as `0` (no overtime business rule is implemented yet)

Displayed pay figures are **estimates based on the rules implemented above**, not an official or legally authoritative payroll statement.

## Main API Endpoints

| Method | Endpoint | Authentication | Purpose |
|---|---|---|---|
| **Authentication** | | | |
| POST | `/api/auth/register` | Rate-limited (`login`) | Register a new user (invite-token gated) |
| POST | `/api/auth/login` | Rate-limited (`login`) | Log in, set auth cookies |
| POST | `/api/auth/demo-login` | Rate-limited (`login`) | Start an isolated, time-limited demo session |
| DELETE | `/api/auth/demo-logout` | `[Authorize]` | End a demo session and clear cookies |
| DELETE | `/api/auth/logout` | `[Authorize]` | Revoke the current device's refresh token |
| POST | `/api/auth/refresh-token` | Cookie-based | Exchange a valid refresh token for a new access token |
| GET | `/api/auth/get-me` | `[Authorize]` | Current user profile + Telegram-linked status |
| POST | `/api/auth/request-password-reset` | Public | Request a password-reset token |
| POST | `/api/auth/reset-password` | Public | Reset password using an email token |
| **Shift Calculator & Pay Cycles** | | | |
| GET | `/api/cwh/shift-calculator/current` | `[Authorize]` | Current pay-cycle settings for the user |
| POST | `/api/cwh/pay-cycle-settings/update` | `[Authorize]` | Create/update pay-cycle anchor date and type |
| **Shifts** | | | |
| PUT | `/shifts/bulks` | `[Authorize]` | Bulk create/update/delete shifts for a date range |
| GET | `/api/cwh/shifts/by-cycle-date` | `[Authorize]` | Shifts + daily pay summaries for the current cycle |
| **Night / End-of-Day Reports** | | | |
| POST | `/api/cwh/eod-report` | `[Authorize]` | Submit a structured end-of-day report with photo uploads |
| GET | `/api/cwh/eod-report/{reportId}` | `[Authorize]` | Retrieve a populated report (owned by the caller) |
| GET | `/api/cwh/is-report-submitted` | `[Authorize]` | Whether today's report has already been submitted |
| GET | `/api/cwh/background-job-status/{jobId}` | `[Authorize]` | Status of the background OCR/extraction job |
| **Telegram** | | | |
| POST | `/api/telegram/webhook` | Public (Telegram-only) | Receives Telegram bot updates |
| GET | `/api/telegram/link-token` | `[Authorize]` | Generate/return a one-time Telegram link token |
| GET | `/api/telegram/is-linked` | `[Authorize]` | Whether the current user has a linked Telegram account |
| **Health** | | | |
| GET | `/api/home/health` | Public, rate-limiting disabled | Liveness check, returns `204 No Content` |

This is not an exhaustive list of every route; see the controllers for full detail.

## Database

The API uses **Entity Framework Core 9** against **SQL Server**, split across **two `DbContext`s** with separate connection strings:

- **`JenianAuthDbContext`** (`ConnectionStrings:JenianAuthConnection`) — ASP.NET Core Identity tables plus `RefreshToken`.
- **`JenianDbContext`** (`ConnectionStrings:JenianDbConnection`) — application/business data: `DeliveryExtractionJob`, `EodReport` (with owned sub-entities `StockUpdate`, `NightTasks`, `AislesFacing`, `Cleaning`, `GeneralCheck`), `UserShift`, `UserDailyPaySummary`, `PayCycleSetting`.

Confirmed constraints and relationships:

- `UserShift` has a **unique index** on `(UserId, StartAt, EndAt)`, preventing duplicate shift entries for the same user and time range (surfaced to callers as `DuplicateShiftException`, HTTP 409).
- `UserDailyPaySummary` has a **unique index** on `(UserId, WorkDate)` — one summary per user per work date.
- `UserDailyPaySummary` → `UserShift` is a one-to-many relationship (`HasForeignKey(s => s.UserDailyPaySummaryId)`), optional and set to `SetNull` on delete.
- `BaseRateUsed` and `GrossPay` on `UserDailyPaySummary` are stored with `decimal(18, 2)` precision.
- `RefreshToken` rows are scoped per user and per `DeviceId`, and are hard-deleted (not just marked revoked) as part of demo-account cleanup.

Schema changes are managed through EF Core migrations under `src/Jenian.Infrastructure/Persistence/Migrations` (`JenianAuthDbContext`) and `src/Jenian.Infrastructure/Persistence/Migrations/JenianDb` (`JenianDbContext`). Migrations for both contexts can optionally be applied automatically at container start-up via the `RUN_MIGRATIONS` configuration flag (see [Docker](#docker)).

## Error Handling and API Responses

- A global exception handler (`GlobalExceptionHandler`, registered via `AddExceptionHandler<T>()` and `UseExceptionHandler()`) intercepts unhandled exceptions and converts them into an RFC 7807 **`ProblemDetails`** response.
- Known exception types are mapped explicitly:
  - `AppException` (and subclasses like `DuplicateShiftException`) → the exception's own `StatusCode` and `ErrorCode`, plus a trace ID.
  - `KeyNotFoundException` → 404
  - `UnauthorizedAccessException` → 401
  - Anything else → 500, with the full exception detail included **only** when running in the Development environment.
- Model-validation failures (e.g. malformed request bodies) are intercepted by a custom `InvalidModelStateResponseFactory` and returned as `400` with `{ "message": "Validation failed", "errors": { ... } }`.
- Most controller actions wrap successful/failed results in a small `ApiResponse<T>` envelope: `{ "success": bool, "data": T | null, "errors": string[] }`.
- A representative unhandled-exception response shape (Production):

```json
{
  "type": "https://httpstatuses.com/500",
  "title": "Unexpected Error",
  "status": 500,
  "instance": "/api/cwh/eod-report",
  "errorCode": null,
  "traceId": "0HN...:00000001"
}
```

No stack traces are exposed outside of the Development environment.

## External Integrations

### OpenAI
Used to post-process raw OCR text — e.g. extracting structured delivery entries from a photographed chat/roster screenshot (`OpenAiService.DeliveryTextExtractor`) — via the `OpenAI` .NET SDK's `ChatClient`. The model name is configurable (`OpenAI:Model`, currently defaulting to `gpt-5-nano` in `appsettings.json`).

### Azure Vision OCR
`AzureVisionParserService` calls Azure AI Vision's `ImageAnalysisClient` (`VisualFeatures.Read`) to extract raw text from uploaded images/screenshots (e.g. rosters, delivery photos), which is then normalised further by OpenAI and/or dedicated roster-parsing helpers (`RosterOcrParser`, `RosterShiftMapper`, `RosterStaffMatcher`).

### Telegram
- Users link their Telegram account to their Jenian account by sending `/start <token>` to the bot, where `<token>` is a one-time link token generated via `GET /api/telegram/link-token`. Once linked, the token is invalidated and the Telegram user ID is stored on the `ApplicationUser`.
- A `/roster` command is also supported, backed by `RosterSessionManager`, which tracks a short-lived (default 60-second) pending session per Telegram chat while the bot waits for the user to send a roster photo.
- The webhook controller receives updates at `POST /api/telegram/webhook` (intended to be registered with Telegram via `setWebhook`) and always returns `200 OK` so Telegram does not retry delivery.
- A `LatestRequestRunner` concurrency helper exists to cancel an in-flight background job per chat ID in favour of the newest one ("latest request wins"); at the time of writing it is wired into `TelegramController` but the active call site is commented out, so webhook processing currently runs synchronously via `ITelegramService.HandleUpdateAsync`.

### File Storage
Uploaded images (e.g. end-of-day report screenshots) are stored in **Azure Blob Storage** via `AzureBlobStorageService`, authenticated with `DefaultAzureCredential`. A hosted service (`BlobContainerInitialiser`) ensures the configured container exists at startup.

## Testing

- **Framework:** xUnit (`xunit` 2.9.3), with `Microsoft.NET.Test.Sdk` and `coverlet.collector` for the test SDK/coverage collector.
- **Project:** a single test project, `tests/Jenian.Application.Tests`, referencing `Jenian.Application` and `Jenian.Domain` directly (no database or HTTP dependency).
- **Coverage:** currently focused entirely on `PharmacyAwardRateServiceTests` — parameterised (`[Theory]`/`[InlineData]`) tests covering weekday, Saturday, Sunday, and public-holiday multipliers for both permanent (full-time/part-time) and casual employment types, and both `Worked` and `Leave` entry types.
- **Test naming:** `MethodUnderTest_Scenario_ExpectedOutcome` (e.g. `GetMultiplier_Weekday_PermanentEmployees_ReturnsCorrectMultiplier`).
- **Mocking:** none observed in this test project — it exercises `PharmacyAwardRateService` directly with real inputs.
- **Scope:** there are currently no authentication tests, no integration tests, and no test project for the Infrastructure or API layers.

Run the tests with:

```bash
dotnet test tests/Jenian.Application.Tests/Jenian.Application.Tests.csproj
```

At the time of writing this runs 70 tests, all passing. This does not represent full coverage of the application — most Infrastructure and API-layer code (auth, Telegram, OCR, controllers) is currently untested by automated tests.

## Local Development

### Prerequisites

- **.NET SDK 9.0.305** or a compatible 9.x SDK (`global.json` uses `rollForward: latestFeature`)
- **SQL Server** reachable locally (the sample `appsettings.Development.json` targets a named local instance), or Docker if you prefer to run SQL Server in a container
- **Docker Desktop** if you want to run the API itself via `docker compose`
- EF Core CLI tools (`dotnet tool install --global dotnet-ef`) if you need to add or apply migrations manually

### Clone

```bash
git clone https://github.com/Brian3010/JenianAPI.git
cd JenianAPI
```

### Restore

```bash
dotnet restore JenianAPI.sln
```

### Configuration

Do not put real secrets in `appsettings.json` or `appsettings.Development.json` — both are committed to source control. The project supports a git-ignored `appsettings.Local.json` (see `src/Jenian.API/appsettings.Local.json.example` for the expected shape), which is loaded after `appsettings.Development.json` and can hold your local `jwt:Key`, `Telegram:BotToken`, `OpenAI:ApiKey`, and `AzureVision` credentials. Alternatively, use [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) or environment variables (see [Environment Variables and Configuration](#environment-variables-and-configuration)).

### Database Setup

Migrations live in the `Jenian.Infrastructure` project. There are two `DbContext`s, each with its own migration history and applied independently. With the API project as the startup project:

```bash
dotnet ef database update --project src/Jenian.Infrastructure --startup-project src/Jenian.API --context JenianAuthDbContext
dotnet ef database update --project src/Jenian.Infrastructure --startup-project src/Jenian.API --context JenianDbContext
```

Alternatively, set `RUN_MIGRATIONS=true` (or `RUN_MIGRATIONS: true` in configuration) before starting the app to apply pending migrations automatically at startup as a one-off job (`Program.cs` exits immediately after running migrations when this flag is set).

### Run the API

```bash
dotnet run --project src/Jenian.API
```

### Swagger

Swagger UI is only enabled when `ASPNETCORE_ENVIRONMENT=Development` (`app.Environment.IsDevelopment()` in `Program.cs`). When running locally in Development mode, it is available at `/swagger` on whichever port `dotnet run` binds to (see `src/Jenian.API/Properties/launchSettings.json` for the configured local ports).

## Environment Variables and Configuration

ASP.NET Core maps environment variables to configuration sections using a double-underscore separator, e.g. `Jwt__Key` maps to `jwt:Key`. Names below reflect the actual keys referenced in code and `appsettings.json`.

| Variable / Section | Required | Purpose |
|---|---|---|
| `ConnectionStrings__JenianAuthConnection` | Yes | SQL Server connection string for Identity/refresh-token data |
| `ConnectionStrings__JenianDbConnection` | Yes | SQL Server connection string for shifts/pay/report data |
| `jwt__Key` | Yes | Symmetric signing key for JWT access tokens |
| `jwt__Issuer` | Yes | JWT issuer, validated on every request |
| `jwt__Audience` | Yes | JWT audience, validated on every request |
| `AuthCookies__AccessTokenMinutes` | Yes | Access-token cookie lifetime (minutes) |
| `AuthCookies__RefreshTokenDays` | Yes | Refresh-token cookie lifetime (days) |
| `AuthCookies__DeviceIdDays` | Yes | Device-ID cookie lifetime (days) |
| `Registration__InviteToken` | Yes | Shared invite token required to register a new user; registration is disabled if unset |
| `Telegram__BotToken` | For Telegram features | Telegram Bot API token |
| `OpenAI__ApiKey` | For OCR/roster text extraction | OpenAI API key |
| `OpenAI__Model` | No (defaults to `gpt-5-nano`) | OpenAI chat model name |
| `AzureVision__VisionEndpoint` | For OCR features | Azure AI Vision resource endpoint |
| `AzureVision__VisionKey` | For OCR features | Azure AI Vision resource key |
| `AzureBlobStorage__AccountUrl` | For file uploads | Azure Blob Storage account URL |
| `AzureBlobStorage__ContainerName` | For file uploads | Blob container name for uploaded files |
| `JenianAPI__BaseUrl` | Yes | Base URL registered on the internal `JenianAPI` named `HttpClient` |
| `RUN_MIGRATIONS` | No (default `false`) | If `true`, applies EF Core migrations at startup then exits |
| `Ollama__BaseUrl` / `Ollama__Model` | No | Optional local LLM endpoint (present in configuration, alternative to OpenAI) |

Never commit real values for any of the above. Use `src/Jenian.API/appsettings.Local.json.example` as a starting template for a local, git-ignored `appsettings.Local.json`.

## Docker

The root `Dockerfile` is a multi-stage build intended to be built **from the repository root** (so all four `src/` projects are visible):

- **Build stage:** `mcr.microsoft.com/dotnet/sdk:9.0` — restores and publishes `Jenian.API` for `linux-x64`, self-contained set to `false`.
- **Runtime stage:** `mcr.microsoft.com/dotnet/aspnet:9.0` — installs the native Linux libraries required by OpenCvSharp4 (`libgomp1`, `libgtk-3-0`, `libcairo2`, etc.), then copies only the published output.
- **Exposed port:** `8080` (`ASPNETCORE_HTTP_PORTS=8080`), matching the ASP.NET Core default for containers since .NET 8.
- **Default environment:** `ASPNETCORE_ENVIRONMENT=Production`; override with `-e ASPNETCORE_ENVIRONMENT=Development` for local testing.

`docker-compose.yml` runs the API container for local testing, mapping host port `8081` to container port `8080`, and passes connection strings, JWT settings, external-service keys, and `AuthCookies__*` values through environment variables. These are read from a local `.env` file (create one with the variables listed in [Environment Variables and Configuration](#environment-variables-and-configuration) — no `.env.example` template is currently committed to this repository).

```bash
docker build -t jenian-api .
docker compose up --build
docker compose down
```

The container is not confirmed to run as a non-root user; the Dockerfile does not set a `USER` instruction.

## Deployment

Deployment is automated via `.github/workflows/azure-deploy.yml`:

```
Push to `main`
  → GitHub Actions checkout + Docker Buildx setup
  → Log in to Azure Container Registry
  → Build Docker image, tag with the Git commit SHA and `latest`
  → Push both tags to Azure Container Registry
  → Log in to Azure (service principal)
  → Deploy the SHA-tagged image to Azure Container Apps
```

The workflow can also be triggered manually (`workflow_dispatch`). It deploys the SHA-tagged image (not `latest`) so every deployment is traceable back to a specific commit. Registry, resource-group, and container-app names, along with Azure credentials, are supplied entirely through GitHub Actions secrets and are not hardcoded in the workflow file.

## Health Checks and Cold Starts

- `GET /api/home/health` returns `204 No Content` and has rate limiting explicitly disabled, so it can be polled frequently without being throttled.
- Because the production target is Azure Container Apps, the container may scale to zero when idle; a request arriving after a scale-to-zero period will experience additional latency while a new instance starts (a cold start), rather than an application error. No explicit client-side retry logic for cold starts is implemented in this repository.

## Security Considerations

Implemented, verifiable protections:

- JWT signature, issuer, audience, and lifetime validation on every authenticated request, plus a live check that the user still exists (and, for demo users, has not expired or been marked for deletion).
- `HttpOnly`, `Secure`, `SameSite=Lax` cookies for access token, refresh token, and device ID — mitigating basic XSS-based token theft and CSRF via cross-site cookie leakage.
- Per-device refresh tokens stored server-side, individually revocable on logout.
- Resource-ownership checks on shift, pay-summary, and report endpoints (queries are filtered by the authenticated user's ID).
- Rate limiting on all endpoints (global) and more aggressively on auth endpoints (login/register/demo-login).
- CORS locked to specific known origins in Production (`ProdCors` policy); wide open only in Development.
- Consistent model-validation responses via `ApiBehaviorOptions`.
- SQL access exclusively through EF Core's parameterised LINQ queries — no raw/interpolated SQL observed.
- Isolated, time-boxed demo accounts with server-side ownership scoping and lazy cleanup of expired sessions.

Known, current limitations (not fixed by this section, described for transparency):
- The refresh token itself is not rotated on every use — the same token value is extended, rather than replaced (a TODO in `AuthService.RefreshTokenAsync`).
- Registration is gated by a single, shared invite token (configured, not hardcoded), not a per-user invite mechanism.

This is a factual summary of implemented mechanisms, not a claim of complete or certified security.

## Related Repository

The Next.js frontend — including UI, forms, and the BFF layer that this API is designed to be called through — is maintained in a separate repository not included here.

## Project Status

**Completed / working today:**
- Registration, login, refresh, logout, and demo-login/logout flows
- Bulk shift save with server-side Pharmacy Award pay calculation and daily/cycle summaries
- Structured end-of-day report submission with photo upload and background OCR/extraction job status polling
- Telegram account linking and `/roster` OCR-based import flow
- Rate limiting, global exception handling, and health-check endpoint

**Experimental / partially implemented:**
- Overtime minute tracking (field exists, calculation currently always returns 0)
- Telegram "latest request wins" cancellation helper (implemented, but not currently wired into the active webhook code path)
- Ollama configuration is present but no corresponding service registration was found wired into `Program.cs`'s DI container

**Not yet implemented:**
- Refresh-token rotation
- Configurable base pay rate / user-specific award profile
- Dynamic public-holiday lookup (currently a hardcoded 2026 Victorian calendar)
- Password-reset email delivery (the reset-token endpoint currently returns the token directly instead of emailing it, per an inline TODO)

## Author

Repository owned by **Brian3010** on GitHub. No additional public contact information is included in this repository.
