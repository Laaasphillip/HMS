namespace HMS.Models;

public class AppointmentSlotConfiguration
{
    public int Id { get; set; }

    /// <summary>
    /// The staff member (doctor) this configuration applies to
    /// </summary>
    public int StaffId { get; set; }

    /// <summary>
    /// Duration of each appointment slot in minutes (e.g., 30)
    /// </summary>
    public int SlotDurationMinutes { get; set; }

    /// <summary>
    /// Buffer time between appointments in minutes (e.g., 5)
    /// </summary>
    public int BufferTimeMinutes { get; set; }

    /// <summary>
    /// Maximum number of patients that can book the same slot (usually 1)
    /// </summary>
    public int MaxPatientsPerSlot { get; set; }

    /// <summary>
    /// How many days in advance patients can book (e.g., 30 days)
    /// </summary>
    public int AdvanceBookingDays { get; set; }

    /// <summary>
    /// Whether this configuration is currently active
    /// </summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Staff Staff { get; set; }
}
