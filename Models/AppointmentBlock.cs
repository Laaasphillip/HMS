namespace HMS.Models;

public class AppointmentBlock
{
    public int Id { get; set; }

    /// <summary>
    /// The staff member this block applies to
    /// </summary>
    public int StaffId { get; set; }

    /// <summary>
    /// Date of the block
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Start time of the blocked period
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// End time of the blocked period
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Reason for blocking: Meeting, Emergency, Leave, Personal, Other
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    /// Additional notes about the block
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this block is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Who created this block
    /// </summary>
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual Staff Staff { get; set; }
}
