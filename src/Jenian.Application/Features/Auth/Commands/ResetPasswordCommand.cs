namespace Jenian.Application.Features.Auth.Commands
{
  public class ResetPasswordCommand
  {
    public required string UserEmail { get; set; }
    public required string EmailToken { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmPassword { get; set; }
  }
}
