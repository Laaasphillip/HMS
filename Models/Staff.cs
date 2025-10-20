namespace HMS.Models
{
    public class Staff
    {
        public int Id { get; set; }
        public string ContractForm { get; set; }
        public string Department { get; set; }
        public decimal Taxes { get; set; }
        public string Bankdetails { get; set; }
        public int Vacationdays { get; set; }
        public int UserId { get; set; }
        public string Specialization { get; set; }
        public decimal HourlyRate { get; set; }
        public DateTime HiredDate { get; set; } = DateTime.UtcNow;
    }
}
