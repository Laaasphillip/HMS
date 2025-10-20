namespace HMS.Models
{
    public class InvoiceItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public int AppointmentId { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        public virtual Invoice Invoice { get; set; }
        public virtual Appointment Appointment { get; set; }
    }
}
