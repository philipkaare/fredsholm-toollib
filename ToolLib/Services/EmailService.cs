using MailKit.Net.Smtp;
using MimeKit;

namespace ToolLib.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var host = _config["Smtp:Host"];
        if (string.IsNullOrEmpty(host))
        {
            _logger.LogWarning("SMTP not configured — email to {To} with subject '{Subject}' was not sent", to, subject);
            return;
        }

        var message = new MimeMessage();
        var fromAddress = _config["Smtp:From"] ?? _config["Smtp:Username"] ?? "noreply@toollib.dk";
        message.From.Add(MailboxAddress.Parse(fromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        var port = int.Parse(_config["Smtp:Port"] ?? "587");
        var useSsl = bool.Parse(_config["Smtp:UseSsl"] ?? "true");
        await client.ConnectAsync(host, port, useSsl);

        var username = _config["Smtp:Username"];
        var smtpPassword = _config["Smtp:Password"] ?? string.Empty;
        if (!string.IsNullOrEmpty(username))
            await client.AuthenticateAsync(username, smtpPassword);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
