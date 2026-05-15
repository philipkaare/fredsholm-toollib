using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToolLib.Data;
using ToolLib.Models;

namespace ToolLib.Services;

public class UserRequestService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<UserRequestService> _logger;

    public UserRequestService(IDbContextFactory<ApplicationDbContext> factory, UserManager<ApplicationUser> userManager,
        IEmailService emailService, IConfiguration config, ILogger<UserRequestService> logger)
    {
        _factory = factory;
        _userManager = userManager;
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    public async Task<List<UserRequest>> GetPendingAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.UserRequests
            .Where(r => r.Status == UserRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();
    }

    public async Task<int> GetPendingCountAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.UserRequests.CountAsync(r => r.Status == UserRequestStatus.Pending);
    }

    public async Task<int> GetTodayEmailCountAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await using var db = await _factory.CreateDbContextAsync();
        return (await db.EmailRateLimits.FindAsync(today))?.Count ?? 0;
    }

    public async Task<bool> CreateAsync(UserRequest request)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var hasExisting = await db.UserRequests
            .AnyAsync(r => r.Email == request.Email && r.Status != UserRequestStatus.Rejected);
        if (hasExisting) return false;

        if (await _userManager.FindByEmailAsync(request.Email) != null) return false;

        db.UserRequests.Add(request);
        await db.SaveChangesAsync();

        var adminEmail = _config["AdminUser:Email"] ?? "admin@toollib.dk";
        var baseUrl = _config["App:BaseUrl"] ?? "https://localhost";
        try
        {
            await _emailService.SendAsync(adminEmail,
                "Ny adgangsanmodning - Fredsholmvej Værktøjsbibliotek",
                $"<p><strong>{request.Name}</strong> ({request.Email}) har anmodet om adgang.</p>" +
                $"<p>Adresse: {System.Web.HttpUtility.HtmlEncode(request.Address)}</p>" +
                (string.IsNullOrEmpty(request.Message) ? "" : $"<p>Besked: {System.Web.HttpUtility.HtmlEncode(request.Message)}</p>") +
                $"<p><a href='{baseUrl}/admin/adgangsanmodninger'>Se og behandl anmodningen her</a></p>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send admin notification for request from {Email}", request.Email);
        }

        return true;
    }

    public async Task ApproveAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var request = await db.UserRequests.FindAsync(id);
        if (request == null || request.Status != UserRequestStatus.Pending) return;

        request.Status = UserRequestStatus.Approved;
        request.ProcessedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        if (await _userManager.FindByEmailAsync(request.Email) == null)
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                FullName = request.Name,
                Address = request.Address
            };
            var tempPw = "Temp!" + Guid.NewGuid().ToString("N")[..8] + "1";
            var result = await _userManager.CreateAsync(user, tempPw);
            if (result.Succeeded)
            {
                var created = await _userManager.FindByEmailAsync(request.Email);
                if (created != null)
                    await _userManager.AddToRoleAsync(created, "User");
            }
        }

        var approvedUser = await _userManager.FindByEmailAsync(request.Email);
        if (approvedUser != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(approvedUser);
            var baseUrl = _config["App:BaseUrl"] ?? "https://localhost";
            var resetUrl = $"{baseUrl}/nulstil-adgangskode?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";
            try
            {
                await _emailService.SendAsync(request.Email,
                    "Velkommen til Fredsholmvej Værktøjsbibliotek!",
                    $"<p>Hej {System.Web.HttpUtility.HtmlEncode(request.Name)},</p>" +
                    $"<p>Din anmodning om adgang til Fredsholmvej Værktøjsbibliotek er godkendt.</p>" +
                    $"<p>Klik på linket herunder for at vælge din adgangskode og komme i gang:</p>" +
                    $"<p><a href='{resetUrl}'>Vælg adgangskode</a></p>" +
                    $"<p>Linket udløber om 24 timer.</p>");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval email to {Email}", request.Email);
            }
        }
    }

    public async Task RejectAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var request = await db.UserRequests.FindAsync(id);
        if (request == null || request.Status != UserRequestStatus.Pending) return;

        request.Status = UserRequestStatus.Rejected;
        request.ProcessedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        try
        {
            await _emailService.SendAsync(request.Email,
                "Adgangsanmodning behandlet - Fredsholmvej Værktøjsbibliotek",
                $"<p>Hej {System.Web.HttpUtility.HtmlEncode(request.Name)},</p>" +
                $"<p>Din anmodning om adgang til Fredsholmvej Værktøjsbibliotek er desværre ikke godkendt denne gang.</p>" +
                $"<p>Kontakt venligst en administrator for mere information.</p>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send rejection email to {Email}", request.Email);
        }
    }
}
