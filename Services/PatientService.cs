using HMS.Data;
using HMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HMS.Services
{
    public class PatientService
    {
        private readonly ApplicationDbContext _context;

        public PatientService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Patient Management
        public async Task<List<Patient>> GetAllPatientsAsync()
        {
            return await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Appointments)
                .Include(p => p.Invoices)
                .OrderByDescending(p => p.Createdat)
                .ToListAsync();
        }
        #endregion

        #region Staff Management
        public async Task<List<Staff>> GetAllStaffAsync()
        {
            return await _context.Staff
                .Include(s => s.User)
                .OrderBy(s => s.Id)
                .ToListAsync();
        }
        #endregion

        #region Profile Management

        // Read Patient
        public async Task<Patient?> GetPatientProfileAsync(string userId)
        {
            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        // Update Patient
        public async Task<(bool Success, string message)> UpdatePatientProfileAsync(Patient patient)
        {
            try
            {
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();
                return (true, "Profile updated succesfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating profile: {ex.Message}");
            }
        }
        #endregion

        #region Appointment Management
        // Read Appointment
        public async Task<List<Appointment>> GetAppointmentsAsync(int patientId)
        {
            return await _context.Appointments
                .Include(a => a.Staff)
                    .ThenInclude(s => s.User)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        // Create Appointment
        public async Task<(bool Success, string Message)> CreateAppointmentAsync(Appointment appointment)
        {
            try
            {
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                return (true, "Appointment created succesfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating appointment: {ex.Message}");
            }
        }

        // Update Appointment
        public async Task<(bool Success, string Message)> UpdateAppointmentAsync(Appointment appointment)
        {
            try
            {
                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();
                return (true, "Appointment updated succesffully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating appointment: {ex.Message}");
            }
        }

        // Delete Appointment
        public async Task<(bool Success, string Message)> DeleteAppointmentAsync(int appointmentId)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(appointmentId);
                if (appointment == null)
                    return (false, "Appointment not found.");

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                return (true, "Appointment deleted successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting appointment {ex.Message}");
            }
        }
        #endregion

        #region Transaction Management
        // Read Transaction
        public async Task<List<Transaction>> GetTransactionsAsync(int patientId)
        {
            return await _context.Transactions
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Patient)
                        .ThenInclude(p => p.User)
                .Where(t => t.Invoice.PatientId == patientId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        // Create Transaction
        public async Task<(bool Success, string Message)> CreateTransactionAsync(Transaction transaction)
        {
            try
            {
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                return (true, "Transaction added succesfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating transaction: {ex.Message}");
            }
        }
        #endregion

        #region Invoice Management
        // Read Invoice
        public async Task<List<Invoice>> GetInvoicesAsync(int patientId)
        {
            return await _context.Invoices
                .Include(i => i.InvoiceItems)
                .Where(i => i.PatientId == patientId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        // Read invoice by ID
        public async Task<Invoice?> GetInvoiceByIdAsync(int invoiceId)
        {
            return await _context.Invoices
                .Include(i => i.InvoiceItems)
                .Include(i => i.Patient)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
        }
        #endregion

        #region Schedule Management
        public async Task<List<Schedule>> GetAllSchedulesAsync()
        {
            return await _context.Schedules
                .Include(s => s.Staff)
                    .ThenInclude(st => st.User)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<List<Schedule>> GetAllAvailableSchedulesAsync()
        {
            return await _context.Schedules
                .Include(s => s.Staff)
                    .ThenInclude(st => st.User)
                .Where(s => s.IsAvailable == true && s.Status == "Available")
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

/*        public async Task<Schedule> GetScheduleByIdAsync(int scheduleId)
        {
            return await _context.Schedules
                .Include(s => s.Staff)
                    .ThenInclude(st => st.User)
                .FirstOrDefaultAsync(s => s.Id == scheduleId);
        }*/

        public async Task<(bool Success, string Message)> BookAppointmentAsync(int scheduleId, int patientId)
        {
            try
            {
                var schedule = await _context.Schedules
                    .Include(s => s.Staff)
                    .FirstOrDefaultAsync(s => s.Id == scheduleId);

                if (schedule == null)
                {
                    return (false, "Schedule not found.");
                }

                if (!schedule.IsAvailable || schedule.Status == "Booked")
                {
                    return (false, "This time slot is not available for booking.");
                }

                var appointment = new Appointment
                {
                    PatientId = patientId,
                    StaffId = schedule.Staff.Id,
                    AppointmentDate = schedule.StartTime,
                    Status = "Scheduled",
                    CreatedAt = DateTime.Now
                };

                _context.Appointments.Add(appointment);

                schedule.IsAvailable = false;
                schedule.Status = "Booked";
                schedule.Appointment = appointment;

                await _context.SaveChangesAsync();

                return (true, "Appointment booked succesfully.");
            }

            catch (Exception ex)
            {
                return (false, $"Error booking appointment: {ex.Message}");
            }
        }

        public async Task<Schedule?> GetScheduleByIdAsync(int id)
        {
            return await _context.Schedules
                .Include(s => s.Staff)
                    .ThenInclude(st => st.User)
                .Include(s => s.Appointment)
                    .ThenInclude(a => a.Patient)
                        .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<(bool Success, string Message)> BookScheduleAsync(int scheduleId, int patientId)
        {
            try
            {
                var schedule = await _context.Schedules
                    .Include(s => s.Staff)
                    .FirstOrDefaultAsync(s => s.Id == scheduleId);

                if (schedule == null)
                    return (false, "Schedule not found.");

                if (!schedule.IsAvailable || schedule.Status == "Booked")
                    return (false, "This time slot is not available for booking.");

                var appointment = new Appointment
                {
                    PatientId = patientId,
                    StaffId = schedule.StaffId,
                    AppointmentDate = schedule.StartTime,
                    Status = "Scheduled",
                    CreatedAt = DateTime.Now
                };

                _context.Appointments.Add(appointment);

                schedule.IsAvailable = false;
                schedule.Status = "Booked";
                schedule.Appointment = appointment;

                await _context.SaveChangesAsync();
                return (true, "Appointment booked successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error booking appointment: {ex.Message}");
            }
        }
        #endregion
    }
}
