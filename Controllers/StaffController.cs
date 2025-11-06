using HMS.DTOs;
using HMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _staffService;
        private readonly ILogger<StaffController> _logger;

        public StaffController(IStaffService staffService, ILogger<StaffController> logger)
        {
            _staffService = staffService;
            _logger = logger;
        }

        #region CRUD Operations

       
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<StaffDto>> CreateStaff([FromBody] CreateStaffDto dto)
        {
            try
            {
                var staff = await _staffService.CreateStaffAsync(dto);
                return CreatedAtAction(nameof(GetStaffById), new { id = staff.Id }, staff);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff");
                return StatusCode(500, new { error = "An error occurred while creating staff" });
            }
        }

     
        [HttpGet("{id}")]
        public async Task<ActionResult<StaffDto>> GetStaffById(int id)
        {
            var staff = await _staffService.GetStaffByIdAsync(id);
            if (staff == null)
                return NotFound(new { error = $"Staff with ID {id} not found" });

            return Ok(staff);
        }

     
        [HttpGet("me")]
        public async Task<ActionResult<StaffDto>> GetMyStaffInfo()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var staff = await _staffService.GetStaffByUserIdAsync(userId);
            if (staff == null)
                return NotFound(new { error = "Staff profile not found for current user" });

            return Ok(staff);
        }

    
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<StaffDto>>> GetAllStaff()
        {
            var staffList = await _staffService.GetAllStaffAsync();
            return Ok(staffList);
        }

        
        [HttpGet("department/{department}")]
        public async Task<ActionResult<List<StaffDto>>> GetStaffByDepartment(string department)
        {
            var staffList = await _staffService.GetStaffByDepartmentAsync(department);
            return Ok(staffList);
        }

       
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<StaffDto>> UpdateStaff(int id, [FromBody] UpdateStaffDto dto)
        {
            try
            {
                dto.Id = id;
                var staff = await _staffService.UpdateStaffAsync(dto);
                return Ok(staff);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating staff {id}");
                return StatusCode(500, new { error = "An error occurred while updating staff" });
            }
        }

  
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteStaff(int id)
        {
            try
            {
                var result = await _staffService.DeleteStaffAsync(id);
                if (!result)
                    return NotFound(new { error = $"Staff with ID {id} not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting staff {id}");
                return StatusCode(500, new { error = "An error occurred while deleting staff" });
            }
        }

        #endregion

        #region Dashboard

        
        [HttpGet("{id}/dashboard")]
        public async Task<ActionResult<StaffDashboardDto>> GetStaffDashboard(int id)
        {
            try
            {
                var dashboard = await _staffService.GetStaffDashboardAsync(id);
                return Ok(dashboard);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

      
        [HttpGet("me/dashboard")]
        public async Task<ActionResult<StaffDashboardDto>> GetMyDashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var dashboard = await _staffService.GetStaffDashboardByUserIdAsync(userId);
                return Ok(dashboard);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        #endregion

        #region Profile

    
        [HttpGet("{id}/profile")]
        public async Task<ActionResult<StaffProfileDto>> GetStaffProfile(int id)
        {
            try
            {
                var profile = await _staffService.GetStaffProfileAsync(id);
                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

       
        [HttpGet("me/profile")]
        public async Task<ActionResult<StaffProfileDto>> GetMyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var profile = await _staffService.GetStaffProfileByUserIdAsync(userId);
                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        #endregion

        #region Appointments

      
        [HttpGet("{id}/appointments")]
        public async Task<ActionResult<List<AppointmentSummaryDto>>> GetStaffAppointments(
            int id,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var appointments = await _staffService.GetStaffAppointmentsAsync(id, startDate, endDate);
            return Ok(appointments);
        }

      
        [HttpGet("{id}/appointments/today")]
        public async Task<ActionResult<List<AppointmentSummaryDto>>> GetTodayAppointments(int id)
        {
            var appointments = await _staffService.GetTodayAppointmentsAsync(id);
            return Ok(appointments);
        }

      
        [HttpGet("{id}/appointments/upcoming")]
        public async Task<ActionResult<List<AppointmentSummaryDto>>> GetUpcomingAppointments(
            int id,
            [FromQuery] int days = 7)
        {
            var appointments = await _staffService.GetUpcomingAppointmentsAsync(id, days);
            return Ok(appointments);
        }

     
        [HttpGet("me/appointments")]
        public async Task<ActionResult<List<AppointmentSummaryDto>>> GetMyAppointments(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var staff = await _staffService.GetStaffByUserIdAsync(userId);
            if (staff == null)
                return NotFound(new { error = "Staff profile not found" });

            var appointments = await _staffService.GetStaffAppointmentsAsync(staff.Id, startDate, endDate);
            return Ok(appointments);
        }

        #endregion

        #region Schedule

        [HttpGet("{id}/schedule")]
        public async Task<ActionResult<List<ScheduleSummaryDto>>> GetStaffSchedule(
            int id,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var schedules = await _staffService.GetStaffScheduleAsync(id, startDate, endDate);
            return Ok(schedules);
        }

      
        [HttpGet("{id}/schedule/week")]
        public async Task<ActionResult<List<ScheduleSummaryDto>>> GetWeekSchedule(int id)
        {
            var schedules = await _staffService.GetWeekScheduleAsync(id);
            return Ok(schedules);
        }

       
        [HttpGet("{id}/schedule/current")]
        public async Task<ActionResult<ScheduleSummaryDto>> GetCurrentSchedule(int id)
        {
            var schedule = await _staffService.GetCurrentScheduleAsync(id);
            if (schedule == null)
                return NotFound(new { error = "No active schedule found" });

            return Ok(schedule);
        }

       
        [HttpGet("{id}/availability")]
        public async Task<ActionResult<bool>> CheckAvailability(int id, [FromQuery] DateTime dateTime)
        {
            var isAvailable = await _staffService.IsStaffAvailableAsync(id, dateTime);
            return Ok(new { staffId = id, dateTime, isAvailable });
        }

        #endregion

        #region Time Reports

       
        [HttpPost("me/timereport/clockin")]
        public async Task<ActionResult<TimeReportSummaryDto>> ClockIn([FromBody] CreateTimeReportDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var staff = await _staffService.GetStaffByUserIdAsync(userId);
                if (staff == null)
                    return NotFound(new { error = "Staff profile not found" });

                dto.StaffId = staff.Id;
                var timeReport = await _staffService.ClockInAsync(dto);
                return Ok(timeReport);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

       
        [HttpPost("me/timereport/clockout")]
        public async Task<ActionResult<TimeReportSummaryDto>> ClockOut([FromBody] CompleteTimeReportDto dto)
        {
            try
            {
                var timeReport = await _staffService.ClockOutAsync(dto);
                return Ok(timeReport);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

    
        [HttpGet("{id}/timereports")]
        public async Task<ActionResult<List<TimeReportSummaryDto>>> GetTimeReports(
            int id,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var reports = await _staffService.GetTimeReportsAsync(id, startDate, endDate);
            return Ok(reports);
        }

        
        [HttpGet("me/timereport/active")]
        public async Task<ActionResult<TimeReportSummaryDto>> GetActiveTimeReport()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var staff = await _staffService.GetStaffByUserIdAsync(userId);
            if (staff == null)
                return NotFound(new { error = "Staff profile not found" });

            var activeReport = await _staffService.GetActiveTimeReportAsync(staff.Id);
            if (activeReport == null)
                return NotFound(new { error = "No active time report found" });

            return Ok(activeReport);
        }

        #endregion

        #region Statistics

        
        [HttpGet("{id}/statistics/appointments")]
        public async Task<ActionResult<Dictionary<string, int>>> GetAppointmentStatistics(
            int id,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var statistics = await _staffService.GetAppointmentStatisticsByStatusAsync(id, start, end);
            return Ok(statistics);
        }

        
        [HttpGet("{id}/statistics/hours")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetHoursStatistics(
            int id,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var statistics = await _staffService.GetHoursWorkedByActivityTypeAsync(id, start, end);
            return Ok(statistics);
        }

       
        [HttpGet("statistics/top-performers")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<StaffDto>>> GetTopPerformers([FromQuery] int count = 10)
        {
            var topStaff = await _staffService.GetTopPerformingStaffAsync(count);
            return Ok(topStaff);
        }

        #endregion

        #region Search & Filter

       
        [HttpGet("search")]
        public async Task<ActionResult<List<StaffDto>>> SearchStaff([FromQuery] string searchTerm)
        {
            var staffList = await _staffService.SearchStaffAsync(searchTerm);
            return Ok(staffList);
        }

        
        [HttpGet("filter")]
        public async Task<ActionResult<List<StaffDto>>> FilterStaff(
            [FromQuery] string department = null,
            [FromQuery] string specialization = null,
            [FromQuery] string contractForm = null)
        {
            var staffList = await _staffService.FilterStaffAsync(department, specialization, contractForm);
            return Ok(staffList);
        }

        #endregion
    }
}