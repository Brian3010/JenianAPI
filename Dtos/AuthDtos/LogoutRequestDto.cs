using System.ComponentModel.DataAnnotations;

namespace JenianAPI.Dtos.AuthDtos
{
  public class LogoutRequestDto
  {

    [Required]
    public required string DeviceId { get; set; }
  }
}
