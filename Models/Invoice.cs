namespace HMS.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int AppointmentId { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Patient Patient { get; set; }
        public virtual Appointment Appointment { get; set; }
    }
}
