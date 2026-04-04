using System.ComponentModel.DataAnnotations;

namespace Jenian.API.Contracts.Auth
{
  public class LoginRequestDto
  {
    [Required]
    [DataType(DataType.Text)]
    public required string UserName { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public required string Password
    {
      get; set;
    }
  }
}
