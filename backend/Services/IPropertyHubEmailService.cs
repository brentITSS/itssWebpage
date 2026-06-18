namespace backend.Services;

public interface IPropertyHubEmailService
{
    bool IsConfigured { get; }

    Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default);
}
