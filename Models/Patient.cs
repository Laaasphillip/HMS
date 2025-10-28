using HMS.Data;

namespace HMS.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime Dateofbirth { get; set; }
        public string Address { get; set; }
        public string Contact { get; set; }
        public string BloodGroup { get; set; }
        public DateTime Createdat { get; set; }
        public string Preferences { get; set; }
        public string Interests { get; set; }

        public ApplicationUser User { get; set; } // One-to-One with AspNetUsers
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>(); // One-to-Many
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>(); // One-to-Many

    }
}
