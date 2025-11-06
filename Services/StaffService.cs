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

        #region CRUD Operations (STAFF)

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

        #region APPOINTMENT Management (Create, Read, Update, Delete)

        /// <summary>
        /// Create a new appointment for this staff member
        /// </summary>
        public async Task<Appointment> CreateAppointmentAsync(int staffId, CreateAppointmentDto dto)
        {
            try
            {
                // Verify staff exists
                if (!await StaffExistsAsync(staffId))
                    throw new KeyNotFoundException($"Staff with ID {staffId} not found.");

                // Verify patient exists
                var patientExists = await _context.Patients.AnyAsync(p => p.Id == dto.PatientId);
                if (!patientExists)
                    throw new KeyNotFoundException($"Patient with ID {dto.PatientId} not found.");

                // Check if staff is available at the requested time
                var isAvailable = await IsStaffAvailableAsync(staffId, dto.AppointmentDate);
                if (!isAvailable)
                    throw new InvalidOperationException("Staff is not available at the requested time.");

                var appointment = new Appointment
                {
                    PatientId = dto.PatientId,
                    StaffId = staffId,
                    ScheduleId = dto.ScheduleId,
                    AppointmentDate = dto.AppointmentDate,
                    DurationMinutes = dto.DurationMinutes,
                    Status = "Scheduled",
                    Reason = dto.Reason,
                    Notes = dto.Notes,
                    CreatedBy = staffId.ToString(),
                    Price = dto.Price,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment created by staff {staffId} for patient {dto.PatientId}");

                return appointment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating appointment for staff {staffId}");
                throw;
            }
        }

        /// <summary>
        /// Update an existing appointment (only staff's own appointments)
        /// </summary>
        public async Task<Appointment> UpdateAppointmentAsync(int staffId, int appointmentId, UpdateAppointmentDto dto)
        {
            try
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == appointmentId && a.StaffId == staffId);

                if (appointment == null)
                    throw new KeyNotFoundException($"Appointment {appointmentId} not found for staff {staffId}");

                // Update fields if provided
                if (dto.AppointmentDate.HasValue)
                {
                    var isAvailable = await IsStaffAvailableAsync(staffId, dto.AppointmentDate.Value);
                    if (!isAvailable)
                        throw new InvalidOperationException("Staff is not available at the requested time.");

                    appointment.AppointmentDate = dto.AppointmentDate.Value;
                }

                if (dto.DurationMinutes.HasValue)
                    appointment.DurationMinutes = dto.DurationMinutes.Value;

                if (!string.IsNullOrEmpty(dto.Status))
                    appointment.Status = dto.Status;

                if (!string.IsNullOrEmpty(dto.Reason))
                    appointment.Reason = dto.Reason;

                if (!string.IsNullOrEmpty(dto.Notes))
                    appointment.Notes = dto.Notes;

                if (dto.Price.HasValue)
                    appointment.Price = dto.Price.Value;

                appointment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment {appointmentId} updated by staff {staffId}");

                return appointment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating appointment {appointmentId}");
                throw;
            }
        }

        /// <summary>
        /// Delete an appointment (only staff's own appointments)
        /// </summary>
        public async Task<bool> DeleteAppointmentAsync(int staffId, int appointmentId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == appointmentId && a.StaffId == staffId);

                if (appointment == null)
                    return false;

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment {appointmentId} deleted by staff {staffId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting appointment {appointmentId}");
                throw;
            }
        }

        /// <summary>
        /// Get appointment by ID (only staff's own appointments)
        /// </summary>
        public async Task<Appointment> GetAppointmentByIdAsync(int staffId, int appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.StaffId == staffId);
        }

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
                .ToListAsync();

            return appointments.Select(a => new AppointmentSummaryDto
            {
                Id = a.Id,
                PatientName = a.Patient.User?.UserName ?? "N/A",
                AppointmentDate = a.AppointmentDate,
                DurationMinutes = a.DurationMinutes,
                Status = a.Status,
                Reason = a.Reason
            }).ToList();
        }

        public async Task<List<AppointmentSummaryDto>> GetTodayAppointmentsAsync(int staffId)
        {
            var today = DateTime.UtcNow.Date;
            return await GetStaffAppointmentsAsync(staffId, today, today.AddDays(1));
        }

        public async Task<List<AppointmentSummaryDto>> GetUpcomingAppointmentsAsync(int staffId, int days = 7)
        {
            var start = DateTime.UtcNow;
            var end = start.AddDays(days);
            return await GetStaffAppointmentsAsync(staffId, start, end);
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

        #region PROFILE Management (Read, Update)

        public async Task<StaffProfileDto> GetStaffProfileAsync(int staffId)
        {
            var staff = await _context.Staff
                .Include(s => s.User)
                .Include(s => s.Appointments)
                .Include(s => s.Schedules)
                .FirstOrDefaultAsync(s => s.Id == staffId);

            if (staff == null)
                throw new KeyNotFoundException($"Staff with ID {staffId} not found.");

            var upcomingAppointments = staff.Appointments
                .Where(a => a.AppointmentDate >= DateTime.UtcNow)
                .OrderBy(a => a.AppointmentDate)
                .Take(5)
                .Select(a => new AppointmentSummaryDto
                {
                    Id = a.Id,
                    PatientName = a.Patient?.User?.UserName ?? "N/A",
                    AppointmentDate = a.AppointmentDate,
                    DurationMinutes = a.DurationMinutes,
                    Status = a.Status,
                    Reason = a.Reason
                }).ToList();

            var weekSchedules = await GetWeekScheduleAsync(staffId);

            var usedVacationDays = await GetUsedVacationDaysAsync(staffId);

            return new StaffProfileDto
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
                UsedVacationDays = usedVacationDays,
                RemainingVacationDays = staff.Vacationdays - usedVacationDays,
                UpcomingAppointments = upcomingAppointments,
                WeekSchedule = weekSchedules
            };
        }

        public async Task<StaffProfileDto> GetStaffProfileByUserIdAsync(string userId)
        {
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == userId);
            if (staff == null)
                throw new KeyNotFoundException($"Staff profile not found for user {userId}");

            return await GetStaffProfileAsync(staff.Id);
        }

        /// <summary>
        /// Update staff profile (own profile only)
        /// </summary>
        public async Task<StaffProfileDto> UpdateStaffProfileAsync(int staffId, UpdateStaffProfileDto dto)
        {
            try
            {
                var staff = await _context.Staff.FindAsync(staffId);
                if (staff == null)
                    throw new KeyNotFoundException($"Staff with ID {staffId} not found.");

                // Update only allowed fields (staff cannot change salary, vacation days, etc.)
                if (!string.IsNullOrEmpty(dto.Bankdetails))
                    staff.Bankdetails = dto.Bankdetails;

                if (!string.IsNullOrEmpty(dto.Specialization))
                    staff.Specialization = dto.Specialization;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Profile updated for staff {staffId}");

                return await GetStaffProfileAsync(staffId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating profile for staff {staffId}");
                throw;
            }
        }

        #endregion

        #region SCHEDULE Management (Read, Update)

        public async Task<List<ScheduleSummaryDto>> GetStaffScheduleAsync(int staffId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Schedules
                .Where(s => s.StaffId == staffId);

            if (startDate.HasValue)
                query = query.Where(s => s.StartTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.EndTime <= endDate.Value);

            var schedules = await query
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            return schedules.Select(s => new ScheduleSummaryDto
            {
                Id = s.Id,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                ShiftType = s.ShiftType,
                Status = s.Status
            }).ToList();
        }

        public async Task<List<ScheduleSummaryDto>> GetWeekScheduleAsync(int staffId)
        {
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
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

        /// <summary>
        /// Update schedule (staff can update their own schedule status)
        /// </summary>
        public async Task<Schedule> UpdateScheduleAsync(int staffId, int scheduleId, UpdateScheduleDto dto)
        {
            try
            {
                var schedule = await _context.Schedules
                    .FirstOrDefaultAsync(s => s.Id == scheduleId && s.StaffId == staffId);

                if (schedule == null)
                    throw new KeyNotFoundException($"Schedule {scheduleId} not found for staff {staffId}");

                // Staff can only update status
                if (!string.IsNullOrEmpty(dto.Status))
                    schedule.Status = dto.Status;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Schedule {scheduleId} updated by staff {staffId}");

                return schedule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating schedule {scheduleId}");
                throw;
            }
        }

        public async Task<bool> IsStaffAvailableAsync(int staffId, DateTime dateTime)
        {
            // Check if staff has a schedule at this time
            var hasSchedule = await _context.Schedules
                .AnyAsync(s => s.StaffId == staffId &&
                              s.StartTime <= dateTime &&
                              s.EndTime >= dateTime);

            if (!hasSchedule)
                return false;

            // Check if staff already has an appointment at this time
            var hasAppointment = await _context.Appointments
                .AnyAsync(a => a.StaffId == staffId &&
                              a.AppointmentDate <= dateTime &&
                              a.AppointmentDate.AddMinutes(a.DurationMinutes) > dateTime);

            return !hasAppointment;
        }

        #endregion

        #region PATIENT Management (Read, Update)

        /// <summary>
        /// Get all patients that this staff member has appointments with
        /// </summary>
        public async Task<List<PatientDto>> GetStaffPatientsAsync(int staffId)
        {
            var patients = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Appointments)
                .Where(p => p.Appointments.Any(a => a.StaffId == staffId))
                .Distinct()
                .ToListAsync();

            return patients.Select(p => new PatientDto
            {
                Id = p.Id,
                UserId = p.UserId,
                UserName = p.User?.UserName ?? "N/A",
                Email = p.User?.Email ?? "N/A",
                PhoneNumber = p.User?.PhoneNumber ?? "N/A",
                Address = p.Address,
                Contact = p.Contact,
                BloodGroup = p.BloodGroup,
                Dateofbirth = p.Dateofbirth,
                Preferences = p.Preferences,
                Interests = p.Interests
            }).ToList();
        }

        /// <summary>
        /// Get patient details (only if staff has appointment with them)
        /// </summary>
        public async Task<PatientDto> GetPatientByIdAsync(int staffId, int patientId)
        {
            // Verify staff has appointment with this patient
            var hasAppointment = await _context.Appointments
                .AnyAsync(a => a.StaffId == staffId && a.PatientId == patientId);

            if (!hasAppointment)
                throw new UnauthorizedAccessException("You don't have access to this patient's information.");

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            return new PatientDto
            {
                Id = patient.Id,
                UserId = patient.UserId,
                UserName = patient.User?.UserName ?? "N/A",
                Email = patient.User?.Email ?? "N/A",
                PhoneNumber = patient.User?.PhoneNumber ?? "N/A",
                Address = patient.Address,
                Contact = patient.Contact,
                BloodGroup = patient.BloodGroup,
                Dateofbirth = patient.Dateofbirth,
                Preferences = patient.Preferences,
                Interests = patient.Interests
            };
        }

        /// <summary>
        /// Update patient information (only if staff has appointment with them)
        /// </summary>
        public async Task<PatientDto> UpdatePatientAsync(int staffId, int patientId, UpdatePatientDto dto)
        {
            try
            {
                // Verify staff has appointment with this patient
                var hasAppointment = await _context.Appointments
                    .AnyAsync(a => a.StaffId == staffId && a.PatientId == patientId);

                if (!hasAppointment)
                    throw new UnauthorizedAccessException("You don't have access to update this patient's information.");

                var patient = await _context.Patients.FindAsync(patientId);
                if (patient == null)
                    throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

                // Update information
                if (!string.IsNullOrEmpty(dto.Contact))
                    patient.Contact = dto.Contact;

                if (!string.IsNullOrEmpty(dto.BloodGroup))
                    patient.BloodGroup = dto.BloodGroup;

                if (!string.IsNullOrEmpty(dto.Preferences))
                    patient.Preferences = dto.Preferences;

                if (!string.IsNullOrEmpty(dto.Interests))
                    patient.Interests = dto.Interests;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Patient {patientId} updated by staff {staffId}");

                return await GetPatientByIdAsync(staffId, patientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating patient {patientId}");
                throw;
            }
        }

        #endregion

        #region INVOICE Management (Create, Read, Update, Delete)

        /// <summary>
        /// Create invoice for an appointment
        /// </summary>
        public async Task<Invoice> CreateInvoiceAsync(int staffId, CreateInvoiceDto dto)
        {
            try
            {
                // Verify appointment belongs to this staff
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId && a.StaffId == staffId);

                if (appointment == null)
                    throw new KeyNotFoundException($"Appointment {dto.AppointmentId} not found for staff {staffId}");

                // Check if invoice already exists for this appointment
                var existingInvoice = await _context.Invoices
                    .AnyAsync(i => i.AppointmentId == dto.AppointmentId);

                if (existingInvoice)
                    throw new InvalidOperationException("Invoice already exists for this appointment.");

                var invoice = new Invoice
                {
                    AppointmentId = dto.AppointmentId,
                    PatientId = appointment.PatientId,
                    InvoiceNumber = GenerateInvoiceNumber(),
                    DueDate = dto.DueDate ?? DateTime.UtcNow.AddDays(30),
                    SubTotal = 0,
                    TaxAmount = 0,
                    TotalAmount = 0,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Add invoice items
                if (dto.Items != null && dto.Items.Any())
                {
                    decimal subTotal = 0;
                    foreach (var itemDto in dto.Items)
                    {
                        var item = new InvoiceItem
                        {
                            InvoiceId = invoice.Id,
                            Description = itemDto.Description,
                            Quantity = itemDto.Quantity,
                            UnitPrice = itemDto.UnitPrice,
                            TotalPrice = itemDto.Quantity * itemDto.UnitPrice
                        };
                        _context.InvoiceItems.Add(item);
                        subTotal += item.TotalPrice;
                    }

                    await _context.SaveChangesAsync();

                    // Calculate totals
                    invoice.SubTotal = subTotal;
                    invoice.TaxAmount = subTotal * 0.25m; // 25% moms
                    invoice.TotalAmount = subTotal + invoice.TaxAmount;

                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"Invoice created by staff {staffId} for appointment {dto.AppointmentId}");

                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating invoice for staff {staffId}");
                throw;
            }
        }

        /// <summary>
        /// Get invoice by ID (only staff's own invoices)
        /// </summary>
        public async Task<Invoice> GetInvoiceByIdAsync(int staffId, int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                .ThenInclude(p => p.User)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Appointment)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Appointment.StaffId == staffId);

            if (invoice == null)
                throw new KeyNotFoundException($"Invoice {invoiceId} not found for staff {staffId}");

            return invoice;
        }

        /// <summary>
        /// Get all invoices for this staff member
        /// </summary>
        public async Task<List<Invoice>> GetStaffRelatedInvoicesAsync(int staffId)
        {
            var invoices = await _context.Invoices
                .Include(i => i.Patient)
                .ThenInclude(p => p.User)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Appointment)
                .Where(i => i.Appointment.StaffId == staffId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return invoices;
        }

        /// <summary>
        /// Update invoice (only staff's own invoices)
        /// </summary>
        public async Task<Invoice> UpdateInvoiceAsync(int staffId, int invoiceId, UpdateInvoiceDto dto)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Appointment)
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Appointment.StaffId == staffId);

                if (invoice == null)
                    throw new KeyNotFoundException($"Invoice {invoiceId} not found for staff {staffId}");

                // Update fields
                if (dto.DueDate.HasValue)
                    invoice.DueDate = dto.DueDate.Value;

                if (!string.IsNullOrEmpty(dto.Status))
                    invoice.Status = dto.Status;

                // Update items if provided
                if (dto.Items != null && dto.Items.Any())
                {
                    // Remove old items
                    _context.InvoiceItems.RemoveRange(invoice.InvoiceItems);

                    // Add new items
                    decimal subTotal = 0;
                    foreach (var itemDto in dto.Items)
                    {
                        var item = new InvoiceItem
                        {
                            InvoiceId = invoice.Id,
                            Description = itemDto.Description,
                            Quantity = itemDto.Quantity,
                            UnitPrice = itemDto.UnitPrice,
                            TotalPrice = itemDto.Quantity * itemDto.UnitPrice
                        };
                        _context.InvoiceItems.Add(item);
                        subTotal += item.TotalPrice;
                    }

                    await _context.SaveChangesAsync();

                    // Recalculate totals
                    invoice.SubTotal = subTotal;
                    invoice.TaxAmount = subTotal * 0.25m;
                    invoice.TotalAmount = subTotal + invoice.TaxAmount;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Invoice {invoiceId} updated by staff {staffId}");

                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating invoice {invoiceId}");
                throw;
            }
        }

        /// <summary>
        /// Delete invoice (only staff's own invoices)
        /// </summary>
        public async Task<bool> DeleteInvoiceAsync(int staffId, int invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Appointment)
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId && i.Appointment.StaffId == staffId);

                if (invoice == null)
                    return false;

                // Remove invoice items first
                _context.InvoiceItems.RemoveRange(invoice.InvoiceItems);

                // Remove invoice
                _context.Invoices.Remove(invoice);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Invoice {invoiceId} deleted by staff {staffId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting invoice {invoiceId}");
                throw;
            }
        }

        private string GenerateInvoiceNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"INV-{timestamp}-{random}";
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

        #region TIME REPORTS

        public async Task<TimeReportSummaryDto> ClockInAsync(CreateTimeReportDto dto)
        {
            try
            {
                var activeReport = await _context.TimeReports
                    .FirstOrDefaultAsync(tr => tr.StaffId == dto.StaffId && tr.ClockOut == null);

                if (activeReport != null)
                    throw new InvalidOperationException("You already have an active time report. Please clock out first.");

                var timeReport = new TimeReport
                {
                    StaffId = dto.StaffId,
                    ScheduleId = dto.ScheduleId,
                    ClockIn = dto.ClockIn,
                    ActivityType = dto.ActivityType,
                    Notes = dto.Notes
                };

                _context.TimeReports.Add(timeReport);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Staff {dto.StaffId} clocked in");

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
                    throw new KeyNotFoundException($"Time report with ID {dto.Id} not found.");

                if (timeReport.ClockOut.HasValue)
                    throw new InvalidOperationException("This time report has already been clocked out.");

                timeReport.ClockOut = dto.ClockOut;
                timeReport.HoursWorked = (decimal)(dto.ClockOut - timeReport.ClockIn).TotalHours;

                if (!string.IsNullOrEmpty(dto.Notes))
                    timeReport.Notes += "\n" + dto.Notes;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Time report {dto.Id} completed");

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
                _logger.LogError(ex, $"Error clocking out time report {dto.Id}");
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
                .ToListAsync();

            return reports.Select(tr => new TimeReportSummaryDto
            {
                Id = tr.Id,
                ClockIn = tr.ClockIn,
                ClockOut = tr.ClockOut,
                HoursWorked = tr.HoursWorked,
                ActivityType = tr.ActivityType,
                Status = tr.ClockOut.HasValue ? "Completed" : "Active"
            }).ToList();
        }

        public async Task<decimal> GetTotalHoursWorkedAsync(int staffId, DateTime startDate, DateTime endDate)
        {
            var totalHours = await _context.TimeReports
                .Where(tr => tr.StaffId == staffId &&
                            tr.ClockIn >= startDate &&
                            tr.ClockIn <= endDate &&
                            tr.ClockOut.HasValue)
                .SumAsync(tr => tr.HoursWorked);

            return totalHours;
        }

        public async Task<TimeReportSummaryDto> GetActiveTimeReportAsync(int staffId)
        {
            var activeReport = await _context.TimeReports
                .Where(tr => tr.StaffId == staffId && tr.ClockOut == null)
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

        #region VACATION Management

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
            // TODO: Implement when Leave DbSet is added to ApplicationDbContext
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

        #region DASHBOARD

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
                }).ToList();

            var upcomingAppointments = staff.Appointments
                .Where(a => a.AppointmentDate >= now)
                .OrderBy(a => a.AppointmentDate)
                .Take(5)
                .Select(a => new AppointmentSummaryDto
                {
                    Id = a.Id,
                    PatientName = a.Patient?.User?.UserName ?? "N/A",
                    AppointmentDate = a.AppointmentDate,
                    DurationMinutes = a.DurationMinutes,
                    Status = a.Status,
                    Reason = a.Reason
                }).ToList();

            var remainingVacationDays = await GetRemainingVacationDaysAsync(staffId);

            return new StaffDashboardDto
            {
                StaffId = staff.Id,
                StaffName = staff.User?.UserName ?? "N/A",

                // Today stats
                TodayAppointments = todayAppointments.Count,
                TodayCompletedAppointments = todayAppointments.Count(a => a.Status == "Completed"),
                TodayHoursWorked = todayTimeReports.Sum(tr => tr.HoursWorked),

                // Week stats
                WeekAppointments = weekAppointments.Count,
                WeekHoursWorked = weekTimeReports.Sum(tr => tr.HoursWorked),
                WeekEarnings = weekTimeReports.Sum(tr => tr.HoursWorked) * staff.HourlyRate,

                // Month stats
                MonthAppointments = monthAppointments.Count,
                MonthHoursWorked = monthTimeReports.Sum(tr => tr.HoursWorked),
                MonthEarnings = monthTimeReports.Sum(tr => tr.HoursWorked) * staff.HourlyRate,

                // Vacation
                RemainingVacationDays = remainingVacationDays,
                PendingVacations = new List<VacationRequestDto>(), // TODO: Add when Leave DbSet available

                // Upcoming
                UpcomingSchedules = upcomingSchedules,
                UpcomingAppointments = upcomingAppointments
            };
        }

        public async Task<StaffDashboardDto> GetStaffDashboardByUserIdAsync(string userId)
        {
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == userId);
            if (staff == null)
                throw new KeyNotFoundException($"Staff profile not found for user {userId}");

            return await GetStaffDashboardAsync(staff.Id);
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

        #endregion

        #region Helper Methods

        private StaffDto MapToStaffDto(Staff staff)
        {
            var usedVacationDays = 0; // TODO: Calculate when Leave DbSet available

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

        public async Task<bool> StaffExistsAsync(int id)
        {
            return await _context.Staff.AnyAsync(s => s.Id == id);
        }

        public async Task<bool> StaffExistsByUserIdAsync(string userId)
        {
            return await _context.Staff.AnyAsync(s => s.UserId == userId);
        }

        #endregion
    }
}