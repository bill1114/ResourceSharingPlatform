using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Services;
using ResourceSharingPlatform.Services.GoogleSheets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Google Sheets backend (replaces EF Core / SQL Server for this trial deployment)
builder.Services.Configure<GoogleSheetsOptions>(builder.Configuration.GetSection(GoogleSheetsOptions.SectionName));
builder.Services.AddHttpClient<GoogleSheetsClient>();
builder.Services.AddScoped<SheetsDataStore>();

// Authentication / Authorization
builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Add Services
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<SupplyTransferService>();
builder.Services.AddScoped<SupplyOutboundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

await AdminSeeder.SeedAdminAsync(app.Services);

app.Run();
