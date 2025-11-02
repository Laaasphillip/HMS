using HMS.Data;
using HMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace HMS.Services
{
    public class AdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        #region Patient Management

        /// <summary>
        /// Get all patients with their user information
        /// </summary>
        public async Task<List<Patient>> GetAllPatientsAsync()
        {
            return await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Appointments)
                .Include(p => p.Invoices)
                .OrderByDescending(p => p.Createdat)
                .ToListAsync();
        }

        /// <summary>
        /// Get patient by ID
        /// </summary>
        public async Task<Patient?> GetPatientByIdAsync(int patientId)
        {
            return await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Staff)
                        .ThenInclude(s => s.User)
                .Include(p => p.Invoices)
                .FirstOrDefaultAsync(p => p.Id == patientId);
        }

        /// <summary>
        /// Create a new patient with user account
        /// </summary>
        public async Task<(bool Success, string Message, Patient? Patient)> CreatePatientAsync(
            string email,
            string password,
            string firstName,
            string lastName,
            DateTime dateOfBirth,
            string address,
            string contact,
            string bloodGroup,
            string? personalNumber,
            string preferences = "",
            string interests = "")
        {
            try
            {
                // Create the user account first
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = "Patient",
                    PersonalNumber = personalNumber,
                    EmailConfirmed = true // Auto-confirm for admin-created accounts
                };

                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, $"Failed to create user account: {errors}", null);
                }

                // Create the patient record
                var patient = new Patient
                {
                    UserId = user.Id,
                    Dateofbirth = dateOfBirth,
                    Address = address,
                    Contact = contact,
                    BloodGroup = bloodGroup,
                    Createdat = DateTime.UtcNow,
                    Preferences = preferences,
                    Interests = interests,
                    User = user
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                return (true, "Patient created successfully", patient);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating patient: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Update an existing patient
        /// </summary>
        public async Task<bool> UpdatePatientAsync(Patient patient)
        {
            try
            {
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating patient: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete a patient and their user account
        /// </summary>
        public async Task<(bool Success, string Message)> DeletePatientAsync(int patientId)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .Include(p => p.Appointments)
                    .Include(p => p.Invoices)
                    .FirstOrDefaultAsync(p => p.Id == patientId);

                if (patient == null)
                    return (false, "Patient not found");

                // Check if patient has appointments
                if (patient.Appointments.Any())
                    return (false, "Cannot delete patient with existing appointments");

                // Check if patient has invoices
                if (patient.Invoices.Any())
                    return (false, "Cannot delete patient with existing invoices");

                // Remove patient (cascade will delete user due to FK configuration)
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();

                return (true, "Patient deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting patient: {ex.Message}");
            }
        }

        #endregion

        #region Staff Management

        /// <summary>
        /// Get all staff members with their user information
        /// </summary>
        public async Task<List<Staff>> GetAllStaffAsync()
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Appointments)
                .Include(s => s.Schedules)
                .OrderByDescending(s => s.HiredDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get staff by ID
        /// </summary>
        public async Task<Staff?> GetStaffByIdAsync(int staffId)
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Appointments)
                    .ThenInclude(a => a.Patient)
                        .ThenInclude(p => p.User)
                .Include(s => s.Schedules)
                .Include(s => s.TimeReports)
                .FirstOrDefaultAsync(s => s.Id == staffId);
        }

        /// <summary>
        /// Create a new staff member with user account
        /// </summary>
        public async Task<(bool Success, string Message, Staff? Staff)> CreateStaffAsync(
            string email,
            string password,
            string firstName,
            string lastName,
            string role,
            string contractForm,
            string department,
            decimal hourlyRate,
            string specialization = "",
            decimal taxes = 0,
            string bankDetails = "",
            int vacationDays = 25,
            string personalNumber = "")
        {
            try
            {
                // Create the user account first
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = role,
                    PersonalNumber = personalNumber,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, $"Failed to create user account: {errors}", null);
                }

                // Create the staff record
                var staff = new Staff
                {
                    UserId = user.Id,
                    ContractForm = contractForm,
                    Department = department,
                    Taxes = taxes,
                    Bankdetails = bankDetails,
                    Vacationdays = vacationDays,
                    Specialization = specialization,
                    HourlyRate = hourlyRate,
                    HiredDate = DateTime.UtcNow,
                    User = user
                };

                _context.Staff.Add(staff);
                await _context.SaveChangesAsync();

                return (true, "Staff member created successfully", staff);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating staff: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Update an existing staff member
        /// </summary>
        public async Task<bool> UpdateStaffAsync(Staff staff)
        {
            try
            {
                _context.Staff.Update(staff);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating staff: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete a staff member and their user account
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteStaffAsync(int staffId)
        {
            try
            {
                var staff = await _context.Staff
                    .Include(s => s.User)
                    .Include(s => s.Appointments)
                    .Include(s => s.Schedules)
                    .FirstOrDefaultAsync(s => s.Id == staffId);

                if (staff == null)
                    return (false, "Staff member not found");

                // Check if staff has appointments
                if (staff.Appointments.Any())
                    return (false, "Cannot delete staff member with existing appointments");

                // Check if staff has schedules
                if (staff.Schedules.Any())
                    return (false, "Cannot delete staff member with existing schedules");

                // Remove staff (cascade will delete user due to FK configuration)
                _context.Staff.Remove(staff);
                await _context.SaveChangesAsync();

                return (true, "Staff member deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting staff: {ex.Message}");
            }
        }

        #endregion

        #region Schedule Management

        /// <summary>
        /// Get all schedules with staff information
        /// </summary>
        public async Task<List<Schedule>> GetAllSchedulesAsync()
        {
            return await _context.Schedules
                .Include(s => s.Staff)
                    .ThenInclude(st => st.User)
                .Include(s => s.Appointment)
                .Include(s => s.TimeReport)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        /// <summary>
        /// Get schedule by ID
        /// </summary>
        public async Task<Schedule?> GetScheduleByIdAsync(int scheduleId)
        {
            return await _context.Schedules
                .Include(s => s.Staff)
                    .ThenInclude(st => st.User)
                .Include(s => s.Appointment)
                .Include(s => s.TimeReport)
                .FirstOrDefaultAsync(s => s.Id == scheduleId);
        }

        /// <summary>
        /// Get schedules by staff ID
        /// </summary>
        public async Task<List<Schedule>> GetSchedulesByStaffIdAsync(int staffId)
        {
            return await _context.Schedules
                .Include(s => s.Appointment)
                .Where(s => s.StaffId == staffId)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        // Add these methods to your AdminService class

        /// <summary>
        /// Create a new schedule using Schedule entity
        /// </summary>
        public async Task<Schedule?> CreateScheduleAsync(Schedule schedule)
        {
            try
            {
                // Validate staff exists
                var staffExists = await _context.Staff.AnyAsync(s => s.Id == schedule.StaffId);
                if (!staffExists)
                    return null;

                // Check for schedule conflicts
                var hasConflict = await _context.Schedules
                    .AnyAsync(s => s.StaffId == schedule.StaffId &&
                                   ((s.StartTime <= schedule.StartTime && s.EndTime > schedule.StartTime) ||
                                    (s.StartTime < schedule.EndTime && s.EndTime >= schedule.EndTime) ||
                                    (s.StartTime >= schedule.StartTime && s.EndTime <= schedule.EndTime)));

                if (hasConflict)
                    return null;

                schedule.CreatedAt = DateTime.UtcNow;
                _context.Schedules.Add(schedule);
                await _context.SaveChangesAsync();

                return schedule;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating schedule: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Update an existing schedule using Schedule entity
        /// </summary>
        public async Task<bool> UpdateScheduleAsync(Schedule schedule)
        {
            try
            {
                _context.Schedules.Update(schedule);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating schedule: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete a schedule and return boolean
        /// </summary>
        public async Task<bool> DeleteScheduleAsync(int scheduleId)
        {
            try
            {
                var schedule = await _context.Schedules
                    .Include(s => s.Appointment)
                    .Include(s => s.TimeReport)
                    .FirstOrDefaultAsync(s => s.Id == scheduleId);

                if (schedule == null)
                    return false;

                // Check if schedule has an appointment
                if (schedule.Appointment != null)
                    return false;

                // Check if schedule has a time report
                if (schedule.TimeReport != null)
                    return false;

                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting schedule: {ex.Message}");
                return false;
            }
        }

        #endregion

    }
}
