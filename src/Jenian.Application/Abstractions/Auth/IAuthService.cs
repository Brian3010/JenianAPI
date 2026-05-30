using Jenian.Application.Common;
using Jenian.Application.Features.Auth.Commands;
using Jenian.Application.Features.Auth.Dtos;

namespace Jenian.Application.Abstractions.Auth
{
  public interface IAuthService
  {
    Task<ServiceResult<AuthResultDto>> LoginAsync(LoginCommand command, CancellationToken cancellationToken);
    Task<ServiceResult<AuthResultDto>> RefreshTokenAsync(RefreshTokenCommand command, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> LogoutAsync(LogoutCommand? command, CancellationToken cancellationToken);

    Task<ServiceResult<RegisterResultDto>> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken);

    Task<ServiceResult<RequestResetPasswordDto>> RequestPasswordResetAsync(string email, CancellationToken cancellationToken);


    Task<ServiceResult<bool>> HasTelegramConnectedAsync(string userId, CancellationToken cancellationToken);


  }
}
