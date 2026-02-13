using JenianAPI.Models.AuthModels;

namespace JenianAPI.Models.BackgroundJobsModels
{

  // Status is like an enum column in SQL.
  // Think of it like a constrained string union in TS: "pending" | "processing" | ...
  public enum JobStatus
  {
    Pending = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3
  }
  public class DeliveryExtractionJob
  {

    public Guid Id { get; set; } = Guid.NewGuid();

    // Optional: a short name/type to know what this job does
    // e.g. "PhotoExtraction", "ShiftExtraction", "DeliveryParsing"
    public string JobType { get; set; } = null!;
    // Output data (usually JSON you’ll send back to frontend)
    public string? Result { get; set; }

    /*
    // Where the input lives (local path, blob URL, etc.)
    public string? InputLocation { get; set; }

    // Extra input data as JSON (for flexible args),
    // similar idea to a "payload" in JS job queues
    public string? InputPayloadJson { get; set; }

    // Output data (usually JSON you’ll send back to frontend)
    public string? ResultJson { get; set; }

    // If it fails, save the error message/stack snapshot
    public string? ErrorMessage { get; set; }
    */
    // Job lifecycle state
    public JobStatus Status { get; set; } = JobStatus.Pending;
    
    // How many times worker tried to run it (for retries)
    public int AttemptCount { get; set; } = 0;

    // Timestamps (always store UTC on server)
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    // Optional: link to the user who triggered the job (from your Identity user)
    public required string UserId { get; set; }



  }
}
