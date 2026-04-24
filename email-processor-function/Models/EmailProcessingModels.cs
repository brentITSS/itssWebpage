namespace email_processor_function.Models;

public class ProcessPropertyHubEmailsRequest
{
    public string? MailboxUser { get; set; }
    public int? MaxEmails { get; set; }
}

public class ProcessPropertyHubEmailsResponse
{
    public string Status { get; set; } = "ok";
    public string MailboxUser { get; set; } = string.Empty;
    public string FolderPath { get; set; } = "Inbox/Property Hub";
    public int FolderTotalItemCount { get; set; }
    public int FolderUnreadItemCount { get; set; }
    public int ReturnedCount { get; set; }
    public List<EmailMessagePreview> Messages { get; set; } = new();
}

public class EmailMessagePreview
{
    public string MessageId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? From { get; set; }
    public DateTimeOffset? ReceivedDateTime { get; set; }
    public bool HasAttachments { get; set; }
}
