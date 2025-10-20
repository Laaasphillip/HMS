namespace HMS.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string TransactionNumber { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Invoice Invoice { get; set; }
    }
}
