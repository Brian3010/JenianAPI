using System.ComponentModel.DataAnnotations;

namespace JenianAPI.Dtos.AuthDtos
{
  public class LogoutRequestDto
  {

    [Required]
    public required string DeviceName { get; set; }

    [Required]
    public required string UserId { get; set; }

  }
}
