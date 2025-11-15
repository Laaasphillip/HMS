using HMS.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HMS.Models
{
    public class Staff
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ContractForm { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 1)]
        public decimal Taxes { get; set; } = 0.30m;

        public string? Bankdetails { get; set; }

        [Range(0, 365)]
        public int Vacationdays { get; set; } = 20;

        [StringLength(100)]
        public string? Specialization { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal HourlyRate { get; set; }

        [Required]
        public DateTime HiredDate { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; }
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>(); 
        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>(); 
        public ICollection<TimeReport> TimeReports { get; set; } = new List<TimeReport>();
/*        public ICollection<Leave> Leaves { get; set; } = new List<Leave>();*/
    }
}
