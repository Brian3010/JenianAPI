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

      return services;
    }

  }
}
