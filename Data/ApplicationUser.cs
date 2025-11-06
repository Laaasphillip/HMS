using HMS.Models;
using Microsoft.AspNetCore.Identity;

namespace HMS.Data
{
   
    public class ApplicationUser : IdentityUser
    {
        public Patient? Patient { get; set; }
        public Staff? Staff { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Role { get; set; }
        public int? PersonalNumber { get; set; }
    }

}
