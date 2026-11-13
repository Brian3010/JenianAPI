using Moq;
using Jenian.Application.Abstractions.BackgroundJobs;
using Jenian.Application.Abstractions.Persistence;
using Jenian.Application.Abstractions.Storage;
using Jenian.Infrastructure.BackgroundJobs.JobPayloads;
using Microsoft.Extensions.Logging.Abstractions;
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

namespace Jenian.Tests.API
{
  public class CWHControllerCurrentPayCycleTests
  {
    [Fact]
    public async Task GetCurrentPayCycleShifts_MissingSetting_ReturnsOkSetupState() {
      // Arrange
      const string userId = "authenticated-user";
      var shiftService = new Mock<IShiftService>(MockBehavior.Strict);
      shiftService.Setup(x => x.GetCurrentPayCycleShiftsAsync(It.Is<GetCurrentPayCycleShiftsCommand>(c => c.UserId == userId), CancellationToken.None))
        .ReturnsAsync(ServiceResult<CurrentPayCycleShiftSummaryResult>.Success(
          new CurrentPayCycleShiftSummaryResult { HasPayCycleSettings = false }));
      var controller = new CWHController(
        NullLogger<CWHController>.Instance,
        new Mock<IBackgroundJobQueue<DeliveryWorkerJob>>(MockBehavior.Strict).Object,
        new Mock<ICWHReportRepository>(MockBehavior.Strict).Object,
        new Mock<IBlobStorageService>(MockBehavior.Strict).Object, shiftService.Object) {
        ControllerContext = new ControllerContext {
          HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(
              [new Claim(JwtRegisteredClaimNames.Sub, userId)],
              "test"))
          }
        }
      };

      // Act

      var actionResult = await controller.GetCurrentPayCycleShifts(CancellationToken.None);

      var ok = Assert.IsType<OkObjectResult>(actionResult);
      var response = Assert.IsType<ApiResponse<CurrentPayCycleShiftSummaryResult>>(ok.Value);
      Assert.True(response.Success);
      Assert.NotNull(response.Data);
      Assert.False(response.Data.HasPayCycleSettings);
      Assert.Empty(response.Errors);
      shiftService.VerifyAll();
    }

    [Fact]
    public async Task GetCurrentPayCycleSummary_AuthenticatedUser_ReturnsTheirSummary() {
      // Arrange
      const string userId = "authenticated-user";
      var shiftService = new Mock<IShiftService>(MockBehavior.Strict);
      shiftService.Setup(x => x.GetCurrentPayCycleSummaryAsync(It.Is<GetCurrentPayCycleSummaryCommand>(c => c.UserId == userId), CancellationToken.None))
        .ReturnsAsync(ServiceResult<CurrentPayCycleSummaryResult>.Success(
          new CurrentPayCycleSummaryResult {
            HasPayCycleSettings = true,
            PayCycle = "Fortnightly",
            StartDate = new DateOnly(2026, 9, 7),
            EndDate = new DateOnly(2026, 9, 20),
            ShiftCount = 3,
            EstimatedGrossPay = 1245.50m
          }));
      var controller = new CWHController(
        NullLogger<CWHController>.Instance,
        new Mock<IBackgroundJobQueue<DeliveryWorkerJob>>(MockBehavior.Strict).Object,
        new Mock<ICWHReportRepository>(MockBehavior.Strict).Object,
        new Mock<IBlobStorageService>(MockBehavior.Strict).Object, shiftService.Object) {
        ControllerContext = new ControllerContext {
          HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(
              [new Claim(JwtRegisteredClaimNames.Sub, userId)],
              "test"))
          }
        }
      };

      // Act

      var actionResult = await controller.GetCurrentPayCycleSummary(CancellationToken.None);

      var ok = Assert.IsType<OkObjectResult>(actionResult);
      var response = Assert.IsType<ApiResponse<CurrentPayCycleSummaryResult>>(ok.Value);
      Assert.True(response.Success);
      Assert.NotNull(response.Data);
      Assert.Equal(3, response.Data.ShiftCount);
      Assert.Equal(1245.50m, response.Data.EstimatedGrossPay);
      shiftService.VerifyAll();
    }

  }
}
