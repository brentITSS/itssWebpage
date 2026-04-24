using Azure.Identity;
using email_processor_function.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace email_processor_function.Services;

public interface IGraphEmailReader
{
    Task<ProcessPropertyHubEmailsResponse> ReadPropertyHubFolderAsync(
        ProcessPropertyHubEmailsRequest request,
        CancellationToken cancellationToken = default);
}

public class GraphEmailReader : IGraphEmailReader
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GraphEmailReader> _logger;

    public GraphEmailReader(IConfiguration configuration, ILogger<GraphEmailReader> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ProcessPropertyHubEmailsResponse> ReadPropertyHubFolderAsync(
        ProcessPropertyHubEmailsRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequired("Graph:TenantId", "Graph__TenantId");
        var clientId = GetRequired("Graph:ClientId", "Graph__ClientId");
        var clientSecret = GetRequired("Graph:ClientSecret", "Graph__ClientSecret");
        var configuredMailbox = GetRequired("Graph:MailboxUser", "Graph__MailboxUser");
        var allowedDomain = _configuration["Graph:AllowedDomain"] ??
                            Environment.GetEnvironmentVariable("Graph__AllowedDomain") ??
                            "itsystemsolutions.co.uk";

        var mailboxUser = (request.MailboxUser ?? configuredMailbox).Trim();
        if (!mailboxUser.EndsWith($"@{allowedDomain}", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Mailbox user '{mailboxUser}' is not allowed. Expected domain: @{allowedDomain}");
        }

        var maxEmails = Math.Clamp(request.MaxEmails ?? 20, 1, 100);

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });

        _logger.LogInformation("Reading Inbox/Property Hub for mailbox {MailboxUser}", mailboxUser);

        var folders = await graphClient.Users[mailboxUser]
            .MailFolders["Inbox"]
            .ChildFolders
            .GetAsync(cfg =>
            {
                cfg.QueryParameters.Filter = "displayName eq 'Property Hub'";
                cfg.QueryParameters.Select = new[] { "id", "displayName", "totalItemCount", "unreadItemCount" };
                cfg.QueryParameters.Top = 1;
            }, cancellationToken);

        var propertyHubFolder = folders?.Value?.FirstOrDefault();
        if (propertyHubFolder?.Id == null)
        {
            throw new InvalidOperationException("Could not find folder 'Inbox/Property Hub' for mailbox.");
        }

        var messagesPage = await graphClient.Users[mailboxUser]
            .MailFolders[propertyHubFolder.Id]
            .Messages
            .GetAsync(cfg =>
            {
                cfg.QueryParameters.Top = maxEmails;
                cfg.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                cfg.QueryParameters.Select = new[] { "id", "subject", "from", "receivedDateTime", "hasAttachments" };
            }, cancellationToken);

        var response = new ProcessPropertyHubEmailsResponse
        {
            MailboxUser = mailboxUser,
            FolderTotalItemCount = propertyHubFolder.TotalItemCount ?? 0,
            FolderUnreadItemCount = propertyHubFolder.UnreadItemCount ?? 0,
            Messages = (messagesPage?.Value ?? new List<Message>())
                .Select(x => new EmailMessagePreview
                {
                    MessageId = x.Id ?? string.Empty,
                    Subject = x.Subject ?? "(no subject)",
                    From = x.From?.EmailAddress?.Address,
                    ReceivedDateTime = x.ReceivedDateTime,
                    HasAttachments = x.HasAttachments ?? false
                })
                .ToList()
        };

        response.ReturnedCount = response.Messages.Count;
        return response;
    }

    private string GetRequired(string configKey, string envKey)
    {
        var value = _configuration[configKey] ?? Environment.GetEnvironmentVariable(envKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required configuration is missing: {envKey}");
        }

        return value;
    }
}
