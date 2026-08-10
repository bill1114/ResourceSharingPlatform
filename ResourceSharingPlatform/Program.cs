using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddSingleton<UploadPathProvider>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<SupplyTransferService>();
builder.Services.AddScoped<SupplyOutboundService>();
builder.Services.AddScoped<SupplyDonationService>();
builder.Services.AddScoped<SupplyDisposalService>();
builder.Services.AddScoped<AIStockInService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve uploaded files from the shared UploadPathProvider location (see its comment for
// why this isn't just wwwroot/uploads) before the default wwwroot static files, so a file
// found there always wins even if a stale copy still exists under wwwroot from before this
// was introduced.
var uploadPathProvider = app.Services.GetRequiredService<UploadPathProvider>();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPathProvider.Root),
    RequestPath = "/uploads"
});
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

await DbInitializer.EnsureInventoryCatalogTablesAsync(app.Services);
await DbInitializer.BackfillInventoryDefinitionsFromSupplyItemsAsync(app.Services);
await DbInitializer.SeedPresetInventoryCatalogAsync(app.Services);
await DbInitializer.EnsureInventoryItemDefinitionStockTypeAsync(app.Services);
await DbInitializer.SeedAdminAsync(app.Services);
await DbInitializer.MergeDuplicateItemsAsync(app.Services);
await DbInitializer.EnsureLineSettingsAsync(app.Services);
await DbInitializer.EnsureAIStockInSettingsAsync(app.Services);

app.Run();
