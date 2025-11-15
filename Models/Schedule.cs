namespace HMS.Models
{
    public class Schedule
    {
        public int Id { get; set; }

        /// <summary>
        /// The staff member (doctor) this schedule applies to
        /// </summary>
        public int StaffId { get; set; }

        /// <summary>
        /// The specific date of this schedule
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Work start time (e.g., 09:00)
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Work end time (e.g., 17:00)
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Optional break start time (e.g., 12:00)
        /// </summary>
        public TimeSpan? BreakStart { get; set; }
                
        /// <summary>
        /// Optional break end time (e.g., 13:00)
        /// </summary>
        public TimeSpan? BreakEnd { get; set; }

        /// <summary>
        /// Type of shift: Morning, Afternoon, Evening, Night, Full Day, On-Call
        /// </summary>
        public string ShiftType { get; set; }

        /// <summary>
        /// Status: Scheduled, Active, Completed, Cancelled
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Whether slots have been generated for this schedule
        /// </summary>
        public bool SlotsGenerated { get; set; }

        /// <summary>
        /// Optional notes about this schedule
        /// </summary>
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Staff Staff { get; set; }
        public virtual TimeReport? TimeReport { get; set; }
        public virtual ICollection<AppointmentSlot> AppointmentSlots { get; set; } = new List<AppointmentSlot>();

        // Keep for backwards compatibility but these should use AppointmentSlot now
        [Obsolete("Use AppointmentSlot instead. This is kept for backwards compatibility.")]
        public virtual Appointment? Appointment { get; set; }
    }
}
