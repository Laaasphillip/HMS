using HMS.Data;
using HMS.DTOs;
using HMS.Models;
using HMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HMS.Services
{
    public class StaffService : IStaffService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StaffService> _logger;

        public StaffService(ApplicationDbContext context, ILogger<StaffService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region CRUD Operations

        public async Task<StaffDto> CreateStaffAsync(CreateStaffDto dto)
        {
            try
            {
                if (await StaffExistsByUserIdAsync(dto.UserId))
                {
                    throw new InvalidOperationException($"User {dto.UserId} is already associated with a staff member.");
                }

                var staff = new Staff
                {
                    UserId = dto.UserId,
                    ContractForm = dto.ContractForm,
                    Department = dto.Department,
                    Taxes = dto.Taxes,
                    Bankdetails = dto.Bankdetails,
                    Vacationdays = dto.Vacationdays,
                    Specialization = dto.Specialization,
                    HourlyRate = dto.HourlyRate,
                    HiredDate = dto.HiredDate
                };

                _context.Staff.Add(staff);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Staff created with ID: {staff.Id}");

                return await GetStaffByIdAsync(staff.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff");
                throw;
            }
        }

        public async Task<StaffDto> GetStaffByIdAsync(int id)
        {
            var staff = await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Appointments)
                .Include(s => s.TimeReports)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
                return null;

            return MapToStaffDto(staff);
        }

        public async Task<StaffDto> GetStaffByUserIdAsync(string userId)
        {
            var staff = await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Appointments)
                .Include(s => s.TimeReports)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (staff == null)
                return null;

            return MapToStaffDto(staff);
        }

        public async Task<List<StaffDto>> GetAllStaffAsync()
        {
            var staffList = await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Appointments)
                .Include(s => s.TimeReports)
                .ToListAsync();

            return staffList.Select(MapToStaffDto).ToList();
        }

        public async Task<List<StaffDto>> GetStaffByDepartmentAsync(string department)
        {
            var staffList = await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Appointments)
                .Include(s => s.TimeReports)
                .Where(s => s.Department == department)
                .ToListAsync();

            return staffList.Select(MapToStaffDto).ToList();
        }

        public async Task<StaffDto> UpdateStaffAsync(UpdateStaffDto dto)
        {
            try
            {
                var staff = await _context.Staff.FindAsync(dto.Id);
                if (staff == null)
                {
                    throw new KeyNotFoundException($"Staff with ID {dto.Id} not found.");
                }

                if (!string.IsNullOrEmpty(dto.ContractForm))
                    staff.ContractForm = dto.ContractForm;

                if (!string.IsNullOrEmpty(dto.Department))
                    staff.Department = dto.Department;

                if (dto.Taxes.HasValue)
                    staff.Taxes = dto.Taxes.Value;

                if (!string.IsNullOrEmpty(dto.Bankdetails))
                    staff.Bankdetails = dto.Bankdetails;

                if (dto.Vacationdays.HasValue)
                    staff.Vacationdays = dto.Vacationdays.Value;

                if (!string.IsNullOrEmpty(dto.Specialization))
                    staff.Specialization = dto.Specialization;

                if (dto.HourlyRate.HasValue)
                    staff.HourlyRate = dto.HourlyRate.Value;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Staff {dto.Id} updated successfully");

                return await GetStaffByIdAsync(dto.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating staff {dto.Id}");
                throw;
            }
        }

        public async Task<bool> DeleteStaffAsync(int id)
        {
            try
            {
                var staff = await _context.Staff.FindAsync(id);
                if (staff == null)
                    return false;

                _context.Staff.Remove(staff);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Staff {id} deleted successfully");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting staff {id}");
                throw;
            }
        }

        #endregion

        #region Dashboard

        public async Task<StaffDashboardDto> GetStaffDashboardAsync(int staffId)
        {
            var staff = await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Appointments)
                .Include(s => s.TimeReports)
                .Include(s => s.Schedules)
                .FirstOrDefaultAsync(s => s.Id == staffId);

            if (staff == null)
                throw new KeyNotFoundException($"Staff with ID {staffId} not found.");

            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var todayAppointments = staff.Appointments
                .Where(a => a.AppointmentDate.Date == today)
                .ToList();

            var todayTimeReports = staff.TimeReports
                .Where(tr => tr.ClockIn.Date == today)
                .ToList();

            var weekAppointments = staff.Appointments
                .Where(a => a.AppointmentDate >= weekStart && a.AppointmentDate < weekEnd)
                .ToList();

            var weekTimeReports = staff.TimeReports
                .Where(tr => tr.ClockIn >= weekStart && tr.ClockIn < weekEnd)
                .ToList();

            var monthAppointments = staff.Appointments
                .Where(a => a.AppointmentDate >= monthStart && a.AppointmentDate < monthEnd)
                .ToList();

            var monthTimeReports = staff.TimeReports
                .Where(tr => tr.ClockIn >= monthStart && tr.ClockIn < monthEnd)
                .ToList();

            var upcomingSchedules = staff.Schedules
                .Where(s => s.StartTime >= now)
                .OrderBy(s => s.StartTime)
                .Take(5)
                .Select(s => new ScheduleSummaryDto
                {
                    Id = s.Id,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    ShiftType = s.ShiftType,
                    Status = s.Status
                })
                .ToList();

            var upcomingAppointments = staff.Appointments
                .Where(a => a.AppointmentDate >= now)
                .OrderBy(a => a.AppointmentDate)
                .Take(5)
                .Select(a => new AppointmentSummaryDto
                {
                    Id = a.Id,
                    PatientName = a.Patient != null ? a.Patient.User.UserName : "N/A",
                    AppointmentDate = a.AppointmentDate,
                    DurationMinutes = a.DurationMinutes,
                    Status = a.Status,
                    Reason = a.Reason
                })
                .ToList();

            var dashboard = new StaffDashboardDto
            {
                StaffId = staffId,
                StaffName = staff.User?.UserName ?? "N/A",

                TodayAppointments = todayAppointments.Count,
                TodayCompletedAppointments = todayAppointments.Count(a => a.Status == "Completed"),
                TodayHoursWorked = todayTimeReports.Sum(tr => tr.HoursWorked),

                WeekAppointments = weekAppointments.Count,
                WeekHoursWorked = weekTimeReports.Sum(tr => tr.HoursWorked),
                WeekEarnings = weekTimeReports.Sum(tr => tr.HoursWorked) * staff.HourlyRate,

                MonthAppointments = monthAppointments.Count,
                MonthHoursWorked = monthTimeReports.Sum(tr => tr.HoursWorked),
                MonthEarnings = monthTimeReports.Sum(tr => tr.HoursWorked) * staff.HourlyRate,

                RemainingVacationDays = await GetRemainingVacationDaysAsync(staffId),
                PendingVacations = new List<VacationRequestDto>(),

                UpcomingSchedules = upcomingSchedules,
                UpcomingAppointments = upcomingAppointments
            };

            return dashboard;
        }

        public async Task<StaffDashboardDto> GetStaffDashboardByUserIdAsync(string userId)
        {
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == userId);
            if (staff == null)
                throw new KeyNotFoundException($"Staff with UserID {userId} not found.");

            return await GetStaffDashboardAsync(staff.Id);
        }

        #endregion

        #region Profile

        public async Task<StaffProfileDto> GetStaffProfileAsync(int staffId)
        {
            var staff = await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Appointments)
                    .ThenInclude(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(s => s.Schedules)
                .FirstOrDefaultAsync(s => s.Id == staffId);

            if (staff == null)
                throw new KeyNotFoundException($"Staff with ID {staffId} not found.");

            var now = DateTime.UtcNow;
            var weekStart = now.Date.AddDays(-(int)now.Date.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);

            var profile = new StaffProfileDto
            {
                Id = staff.Id,
                FullName = staff.User?.UserName ?? "N/A",
                Email = staff.User?.Email ?? "N/A",
                PhoneNumber = staff.User?.PhoneNumber ?? "N/A",
                ContractForm = staff.ContractForm,
                Department = staff.Department,
                Specialization = staff.Specialization,
                HourlyRate = staff.HourlyRate,
                HiredDate = staff.HiredDate,
                Vacationdays = staff.Vacationdays,
                UsedVacationDays = await GetUsedVacationDaysAsync(staffId),
                RemainingVacationDays = await GetRemainingVacationDaysAsync(staffId),
                UpcomingAppointments = staff.Appointments
                    .Where(a => a.AppointmentDate >= now)
                    .OrderBy(a => a.AppointmentDate)
                    .Take(10)
                    .Select(a => new AppointmentSummaryDto
                    {
                        Id = a.Id,
                        PatientName = a.Patient?.User?.UserName ?? "N/A",
                        AppointmentDate = a.AppointmentDate,
                        DurationMinutes = a.DurationMinutes,
                        Status = a.Status,
                        Reason = a.Reason
                    })
                    .ToList(),
                WeekSchedule = staff.Schedules
                    .Where(s => s.StartTime >= weekStart && s.StartTime < weekEnd)
                    .OrderBy(s => s.StartTime)
                    .Select(s => new ScheduleSummaryDto
                    {
                        Id = s.Id,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        ShiftType = s.ShiftType,
                        Status = s.Status
                    })
                    .ToList()
            };

            return profile;
        }

        public async Task<StaffProfileDto> GetStaffProfileByUserIdAsync(string userId)
        {
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == userId);
            if (staff == null)
                throw new KeyNotFoundException($"Staff with UserID {userId} not found.");

            return await GetStaffProfileAsync(staff.Id);
        }

        #endregion

        #region Appointments

        public async Task<List<AppointmentSummaryDto>> GetStaffAppointmentsAsync(int staffId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Where(a => a.StaffId == staffId);

            if (startDate.HasValue)
                query = query.Where(a => a.AppointmentDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.AppointmentDate <= endDate.Value);

            var appointments = await query
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new AppointmentSummaryDto
                {
                    Id = a.Id,
                    PatientName = a.Patient.User.UserName,
                    AppointmentDate = a.AppointmentDate,
                    DurationMinutes = a.DurationMinutes,
                    Status = a.Status,
                    Reason = a.Reason
                })
                .ToListAsync();

            return appointments;
        }

        public async Task<List<AppointmentSummaryDto>> GetTodayAppointmentsAsync(int staffId)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            return await GetStaffAppointmentsAsync(staffId, today, tomorrow);
        }

        public async Task<List<AppointmentSummaryDto>> GetUpcomingAppointmentsAsync(int staffId, int days = 7)
        {
            var now = DateTime.UtcNow;
            var endDate = now.AddDays(days);

            return await GetStaffAppointmentsAsync(staffId, now, endDate);
        }

        public async Task<int> GetAppointmentCountAsync(int staffId, DateTime startDate, DateTime endDate)
        {
            return await _context.Appointments
                .Where(a => a.StaffId == staffId &&
                           a.AppointmentDate >= startDate &&
                           a.AppointmentDate <= endDate)
                .CountAsync();
        }

        #endregion

        #region Schedule

        public async Task<List<ScheduleSummaryDto>> GetStaffScheduleAsync(int staffId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Schedules
                .Where(s => s.StaffId == staffId);

            if (startDate.HasValue)
                query = query.Where(s => s.StartTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.StartTime <= endDate.Value);

            var schedules = await query
                .OrderBy(s => s.StartTime)
                .Select(s => new ScheduleSummaryDto
                {
                    Id = s.Id,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    ShiftType = s.ShiftType,
                    Status = s.Status
                })
                .ToListAsync();

            return schedules;
        }

        public async Task<List<ScheduleSummaryDto>> GetWeekScheduleAsync(int staffId)
        {
            var now = DateTime.UtcNow;
            var weekStart = now.Date.AddDays(-(int)now.Date.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);

            return await GetStaffScheduleAsync(staffId, weekStart, weekEnd);
        }

        public async Task<ScheduleSummaryDto> GetCurrentScheduleAsync(int staffId)
        {
            var now = DateTime.UtcNow;

            var schedule = await _context.Schedules
                .Where(s => s.StaffId == staffId &&
                           s.StartTime <= now &&
                           s.EndTime >= now)
                .OrderBy(s => s.StartTime)
                .FirstOrDefaultAsync();

            if (schedule == null)
                return null;

            return new ScheduleSummaryDto
            {
                Id = schedule.Id,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                ShiftType = schedule.ShiftType,
                Status = schedule.Status
            };
        }

        public async Task<bool> IsStaffAvailableAsync(int staffId, DateTime dateTime)
        {
            var hasSchedule = await _context.Schedules
                .AnyAsync(s => s.StaffId == staffId &&
                              s.StartTime <= dateTime &&
                              s.EndTime >= dateTime &&
                              s.Status == "Active");

            if (!hasSchedule)
                return false;

            var hasAppointment = await _context.Appointments
                .AnyAsync(a => a.StaffId == staffId &&
                              a.AppointmentDate <= dateTime &&
                              a.AppointmentDate.AddMinutes(a.DurationMinutes) > dateTime);

            return !hasAppointment;
        }

        #endregion

        #region Time Reports

        public async Task<TimeReportSummaryDto> ClockInAsync(CreateTimeReportDto dto)
        {
            try
            {
                var activeReport = await GetActiveTimeReportAsync(dto.StaffId);
                if (activeReport != null)
                {
                    throw new InvalidOperationException($"Staff {dto.StaffId} already has an active time report.");
                }

                var timeReport = new TimeReport
                {
                    StaffId = dto.StaffId,
                    ScheduleId = dto.ScheduleId ?? 0,
                    ClockIn = dto.ClockIn,
                    ActivityType = dto.ActivityType,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeReports.Add(timeReport);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Staff {dto.StaffId} clocked in at {dto.ClockIn}");

                return new TimeReportSummaryDto
                {
                    Id = timeReport.Id,
                    ClockIn = timeReport.ClockIn,
                    ClockOut = null,
                    HoursWorked = 0,
                    ActivityType = timeReport.ActivityType,
                    Status = "Active"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error clocking in for staff {dto.StaffId}");
                throw;
            }
        }

        public async Task<TimeReportSummaryDto> ClockOutAsync(CompleteTimeReportDto dto)
        {
            try
            {
                var timeReport = await _context.TimeReports.FindAsync(dto.Id);
                if (timeReport == null)
                {
                    throw new KeyNotFoundException($"Time report {dto.Id} not found.");
                }

                timeReport.ClockOut = dto.ClockOut;

                var timeSpan = timeReport.ClockOut - timeReport.ClockIn;
                timeReport.HoursWorked = (decimal)timeSpan.TotalHours;

                if (!string.IsNullOrEmpty(dto.Notes))
                {
                    timeReport.Notes = string.IsNullOrEmpty(timeReport.Notes)
                        ? dto.Notes
                        : $"{timeReport.Notes}\n{dto.Notes}";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Staff {timeReport.StaffId} clocked out at {dto.ClockOut}, worked {timeReport.HoursWorked:F2} hours");

                return new TimeReportSummaryDto
                {
                    Id = timeReport.Id,
                    ClockIn = timeReport.ClockIn,
                    ClockOut = timeReport.ClockOut,
                    HoursWorked = timeReport.HoursWorked,
                    ActivityType = timeReport.ActivityType,
                    Status = "Completed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error clocking out for time report {dto.Id}");
                throw;
            }
        }

        public async Task<List<TimeReportSummaryDto>> GetTimeReportsAsync(int staffId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.TimeReports
                .Where(tr => tr.StaffId == staffId);

            if (startDate.HasValue)
                query = query.Where(tr => tr.ClockIn >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(tr => tr.ClockIn <= endDate.Value);

            var reports = await query
                .OrderByDescending(tr => tr.ClockIn)
                .Select(tr => new TimeReportSummaryDto
                {
                    Id = tr.Id,
                    ClockIn = tr.ClockIn,
                    ClockOut = tr.ClockOut,
                    HoursWorked = tr.HoursWorked,
                    ActivityType = tr.ActivityType,
                    Status = tr.ClockOut == default ? "Active" : "Completed"
                })
                .ToListAsync();

            return reports;
        }

        public async Task<decimal> GetTotalHoursWorkedAsync(int staffId, DateTime startDate, DateTime endDate)
        {
            return await _context.TimeReports
                .Where(tr => tr.StaffId == staffId &&
                            tr.ClockIn >= startDate &&
                            tr.ClockIn <= endDate)
                .SumAsync(tr => tr.HoursWorked);
        }

        public async Task<TimeReportSummaryDto> GetActiveTimeReportAsync(int staffId)
        {
            var activeReport = await _context.TimeReports
                .Where(tr => tr.StaffId == staffId && tr.ClockOut == default)
                .OrderByDescending(tr => tr.ClockIn)
                .FirstOrDefaultAsync();

            if (activeReport == null)
                return null;

            return new TimeReportSummaryDto
            {
                Id = activeReport.Id,
                ClockIn = activeReport.ClockIn,
                ClockOut = null,
                HoursWorked = 0,
                ActivityType = activeReport.ActivityType,
                Status = "Active"
            };
        }

        #endregion

        #region Invoices

        public async Task<List<Invoice>> GetStaffRelatedInvoicesAsync(int staffId)
        {
            var invoices = await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Appointment)
                .Where(i => i.Appointment.StaffId == staffId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return invoices;
        }

        public async Task<decimal> GetStaffEarningsAsync(int staffId, DateTime startDate, DateTime endDate)
        {
            var hoursWorked = await GetTotalHoursWorkedAsync(staffId, startDate, endDate);

            var staff = await _context.Staff.FindAsync(staffId);
            if (staff == null)
                return 0;

            return hoursWorked * staff.HourlyRate;
        }

        #endregion

        #region Vacation Management

        public async Task<int> GetRemainingVacationDaysAsync(int staffId)
        {
            var staff = await _context.Staff.FindAsync(staffId);
            if (staff == null)
                return 0;

            var usedDays = await GetUsedVacationDaysAsync(staffId);
            return staff.Vacationdays - usedDays;
        }

        public async Task<int> GetUsedVacationDaysAsync(int staffId)
        {
            return 0;
        }

        public async Task<bool> UpdateVacationDaysAsync(int staffId, int days)
        {
            try
            {
                var staff = await _context.Staff.FindAsync(staffId);
                if (staff == null)
                    return false;

                staff.Vacationdays = days;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Updated vacation days for staff {staffId} to {days}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vacation days for staff {staffId}");
                throw;
            }
        }

        #endregion

        #region Statistics & Reports

        public async Task<Dictionary<string, int>> GetAppointmentStatisticsByStatusAsync(int staffId, DateTime startDate, DateTime endDate)
        {
            var appointments = await _context.Appointments
                .Where(a => a.StaffId == staffId &&
                           a.AppointmentDate >= startDate &&
                           a.AppointmentDate <= endDate)
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return appointments.ToDictionary(x => x.Status, x => x.Count);
        }

        public async Task<Dictionary<string, decimal>> GetHoursWorkedByActivityTypeAsync(int staffId, DateTime startDate, DateTime endDate)
        {
            var timeReports = await _context.TimeReports
                .Where(tr => tr.StaffId == staffId &&
                            tr.ClockIn >= startDate &&
                            tr.ClockIn <= endDate)
                .GroupBy(tr => tr.ActivityType)
                .Select(g => new { ActivityType = g.Key, Hours = g.Sum(tr => tr.HoursWorked) })
                .ToListAsync();

            return timeReports.ToDictionary(x => x.ActivityType, x => x.Hours);
        }

        public async Task<List<StaffDto>> GetTopPerformingStaffAsync(int count = 10)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var topStaff = await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Appointments)
                .Include(s => s.TimeReports)
                .Select(s => new
                {
                    Staff = s,
                    AppointmentCount = s.Appointments.Count(a => a.AppointmentDate >= monthStart),
                    HoursWorked = s.TimeReports
                        .Where(tr => tr.ClockIn >= monthStart)
                        .Sum(tr => tr.HoursWorked)
                })
                .OrderByDescending(x => x.AppointmentCount)
                .ThenByDescending(x => x.HoursWorked)
                .Take(count)
                .ToListAsync();

            return topStaff.Select(x => MapToStaffDto(x.Staff)).ToList();
        }

        #endregion

        #region Search & Filter

        public async Task<List<StaffDto>> SearchStaffAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllStaffAsync();

            searchTerm = searchTerm.ToLower().Trim();

            var staffList = await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Appointments)
                .Include(s => s.TimeReports)
                .Where(s =>
                    s.User.UserName.ToLower().Contains(searchTerm) ||
                    s.User.Email.ToLower().Contains(searchTerm) ||
                    s.Department.ToLower().Contains(searchTerm) ||
                    s.Specialization.ToLower().Contains(searchTerm))
                .ToListAsync();

            return staffList.Select(MapToStaffDto).ToList();
        }

        public async Task<List<StaffDto>> FilterStaffAsync(string department = null, string specialization = null, string contractForm = null)
        {
            var query = _context.Staff
                .Include(s => s.User)
                .Include(s => s.Appointments)
                .Include(s => s.TimeReports)
                .AsQueryable();

            if (!string.IsNullOrEmpty(department))
                query = query.Where(s => s.Department == department);

            if (!string.IsNullOrEmpty(specialization))
                query = query.Where(s => s.Specialization == specialization);

            if (!string.IsNullOrEmpty(contractForm))
                query = query.Where(s => s.ContractForm == contractForm);

            var staffList = await query.ToListAsync();

            return staffList.Select(MapToStaffDto).ToList();
        }

        public async Task<bool> StaffExistsAsync(int id)
        {
            return await _context.Staff.AnyAsync(s => s.Id == id);
        }

        public async Task<bool> StaffExistsByUserIdAsync(string userId)
        {
            return await _context.Staff.AnyAsync(s => s.UserId == userId);
        }

        #endregion

        #region Helper Methods

        private StaffDto MapToStaffDto(Staff staff)
        {
            var usedVacationDays = CalculateUsedVacationDays(staff);

            return new StaffDto
            {
                Id = staff.Id,
                UserId = staff.UserId,
                UserName = staff.User?.UserName ?? "N/A",
                Email = staff.User?.Email ?? "N/A",
                PhoneNumber = staff.User?.PhoneNumber ?? "N/A",
                ContractForm = staff.ContractForm,
                Department = staff.Department,
                Taxes = staff.Taxes,
                Bankdetails = staff.Bankdetails,
                Vacationdays = staff.Vacationdays,
                UsedVacationDays = usedVacationDays,
                RemainingVacationDays = staff.Vacationdays - usedVacationDays,
                Specialization = staff.Specialization,
                HourlyRate = staff.HourlyRate,
                HiredDate = staff.HiredDate,
                TotalAppointments = staff.Appointments?.Count ?? 0,
                TotalHoursWorked = staff.TimeReports?.Sum(tr => tr.HoursWorked) ?? 0
            };
        }

        private int CalculateUsedVacationDays(Staff staff)
        {
            return 0;
        }

        #endregion
    }
}