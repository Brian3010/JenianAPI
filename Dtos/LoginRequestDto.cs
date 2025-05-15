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

    [Required]
    public required string DeviceName { get; set; } // get this from Frontend (best practice)


    //public string? DeviceIpAddress { get; set; }    
  }
}
