using Jenian.Application.Common;
using Jenian.Application.Features.Shifts.Commands;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Application.Features.Shifts.Services;
using Jenian.Tests.TestSupport;
using Moq;

namespace Jenian.Tests.Application.Shifts.Services
{
  public class ShiftSaveWorkflowTests
  {
    private static SaveShiftsCommand CreateCommand() => new() {
      UserId = "user-123", RangeStartDate = new(2026, 9, 14), RangeEndDate = new(2026, 9, 20), ShiftDtos = []
    };

    [Fact]
    public async Task SaveShiftsAsync_ValidChanges_FlushesBeforeRecalculationInsideTransaction() {
      // Arrange
      var context = new ShiftServiceContext();
      var command = CreateCommand();
      using var cancellation = new CancellationTokenSource();
      var token = cancellation.Token;
      var inTransaction = false;
      var saves = 0;
      context.Validator.Setup(x => x.ValidateSaveShifts(command.ShiftDtos, command.RangeStartDate, command.RangeEndDate))
        .Returns(ValidationResult.Success());
      context.UnitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<ServiceResult<ShiftSummaryResult>>>>(), token))
        .Returns(async (Func<CancellationToken, Task<ServiceResult<ShiftSummaryResult>>> operation, CancellationToken ct) => {
          inTransaction = true;
          try { return await operation(ct); }
          finally { inTransaction = false; }
        });
      context.Mutation.Setup(x => x.ApplyAsync(command, token)).Returns(() => {
        Assert.True(inTransaction);
        Assert.Equal(0, saves);
        return Task.FromResult(ServiceResult<ShiftMutationResult>.Success(new([command.RangeStartDate])));
      });
      context.UnitOfWork.Setup(x => x.SaveChangesAsync(token)).Returns(() => {
        Assert.True(inTransaction);
        return Task.FromResult(++saves);
      });
      context.Calculation.Setup(x => x.RecalculateForDatesAsync(command.UserId,
        It.Is<HashSet<DateOnly>>(dates => dates.SetEquals(new[] { command.RangeStartDate })), token))
        .Callback(() => { Assert.True(inTransaction); Assert.Equal(1, saves); }).Returns(Task.CompletedTask);
      context.Shifts.Setup(x => x.GetByIdsAndRangeAsync(command.UserId, command.RangeStartDate, command.RangeEndDate, token))
        .Returns(() => { Assert.True(inTransaction); Assert.Equal(2, saves); return Task.FromResult<IEnumerable<Jenian.Domain.Entities.UserShift>>([]); });
      context.Summaries.Setup(x => x.GetByIdAndRangeAsync(command.UserId, command.RangeStartDate, command.RangeEndDate, token))
        .Returns(() => { Assert.True(inTransaction); Assert.Equal(2, saves); return Task.FromResult<IEnumerable<Jenian.Domain.Entities.UserDailyPaySummary>>([]); });

      // Act

      var result = await context.CreateService().SaveShiftsAsync(command, token);

      // Assert
      Assert.True(result.IsSuccess);
      Assert.False(inTransaction);
      context.UnitOfWork.Verify(x => x.SaveChangesAsync(token), Times.Exactly(2));
      context.Mutation.VerifyAll();
      context.Calculation.VerifyAll();
      context.Shifts.VerifyAll();
      context.Summaries.VerifyAll();
    }

    [Fact]
    public async Task SaveShiftsAsync_ValidationFailure_ReturnsErrorsWithoutStartingTransaction() {
      // Arrange
      var context = new ShiftServiceContext();
      var command = CreateCommand();
      context.Validator.Setup(x => x.ValidateSaveShifts(command.ShiftDtos, command.RangeStartDate, command.RangeEndDate))
        .Returns(ValidationResult.Failure(["invalid shift"]));

      // Act

      var result = await context.CreateService().SaveShiftsAsync(command, CancellationToken.None);

      // Assert
      Assert.False(result.IsSuccess);
      Assert.Equal(["invalid shift"], result.Errors);
      context.UnitOfWork.VerifyNoOtherCalls();
      context.Mutation.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveShiftsAsync_WorkflowFailure_DoesNotLoadSuccessResponse(bool recalculationFails) {
      // Arrange
      var context = new ShiftServiceContext();
      var command = CreateCommand();
      context.Validator.Setup(x => x.ValidateSaveShifts(command.ShiftDtos, command.RangeStartDate, command.RangeEndDate))
        .Returns(ValidationResult.Success());
      context.UnitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<ServiceResult<ShiftSummaryResult>>>>(), CancellationToken.None))
        .Returns((Func<CancellationToken, Task<ServiceResult<ShiftSummaryResult>>> operation, CancellationToken ct) => operation(ct));
      context.Mutation.Setup(x => x.ApplyAsync(command, CancellationToken.None)).ReturnsAsync(recalculationFails
        ? ServiceResult<ShiftMutationResult>.Success(new([command.RangeStartDate]))
        : ServiceResult<ShiftMutationResult>.Failure(["mutation failed"]));
      if (recalculationFails) {
        context.UnitOfWork.Setup(x => x.SaveChangesAsync(CancellationToken.None)).ReturnsAsync(1);
        context.Calculation.Setup(x => x.RecalculateForDatesAsync(command.UserId, It.IsAny<HashSet<DateOnly>>(), CancellationToken.None))
          .ThrowsAsync(new InvalidOperationException("recalculation failed"));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => context.CreateService().SaveShiftsAsync(command, CancellationToken.None));
        Assert.Equal("recalculation failed", error.Message);
        context.UnitOfWork.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
      } else {
        // Act
        var result = await context.CreateService().SaveShiftsAsync(command, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(["mutation failed"], result.Errors);
        context.UnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        context.Calculation.VerifyNoOtherCalls();
      }
      context.Shifts.VerifyNoOtherCalls();
      context.Summaries.VerifyNoOtherCalls();
    }
  }
}
