using System.ComponentModel.DataAnnotations;

namespace JenianAPI.Dtos
{
  public class RegisterRequestDto
  {
    [Required]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }


  }
}
