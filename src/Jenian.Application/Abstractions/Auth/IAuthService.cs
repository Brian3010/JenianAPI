using Jenian.Application.Common;
using Jenian.Application.Features.Auth.Commands;
using Jenian.Application.Features.Auth.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Abstractions.Auth
{
  public interface IAuthService
  {
    Task<ServiceResult<AuthResultDto>> LoginAsync(LoginCommand command, CancellationToken cancellationToken);
    Task<ServiceResult<AuthResultDto>> RefreshTokenAsync(RefreshTokenCommand command, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> LogoutAsync(LogoutCommand? command, CancellationToken cancellationToken);

    Task<ServiceResult<RegisterResultDto>> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken);


  }
}
