using System.ComponentModel.DataAnnotations;

namespace JenianAPI.Dtos.AuthDtos
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

    //[Required]
    //public required string DeviceName { get; set; } // get this from Frontend (best practice)


    //public string? DeviceIpAddress { get; set; }    
  }
}
