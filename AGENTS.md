# Repository Guidelines

## Project Structure & Module Organization
`src/` contains four .NET 9 projects: `Jenian.Domain` (entities), `Jenian.Application` (feature services, commands, DTOs, and abstractions), `Jenian.Infrastructure` (EF Core, Identity, integrations, and workers), and `Jenian.API` (controllers, HTTP contracts, and host). Register services through existing dependency-injection extensions. Tests live in `tests/Jenian.Application.Tests/`; reference notes live in `docs/notes/`.

## Build, Test, and Development Commands
Use the SDK selected by `global.json`.

```powershell
dotnet restore JenianAPI.sln
# Build all projects using restored dependencies.
dotnet build JenianAPI.sln --no-restore
# Run the xUnit suite.
dotnet test tests/Jenian.Application.Tests/Jenian.Application.Tests.csproj --no-restore
# Start the API with local configuration.
dotnet run --project src/Jenian.API
```

Restore downloads dependencies. Running locally requires configured SQL Server and external services; consult `README.md`.

## Coding Style & Naming Conventions
Follow `.editorconfig`: two spaces, CRLF, PascalCase types/members, `I`-prefixed interfaces, and `_camelCase` fields. It prefers block-scoped namespaces; respect nearby conventions and avoid unrelated formatting. Use constructor injection, asynchronous methods, cancellation tokens, `ServiceResult<T>`, and existing API response/error patterns. Keep Domain independent; keep business logic and abstractions in Application, and persistence/external integrations in Infrastructure. Preserve public contracts unless the requested change requires otherwise.

## Testing Guidelines
Use xUnit `[Fact]`/`[Theory]` and descriptive `Method_Scenario_ExpectedOutcome` names. Existing tests use handwritten fakes, fixed `TimeProvider` instances, and EF InMemory. Add focused regression tests for changed business or security behavior. InMemory and fake transactions do not establish SQL Server correctness. Report checks performed, results, and anything unverified.

## Commit & Pull Request Guidelines
Keep commits focused with short imperative subjects, such as `Make bulk shift saves atomic`. PRs should explain the problem, resulting behavior, verification, and limitations; link relevant issues and identify contract, configuration, or migration changes.

## Business, Security & Configuration
Enforce user ownership using validated claims. Preserve atomic shift saves and summary recalculation, including old/new work dates. Make intended changes to timezone, payroll, or security behavior explicit and test them. Preserve JWT validation, invite gating, session revocation, demo expiry, cookie protections, and rate limiting outside the requested scope.

Never commit secrets or log tokens, cookies, or credentials. Access secret-bearing configuration only when needed and authorized. Keep migrations with the correct DbContext; applying migrations or invoking deployed services requires task authorization.

Update this guide when authorized changes make its guidance outdated; base updates on verified behavior.
