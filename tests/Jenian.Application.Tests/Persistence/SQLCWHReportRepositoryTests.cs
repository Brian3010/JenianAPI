using Jenian.Domain.Entities;
using Jenian.Infrastructure.Persistence.App;
using Jenian.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jenian.Application.Tests.Persistence;

public class SQLCWHReportRepositoryTests
{
  [Fact]
  public async Task AddOrUpdateEodReportAsync_AddsReport_WhenNoReportExistsToday() {
    await using var dbContext = CreateDbContext();
    var repository = CreateRepository(dbContext);
    var incomingReport = CreateReport("user-1", "new delivery");

    var reportId = await repository.AddOrUpdateEodReportAsync(
      incomingReport.UserId,
      incomingReport,
      CancellationToken.None);

    var savedReport = await dbContext.EodReports.SingleAsync();
    Assert.Equal(incomingReport.Id, reportId);
    Assert.Equal(incomingReport.Id, savedReport.Id);
    Assert.Equal("new delivery", savedReport.Delivery);
  }

  [Fact]
  public async Task AddOrUpdateEodReportAsync_UpdatesTodaysReport_AndReturnsExistingId() {
    await using var dbContext = CreateDbContext();
    var existingReport = CreateReport("user-1", "old delivery");
    dbContext.EodReports.Add(existingReport);
    await dbContext.SaveChangesAsync();

    var repository = CreateRepository(dbContext);
    var incomingReport = CreateReport("user-1", "updated delivery");

    var reportId = await repository.AddOrUpdateEodReportAsync(
      incomingReport.UserId,
      incomingReport,
      CancellationToken.None);

    var savedReport = await dbContext.EodReports.SingleAsync();
    Assert.Equal(existingReport.Id, reportId);
    Assert.Equal(existingReport.Id, savedReport.Id);
    Assert.Equal("updated delivery", savedReport.Delivery);
  }

  private static JenianDbContext CreateDbContext() {
    var options = new DbContextOptionsBuilder<JenianDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new JenianDbContext(options);
  }

  private static SQLCWHReportRepository CreateRepository(JenianDbContext dbContext) {
    return new SQLCWHReportRepository(
      dbContext,
      NullLogger<SQLCWHReportRepository>.Instance);
  }

  private static EodReport CreateReport(string userId, string delivery) {
    return new EodReport {
      UserId = userId,
      Delivery = delivery,
      GeneralCheck = new GeneralCheck {
        FreeTrolleys = "Done",
        FreeCages = "Done",
        NumOfMyPals = "1",
        NumOfFragKeys = "1",
        NumOfLiftPasses = "1",
        NumOfAugmodos = "1"
      }
    };
  }
}
