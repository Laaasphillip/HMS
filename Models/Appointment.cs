namespace HMS.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int StaffId { get; set; }
        public int ScheduleId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public string CreatedBy { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Patient Patient { get; set; }
        public virtual Staff Staff { get; set; }
        public virtual Schedule Schedule { get; set; }
        public virtual Invoice Invoice { get; set; }

    }
}
