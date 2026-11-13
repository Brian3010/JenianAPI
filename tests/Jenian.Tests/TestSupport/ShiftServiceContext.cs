using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.PaySummaries.Services;
using Jenian.Application.Features.Shifts.Services;
using Jenian.Application.Features.Shifts.Validations;
using Moq;
namespace Jenian.Tests.TestSupport
{
  internal sealed class ShiftServiceContext
  {
    public Mock<IShiftRepository> Shifts { get; } = new(MockBehavior.Strict);
    public Mock<IPaySummaryRepository> Summaries { get; } = new(MockBehavior.Strict);
    public Mock<IShiftValidator> Validator { get; } = new(MockBehavior.Strict);
    public Mock<IShiftMutationService> Mutation { get; } = new(MockBehavior.Strict);
    public Mock<IPayCalculationService> Calculation { get; } = new(MockBehavior.Strict);
    public Mock<IUnitOfWork> UnitOfWork { get; } = new(MockBehavior.Strict);
    public Mock<IPayCalculator> Calculator { get; } = new(MockBehavior.Strict);
    public ShiftService CreateService() => new(Shifts.Object, Validator.Object, Mutation.Object,
      Calculation.Object, Summaries.Object, UnitOfWork.Object, Calculator.Object);
  }
}
