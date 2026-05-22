namespace Jenian.Application.Common
{
  public class ValidationResult
  {
    public bool IsValid { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = [];

    public static ValidationResult Success() {
      return new ValidationResult { IsValid = true, Errors = [] };
    }

    public static ValidationResult Failure(IReadOnlyList<string> errors) {
      return new ValidationResult { IsValid = false, Errors = errors };
    }

  }
}
