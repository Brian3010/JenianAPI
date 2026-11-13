using Moq;
using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Common;
using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.PaySummaries.Services;
using Jenian.Application.Features.Shifts.Commands;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Application.Features.Shifts.Services;
using Jenian.Application.Features.Shifts.Validations;
using Jenian.Domain.Entities;

namespace Jenian.Tests.Application.Shifts.Services
{
  public class ShiftMutationServiceTests
  {
    [Fact]
    public async Task ApplyAsync_MixedChanges_ReturnsEveryAffectedWorkDate() {
      // Arrange
      const string userId = "user-123";
      var shiftToUpdate = CreateStoredShift(
        userId,
        new DateTimeOffset(2026, 9, 13, 22, 0, 0, TimeSpan.Zero));
      var shiftToDelete = CreateStoredShift(
        userId,
        new DateTimeOffset(2026, 9, 15, 22, 0, 0, TimeSpan.Zero));
      var repository = new Mock<IShiftRepository>(MockBehavior.Strict);
      using var cancellation = new CancellationTokenSource();
      var token = cancellation.Token;
      repository.Setup(x => x.GetByIdsForUserAsync(userId, It.Is<IEnumerable<Guid>>(ids =>
        ids.ToHashSet().SetEquals(new[] { shiftToUpdate.Id, shiftToDelete.Id })), token))
        .ReturnsAsync([shiftToUpdate, shiftToDelete]);
      List<UserShift> added = [];
      repository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<UserShift>>(), token))
        .Callback<IEnumerable<UserShift>, CancellationToken>((items, _) => added.AddRange(items)).Returns(Task.CompletedTask);
      repository.Setup(x => x.RemoveByIdsForUserAsync(userId,
        It.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(new[] { shiftToDelete.Id })), token)).Returns(Task.CompletedTask);
      var service = new ShiftMutationService(repository.Object);
      var command = new SaveShiftsCommand {
        UserId = userId,
        RangeStartDate = new DateOnly(2026, 9, 14),
        RangeEndDate = new DateOnly(2026, 9, 20),
        ShiftDtos = [
          CreateShiftDto(
            shiftToUpdate.Id,
            new DateTimeOffset(2026, 9, 14, 22, 0, 0, TimeSpan.Zero)),
          CreateShiftDto(
            null,
            new DateTimeOffset(2026, 9, 16, 22, 0, 0, TimeSpan.Zero))
        ],
        DeletedShiftIds = [shiftToDelete.Id]
      };

      // Act

      var result = await service.ApplyAsync(command, token);

      // Assert
      Assert.True(result.IsSuccess);
      Assert.NotNull(result.Data);
      Assert.Equal(
        [
          new DateOnly(2026, 9, 14),
          new DateOnly(2026, 9, 15),
          new DateOnly(2026, 9, 16),
          new DateOnly(2026, 9, 17)
        ],
        result.Data.AffectedWorkDates.OrderBy(date => date));
      Assert.Equal(new DateTimeOffset(2026, 9, 14, 22, 0, 0, TimeSpan.Zero), shiftToUpdate.StartAt);
      repository.VerifyAll();
      var created = Assert.Single(added);
      var expected = command.ShiftDtos[1];
      Assert.Equal((command.UserId, expected.StartAt, expected.EndAt, expected.TimeZoneId,
        expected.UnpaidBreakMinutes, expected.PaidBreakMinutes, expected.EntryType, expected.EmploymentType, ShiftSource.Manual),
        (created.UserId, created.StartAt, created.EndAt, created.TimeZoneId,
        created.UnpaidBreakMinutes, created.PaidBreakMinutes, created.EntryType, created.EmploymentType, created.Source));
      var update = command.ShiftDtos[0];
      Assert.Equal((update.StartAt, update.EndAt, update.TimeZoneId, update.UnpaidBreakMinutes,
        update.PaidBreakMinutes, update.EntryType, update.EmploymentType),
        (shiftToUpdate.StartAt, shiftToUpdate.EndAt, shiftToUpdate.TimeZoneId, shiftToUpdate.UnpaidBreakMinutes,
        shiftToUpdate.PaidBreakMinutes, shiftToUpdate.EntryType, shiftToUpdate.EmploymentType));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApplyAsync_MissingExistingId_ReturnsFailureBeforeMutation(bool delete) {
      // Arrange
      var missingId = Guid.NewGuid();
      var repository = new Mock<IShiftRepository>(MockBehavior.Strict);
      repository.Setup(x => x.GetByIdsForUserAsync("user-123", It.IsAny<IEnumerable<Guid>>(), CancellationToken.None)).ReturnsAsync([]);
      var service = new ShiftMutationService(repository.Object);
      var command = new SaveShiftsCommand {
        UserId = "user-123",
        RangeStartDate = new DateOnly(2026, 9, 14),
        RangeEndDate = new DateOnly(2026, 9, 20),
        ShiftDtos = delete ? [] : [CreateShiftDto(missingId, DateTimeOffset.Parse("2026-09-13T22:00:00Z"))],
        DeletedShiftIds = delete ? [missingId] : []
      };

      // Act

      var result = await service.ApplyAsync(command, CancellationToken.None);

      // Assert
      Assert.False(result.IsSuccess);
      Assert.Contains(missingId.ToString(), Assert.Single(result.Errors));
      repository.VerifyAll();
      repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAsync_UpdateAndDeleteSameId_ReturnsFailureWithoutRepositoryCalls() {
      // Arrange
      var id = Guid.NewGuid();
      var repository = new Mock<IShiftRepository>(MockBehavior.Strict);
      var command = CreateEmptyCommand();
      command.ShiftDtos = [CreateShiftDto(id, DateTimeOffset.Parse("2026-09-13T22:00:00Z"))];
      command.DeletedShiftIds = [id];

      // Act

      var result = await new ShiftMutationService(repository.Object).ApplyAsync(command, CancellationToken.None);

      // Assert
      Assert.False(result.IsSuccess);
      Assert.Contains(id.ToString(), Assert.Single(result.Errors));
      repository.VerifyNoOtherCalls();
    }
    private static SaveShiftsCommand CreateEmptyCommand() => new() {
      UserId = "user-123",
      RangeStartDate = new DateOnly(2026, 9, 14),
      RangeEndDate = new DateOnly(2026, 9, 20),
      ShiftDtos = []
    };

    private static UserShift CreateStoredShift(string userId, DateTimeOffset startAt) => new() {
      UserId = userId,
      StartAt = startAt,
      EndAt = startAt.AddHours(8),
      TimeZoneId = "Australia/Melbourne",
      EmploymentType = EmploymentType.FullTime,
      EntryType = ShiftEntryType.Worked
    };

    private static ShiftDto CreateShiftDto(Guid? id, DateTimeOffset startAt) => new() {
      Id = id,
      StartAt = startAt,
      EndAt = startAt.AddHours(8),
      TimeZoneId = "Australia/Melbourne",
      EmploymentType = EmploymentType.PartTime,
      PaidBreakMinutes = 15,
      UnpaidBreakMinutes = 30,
      EntryType = ShiftEntryType.Leave
    };

  }
}
