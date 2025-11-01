using HMS.DTOs;
using HMS.Models;

namespace HMS.Services.Interfaces
{
    public interface IStaffService
    {
        // CRUD
        Task<StaffDto> CreateStaffAsync(CreateStaffDto dto);
        Task<StaffDto> GetStaffByIdAsync(int id);
        Task<StaffDto> GetStaffByUserIdAsync(string userId);
        Task<List<StaffDto>> GetAllStaffAsync();
        Task<List<StaffDto>> GetStaffByDepartmentAsync(string department);
        Task<StaffDto> UpdateStaffAsync(UpdateStaffDto dto);
        Task<bool> DeleteStaffAsync(int id);

        
        Task<StaffDashboardDto> GetStaffDashboardAsync(int staffId);
        Task<StaffDashboardDto> GetStaffDashboardByUserIdAsync(string userId);

       
        Task<StaffProfileDto> GetStaffProfileAsync(int staffId);
        Task<StaffProfileDto> GetStaffProfileByUserIdAsync(string userId);

       
        Task<List<AppointmentSummaryDto>> GetStaffAppointmentsAsync(int staffId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<AppointmentSummaryDto>> GetTodayAppointmentsAsync(int staffId);
        Task<List<AppointmentSummaryDto>> GetUpcomingAppointmentsAsync(int staffId, int days = 7);
        Task<int> GetAppointmentCountAsync(int staffId, DateTime startDate, DateTime endDate);

        Task<List<ScheduleSummaryDto>> GetStaffScheduleAsync(int staffId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<ScheduleSummaryDto>> GetWeekScheduleAsync(int staffId);
        Task<ScheduleSummaryDto> GetCurrentScheduleAsync(int staffId);
        Task<bool> IsStaffAvailableAsync(int staffId, DateTime dateTime);

        Task<TimeReportSummaryDto> ClockInAsync(CreateTimeReportDto dto);
        Task<TimeReportSummaryDto> ClockOutAsync(CompleteTimeReportDto dto);
        Task<List<TimeReportSummaryDto>> GetTimeReportsAsync(int staffId, DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetTotalHoursWorkedAsync(int staffId, DateTime startDate, DateTime endDate);
        Task<TimeReportSummaryDto> GetActiveTimeReportAsync(int staffId);

        Task<List<Invoice>> GetStaffRelatedInvoicesAsync(int staffId);
        Task<decimal> GetStaffEarningsAsync(int staffId, DateTime startDate, DateTime endDate);

        
        Task<int> GetRemainingVacationDaysAsync(int staffId);
        Task<int> GetUsedVacationDaysAsync(int staffId);
        Task<bool> UpdateVacationDaysAsync(int staffId, int days);

       
        Task<Dictionary<string, int>> GetAppointmentStatisticsByStatusAsync(int staffId, DateTime startDate, DateTime endDate);
        Task<Dictionary<string, decimal>> GetHoursWorkedByActivityTypeAsync(int staffId, DateTime startDate, DateTime endDate);
        Task<List<StaffDto>> GetTopPerformingStaffAsync(int count = 10);

        
        Task<List<StaffDto>> SearchStaffAsync(string searchTerm);
        Task<List<StaffDto>> FilterStaffAsync(string department = null, string specialization = null, string contractForm = null);
        Task<bool> StaffExistsAsync(int id);
        Task<bool> StaffExistsByUserIdAsync(string userId);
    }
}