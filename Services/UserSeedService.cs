using HMS.Data;
using HMS.Models;
using Microsoft.AspNetCore.Identity;

namespace HMS.Services
{
    /// <summary>
    /// Service to seed initial test users with roles in the database on application startup
    /// </summary>
    public class UserSeedService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserSeedService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        /// <summary>
        /// Seeds test users with Admin, Staff, and Patient roles
        /// </summary>
        public async Task SeedUsersAsync()
        {
            Console.WriteLine("Starting user seeding...");

            // Seed Admin User
            await SeedAdminUserAsync();

            // Seed Staff User
            await SeedStaffUserAsync();

            // Seed Patient User
            await SeedPatientUserAsync();

            Console.WriteLine("User seeding completed.");
        }

        private async Task SeedAdminUserAsync()
        {
            const string adminEmail = "admin@hms.com";
            const string adminPassword = "Admin@123";

            var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin != null)
            {
                Console.WriteLine($"Admin user '{adminEmail}' already exists.");
                return;
            }

            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                PersonalNumber = "100001"
            };

            var result = await _userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine($"✓ Admin user created: {adminEmail} / {adminPassword}");
            }
            else
            {
                Console.WriteLine($"✗ Error creating admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        private async Task SeedStaffUserAsync()
        {
            const string staffEmail = "staff@hms.com";
            const string staffPassword = "Staff@123";

            var existingStaff = await _userManager.FindByEmailAsync(staffEmail);
            if (existingStaff != null)
            {
                Console.WriteLine($"Staff user '{staffEmail}' already exists.");
                return;
            }

            var staffUser = new ApplicationUser
            {
                UserName = staffEmail,
                Email = staffEmail,
                EmailConfirmed = true,
                FirstName = "John",
                LastName = "Doe",
                PersonalNumber = "200001"
            };

            var result = await _userManager.CreateAsync(staffUser, staffPassword);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(staffUser, "Staff");

                // Create Staff record
                var staff = new Staff
                {
                    UserId = staffUser.Id,
                    Department = "Cardiology",
                    Specialization = "Cardiologist",
                    ContractForm = "Full-time",
                    HourlyRate = 75.00m,
                    Taxes = 0.30m,
                    Vacationdays = 25,
                    Bankdetails = "Bank Account: 1234-5678-9012",
                    HiredDate = DateTime.UtcNow.AddYears(-2)
                };

                _context.Staff.Add(staff);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✓ Staff user created: {staffEmail} / {staffPassword}");
            }
            else
            {
                Console.WriteLine($"✗ Error creating staff user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        private async Task SeedPatientUserAsync()
        {
            const string patientEmail = "patient@hms.com";
            const string patientPassword = "Patient@123";

            var existingPatient = await _userManager.FindByEmailAsync(patientEmail);
            if (existingPatient != null)
            {
                Console.WriteLine($"Patient user '{patientEmail}' already exists.");
                return;
            }

            var patientUser = new ApplicationUser
            {
                UserName = patientEmail,
                Email = patientEmail,
                EmailConfirmed = true,
                FirstName = "Jane",
                LastName = "Smith",
                PersonalNumber = "300001"
            };

            var result = await _userManager.CreateAsync(patientUser, patientPassword);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(patientUser, "Patient");

                // Create Patient record
                var patient = new Patient
                {
                    UserId = patientUser.Id,
                    Dateofbirth = new DateTime(1990, 5, 15),
                    BloodGroup = "O+",
                    Address = "123 Main Street, City, State 12345",
                    Contact = "+1-555-0100",
                    Preferences = "Morning appointments preferred",
                    Interests = "Fitness, Nutrition",
                    Createdat = DateTime.UtcNow
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✓ Patient user created: {patientEmail} / {patientPassword}");
            }
            else
            {
                Console.WriteLine($"✗ Error creating patient user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}
