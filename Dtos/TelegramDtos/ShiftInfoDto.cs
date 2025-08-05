namespace JenianAPI.Dtos.TelegramDtos
{
  public class ShiftInfoDto
  {
    public string Name { get; set; }
    public DateTime ShiftDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Location { get; set; }
    public string RawOutput { get; set; } // For debugging if AI response was plain text
  }
}
