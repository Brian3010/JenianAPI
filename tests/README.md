# Test guide

Run `dotnet test tests/Jenian.Tests/Jenian.Tests.csproj --no-restore` after restoring the solution.

Use strict Moq dependencies and configure only calls needed by a scenario. `ShiftServiceContext` supplies non-null dependencies without default success behavior. Keep reusable helpers small and business inputs visible. Use Arrange/Act/Assert sections and `Method_Scenario_ExpectedOutcome` names. Use typed theory data when inputs vary under the same rule, with explicit expected results independent of production calculations.

## Scenario migration map

All 94 original cases are retained by behavior. The suite has 101 cases after seven additions.

| Original group | Replacement | Preserved scenarios |
| --- | --- | --- |
| PharmacyAwardRateServiceTests | Application/Payroll/PharmacyAward*Tests | All multiplier rows, segment cases, worked-pay and annual-leave-pay rows |
| PayCalculatorPayCycleTests | Application/Payroll | Fortnight boundaries, Melbourne date boundary, weekly anchor, monthly calendar |
| PayCalculatorWorkDateTests | Application/Payroll | Both offsets, equivalent local dates, UTC candidate range, paid breaks |
| CurrentPayCycleShiftServiceTests | Application/Shifts/Services | Inclusive query arguments, mapping, aggregates, both missing-setting states |
| ShiftSaveWorkflowTests mutation cases | ShiftMutationServiceTests | Add/update/delete with old/new dates, missing update ID before mutation |
| ShiftSaveWorkflowTests orchestration cases | ShiftSaveWorkflowTests | Flush/recalculation ordering inside callback, recalculation exception propagation |
| CWHControllerCurrentPayCycleTests | API | Setup response, authenticated user passed to summary service |
| SQLCWHReportRepositoryTests | Infrastructure | Insert report, update today's report retaining ID |

## Added checks

Weekly rollover; Melbourne daylight-saving conversion; configured cycle without data; missing deletion ID; conflicting update/delete ID; validation failure before transaction; mutation failure before saves/recalculation.

## Limits

Mocked transactions verify orchestration, not database rollback. Report tests use a fresh EF InMemory database per case. Direct controller tests do not exercise authentication middleware. SQL Server and HTTP integration coverage remain separate work. Production behavior and package versions are unchanged.
