using HMS.DTOs;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace YourAppNamespace.Services
{
    public class UserService
    {
        private readonly AuthenticationStateProvider _authStateProvider;
        private ClaimsPrincipal? _cachedUser;

        public UserService(AuthenticationStateProvider authStateProvider)
        {
            _authStateProvider = authStateProvider;
        }

        public async Task<ClaimsPrincipal> GetUserAsync()
        {
            if (_cachedUser == null)
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                _cachedUser = authState.User;
            }

            return _cachedUser;
        }

        public async Task<string?> GetUserIdAsync()
        {
            var user = await GetUserAsync();
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public async Task<string?> GetUserNameAsync()
        {
            var user = await GetUserAsync();
            return user.Identity?.Name;
        }

        public async Task<bool> IsInRoleAsync(string role)
        {
            var user = await GetUserAsync();
            return user.IsInRole(role);
        }
    }
    public class StaffHttpService
    {
        private readonly HttpClient _httpClient;

        public StaffHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<StaffDto>> GetAllStaffAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/staff");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<StaffDto>>() ?? new List<StaffDto>();
                }
                return new List<StaffDto>();
            }
            catch
            {
                throw;
            }
        }

        public async Task<StaffDto?> GetStaffByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<StaffDto>($"/api/staff/{id}");
        }

        public async Task<StaffDto?> CreateStaffAsync(CreateStaffDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/staff", dto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StaffDto>();
        }

        public async Task<StaffDto?> UpdateStaffAsync(int id, UpdateStaffDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/staff/{id}", dto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StaffDto>();
        }

        public async Task<bool> DeleteStaffAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"/api/staff/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<StaffDto>> SearchStaffAsync(string searchTerm)
        {
            var response = await _httpClient.GetAsync($"/api/staff/search?searchTerm={Uri.EscapeDataString(searchTerm)}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<StaffDto>>() ?? new List<StaffDto>();
            }
            return new List<StaffDto>();
        }
    }

}
