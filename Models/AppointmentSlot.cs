namespace HMS.Models;

public class AppointmentSlot
{
    public int Id { get; set; }

    /// <summary>
    /// Reference to the schedule this slot was generated from
    /// </summary>
    public int ScheduleId { get; set; }

    /// <summary>
    /// The staff member (doctor) for this slot
    /// </summary>
    public int StaffId { get; set; }

    /// <summary>
    /// The specific date of this slot
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Start time of the slot (e.g., 09:00)
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// End time of the slot (e.g., 09:30)
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Status: Available, Booked, Blocked, Cancelled
    /// </summary>
    public string Status { get; set; } = "Available";

    /// <summary>
    /// Maximum number of patients that can book this slot
    /// </summary>
    public int MaxCapacity { get; set; }

    /// <summary>
    /// Current number of bookings for this slot
    /// </summary>
    public int CurrentBookings { get; set; }

    /// <summary>
    /// Optional notes about this specific slot
    /// </summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual Schedule Schedule { get; set; }
    public virtual Staff Staff { get; set; }
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
