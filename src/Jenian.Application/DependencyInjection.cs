using Jenian.Application.Features.Shifts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Jenian.Application
{
  public static class DependencyInjection
  {

    public static IServiceCollection AddApplication(this IServiceCollection services) {
      services.AddScoped<IShiftService, ShiftService>();

      return services;
    }

  }
}
