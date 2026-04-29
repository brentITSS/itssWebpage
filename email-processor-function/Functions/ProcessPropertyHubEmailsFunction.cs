using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using email_processor_function.Models;
using email_processor_function.Services;

namespace email_processor_function.Functions;

public class ProcessPropertyHubEmailsFunction
{
    private readonly ILogger<ProcessPropertyHubEmailsFunction> _logger;
    private readonly IGraphEmailReader _graphEmailReader;

    public ProcessPropertyHubEmailsFunction(
        ILogger<ProcessPropertyHubEmailsFunction> logger,
        IGraphEmailReader graphEmailReader)
    {
        _logger = logger;
        _graphEmailReader = graphEmailReader;
    }

    [Function("ProcessPropertyHubEmails")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "process-propertyhub-emails")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation(
                "Property Hub email processing trigger received at {TimestampUtc}.",
                DateTime.UtcNow);

            ProcessPropertyHubEmailsRequest request;
            if (req.ContentLength.GetValueOrDefault() > 0)
            {
                request = await JsonSerializer.DeserializeAsync<ProcessPropertyHubEmailsRequest>(
                              req.Body,
                              new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                          ?? new ProcessPropertyHubEmailsRequest();
            }
            else
            {
                request = new ProcessPropertyHubEmailsRequest();
            }

            var result = await _graphEmailReader.ReadPropertyHubFolderAsync(request, req.HttpContext.RequestAborted);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Inbox/Property Hub emails.");
            var errorSource = ClassifyErrorSource(ex);
            return new ObjectResult(new
            {
                status = "error",
                source = errorSource,
                message = $"[{errorSource}] {ex.Message}"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    private static string ClassifyErrorSource(Exception ex)
    {
        if (ex is InvalidOperationException invalidOp)
        {
            var opMessage = invalidOp.Message.ToLowerInvariant();
            if (opMessage.Contains("required configuration is missing"))
            {
                return "Config";
            }
            if (opMessage.Contains("mailbox user") || opMessage.Contains("folder"))
            {
                return "MailboxAccess";
            }
        }

        var message = ex.ToString().ToLowerInvariant();
        if (message.Contains("clientsecretcredential") ||
            message.Contains("aadsts") ||
            message.Contains("unauthorized") ||
            message.Contains("forbidden"))
        {
            return "GraphAuth";
        }

        if (message.Contains("sql") ||
            message.Contains("connectionstring") ||
            message.Contains("login failed"))
        {
            return "SqlAuth";
        }

        if (message.Contains("graph"))
        {
            return "GraphApi";
        }

        return "Unknown";
    }
}
