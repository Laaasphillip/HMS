namespace HMS.Models
{
    public class TimeReport
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public int ScheduleId { get; set; }
        public DateTime ClockIn { get; set; }
        public DateTime ClockOut { get; set; }
        public decimal HoursWorked { get; set; }
        public string ActivityType { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }


        public virtual Staff Staff { get; set; }
        public virtual Schedule Schedule { get; set; }

    }
}
