namespace HMS.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        /// <summary>
        /// The patient who booked this appointment
        /// </summary>
        public int PatientId { get; set; }

        /// <summary>
        /// The staff member (doctor) for this appointment
        /// </summary>
        public int StaffId { get; set; }

        /// <summary>
        /// The specific time slot that was booked (NEW - Primary way to track slot)
        /// </summary>
        public int? AppointmentSlotId { get; set; }

        /// <summary>
        /// Legacy: Reference to schedule (keep for backwards compatibility)
        /// </summary>
        public int? ScheduleId { get; set; }

        /// <summary>
        /// Date and time of the appointment
        /// </summary>
        public DateTime AppointmentDate { get; set; }

        /// <summary>
        /// Duration of the appointment in minutes
        /// </summary>
        public int DurationMinutes { get; set; }

        /// <summary>
        /// Status: Scheduled, Confirmed, InProgress, Completed, Cancelled, NoShow
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Reason for the appointment
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Additional notes from patient or staff
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Who created this appointment
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Consultation fee/price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// When the appointment was booked
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the appointment was confirmed (if applicable)
        /// </summary>
        public DateTime? ConfirmedAt { get; set; }

        /// <summary>
        /// When the appointment was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; }
        public virtual Staff Staff { get; set; }
        public virtual AppointmentSlot? AppointmentSlot { get; set; }
        public virtual Schedule? Schedule { get; set; }
        public virtual Invoice? Invoice { get; set; }

    }
}
