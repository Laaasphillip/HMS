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
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddRadzenComponents();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();


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

app.Run();