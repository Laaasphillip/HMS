using HMS.Data;

namespace HMS.Models
{
    public class Staff
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string ContractForm { get; set; }
        public string Department { get; set; }
        public decimal Taxes { get; set; }
        public string Bankdetails { get; set; }
        public int Vacationdays { get; set; }
        public string Specialization { get; set; }
        public decimal HourlyRate { get; set; }
        public DateTime HiredDate { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; } // One-to-One with AspNetUsers
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>(); // One-to-Many
        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>(); // One-to-Many
        public ICollection<TimeReport> TimeReports { get; set; } = new List<TimeReport>(); // One-to-Many
    }
}
