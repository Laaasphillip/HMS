using HMS.Data;
using HMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HMS.Services
{
    public class InvoiceService
    {
        private readonly ApplicationDbContext _context;
        public InvoiceService (ApplicationDbContext context)
        {
            _context = context;
        }

        // Get invoices with related patient & appointment
        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Appointment)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Transactions)
                .ToListAsync();
        }

        // Get invoice by ID
        public async Task<Invoice?> GetInvoiceIdAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Appointment)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Transactions)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        // Create invoice
        public async Task AddInvoiceAsync(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
        }

        // Delete invoice
        public async Task DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }
        }
    }
}
