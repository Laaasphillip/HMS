using HMS.Components;
using HMS.Components.Account;
using HMS.Data;
using HMS.Services;
using HMS.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;
using YourAppNamespace.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<PatientService>();


builder.Services.AddScoped<IStaffService, StaffService>();

builder.Services.AddHttpClient<StaffHttpService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:5001"); 
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
// Add Authorization policies
builder.Services.AddAuthorizationCore(options =>
{
    // Admin-only policy
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Admin or Staff policy
    options.AddPolicy("AdminOrStaff", policy =>
        policy.RequireRole("Admin", "Staff"));

    // Admin or Patient policy
    options.AddPolicy("AdminOrPatient", policy =>
        policy.RequireRole("Admin", "Patient"));

    // Patient policy
    options.AddPolicy("PatientOnly", policy =>
        policy.RequireRole("Patient"));

    // Staff policy
    options.AddPolicy("StaffOnly", policy =>
        policy.RequireRole("Staff"));

    // All authenticated users (Admin, Staff, Patient)
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());
});
builder.Services.AddRadzenComponents();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<RoleSeedService>();
builder.Services.AddScoped<UserSeedService>();


builder.Services.AddControllers();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.MapControllers();


app.MapAdditionalIdentityEndpoints();
// Seed roles and users on application startup
using (var scope = app.Services.CreateScope())
{
    // Seed roles first
    var roleSeedService = scope.ServiceProvider.GetRequiredService<RoleSeedService>();
    await roleSeedService.SeedRolesAsync();

    // Then seed users with roles
    var userSeedService = scope.ServiceProvider.GetRequiredService<UserSeedService>();
    await userSeedService.SeedUsersAsync();
}

app.Run();