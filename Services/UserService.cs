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
}
