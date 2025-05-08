using System.ComponentModel.DataAnnotations;

namespace JenianAPI.Dtos
{
  public class LoginRequestDto
  {
    [Required]
    [DataType(DataType.EmailAddress)]
    public required string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public required string Password
    {
      get; set;
    }

    public string? DeviceName { get; set; }         // Optional for now
    //public string? DeviceIpAddress { get; set; }    // Optional
  }
}
