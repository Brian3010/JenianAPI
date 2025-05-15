using System.ComponentModel.DataAnnotations;

namespace JenianAPI.Dtos
{
  public class LogoutRequestDto
  {

    [Required]
    public required string DeviceName { get; set; }

    [Required]
    public required string UserId { get; set; }



  }
}
