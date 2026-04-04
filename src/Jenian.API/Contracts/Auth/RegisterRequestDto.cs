using System.ComponentModel.DataAnnotations;

namespace Jenian.API.Contracts.Auth
{
  public class RegisterRequestDto
  {
    [Required]
    [DataType(DataType.EmailAddress)]
    public required string Email { get; set; }

    [Required]
    [DataType(DataType.Text)]
    public required string UserName { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public required string Password { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public required string ConfirmPassword { get; set; }


  }
}
