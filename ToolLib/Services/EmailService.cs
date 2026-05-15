using System.Data;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ToolLib.Data;
using ToolLib.Models;

namespace ToolLib.Services;

public class EmailService : IEmailService
{
    private const int DailyLimit = 20;

    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public EmailService(HttpClient http, IConfiguration config, ILogger<EmailService> logger, IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _dbFactory = dbFactory;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var apiToken = _config["MailerSend:ApiToken"];
        var fromEmail = _config["MailerSend:FromEmail"];
        if (string.IsNullOrEmpty(apiToken) || string.IsNullOrEmpty(fromEmail))
        {
            _logger.LogWarning("MailerSend not configured — email to {To} with subject '{Subject}' was not sent", to, subject);
            return;
        }

        if (!await TryIncrementRateLimitAsync())
        {
            _logger.LogWarning("Daily email rate limit ({Limit}) reached — email to {To} with subject '{Subject}' was not sent", DailyLimit, to, subject);
            return;
        }

        var fromName = _config["MailerSend:FromName"];
        var from = string.IsNullOrEmpty(fromName)
            ? (object)new { email = fromEmail }
            : new { email = fromEmail, name = fromName };

        var payload = new
        {
            from,
            to = new[] { new { email = to } },
            subject,
            html = htmlBody,
            text = StripHtml(htmlBody)
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mailersend.com/v1/email");
        request.Headers.Add("Authorization", $"Bearer {apiToken}");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("MailerSend API returned {Status} for email to {To}: {Body}", response.StatusCode, to, body);
            response.EnsureSuccessStatusCode();
        }
    }

    private async Task<bool> TryIncrementRateLimitAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await using var db = _dbFactory.CreateDbContext();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var record = await db.EmailRateLimits.FindAsync(today);
        if (record == null)
        {
            db.EmailRateLimits.Add(new EmailRateLimit { Date = today, Count = 1 });
        }
        else if (record.Count >= DailyLimit)
        {
            return false;
        }
        else
        {
            record.Count++;
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }

    private static string StripHtml(string html)
    {
        var withoutTags = Regex.Replace(html, "<[^>]+>", " ");
        var decoded = System.Net.WebUtility.HtmlDecode(withoutTags);
        return Regex.Replace(decoded, @"\s+", " ").Trim();
    }
}
