using System.Net.Mail;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;

namespace backend.Services;

public class GraphPropertyHubEmailService : IPropertyHubEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GraphPropertyHubEmailService> _logger;

    public GraphPropertyHubEmailService(
        IConfiguration configuration,
        ILogger<GraphPropertyHubEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(GetConfig("Graph:TenantId", "Graph__TenantId"))
        && !string.IsNullOrWhiteSpace(GetConfig("Graph:ClientId", "Graph__ClientId"))
        && !string.IsNullOrWhiteSpace(GetConfig("Graph:ClientSecret", "Graph__ClientSecret"))
        && !string.IsNullOrWhiteSpace(GetConfig("Graph:MailboxUser", "Graph__MailboxUser"));

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequired("Graph:TenantId", "Graph__TenantId");
        var clientId = GetRequired("Graph:ClientId", "Graph__ClientId");
        var clientSecret = GetRequired("Graph:ClientSecret", "Graph__ClientSecret");
        var mailboxUser = GetRequired("Graph:MailboxUser", "Graph__MailboxUser");

        var normalizedTo = toEmail.Trim();
        if (!IsValidEmail(normalizedTo))
        {
            throw new InvalidOperationException($"Recipient email address '{toEmail}' is not valid.");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException("Email subject is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("Email body is required.");
        }

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });

        var message = new Message
        {
            Subject = subject.Trim(),
            Body = new ItemBody
            {
                ContentType = isHtml ? BodyType.Html : BodyType.Text,
                Content = body,
            },
            ToRecipients = new List<Recipient>
            {
                new()
                {
                    EmailAddress = new EmailAddress { Address = normalizedTo },
                },
            },
        };

        _logger.LogInformation("Sending Property Hub email from {MailboxUser} to {Recipient}", mailboxUser, normalizedTo);

        await graphClient.Users[mailboxUser].SendMail.PostAsync(
            new SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = true,
            },
            cancellationToken: cancellationToken);
    }

    private string? GetConfig(string configKey, string envKey)
    {
        return _configuration[configKey] ?? Environment.GetEnvironmentVariable(envKey);
    }

    private string GetRequired(string configKey, string envKey)
    {
        var value = GetConfig(configKey, envKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Property Hub email is not configured. Set '{configKey}' or '{envKey}' (and other Graph settings).");
        }

        return value.Trim();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return email.Contains('@', StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
