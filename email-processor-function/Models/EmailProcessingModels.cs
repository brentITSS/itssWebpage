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
    public int EligibleCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SkippedCompletedCount { get; set; }
    public int UnclassifiedCount { get; set; }
    public List<EmailMessagePreview> Messages { get; set; } = new();
}

public class EmailMessagePreview
{
    public string MessageId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? From { get; set; }
    public DateTimeOffset? ReceivedDateTime { get; set; }
    public bool HasAttachments { get; set; }
    public List<string> Categories { get; set; } = new();
    public string ProcessingStatus { get; set; } = "processed";
    public string ClassificationLabel { get; set; } = "Unclassified";
    public double ClassificationScore { get; set; }
    public string ClassificationExplainability { get; set; } = string.Empty;
    public List<AttachmentPreview> Attachments { get; set; } = new();
}

public class AttachmentPreview
{
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string ExtractionStatus { get; set; } = "not_processed";
}

public class ClassificationTemplate
{
    public int DocumentClassificationLabelId { get; set; }
    public string ClassificationLabel { get; set; } = string.Empty;
    public string? ClassificationDescription { get; set; }
    public string ClassificationPrompt { get; set; } = string.Empty;
}

public class WorkflowRule
{
    public int DocumentWorkflowRuleId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public string ClassificationLabel { get; set; } = string.Empty;
    public double MinimumScore { get; set; }
    public int Priority { get; set; }
    public bool StopOnFailure { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public List<WorkflowStep> Steps { get; set; } = new();
}

public class WorkflowStep
{
    public int StepOrder { get; set; }
    public string StepType { get; set; } = string.Empty;
    public string? StepConfigJson { get; set; }
}
