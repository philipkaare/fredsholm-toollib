using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;
using ToolLib.Data;
using ToolLib.Models;
using ToolLib.Services;

// Load .env file for local development (Docker Compose injects these as real env vars)
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null)
    {
        var candidate = Path.Combine(dir.FullName, ".env");
        if (File.Exists(candidate))
        {
            foreach (var line in File.ReadAllLines(candidate))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
                var sep = trimmed.IndexOf('=');
                if (sep <= 0) continue;
                var key = trimmed[..sep].Trim();
                var value = trimmed[(sep + 1)..].Trim();
                if (Environment.GetEnvironmentVariable(key) == null)
                    Environment.SetEnvironmentVariable(key, value);
            }
            break;
        }
        dir = dir.Parent;
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/adgang-naegtet";
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddRadzenComponents();
builder.Services.AddScoped<MongoImageService>();
builder.Services.AddScoped<ToolService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<UserRequestService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        
        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
        
        var adminEmail = builder.Configuration["AdminUser:Email"] ?? "admin@toollib.dk";
        var passwordFromConfig = builder.Configuration["AdminUser:Password"];
        var passwordFromEnv = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        var adminPassword = passwordFromConfig ?? passwordFromEnv;

        logger.LogInformation("Admin password source: {Source}, length: {Length}, hasUpper: {HasUpper}, hasDigit: {HasDigit}, hasSpecial: {HasSpecial}",
            passwordFromConfig != null ? "AdminUser:Password (config)" : (passwordFromEnv != null ? "ADMIN_PASSWORD (env)" : "none"),
            adminPassword?.Length ?? 0,
            adminPassword?.Any(char.IsUpper) ?? false,
            adminPassword?.Any(char.IsDigit) ?? false,
            adminPassword?.Any(c => !char.IsLetterOrDigit(c)) ?? false);

        if (string.IsNullOrEmpty(adminPassword))
            throw new InvalidOperationException("ADMIN_PASSWORD must be set in .env or as an environment variable.");
        
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                var createdAdmin = await userManager.FindByIdAsync(admin.Id);
                if (createdAdmin != null)
                    await userManager.AddToRoleAsync(createdAdmin, "Admin");
                else
                    logger.LogError("Admin user was created but could not be retrieved for role assignment.");
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred seeding the database.");
    }
}

app.Run();
