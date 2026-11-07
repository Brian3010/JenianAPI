# Codex repository investigation and proposed AGENTS.md

Saved: 2026-10-21 (Australia/Sydney). Investigation performed in the preceding conversation on 2026-10-20. Findings describe that inspection, not a fresh verification on the save date.

Status: reference note and unapproved proposal. This note does not establish permanent architectural rules or decisions, and the proposed `AGENTS.md` has not been installed.

## Original question and context

The user was introducing Codex into this existing repository for the first time and asked:

> Before making any changes, investigate:
> 1. Project structure and architecture.
> 2. Existing coding conventions and patterns.
> 3. Important business logic and boundaries.
> 4. Testing framework and verification commands.
> 5. Security-sensitive areas, especially authentication.
> 6. Existing documentation and configurations.
>
> Based on your investigation, propose a small AGENTS.md that accurately describes how an AI coding agent should work in this repository.
>
> Separate verified findings from assumptions.
> Do not modify any files, access secrets, or implement anything yet.

The repository inspected was `D:\projects\Jenian\JenianAPI`. The investigation read project files, representative application and infrastructure code, controllers, tests, documentation, and deployment configuration. No files were changed, no secret-bearing configuration was opened, and no application, build, tests, or migrations were run during that investigation.

The subsequent request authorized saving this answer under `docs/notes/`, preserving its context and detail without modifying application code or making the proposal permanent. No existing related Markdown notes were found under `docs/` when saving this note.

## Verified findings

1. **Structure and architecture.** Four .NET 9 projects: Domain contains entities; Application contains feature services, commands, DTOs, and abstractions; Infrastructure provides EF Core, Identity, external integrations, and workers; API hosts controllers and middleware. Dependencies generally follow these layers, although some Application DTOs use `Jenian.API.Contracts.Auth` namespaces. See the [Application project file](../../src/Jenian.Application/Jenian.Application.csproj).

2. **Conventions.** Nullable reference types and implicit usings are enabled. [`.editorconfig`](../../.editorconfig) specifies two spaces, CRLF, block-scoped namespaces, PascalCase members, `I`-prefixed interfaces, and `_camelCase` fields. Existing code varies, particularly tests using file-scoped namespaces. Services use constructor injection, asynchronous methods, cancellation tokens, and `ServiceResult<T>`; controllers commonly return `ApiResponse<T>`, while exceptions use `ProblemDetails`.

3. **Business boundaries.** [Shift saves](../../src/Jenian.Application/Features/Shifts/Services/ShiftService.cs) validate, apply user-scoped mutations, flush shifts, recalculate summaries for affected dates, and save again within one transaction. Payroll depends on local work dates, employment/entry types, rate boundaries, breaks, and rounding. The implementation currently hardcodes a base rate and Victorian 2026 holidays; overtime remains a placeholder. Two SQL Server contexts separate application data from Identity/session data.

4. **Testing and verification.** [Tests](../../tests/Jenian.Application.Tests/Jenian.Application.Tests.csproj) use xUnit, EF InMemory, handwritten fakes, and fixed `TimeProvider` instances; Moq and coverlet are installed. Tests cover payroll, pay cycles, shift workflows, controller behavior, and report persistence. The README’s narrower coverage description is outdated. The deployment workflow builds/publishes Docker images but has no test step.

5. **Security-sensitive areas.** Identity handles passwords; registration fails closed without a valid invite token. JWT validation checks issuer, audience, lifetime, signing key, user existence, and demo status. Auth cookies use `HttpOnly`, `Secure`, and `SameSite=Lax`. Refresh tokens are stored directly and reused on refresh. Password-reset email delivery remains unfinished. The Telegram webhook has no visible application-level sender-secret check, and background-job status retrieval is authenticated but lacks user ownership filtering. These observations come from [authentication code](../../src/Jenian.Infrastructure/Identity/AuthService.cs), [TelegramController](../../src/Jenian.API/Controllers/TelegramController.cs), and [CWHController](../../src/Jenian.API/Controllers/CWHController.cs).

6. **Documentation/configuration.** The repository has a substantial README, Docker/Compose configuration, Azure deployment workflow, SDK pin, editor settings, and an OpenCV native-dependency troubleshooting note. `global.json` selects SDK `9.0.305` with `latestFeature` roll-forward. Startup supports a migration-only mode that updates both contexts and exits.

## Assumptions and unverified points

- The separate Next.js BFF described in the README was not inspected. API code uses bearer authentication and writes cookies; cookie forwarding/conversion cannot be verified here.
- SDK availability, passing tests, SQL Server behavior, deployed security controls, and external integrations remain unverified.
- Payroll rules were inspected as implementation behavior, not validated against an award.
- The commands below are proposed verification steps; they were not executed because they can write build artifacts.

## Reasoning behind the proposal

The proposal captures the observed project boundaries, conventions, and verification entry points in a small root-level guide. It emphasizes user ownership, atomic shift and summary updates, timezone-sensitive calculations, and authentication because changes in these areas can affect isolation or derived business data.

It recommends following nearby patterns without treating the repository as perfectly uniform: namespaces and formatting vary, and the README’s test description does not match the inspected tests. It also distinguishes unit-test evidence from real SQL Server behavior. These are recommendations for future agent work, not evidence that the current implementation is fully tested or secure.

## Proposed root AGENTS.md — not approved or installed

The following is the original proposed content. Its imperative wording is part of the draft, not an instruction activated by this reference note.

```markdown
# Repository guidance

## Structure
- .NET 9; use the SDK selected by global.json.
- Domain: entities. Application: feature services, commands, DTOs,
  validation, and abstractions. Infrastructure: persistence, Identity,
  integrations, and workers. API: HTTP contracts, controllers, and host.
- Keep changes in the appropriate layer and register services through
  the existing dependency-injection extensions. Avoid adding cross-layer
  coupling.

## Conventions
- Follow .editorconfig and nearby code; avoid unrelated reformatting.
- Use existing constructor injection, async/cancellation patterns,
  ServiceResult<T>, ApiResponse<T>, and exception handling.
- Keep changes focused; preserve public contracts unless requested.

## Business boundaries
- Derive user identity from validated claims and enforce ownership in
  data access; [Authorize] alone does not establish ownership.
- Preserve atomic shift mutation and daily-summary recalculation,
  including old and new work dates.
- Preserve timezone, pay-cycle, rate, break, and decimal-rounding
  behavior. Do not silently change hardcoded payroll assumptions.

## Security and persistence
- Preserve JWT validation, invite gating, session revocation, demo expiry,
  cookie protections, and rate limiting.
- Never expose credentials, tokens, cookies, or personal data in logs.
- Do not read secret-bearing local configuration unless explicitly needed
  and authorized. Never commit secrets.
- Keep migrations associated with the correct DbContext. Do not apply
  migrations or invoke deployed services without task authorization.

## Verification
- Add focused regression tests for changed business or security behavior.
  Follow existing xUnit, fake-service, and TimeProvider patterns.
- From the repository root:
  dotnet restore JenianAPI.sln
  dotnet build JenianAPI.sln --no-restore
  dotnet test tests/Jenian.Application.Tests/Jenian.Application.Tests.csproj --no-restore
- EF InMemory and fake transactions do not verify SQL Server behavior.
- Consult README.md, Dockerfile, and fixed-bug-doc for setup and native
  dependencies; confirm documentation against code.
- Report changes, checks performed, and anything left unverified.
```

## Open questions

- Should the proposed root `AGENTS.md` be adopted or revised? No approval to adopt it was given as part of this note-saving request.
- What verification succeeds in the local development environment? Builds and tests were deliberately not run during the investigation.
- What controls exist in the frontend BFF and deployed infrastructure? Those surfaces were outside the inspection.
- Are the current payroll assumptions intended to remain, or should they change in a separately scoped task? No payroll changes were requested.
