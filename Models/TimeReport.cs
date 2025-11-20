namespace HMS.Models
{
    public class TimeReport
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public int? ScheduleId { get; set; }
        public DateTime ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public decimal HoursWorked { get; set; }
        public string ActivityType { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        // showing deviation from Staff scheduled time
        public string ApprovalStatus { get; set; } = "Pending"; // Pending, Approved, Rejected
        public int? LateArrivalMinutes { get; set; } // Minutes late compared to schedule
        public int? EarlyDepartureMinutes { get; set; } // Minutes early compared to schedule
        public string? ApprovedBy { get; set; } // UserId of approving admin
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNotes { get; set; }

        public virtual Staff Staff { get; set; }
        public virtual Schedule Schedule { get; set; }

    }
}