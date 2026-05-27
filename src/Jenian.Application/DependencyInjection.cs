using Jenian.Application.Features.Payroll;
using Jenian.Application.Features.PaySummaries.Services;
using Jenian.Application.Features.Shifts.Services;
using Jenian.Application.Features.Shifts.Validations;
using Microsoft.Extensions.DependencyInjection;

namespace Jenian.Application
{
  public static class DependencyInjection
  {

    public static IServiceCollection AddApplication(this IServiceCollection services) {
      services.AddScoped<IShiftService, ShiftService>();
      services.AddScoped<IShiftValidator, ShiftValidator>();
      services.AddScoped<IPayCalculationService, PayCalculationService>();
      services.AddScoped<IAwardRateService, PharmacyAwardRateService>();
      services.AddScoped<IPublicHolidayService, PublicHolidayService>();
      services.AddScoped<IPayCalculator, PayCalculator>();

      return services;
    }

  }
}
