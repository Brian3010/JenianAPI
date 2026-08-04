namespace Jenian.Infrastructure.Identity.Options;

public sealed class RegistrationOptions
{
  public const string SectionName = "Registration";

  public string? InviteToken { get; init; }
}