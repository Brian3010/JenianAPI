using Jenian.API.Contracts.Common;
using Jenian.API.Controllers;
using Jenian.Application.Common;
using Jenian.Application.Features.Shifts.Commands;
using Jenian.Application.Features.Shifts.Dtos;
using Jenian.Application.Features.Shifts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Jenian.Application.Tests.Controllers;

public class CWHControllerCurrentPayCycleTests
{
  [Fact]
  public async Task GetCurrentPayCycleShifts_MissingSetting_ReturnsOkSetupState() {
    const string userId = "authenticated-user";
    var shiftService = new StubShiftService {
      CurrentCycleResult = ServiceResult<CurrentPayCycleShiftSummaryResult>.Success(
        new CurrentPayCycleShiftSummaryResult { HasPayCycleSettings = false })
    };
    var controller = new CWHController(
      null!, null!, null!, null!, null!, null!, shiftService) {
      ControllerContext = new ControllerContext {
        HttpContext = new DefaultHttpContext {
          User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, userId)],
            "test"))
        }
      }
    };

    var actionResult = await controller.GetCurrentPayCycleShifts(CancellationToken.None);

    var ok = Assert.IsType<OkObjectResult>(actionResult);
    var response = Assert.IsType<ApiResponse<CurrentPayCycleShiftSummaryResult>>(ok.Value);
    Assert.True(response.Success);
    Assert.NotNull(response.Data);
    Assert.False(response.Data.HasPayCycleSettings);
    Assert.Empty(response.Errors);
    Assert.Equal(userId, shiftService.ReceivedCommand?.UserId);
  }

  [Fact]
  public async Task GetCurrentPayCycleSummary_ReturnsAuthenticatedUsersSummary() {
    const string userId = "authenticated-user";
    var shiftService = new StubShiftService {
      CurrentCycleResult = ServiceResult<CurrentPayCycleShiftSummaryResult>.Success(null),
      CurrentSummaryResult = ServiceResult<CurrentPayCycleSummaryResult>.Success(
        new CurrentPayCycleSummaryResult {
          HasPayCycleSettings = true,
          PayCycle = "Fortnightly",
          StartDate = new DateOnly(2026, 9, 7),
          EndDate = new DateOnly(2026, 9, 20),
          ShiftCount = 3,
          EstimatedGrossPay = 1245.50m
        })
    };
    var controller = new CWHController(
      null!, null!, null!, null!, null!, null!, shiftService) {
      ControllerContext = new ControllerContext {
        HttpContext = new DefaultHttpContext {
          User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, userId)],
            "test"))
        }
      }
    };

    var actionResult = await controller.GetCurrentPayCycleSummary(CancellationToken.None);

    var ok = Assert.IsType<OkObjectResult>(actionResult);
    var response = Assert.IsType<ApiResponse<CurrentPayCycleSummaryResult>>(ok.Value);
    Assert.True(response.Success);
    Assert.NotNull(response.Data);
    Assert.Equal(3, response.Data.ShiftCount);
    Assert.Equal(1245.50m, response.Data.EstimatedGrossPay);
    Assert.Equal(userId, shiftService.ReceivedSummaryCommand?.UserId);
  }

  private sealed class StubShiftService : IShiftService
  {
    public required ServiceResult<CurrentPayCycleShiftSummaryResult> CurrentCycleResult { get; init; }
    public ServiceResult<CurrentPayCycleSummaryResult> CurrentSummaryResult { get; init; } =
      ServiceResult<CurrentPayCycleSummaryResult>.Success(null);
    public GetCurrentPayCycleShiftsCommand? ReceivedCommand { get; private set; }
    public GetCurrentPayCycleSummaryCommand? ReceivedSummaryCommand { get; private set; }

    public Task<ServiceResult<CurrentPayCycleShiftSummaryResult>> GetCurrentPayCycleShiftsAsync(
      GetCurrentPayCycleShiftsCommand command,
      CancellationToken cancellationToken) {
      ReceivedCommand = command;
      return Task.FromResult(CurrentCycleResult);
    }

    public Task<ServiceResult<CurrentPayCycleSummaryResult>> GetCurrentPayCycleSummaryAsync(
      GetCurrentPayCycleSummaryCommand command,
      CancellationToken cancellationToken) {
      ReceivedSummaryCommand = command;
      return Task.FromResult(CurrentSummaryResult);
    }

    public Task<ServiceResult<ShiftSummaryResult>> SaveShiftsAsync(SaveShiftsCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<ServiceResult<ShiftSummaryResult>> GetShiftsByUserAndDateRangeAsync(GetShiftsForUserByDateRangeCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<ServiceResult<PayCycleSettingsDto>> GetCurrentPayCycleSettingsForUserAsync(string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<ServiceResult<PayCycleSettingsDto>> UpdatePayCycleSettingsForUserAsync(CreatePayCycleSettingsCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
  }
}
