namespace HMS.Models
{
    public class Schedule
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime BreakStart { get; set; }
        public DateTime BreakEnd { get; set; }
        public string ShiftType { get; set; }
        public string Status { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
