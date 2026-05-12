namespace Jenian.Domain.Entities
{
  public class PayCycleSetting
  {
    public Guid Id { get; private set; } = Guid.NewGuid();
    public required string UserId { get; set; }

    public DateOnly AnchorStartDate { get; set; }
    public required PayCycleType PayCycleType { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
  }

  public enum PayCycleType
  {
    Weekly = 1,
    Fortnightly = 2,
    Monthly = 3
  }
}
