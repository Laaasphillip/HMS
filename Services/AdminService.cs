using HMS.Data;
using HMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HMS.Services
{
    public class AdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminService(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private ClaimsPrincipal CurrentUser => _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedAccessException("No user context available");

        private async Task<bool> IsAuthorizedAsync(string policy)
        {
            var result = await _authorizationService.AuthorizeAsync(CurrentUser, policy);
            return result.Succeeded;
        }

        private async Task EnsureAuthorizedAsync(string policy, string operation)
        {
            if (!await IsAuthorizedAsync(policy))
            {
                var role = CurrentUser.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
                throw new UnauthorizedAccessException($"User with role '{role}' is not authorized to {operation}");
            }
        }
        #region TimeReport Management

        public async Task<List<TimeReport>> GetAllTimeReportsAsync()
        {
            await EnsureAuthorizedAsync("AdminOnly", "view all time reports");

            return await _context.TimeReports
        .Include(tr => tr.Staff)
            .ThenInclude(s => s.User)
        .Include(tr => tr.Schedule)
        .OrderByDescending(tr => tr.ClockIn)
        .ToListAsync();

        }

        public async Task<TimeReport?> GetTimeReportByIdAsync(int id)
        {
            await EnsureAuthorizedAsync("AdminOnly", "view time report");

            return await _context.TimeReports
                .Include(tr => tr.Staff)
                    .ThenInclude(s => s.User)
                .Include(tr => tr.Schedule)
                .FirstOrDefaultAsync(tr => tr.Id == id);
        }

        public async Task<List<TimeReport>> GetTimeReportsByStaffIdAsync(int staffId)
        {
            await EnsureAuthorizedAsync("AdminOnly", "view staff time reports");

            // Validate that the staff member exists
            var staffExists = await _context.Staff.AnyAsync(s => s.Id == staffId);
            if (!staffExists)
                throw new ArgumentException($"Staff member with ID {staffId} not found");

            return await _context.TimeReports
                .Include(tr => tr.Staff)
                    .ThenInclude(s => s.User)
                .Include(tr => tr.Schedule)
                .Where(tr => tr.StaffId == staffId)
                .OrderByDescending(tr => tr.ClockIn)
                .ToListAsync();
        }
        public async Task<(bool Success, string Message)> DeleteTimeReportAsync(int id)
        {
            try
            {
                // Check authorization - only Admin can delete time reports
                await EnsureAuthorizedAsync("AdminOnly", "delete time report");

                // Find the time report
                var timeReport = await _context.TimeReports.FindAsync(id);

                if (timeReport == null)
                    return (false, "Time report not found");

                // Remove the time report
                _context.TimeReports.Remove(timeReport);
                await _context.SaveChangesAsync();

                return (true, "Time report deleted successfully");
            }
            catch (UnauthorizedAccessException ex)
            {
                return (false, $"Access Denied: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting time report: {ex.Message}");
            }
        }
        public async Task<(bool Success, string Message, TimeReport? TimeReport)> CreateTimeReportAsync(
             int staffId,
             int? scheduleId,
             DateTime clockIn,
             DateTime? clockOut,
             string activityType,
             string notes = "")
        {
            try
            {
                // Check authorization - Admin or Staff can create time reports
                await EnsureAuthorizedAsync("AdminOrStaff", "create time report");

                // Validate that the staff member exists
                var staffExists = await _context.Staff.AnyAsync(s => s.Id == staffId);
                if (!staffExists)
                    return (false, "Staff member not found", null);

                // Load schedule if provided
                Schedule? schedule = null;
                if (scheduleId.HasValue)
                {
                    schedule = await _context.Schedules.FindAsync(scheduleId.Value);
                    if (schedule == null)
                        return (false, "Schedule not found", null);
                }

                // Calculate hours worked
                decimal hoursWorked = 0;
                if (clockOut.HasValue)
                {
                    var duration = clockOut.Value - clockIn;
                    hoursWorked = (decimal)duration.TotalHours;
                }

                // Create time report
                var timeReport = new TimeReport
                {
                    StaffId = staffId,
                    ScheduleId = scheduleId,
                    ClockIn = clockIn,
                    ClockOut = clockOut,
                    HoursWorked = hoursWorked,
                    ActivityType = activityType,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow,
                    ApprovalStatus = "Pending"
                };

                // Calculate deviations if schedule exists
                CalculateDeviations(timeReport, schedule);

                _context.TimeReports.Add(timeReport);
                await _context.SaveChangesAsync();

                // Load navigation properties
                await _context.Entry(timeReport)
                    .Reference(tr => tr.Staff)
                    .LoadAsync();

                if (timeReport.Staff != null)
                {
                    await _context.Entry(timeReport.Staff)
                        .Reference(s => s.User)
                        .LoadAsync();
                }
                if (scheduleId.HasValue)
                {
                    await _context.Entry(timeReport)
                        .Reference(tr => tr.Schedule)
                        .LoadAsync();
                }

                return (true, "Time report created successfully", timeReport);
            }
            catch (UnauthorizedAccessException ex)
            {
                return (false, $"Access Denied: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating time report: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateTimeReportAsync(TimeReport timeReport)
        {
            try
            {
                // Check authorization - Admin or Staff can update time reports
                await EnsureAuthorizedAsync("AdminOrStaff", "update time report");

                // Validate that the time report exists
                var existingReport = await _context.TimeReports.FindAsync(timeReport.Id);
                if (existingReport == null)
                    return (false, "Time report not found");

                // Validate that the staff member exists
                var staffExists = await _context.Staff.AnyAsync(s => s.Id == timeReport.StaffId);
                if (!staffExists)
                    return (false, "Staff member not found");

                // Validate schedule if provided
                if (timeReport.ScheduleId.HasValue)
                {
                    var scheduleExists = await _context.Schedules.AnyAsync(s => s.Id == timeReport.ScheduleId.Value);
                    if (!scheduleExists)
                        return (false, "Schedule not found");
                }

                // Recalculate hours worked 
                if (timeReport.ClockOut.HasValue)
                {
                    var duration = timeReport.ClockOut.Value - timeReport.ClockIn;
                    timeReport.HoursWorked = (decimal)duration.TotalHours;
                }
                else
                {
                    timeReport.HoursWorked = 0;
                }

                // Load schedule and deviations
                Schedule? schedule = null;
                if (timeReport.ScheduleId.HasValue)
                {
                    schedule = await _context.Schedules.FindAsync(timeReport.ScheduleId.Value);
                }
                CalculateDeviations(timeReport, schedule);

                // Update the time report
                existingReport.StaffId = timeReport.StaffId;
                existingReport.ScheduleId = timeReport.ScheduleId;
                existingReport.ClockIn = timeReport.ClockIn;
                existingReport.ClockOut = timeReport.ClockOut;
                existingReport.HoursWorked = timeReport.HoursWorked;
                existingReport.ActivityType = timeReport.ActivityType;
                existingReport.Notes = timeReport.Notes;

                _context.TimeReports.Update(existingReport);
                await _context.SaveChangesAsync();

                return (true, "Time report updated successfully");
            }
            catch (UnauthorizedAccessException ex)
            {
                return (false, $"Access Denied: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating time report: {ex.Message}");
            }
        }

        // Get pending time reports that need approval
        public async Task<List<TimeReport>> GetPendingTimeReportsAsync()
        {
            await EnsureAuthorizedAsync("AdminOnly", "view pending time reports");

            return await _context.TimeReports
                .Include(tr => tr.Staff)
                    .ThenInclude(s => s.User)
                .Include(tr => tr.Schedule)
                .Where(tr => tr.ApprovalStatus == "Pending" &&
                            (tr.LateArrivalMinutes > 0 || tr.EarlyDepartureMinutes > 0))
                .OrderByDescending(tr => tr.ClockIn)
                .ToListAsync();
        }

        // Approve or reject a time report
        public async Task<(bool Success, string Message)> ApproveTimeReportAsync(
            int timeReportId,
            bool isApproved,
            string? approvalNotes = null)
        {
            try
            {
                await EnsureAuthorizedAsync("AdminOnly", "approve time report");

                var timeReport = await _context.TimeReports.FindAsync(timeReportId);
                if (timeReport == null)
                    return (false, "Time report not found");

                var userId = CurrentUser.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                timeReport.ApprovalStatus = isApproved ? "Approved" : "Rejected";
                timeReport.ApprovedBy = userId;
                timeReport.ApprovedAt = DateTime.UtcNow;
                timeReport.ApprovalNotes = approvalNotes;

                _context.TimeReports.Update(timeReport);
                await _context.SaveChangesAsync();

                var statusText = isApproved ? "approved" : "rejected";
                return (true, $"Time report {statusText} successfully");
            }
            catch (UnauthorizedAccessException ex)
            {
                return (false, $"Access Denied: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Error processing time report: {ex.Message}");
            }
        }

        // Calculate deviations between clock in/out and scheduled times
        private void CalculateDeviations(TimeReport timeReport, Schedule? schedule)
        {
            if (schedule == null)
            {
                timeReport.LateArrivalMinutes = null;
                timeReport.EarlyDepartureMinutes = null;
                return;
            }

            // Calculate late arrival
            var scheduledStart = schedule.Date.Date + schedule.StartTime;
            var actualStart = timeReport.ClockIn;

            var arrivalDifference = (actualStart - scheduledStart).TotalMinutes;
            timeReport.LateArrivalMinutes = arrivalDifference > 0 ? (int)arrivalDifference : 0;

            // Calculate early departure (only if clocked out)
            if (timeReport.ClockOut.HasValue)
            {
                var scheduledEnd = schedule.Date.Date + schedule.EndTime;
                var actualEnd = timeReport.ClockOut.Value;

                var departureDifference = (scheduledEnd - actualEnd).TotalMinutes;
                timeReport.EarlyDepartureMinutes = departureDifference > 0 ? (int)departureDifference : 0;
            }
            else
            {
                timeReport.EarlyDepartureMinutes = null;
            }
        }

        #region Staff TimeReport Methods

        // Helper method to get current staff member
        private async Task<Staff?> GetCurrentStaffAsync()
        {
            var userId = CurrentUser.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _context.Staff
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

       
        /// Get all time reports for the current logged-in staff member
        
        public async Task<List<TimeReport>> GetMyTimeReportsAsync()
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "view own time reports");

            var currentStaff = await GetCurrentStaffAsync();
            if (currentStaff == null)
                throw new InvalidOperationException("Current user is not associated with a staff member");

            return await _context.TimeReports
                .Include(tr => tr.Staff)
                    .ThenInclude(s => s.User)
                .Include(tr => tr.Schedule)
                .Where(tr => tr.StaffId == currentStaff.Id)
                .OrderByDescending(tr => tr.ClockIn)
                .ToListAsync();
        }

        
        /// Get the current active time report for the current staff member
        
        public async Task<TimeReport?> GetActiveTimeReportAsync()
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "view active time report");

            var currentStaff = await GetCurrentStaffAsync();
            if (currentStaff == null)
                return null;

            return await _context.TimeReports
                .Include(tr => tr.Staff)
                    .ThenInclude(s => s.User)
                .Include(tr => tr.Schedule)
                .Where(tr => tr.StaffId == currentStaff.Id && tr.ClockOut == null)
                .OrderByDescending(tr => tr.ClockIn)
                .FirstOrDefaultAsync();
        }

       
        /// Clock In - Creates a new time report with current time as clock in
        
        public async Task<(bool Success, string Message, TimeReport? TimeReport)> ClockInAsync(string activityType = "Regular Work", string notes = "")
        {
            try
            {
                await EnsureAuthorizedAsync("AdminOrStaff", "clock in");

                var currentStaff = await GetCurrentStaffAsync();
                if (currentStaff == null)
                    return (false, "Current user is not associated with a staff member", null);

                // Check if there's already an active time report
                var activeTimeReport = await GetActiveTimeReportAsync();
                if (activeTimeReport != null)
                {
                    return (false, $"You are already clocked in since {activeTimeReport.ClockIn:HH:mm}. Please clock out first.", null);
                }

                // Get today's schedule if exists
                var todaySchedule = await _context.Schedules
                    .Where(s => s.StaffId == currentStaff.Id && s.Date.Date == DateTime.Today)
                    .FirstOrDefaultAsync();

                // Create new time report
                var timeReport = new TimeReport
                {
                    StaffId = currentStaff.Id,
                    ScheduleId = todaySchedule?.Id,
                    ClockIn = DateTime.Now,
                    ClockOut = null,
                    HoursWorked = 0,
                    ActivityType = activityType,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow,
                    ApprovalStatus = "Pending"
                };

                // Calculate deviations if schedule exists
                CalculateDeviations(timeReport, todaySchedule);

                _context.TimeReports.Add(timeReport);
                await _context.SaveChangesAsync();

                // Load navigation properties
                await _context.Entry(timeReport)
                    .Reference(tr => tr.Staff)
                    .LoadAsync();

                if (timeReport.Staff != null)
                {
                    await _context.Entry(timeReport.Staff)
                        .Reference(s => s.User)
                        .LoadAsync();
                }
                if (todaySchedule != null)
                {
                    await _context.Entry(timeReport)
                        .Reference(tr => tr.Schedule)
                        .LoadAsync();
                }

                return (true, $"Successfully clocked in at {timeReport.ClockIn:HH:mm}", timeReport);
            }
            catch (UnauthorizedAccessException ex)
            {
                return (false, $"Access Denied: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error clocking in: {ex.Message}", null);
            }
        }

        
        //Clock Out - Updates the active time report
       
        public async Task<(bool Success, string Message, TimeReport? TimeReport)> ClockOutAsync(string notes = "")
        {
            try
            {
                await EnsureAuthorizedAsync("AdminOrStaff", "clock out");

                var currentStaff = await GetCurrentStaffAsync();
                if (currentStaff == null)
                    return (false, "Current user is not associated with a staff member", null);

                // Get the active time report
                var activeTimeReport = await GetActiveTimeReportAsync();
                if (activeTimeReport == null)
                {
                    return (false, "No active clock-in found. Please clock in first.", null);
                }

                // Update clock out time
                activeTimeReport.ClockOut = DateTime.Now;

                // Calculate hours worked
                var duration = activeTimeReport.ClockOut.Value - activeTimeReport.ClockIn;
                activeTimeReport.HoursWorked = (decimal)duration.TotalHours;

                // Update notes if provided
                if (!string.IsNullOrEmpty(notes))
                {
                    activeTimeReport.Notes = string.IsNullOrEmpty(activeTimeReport.Notes)
                        ? notes
                        : $"{activeTimeReport.Notes}; {notes}";
                }

                // Recalculate deviations with updated clock out time
                var schedule = activeTimeReport.Schedule;
                if (schedule == null && activeTimeReport.ScheduleId.HasValue)
                {
                    schedule = await _context.Schedules.FindAsync(activeTimeReport.ScheduleId.Value);
                }
                CalculateDeviations(activeTimeReport, schedule);

                _context.TimeReports.Update(activeTimeReport);
                await _context.SaveChangesAsync();

                return (true, $"Successfully clocked out at {activeTimeReport.ClockOut:HH:mm}. Hours worked: {activeTimeReport.HoursWorked:F2}", activeTimeReport);
            }
            catch (UnauthorizedAccessException ex)
            {
                return (false, $"Access Denied: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error clocking out: {ex.Message}", null);
            }
        }

        #endregion
        #endregion

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
                await EnsureAuthorizedAsync("AdminOrStaff", "create patient");
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    PersonalNumber = personalNumber,
                    EmailConfirmed = true // Auto-confirm for admin-created accounts
                };

                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, $"Failed to create user account: {errors}", null);
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "Patient");
                }

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

        public async Task<bool> UpdatePatientAsync(Patient patient)
        {
            try
            {
                await EnsureAuthorizedAsync("AdminOrStaff", "update patient");
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

        public async Task<(bool Success, string Message)> DeletePatientAsync(int patientId)
        {
            try
            {
                await EnsureAuthorizedAsync("AdminOrStaff", "delete patient");
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .Include(p => p.Appointments)
                    .Include(p => p.Invoices)
                    .FirstOrDefaultAsync(p => p.Id == patientId);

                if (patient == null)
                    return (false, "Patient not found");

                if (patient.Appointments.Any())
                    return (false, "Cannot delete patient with existing appointments");

                if (patient.Invoices.Any())
                    return (false, "Cannot delete patient with existing invoices");

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

        public async Task<List<Staff>> GetAllStaffAsync()
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Appointments)
                .Include(s => s.Schedules)
                .OrderByDescending(s => s.HiredDate)
                .ToListAsync();
        }

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

        public async Task<(bool Success, string Message, Staff? Staff)> CreateStaffAsync(
            string email,
            string password,
            string firstName,
            string lastName,
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
                await EnsureAuthorizedAsync("AdminOrStaff", "create staff");
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    PersonalNumber = personalNumber,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, $"Failed to create user account: {errors}", null);
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "Staff");
                }
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

        public async Task<bool> UpdateStaffAsync(Staff staff)
        {
            try
            {
                await EnsureAuthorizedAsync("AdminOrStaff", "update staff");
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

        public async Task<(bool Success, string Message)> DeleteStaffAsync(int staffId)
        {
            try
            {
                await EnsureAuthorizedAsync("AdminOrStaff", "delete staff");
                var staff = await _context.Staff
                    .Include(s => s.User)
                    .Include(s => s.Appointments)
                    .Include(s => s.Schedules)
                    .FirstOrDefaultAsync(s => s.Id == staffId);

                if (staff == null)
                    return (false, "Staff member not found");

                if (staff.Appointments.Any())
                    return (false, "Cannot delete staff member with existing appointments");

                if (staff.Schedules.Any())
                    return (false, "Cannot delete staff member with existing schedules");

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

        public async Task<List<Schedule>> GetAllSchedulesAsync()
        {
            return await _context.Schedules
                .Include(s => s.Staff)
                    .ThenInclude(staff => staff.User)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<Schedule?> GetScheduleByIdAsync(int id)
        {
            return await _context.Schedules
                .Include(s => s.Staff)
                    .ThenInclude(staff => staff.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Schedule>> GetSchedulesByStaffIdAsync(int staffId)
        {
            return await _context.Schedules
                .Include(s => s.Staff)
                    .ThenInclude(staff => staff.User)
                .Where(s => s.StaffId == staffId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<Schedule> CreateScheduleAsync(Schedule schedule)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "create schedules");

            schedule.CreatedAt = DateTime.UtcNow;
            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();
            return schedule;
        }

        public async Task<bool> UpdateScheduleAsync(Schedule schedule)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "update schedules");

            _context.Schedules.Update(schedule);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteScheduleAsync(int id)
        {
            await EnsureAuthorizedAsync("AdminOnly", "delete schedules");

            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
                return false;

            _context.Schedules.Remove(schedule);
            return await _context.SaveChangesAsync() > 0;
        }

        #endregion

        #region Appointment Operations

        #region Appointment CRUD
        public async Task<List<Appointment>> GetAllAppointmentsAsync()
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Staff)
                    .ThenInclude(s => s.User)
                .Include(a => a.Schedule)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<Appointment?> GetAppointmentByIdAsync(int id)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Staff)
                    .ThenInclude(s => s.User)
                .Include(a => a.Schedule)
                .Include(a => a.Invoice)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<Appointment>> GetAppointmentsByPatientIdAsync(int patientId)
        {
            return await _context.Appointments
                .Include(a => a.Staff)
                    .ThenInclude(s => s.User)
                .Include(a => a.AppointmentSlot)
                .Include(a => a.Schedule)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetAppointmentsByStaffIdAsync(int staffId)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Schedule)
                .Include(a => a.AppointmentSlot)
                .Where(a => a.StaffId == staffId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<Appointment> CreateAppointmentAsync(Appointment appointment)
        {
            await EnsureAuthorizedAsync("Authenticated", "create appointments");

            appointment.CreatedAt = DateTime.UtcNow;
            appointment.UpdatedAt = DateTime.UtcNow;
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<bool> UpdateAppointmentAsync(Appointment appointment)
        {
            await EnsureAuthorizedAsync("Authenticated", "update appointments");

            appointment.UpdatedAt = DateTime.UtcNow;
            _context.Appointments.Update(appointment);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAppointmentAsync(int id)
        {
            await EnsureAuthorizedAsync("AdminOnly", "delete appointments");

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return false;

            _context.Appointments.Remove(appointment);
            return await _context.SaveChangesAsync() > 0;
        }

        #endregion

        #region AppointmentSlot Operations (Delegate to AppointmentSlotService)

        public async Task<List<AppointmentSlot>> GetAllAppointmentSlotsAsync()
        {
            return await _context.AppointmentSlots
                .Include(s => s.Staff)
                    .ThenInclude(staff => staff.User)
                .Include(s => s.Schedule)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<AppointmentSlot?> GetAppointmentSlotByIdAsync(int id)
        {
            return await _context.AppointmentSlots
                .Include(s => s.Staff)
                    .ThenInclude(staff => staff.User)
                .Include(s => s.Schedule)
                .Include(s => s.Appointments)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<AppointmentSlot>> GetAppointmentSlotsByStaffIdAsync(int staffId)
        {
            return await _context.AppointmentSlots
                .Include(s => s.Staff)
                    .ThenInclude(staff => staff.User)
                .Include(s => s.Schedule)
                .Where(s => s.StaffId == staffId)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<List<AppointmentSlot>> GetAvailableAppointmentSlotsAsync(int? staffId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.AppointmentSlots
                .Include(s => s.Staff)
                    .ThenInclude(staff => staff.User)
                .Where(s => s.Status == "Available" && s.CurrentBookings < s.MaxCapacity);

            if (staffId.HasValue)
                query = query.Where(s => s.StaffId == staffId.Value);

            if (fromDate.HasValue)
                query = query.Where(s => s.Date >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(s => s.Date <= toDate.Value);

            return await query
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<AppointmentSlot> CreateAppointmentSlotAsync(AppointmentSlot slot)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "create appointment slots");

            slot.CreatedAt = DateTime.UtcNow;
            _context.AppointmentSlots.Add(slot);
            await _context.SaveChangesAsync();
            return slot;
        }

        public async Task<bool> UpdateAppointmentSlotAsync(AppointmentSlot slot)
        {
            await EnsureAuthorizedAsync("Authenticated", "update appointment slots");

            _context.AppointmentSlots.Update(slot);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAppointmentSlotAsync(int id)
        {
            await EnsureAuthorizedAsync("AdminOnly", "delete appointment slots");

            var slot = await _context.AppointmentSlots.FindAsync(id);
            if (slot == null)
                return false;

            // Don't allow deleting slots that have bookings
            if (slot.CurrentBookings > 0)
                throw new InvalidOperationException("Cannot delete appointment slot with existing bookings");

            _context.AppointmentSlots.Remove(slot);
            return await _context.SaveChangesAsync() > 0;
        }


        /// Book an appointment slot (increment bookings counter)

        public async Task<bool> BookAppointmentSlotAsync(int slotId)
        {
            var slot = await _context.AppointmentSlots.FindAsync(slotId);
            if (slot == null)
                return false;

            if (slot.Status != "Available")
                throw new InvalidOperationException("Appointment slot is not available for booking");

            if (slot.CurrentBookings >= slot.MaxCapacity)
                throw new InvalidOperationException("Appointment slot is fully booked");

            slot.CurrentBookings++;

            // Mark as booked if at capacity
            if (slot.CurrentBookings >= slot.MaxCapacity)
                slot.Status = "Booked";

            _context.AppointmentSlots.Update(slot);
            return await _context.SaveChangesAsync() > 0;
        }


        /// Cancel a booking in an appointment slot 

        public async Task<bool> CancelAppointmentSlotBookingAsync(int slotId)
        {
            var slot = await _context.AppointmentSlots.FindAsync(slotId);
            if (slot == null)
                return false;

            if (slot.CurrentBookings > 0)
            {
                slot.CurrentBookings--;

                // Mark as available if below capacity
                if (slot.CurrentBookings < slot.MaxCapacity && slot.Status == "Booked")
                    slot.Status = "Available";

                _context.AppointmentSlots.Update(slot);
                return await _context.SaveChangesAsync() > 0;
            }

            return false;
        }

        #endregion

        #region AppointmentSlotConfiguration Operations

        public async Task<List<AppointmentSlotConfiguration>> GetAllSlotConfigurationsAsync()
        {
            return await _context.AppointmentSlotConfigurations
                .Include(c => c.Staff)
                    .ThenInclude(s => s.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        // Alias for UI compatibility
        public async Task<List<AppointmentSlotConfiguration>> GetAllAppointmentSlotConfigurationsAsync()
        {
            return await GetAllSlotConfigurationsAsync();
        }

        public async Task<AppointmentSlotConfiguration?> GetSlotConfigurationByIdAsync(int id)
        {
            return await _context.AppointmentSlotConfigurations
                .Include(c => c.Staff)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<AppointmentSlotConfiguration?> GetSlotConfigurationByStaffIdAsync(int staffId)
        {
            return await _context.AppointmentSlotConfigurations
                .Where(c => c.StaffId == staffId && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<AppointmentSlotConfiguration> CreateSlotConfigurationAsync(AppointmentSlotConfiguration config)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "create slot configurations");

            config.CreatedAt = DateTime.UtcNow;
            _context.AppointmentSlotConfigurations.Add(config);
            await _context.SaveChangesAsync();
            return config;
        }

        // Alias for UI compatibility
        public async Task<AppointmentSlotConfiguration> CreateAppointmentSlotConfigurationAsync(AppointmentSlotConfiguration config)
        {
            return await CreateSlotConfigurationAsync(config);
        }

        public async Task<bool> UpdateSlotConfigurationAsync(AppointmentSlotConfiguration config)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "update slot configurations");
            _context.ChangeTracker.Clear();
            config.UpdatedAt = DateTime.UtcNow;
            _context.AppointmentSlotConfigurations.Update(config);
            return await _context.SaveChangesAsync() > 0;
        }

        // Alias for UI compatibility
        public async Task<bool> UpdateAppointmentSlotConfigurationAsync(AppointmentSlotConfiguration config)
        {
            return await UpdateSlotConfigurationAsync(config);
        }

        public async Task<bool> DeleteSlotConfigurationAsync(int id)
        {
            await EnsureAuthorizedAsync("AdminOnly", "delete slot configurations");

            var config = await _context.AppointmentSlotConfigurations.FindAsync(id);
            if (config == null)
                return false;

            _context.AppointmentSlotConfigurations.Remove(config);
            return await _context.SaveChangesAsync() > 0;
        }

        // Alias for UI compatibility
        public async Task<bool> DeleteAppointmentSlotConfigurationAsync(int id)
        {
            return await DeleteSlotConfigurationAsync(id);
        }

        #endregion

        #region AppointmentBlock Operations

        public async Task<List<AppointmentBlock>> GetAllAppointmentBlocksAsync()
        {
            return await _context.AppointmentBlocks
                .Include(b => b.Staff)
                    .ThenInclude(s => s.User)
                .Where(b => b.IsActive)
                .OrderBy(b => b.Date)
                .ThenBy(b => b.StartTime)
                .ToListAsync();
        }

        public async Task<AppointmentBlock?> GetAppointmentBlockByIdAsync(int id)
        {
            return await _context.AppointmentBlocks
                .Include(b => b.Staff)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<List<AppointmentBlock>> GetAppointmentBlocksByStaffIdAsync(int staffId)
        {
            return await _context.AppointmentBlocks
                .Include(b => b.Staff)
                    .ThenInclude(s => s.User)
                .Where(b => b.StaffId == staffId && b.IsActive)
                .OrderBy(b => b.Date)
                .ThenBy(b => b.StartTime)
                .ToListAsync();
        }

        public async Task<AppointmentBlock> CreateAppointmentBlockAsync(AppointmentBlock block)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "create appointment blocks");

            block.CreatedAt = DateTime.UtcNow;
            block.IsActive = true;
            _context.AppointmentBlocks.Add(block);
            await _context.SaveChangesAsync();

            // Block any existing slots that overlap with this block
            await BlockOverlappingSlotsAsync(block);

            return block;
        }

        public async Task<bool> UpdateAppointmentBlockAsync(AppointmentBlock block)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "update appointment blocks");
            _context.ChangeTracker.Clear();
            _context.AppointmentBlocks.Update(block);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAppointmentBlockAsync(int id)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "delete appointment blocks");

            var block = await _context.AppointmentBlocks.FindAsync(id);
            if (block == null)
                return false;

            // Soft delete by marking as inactive
            block.IsActive = false;
            _context.AppointmentBlocks.Update(block);

            // Unblock any slots that were blocked by this block
            await UnblockOverlappingSlotsAsync(block);

            return await _context.SaveChangesAsync() > 0;
        }

        private async Task BlockOverlappingSlotsAsync(AppointmentBlock block)
        {
            var overlappingSlots = await _context.AppointmentSlots
                .Where(s => s.StaffId == block.StaffId
                    && s.Date.Date == block.Date.Date
                    && s.StartTime < block.EndTime
                    && s.EndTime > block.StartTime
                    && s.Status == "Available")
                .ToListAsync();

            foreach (var slot in overlappingSlots)
            {
                slot.Status = "Blocked";
                _context.AppointmentSlots.Update(slot);
            }

            await _context.SaveChangesAsync();
        }

        private async Task UnblockOverlappingSlotsAsync(AppointmentBlock block)
        {
            var overlappingSlots = await _context.AppointmentSlots
                .Where(s => s.StaffId == block.StaffId
                    && s.Date.Date == block.Date.Date
                    && s.StartTime < block.EndTime
                    && s.EndTime > block.StartTime
                    && s.Status == "Blocked"
                    && s.CurrentBookings < s.MaxCapacity)
                .ToListAsync();

            // Check if there are other active blocks that still overlap
            var otherActiveBlocks = await _context.AppointmentBlocks
                .Where(b => b.Id != block.Id
                    && b.StaffId == block.StaffId
                    && b.Date.Date == block.Date.Date
                    && b.IsActive)
                .ToListAsync();

            foreach (var slot in overlappingSlots)
            {
                // Only unblock if no other blocks overlap with this slot
                var hasOtherBlock = otherActiveBlocks.Any(b =>
                    slot.StartTime < b.EndTime && slot.EndTime > b.StartTime);

                if (!hasOtherBlock)
                {
                    slot.Status = "Available";
                    _context.AppointmentSlots.Update(slot);
                }
            }

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Slot Generation


        /// Generate appointment slots for a schedule based on staff configuration

        public async Task<List<AppointmentSlot>> GenerateSlotsForScheduleAsync(int scheduleId)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "generate appointment slots");

            var schedule = await _context.Schedules
                .Include(s => s.Staff)
                .FirstOrDefaultAsync(s => s.Id == scheduleId);

            if (schedule == null)
                throw new InvalidOperationException($"Schedule with ID {scheduleId} not found");

            // Check if slots have already been generated
            if (schedule.SlotsGenerated)
                throw new InvalidOperationException("Slots have already been generated for this schedule");

            // Get configuration for this staff member
            var config = await GetSlotConfigurationByStaffIdAsync(schedule.StaffId);
            if (config == null)
            {
                // Create a default configuration if none exists
                config = new AppointmentSlotConfiguration
                {
                    StaffId = schedule.StaffId,
                    SlotDurationMinutes = 30,
                    BufferTimeMinutes = 0,
                    MaxPatientsPerSlot = 1,
                    AdvanceBookingDays = 30,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await CreateSlotConfigurationAsync(config);
            }

            var generatedSlots = new List<AppointmentSlot>();

            // Calculate total slot duration (slot + buffer)
            var totalSlotDuration = config.SlotDurationMinutes + config.BufferTimeMinutes;

            // Generate slots from start time to end time
            var currentTime = schedule.StartTime;
            var scheduleDate = schedule.Date.Date;

            // Get any blocks for this staff on this date
            var blocks = await GetAppointmentBlocksByStaffIdAsync(schedule.StaffId);
            blocks = blocks.Where(b => b.Date.Date == scheduleDate).ToList();

            while (currentTime.Add(TimeSpan.FromMinutes(config.SlotDurationMinutes)) <= schedule.EndTime)
            {
                // Skip break time
                if (schedule.BreakStart.HasValue && schedule.BreakEnd.HasValue)
                {
                    if (currentTime >= schedule.BreakStart.Value && currentTime < schedule.BreakEnd.Value)
                    {
                        currentTime = schedule.BreakEnd.Value;
                        continue;
                    }
                }

                var slotEndTime = currentTime.Add(TimeSpan.FromMinutes(config.SlotDurationMinutes));

                // Check if this slot overlaps with any blocks 
                var isBlocked = blocks.Any(b =>
                    currentTime < b.EndTime && slotEndTime > b.StartTime);

                var slot = new AppointmentSlot
                {
                    ScheduleId = scheduleId,
                    StaffId = schedule.StaffId,
                    Date = scheduleDate,
                    StartTime = currentTime,
                    EndTime = slotEndTime,
                    Status = isBlocked ? "Blocked" : "Available",
                    MaxCapacity = config.MaxPatientsPerSlot,
                    CurrentBookings = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AppointmentSlots.Add(slot);
                generatedSlots.Add(slot);

                // Move to next slot (including buffer time)
                currentTime = currentTime.Add(TimeSpan.FromMinutes(totalSlotDuration));
            }

            // Mark schedule as having slots generated
            schedule.SlotsGenerated = true;
            schedule.UpdatedAt = DateTime.UtcNow;
            _context.Schedules.Update(schedule);

            await _context.SaveChangesAsync();

            return generatedSlots;
        }


        /// Regenerate slots for a schedule
        public async Task<List<AppointmentSlot>> RegenerateSlotsForScheduleAsync(int scheduleId)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "regenerate appointment slots");

            // Delete existing slots without bookings
            var existingSlots = await _context.AppointmentSlots
                .Where(s => s.ScheduleId == scheduleId && s.CurrentBookings == 0)
                .ToListAsync();

            _context.AppointmentSlots.RemoveRange(existingSlots);
            await _context.SaveChangesAsync();

            // Reset the SlotsGenerated flag
            var schedule = await _context.Schedules.FindAsync(scheduleId);
            if (schedule != null)
            {
                schedule.SlotsGenerated = false;
                _context.Schedules.Update(schedule);
                await _context.SaveChangesAsync();
            }

            // Generate new slots
            return await GenerateSlotsForScheduleAsync(scheduleId);
        }

        #endregion

        #endregion

        #region Statistics

        public async Task<int> GetTotalPatientsCountAsync()
        {
            return await _context.Patients.CountAsync();
        }

        public async Task<int> GetTotalStaffCountAsync()
        {
            return await _context.Staff.CountAsync();
        }

        public async Task<int> GetTotalSchedulesCountAsync()
        {
            return await _context.Schedules.CountAsync();
        }

        public async Task<decimal> GetTotalTransactionAmountAsync()
        {
            return await _context.Transactions
                .Where(t => t.Status == "Completed" || t.Status == "Success")
                .SumAsync(t => t.Amount);
        }

        public async Task<int> GetTotalAppointmentsCountAsync()
        {
            return await _context.Appointments.CountAsync();
        }

        public async Task<int> GetTotalInvoicesCountAsync()
        {
            return await _context.Invoices.CountAsync();
        }

        public async Task<int> GetTotalTimeReportsCountAsync()
        {
            return await _context.TimeReports.CountAsync();
        }

        public async Task<int> GetPendingInvoicesCountAsync()
        {
            return await _context.Invoices
                .Where(i => i.Status == "Pending")
                .CountAsync();
        }

        public async Task<decimal> GetPendingInvoicesAmountAsync()
        {
            return await _context.Invoices
                .Where(i => i.Status == "Pending")
                .SumAsync(i => i.TotalAmount);
        }

        public async Task<List<ApplicationUser>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.Patient)
                .Include(u => u.Staff)
                .ToListAsync();
        }
        public async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            return await _context.Users
                .Include(u => u.Patient)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            return await _userManager.Users
                .Include(u => u.Patient)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            if (user == null) return new List<string>();
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<IList<string>> GetCurrentUserRolesAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return new List<string>();
            return await GetUserRolesAsync(user);
        }

        public async Task<bool> UpdateUserAsync(ApplicationUser user)
        {
            if (user == null) return false;

            try
            {
                //Update Identity fields
                var identityResult = await _userManager.UpdateAsync(user);
                if (!identityResult.Succeeded)
                {
                    Console.WriteLine("Identity update failed: " + string.Join(", ", identityResult.Errors.Select(e => e.Description)));
                    return false;
                }

                //Save custom fields in ApplicationUser
                _context.Users.Update(user);
                var rowsAffected = await _context.SaveChangesAsync();

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Invoice Management

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.Patient)
                    .ThenInclude(p => p.User)
                .Include(i => i.Appointment)
                    .ThenInclude(a => a.Patient)
                        .ThenInclude(p => p.User)
                .Include(i => i.Appointment)
                    .ThenInclude(a => a.Staff)
                        .ThenInclude(s => s.User)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Transactions)
                .OrderByDescending(i => i.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.Patient)
                    .ThenInclude(p => p.User)
                .Include(i => i.Appointment)
                    .ThenInclude(a => a.Patient)
                        .ThenInclude(p => p.User)
                .Include(i => i.Appointment)
                    .ThenInclude(a => a.Staff)
                        .ThenInclude(s => s.User)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Transactions)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<List<Invoice>> GetInvoicesByPatientIdAsync(int patientId)
        {
            return await _context.Invoices
                .Include(i => i.Appointment)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Transactions)
                .Where(i => i.PatientId == patientId)
                .OrderByDescending(i => i.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "create invoices");

            // Generate invoice number if not provided
            if (string.IsNullOrEmpty(invoice.InvoiceNumber))
            {
                invoice.InvoiceNumber = await GenerateInvoiceNumberAsync();
            }

            invoice.CreatedAt = DateTime.UtcNow;

            // Verify appointment exists
            if (invoice.AppointmentId.HasValue)
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == invoice.AppointmentId.Value);

                if (appointment == null)
                    throw new InvalidOperationException("Appointment not found for this invoice.");

                // Set patient ID from appointment if not set
                if (invoice.PatientId == 0)
                {
                    invoice.PatientId = appointment.PatientId;
                }
            }

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return invoice;
        }

        public async Task<bool> UpdateInvoiceAsync(Invoice invoice)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "update invoices");

            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoice.Id);

            if (existingInvoice == null)
                return false;

            existingInvoice.SubTotal = invoice.SubTotal;
            existingInvoice.TaxAmount = invoice.TaxAmount;
            existingInvoice.TotalAmount = invoice.TotalAmount;
            existingInvoice.Status = invoice.Status;
            existingInvoice.DueDate = invoice.DueDate;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            await EnsureAuthorizedAsync("AdminOnly", "delete invoices");

            var invoice = await _context.Invoices
                .Include(i => i.Transactions)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return false;

            if (invoice.Transactions.Any())
            {
                _context.Transactions.RemoveRange(invoice.Transactions);
            }
            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                _context.InvoiceItems.RemoveRange(invoice.InvoiceItems);
            }
            _context.Invoices.Remove(invoice);
            return await _context.SaveChangesAsync() > 0;
        }

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var lastInvoice = await _context.Invoices
                .OrderByDescending(i => i.Id)
                .FirstOrDefaultAsync();

            var lastNumber = 0;
            if (lastInvoice != null && !string.IsNullOrEmpty(lastInvoice.InvoiceNumber))
            {
                var numberPart = lastInvoice.InvoiceNumber.Replace("INV-", "");
                int.TryParse(numberPart, out lastNumber);
            }

            return $"INV-{(lastNumber + 1):D6}";
        }

        #endregion

        #region InvoiceItem Management

        public async Task<List<InvoiceItem>> GetInvoiceItemsByInvoiceIdAsync(int invoiceId)
        {
            return await _context.InvoiceItems
                .Where(i => i.InvoiceId == invoiceId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<InvoiceItem?> GetInvoiceItemByIdAsync(int id)
        {
            return await _context.InvoiceItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<InvoiceItem> CreateInvoiceItemAsync(InvoiceItem item)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "create invoice items");

            // Calculate total price
            item.TotalPrice = item.Quantity * item.UnitPrice;

            _context.InvoiceItems.Add(item);
            await _context.SaveChangesAsync();

            // Update invoice totals
            await RecalculateInvoiceTotalsAsync(item.InvoiceId);

            return item;
        }

        public async Task<bool> UpdateInvoiceItemAsync(InvoiceItem item)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "update invoice items");

            var existingItem = await _context.InvoiceItems
                .FirstOrDefaultAsync(i => i.Id == item.Id);

            if (existingItem == null)
                return false;

            existingItem.Description = item.Description;
            existingItem.Quantity = item.Quantity;
            existingItem.UnitPrice = item.UnitPrice;
            existingItem.TotalPrice = item.Quantity * item.UnitPrice;

            await _context.SaveChangesAsync();

            // Update invoice totals
            await RecalculateInvoiceTotalsAsync(existingItem.InvoiceId);

            return true;
        }

        public async Task<bool> DeleteInvoiceItemAsync(int id)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "delete invoice items");

            var item = await _context.InvoiceItems.FindAsync(id);
            if (item == null)
                return false;

            var invoiceId = item.InvoiceId;

            _context.InvoiceItems.Remove(item);
            await _context.SaveChangesAsync();

            // Update invoice totals
            await RecalculateInvoiceTotalsAsync(invoiceId);

            return true;
        }

        private async Task RecalculateInvoiceTotalsAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                return;

            // Calculate subtotal from items
            invoice.SubTotal = invoice.InvoiceItems.Sum(i => i.TotalPrice);

            // Recalculate tax and total
            // Preserve the tax percentage
            var taxRate = invoice.SubTotal > 0 ? invoice.TaxAmount / invoice.SubTotal : 0;
            invoice.TaxAmount = invoice.SubTotal * taxRate;
            invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Transaction Management

        public async Task<List<Transaction>> GetAllTransactionsAsync()
        {
            return await _context.Transactions
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Patient)
                        .ThenInclude(p => p.User)
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Appointment)
                .OrderByDescending(t => t.TransactionDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Transaction?> GetTransactionByIdAsync(int id)
        {
            return await _context.Transactions
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Patient)
                        .ThenInclude(p => p.User)
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Appointment)
                        .ThenInclude(a => a.Staff)
                            .ThenInclude(s => s.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<Transaction>> GetTransactionsByInvoiceIdAsync(int invoiceId)
        {
            return await _context.Transactions
                .Include(t => t.Invoice)
                .Where(t => t.InvoiceId == invoiceId)
                .OrderByDescending(t => t.TransactionDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetTransactionsByPatientIdAsync(int patientId)
        {
            return await _context.Transactions
                .Include(t => t.Invoice)
                    .ThenInclude(i => i.Appointment)
                .Where(t => t.Invoice.PatientId == patientId)
                .OrderByDescending(t => t.TransactionDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            await EnsureAuthorizedAsync("Authenticated", "create transactions");

            // Generate transaction number if not provided
            if (string.IsNullOrEmpty(transaction.TransactionNumber))
            {
                transaction.TransactionNumber = await GenerateTransactionNumberAsync();
            }

            transaction.CreatedAt = DateTime.UtcNow;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Update invoice status if transaction is completed
            if (transaction.Status == "Completed" || transaction.Status == "Success")
            {
                var invoice = await _context.Invoices.FindAsync(transaction.InvoiceId);
                if (invoice != null)
                {
                    // Check if invoice is fully paid
                    var totalPaid = await _context.Transactions
                        .Where(t => t.InvoiceId == transaction.InvoiceId &&
                               (t.Status == "Completed" || t.Status == "Success"))
                        .SumAsync(t => t.Amount);

                    if (totalPaid >= invoice.TotalAmount)
                    {
                        invoice.Status = "Paid";
                        _context.Invoices.Update(invoice);
                        await _context.SaveChangesAsync();
                    }
                    else if (totalPaid > 0 && totalPaid < invoice.TotalAmount)
                    {
                        invoice.Status = "Partially Paid";
                        _context.Invoices.Update(invoice);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return transaction;
        }

        public async Task<bool> UpdateTransactionAsync(Transaction transaction)
        {
            await EnsureAuthorizedAsync("AdminOrStaff", "update transactions");

            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transaction.Id);

            if (existingTransaction == null)
                return false;

            existingTransaction.Status = transaction.Status;
            existingTransaction.PaymentMethod = transaction.PaymentMethod;
            existingTransaction.Amount = transaction.Amount;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteTransactionAsync(int id)
        {
            await EnsureAuthorizedAsync("AdminOnly", "delete transactions");

            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
                return false;

            _context.Transactions.Remove(transaction);
            return await _context.SaveChangesAsync() > 0;
        }

        private async Task<string> GenerateTransactionNumberAsync()
        {
            var lastTransaction = await _context.Transactions
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            var lastNumber = lastTransaction != null ?
                int.Parse(lastTransaction.TransactionNumber.Replace("TXN", "")) : 0;

            return $"TXN{(lastNumber + 1):D6}";
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Transactions
                .Where(t => t.Status == "Completed" || t.Status == "Success")
                .SumAsync(t => t.Amount);
        }

        public async Task<int> GetCompletedTransactionsCountAsync()
        {
            return await _context.Transactions
                .Where(t => t.Status == "Completed" || t.Status == "Success")
                .CountAsync();
        }

        public async Task<int> GetPendingTransactionsCountAsync()
        {
            return await _context.Transactions
                .Where(t => t.Status == "Pending")
                .CountAsync();
        }

        public async Task<int> GetRefundedTransactionsCountAsync()
        {
            return await _context.Transactions
                .Where(t => t.Status == "Refunded")
                .CountAsync();
        }

        #endregion
    }
}