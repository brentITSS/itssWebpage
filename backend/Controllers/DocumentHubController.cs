using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/document-hub")]
[Authorize]
public class DocumentHubController : ControllerBase
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "from", "this", "that", "are", "was", "were", "into", "onto", "your", "their",
        "have", "has", "had", "will", "shall", "would", "could", "should", "about", "over", "under", "between", "per",
        "each", "any", "all", "not", "but", "can", "may", "might", "than", "then", "also", "such", "via"
    };
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;
    private readonly IDocumentAiService _documentAiService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public DocumentHubController(
        ApplicationDbContext context,
        IAuthService authService,
        IDocumentAiService documentAiService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _context = context;
        _authService = authService;
        _documentAiService = documentAiService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet("label-sets")]
    public async Task<ActionResult<List<DocumentLabelSetDto>>> GetLabelSets()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!HasPropertyHubAccess(currentUser)) return Forbid("Access denied: Property Hub access required.");

        var items = await _context.DocumentLabelSets
            .AsNoTracking()
            .Include(x => x.ClassificationLabels)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync();

        return Ok(items.Select(MapLabelSet).ToList());
    }

    [HttpPost("label-sets")]
    public async Task<ActionResult<DocumentLabelSetDto>> CreateLabelSet([FromBody] CreateDocumentLabelSetRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        if (string.IsNullOrWhiteSpace(request.LabelSetName))
        {
            return BadRequest(new { message = "Label set name is required." });
        }

        var userId = GetCurrentUserId();
        var entity = new DocumentLabelSet
        {
            LabelSetName = request.LabelSetName.Trim(),
            LabelSetDescription = request.LabelSetDescription?.Trim(),
            IsActive = true,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };

        _context.DocumentLabelSets.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(MapLabelSet(entity));
    }

    [HttpPut("label-sets/{labelSetId:int}")]
    public async Task<ActionResult<DocumentLabelSetDto>> UpdateLabelSet(int labelSetId, [FromBody] UpdateDocumentLabelSetRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        var entity = await _context.DocumentLabelSets
            .Include(x => x.ClassificationLabels)
            .FirstOrDefaultAsync(x => x.DocumentLabelSetId == labelSetId);
        if (entity == null)
        {
            return NotFound(new { message = "Label set not found." });
        }

        if (request.LabelSetName != null)
        {
            var normalizedName = request.LabelSetName.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return BadRequest(new { message = "Label set name cannot be empty." });
            }
            entity.LabelSetName = normalizedName;
        }

        if (request.LabelSetDescription != null)
        {
            entity.LabelSetDescription = request.LabelSetDescription.Trim();
        }

        if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
        }

        if (request.Labels != null)
        {
            var replacementLabels = request.Labels
                .Where(x => !string.IsNullOrWhiteSpace(x.ClassificationLabel) && !string.IsNullOrWhiteSpace(x.ClassificationPrompt))
                .Select(x => new DocumentClassificationLabel
                {
                    DocumentLabelSetId = labelSetId,
                    ClassificationLabel = x.ClassificationLabel.Trim(),
                    ClassificationDescription = x.ClassificationDescription?.Trim(),
                    ClassificationPrompt = x.ClassificationPrompt.Trim(),
                    SeedDocumentName = x.SeedDocumentName?.Trim(),
                    SeedDocumentHash = x.SeedDocumentHash?.Trim(),
                    IsAutoGenerated = x.IsAutoGenerated,
                    IsActive = x.IsActive,
                    CreatedByUserId = GetCurrentUserId(),
                    CreatedDate = DateTime.UtcNow
                })
                .ToList();

            _context.DocumentClassificationLabels.RemoveRange(entity.ClassificationLabels);
            entity.ClassificationLabels = replacementLabels;
        }

        entity.UpdatedByUserId = GetCurrentUserId();
        entity.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var reloaded = await _context.DocumentLabelSets
            .AsNoTracking()
            .Include(x => x.ClassificationLabels)
            .FirstOrDefaultAsync(x => x.DocumentLabelSetId == labelSetId);
        if (reloaded == null) return NotFound(new { message = "Label set not found after update." });

        return Ok(MapLabelSet(reloaded));
    }

    [HttpDelete("label-sets/{labelSetId:int}")]
    public async Task<ActionResult> DeleteLabelSet(int labelSetId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        var entity = await _context.DocumentLabelSets
            .Include(x => x.ClassificationLabels)
            .FirstOrDefaultAsync(x => x.DocumentLabelSetId == labelSetId);
        if (entity == null)
        {
            return NotFound(new { message = "Label set not found." });
        }

        if (entity.ClassificationLabels.Count > 0)
        {
            _context.DocumentClassificationLabels.RemoveRange(entity.ClassificationLabels);
        }
        _context.DocumentLabelSets.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("label-sets/{labelSetId:int}/labels")]
    public async Task<ActionResult<DocumentClassificationLabelDto>> CreateClassificationLabel(int labelSetId, [FromBody] CreateDocumentClassificationLabelRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        if (string.IsNullOrWhiteSpace(request.ClassificationLabel) || string.IsNullOrWhiteSpace(request.ClassificationPrompt))
        {
            return BadRequest(new { message = "Classification label and prompt are required." });
        }

        var labelSet = await _context.DocumentLabelSets.FirstOrDefaultAsync(x => x.DocumentLabelSetId == labelSetId);
        if (labelSet == null) return NotFound(new { message = "Label set not found." });

        var userId = GetCurrentUserId();
        var entity = new DocumentClassificationLabel
        {
            DocumentLabelSetId = labelSetId,
            ClassificationLabel = request.ClassificationLabel.Trim(),
            ClassificationDescription = request.ClassificationDescription?.Trim(),
            ClassificationPrompt = request.ClassificationPrompt.Trim(),
            SeedDocumentName = request.SeedDocumentName?.Trim(),
            SeedDocumentHash = request.SeedDocumentHash?.Trim(),
            IsAutoGenerated = request.IsAutoGenerated,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };

        _context.DocumentClassificationLabels.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(MapClassificationLabel(entity));
    }

    [HttpGet("summarisation-templates")]
    public async Task<ActionResult<List<DocumentSummarisationTemplateDto>>> GetSummarisationTemplates()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!HasPropertyHubAccess(currentUser)) return Forbid("Access denied: Property Hub access required.");

        var items = await _context.DocumentSummarisationTemplates
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync();

        return Ok(items.Select(MapSummarisationTemplate).ToList());
    }

    [HttpPost("summarisation-templates")]
    public async Task<ActionResult<DocumentSummarisationTemplateDto>> CreateSummarisationTemplate([FromBody] CreateDocumentSummarisationTemplateRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        if (string.IsNullOrWhiteSpace(request.SummarisationName) || string.IsNullOrWhiteSpace(request.SummarisationPrompt))
        {
            return BadRequest(new { message = "Summarisation name and prompt are required." });
        }

        var userId = GetCurrentUserId();
        var entity = new DocumentSummarisationTemplate
        {
            SummarisationName = request.SummarisationName.Trim(),
            SummarisationDescription = request.SummarisationDescription?.Trim(),
            SummarisationPrompt = request.SummarisationPrompt.Trim(),
            IsActive = true,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };

        _context.DocumentSummarisationTemplates.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(MapSummarisationTemplate(entity));
    }

    [HttpPut("summarisation-templates/{templateId:int}")]
    public async Task<ActionResult<DocumentSummarisationTemplateDto>> UpdateSummarisationTemplate(int templateId, [FromBody] UpdateDocumentSummarisationTemplateRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        var entity = await _context.DocumentSummarisationTemplates.FirstOrDefaultAsync(x => x.DocumentSummarisationTemplateId == templateId);
        if (entity == null)
        {
            return NotFound(new { message = "Summarisation template not found." });
        }

        if (request.SummarisationName != null)
        {
            entity.SummarisationName = request.SummarisationName.Trim();
        }

        if (request.SummarisationDescription != null)
        {
            entity.SummarisationDescription = request.SummarisationDescription.Trim();
        }

        if (request.SummarisationPrompt != null)
        {
            entity.SummarisationPrompt = request.SummarisationPrompt.Trim();
        }

        if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
        }

        entity.UpdatedByUserId = GetCurrentUserId();
        entity.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(MapSummarisationTemplate(entity));
    }

    [HttpDelete("summarisation-templates/{templateId:int}")]
    public async Task<ActionResult> DeleteSummarisationTemplate(int templateId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        var entity = await _context.DocumentSummarisationTemplates.FirstOrDefaultAsync(x => x.DocumentSummarisationTemplateId == templateId);
        if (entity == null)
        {
            return NotFound(new { message = "Summarisation template not found." });
        }

        _context.DocumentSummarisationTemplates.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("extraction-templates")]
    public async Task<ActionResult<List<DocumentExtractionTemplateDto>>> GetExtractionTemplates()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!HasPropertyHubAccess(currentUser)) return Forbid("Access denied: Property Hub access required.");

        var items = await _context.DocumentExtractionTemplates
            .AsNoTracking()
            .Include(x => x.ExtractionFields)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync();

        return Ok(items.Select(MapExtractionTemplate).ToList());
    }

    [HttpPost("extraction-templates")]
    public async Task<ActionResult<DocumentExtractionTemplateDto>> CreateExtractionTemplate([FromBody] CreateDocumentExtractionTemplateRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        if (string.IsNullOrWhiteSpace(request.ExtractionTemplateName))
        {
            return BadRequest(new { message = "Extraction template name is required." });
        }

        var userId = GetCurrentUserId();
        var entity = new DocumentExtractionTemplate
        {
            ExtractionTemplateName = request.ExtractionTemplateName.Trim(),
            ExtractionTemplateDescription = request.ExtractionTemplateDescription?.Trim(),
            IsActive = true,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };

        _context.DocumentExtractionTemplates.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(MapExtractionTemplate(entity));
    }

    [HttpPut("extraction-templates/{templateId:int}")]
    public async Task<ActionResult<DocumentExtractionTemplateDto>> UpdateExtractionTemplate(
        int templateId,
        [FromBody] UpdateDocumentExtractionTemplateRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        var entity = await _context.DocumentExtractionTemplates
            .Include(x => x.ExtractionFields)
            .FirstOrDefaultAsync(x => x.DocumentExtractionTemplateId == templateId);
        if (entity == null)
        {
            return NotFound(new { message = "Extraction template not found." });
        }

        if (request.ExtractionTemplateName != null)
        {
            var normalizedName = request.ExtractionTemplateName.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return BadRequest(new { message = "Extraction template name cannot be empty." });
            }
            entity.ExtractionTemplateName = normalizedName;
        }

        if (request.ExtractionTemplateDescription != null)
        {
            entity.ExtractionTemplateDescription = request.ExtractionTemplateDescription.Trim();
        }

        if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
        }

        if (request.Fields != null)
        {
            var replacementFields = request.Fields
                .Where(x => !string.IsNullOrWhiteSpace(x.FieldName))
                .Select(x => new DocumentExtractionField
                {
                    DocumentExtractionTemplateId = templateId,
                    FieldName = x.FieldName.Trim(),
                    ExampleValue = x.ExampleValue?.Trim(),
                    AnchorTextBefore = x.AnchorTextBefore?.Trim(),
                    AnchorTextAfter = x.AnchorTextAfter?.Trim(),
                    FieldExtractionPrompt = x.FieldExtractionPrompt?.Trim(),
                    PageNumber = x.PageNumber,
                    BoundingBoxJson = x.BoundingBoxJson,
                    IsActive = x.IsActive,
                    CreatedByUserId = GetCurrentUserId(),
                    CreatedDate = DateTime.UtcNow
                })
                .ToList();

            _context.DocumentExtractionFields.RemoveRange(entity.ExtractionFields);
            entity.ExtractionFields = replacementFields;
        }

        entity.UpdatedByUserId = GetCurrentUserId();
        entity.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var reloaded = await _context.DocumentExtractionTemplates
            .AsNoTracking()
            .Include(x => x.ExtractionFields)
            .FirstOrDefaultAsync(x => x.DocumentExtractionTemplateId == templateId);
        if (reloaded == null) return NotFound(new { message = "Extraction template not found after update." });

        return Ok(MapExtractionTemplate(reloaded));
    }

    [HttpDelete("extraction-templates/{templateId:int}")]
    public async Task<ActionResult> DeleteExtractionTemplate(int templateId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        var entity = await _context.DocumentExtractionTemplates
            .Include(x => x.ExtractionFields)
            .FirstOrDefaultAsync(x => x.DocumentExtractionTemplateId == templateId);
        if (entity == null)
        {
            return NotFound(new { message = "Extraction template not found." });
        }

        if (entity.ExtractionFields.Count > 0)
        {
            _context.DocumentExtractionFields.RemoveRange(entity.ExtractionFields);
        }
        _context.DocumentExtractionTemplates.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("workflow-rules")]
    public async Task<ActionResult<List<DocumentWorkflowRuleDto>>> GetWorkflowRules()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!HasPropertyHubAccess(currentUser)) return Forbid("Access denied: Property Hub access required.");

        var rules = await _context.DocumentWorkflowRules
            .AsNoTracking()
            .Include(x => x.Steps)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.DocumentWorkflowRuleId)
            .ToListAsync();

        return Ok(rules.Select(MapWorkflowRule).ToList());
    }

    [HttpPost("workflow-rules")]
    public async Task<ActionResult<DocumentWorkflowRuleDto>> CreateWorkflowRule([FromBody] CreateDocumentWorkflowRuleRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        if (string.IsNullOrWhiteSpace(request.WorkflowName) || string.IsNullOrWhiteSpace(request.ClassificationLabel))
        {
            return BadRequest(new { message = "WorkflowName and ClassificationLabel are required." });
        }

        if (request.MinimumScore is < 0 or > 1)
        {
            return BadRequest(new { message = "MinimumScore must be between 0 and 1." });
        }

        var userId = GetCurrentUserId();
        var rule = new DocumentWorkflowRule
        {
            WorkflowName = request.WorkflowName.Trim(),
            ClassificationLabel = request.ClassificationLabel.Trim(),
            MinimumScore = request.MinimumScore,
            Priority = request.Priority,
            StopOnFailure = request.StopOnFailure,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };

        var steps = request.Steps
            .Where(s => !string.IsNullOrWhiteSpace(s.StepType))
            .OrderBy(s => s.StepOrder)
            .Select((s, idx) => new DocumentWorkflowStep
            {
                StepOrder = s.StepOrder > 0 ? s.StepOrder : idx + 1,
                StepType = s.StepType.Trim(),
                StepConfigJson = s.StepConfigJson,
                IsActive = s.IsActive,
                CreatedByUserId = userId,
                CreatedDate = DateTime.UtcNow
            })
            .ToList();
        rule.Steps = steps;

        _context.DocumentWorkflowRules.Add(rule);
        await _context.SaveChangesAsync();

        var created = await _context.DocumentWorkflowRules
            .AsNoTracking()
            .Include(x => x.Steps)
            .FirstAsync(x => x.DocumentWorkflowRuleId == rule.DocumentWorkflowRuleId);

        return Ok(MapWorkflowRule(created));
    }

    [HttpPut("workflow-rules/{ruleId:int}")]
    public async Task<ActionResult<DocumentWorkflowRuleDto>> UpdateWorkflowRule(int ruleId, [FromBody] UpdateDocumentWorkflowRuleRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        var rule = await _context.DocumentWorkflowRules
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.DocumentWorkflowRuleId == ruleId);
        if (rule == null)
        {
            return NotFound(new { message = "Workflow rule not found." });
        }

        if (request.WorkflowName != null)
        {
            var name = request.WorkflowName.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "WorkflowName cannot be empty." });
            }
            rule.WorkflowName = name;
        }

        if (request.ClassificationLabel != null)
        {
            var label = request.ClassificationLabel.Trim();
            if (string.IsNullOrWhiteSpace(label))
            {
                return BadRequest(new { message = "ClassificationLabel cannot be empty." });
            }
            rule.ClassificationLabel = label;
        }

        if (request.MinimumScore.HasValue)
        {
            if (request.MinimumScore.Value is < 0 or > 1)
            {
                return BadRequest(new { message = "MinimumScore must be between 0 and 1." });
            }
            rule.MinimumScore = request.MinimumScore.Value;
        }

        if (request.Priority.HasValue) rule.Priority = request.Priority.Value;
        if (request.StopOnFailure.HasValue) rule.StopOnFailure = request.StopOnFailure.Value;
        if (request.IsActive.HasValue) rule.IsActive = request.IsActive.Value;

        if (request.Steps != null)
        {
            _context.DocumentWorkflowSteps.RemoveRange(rule.Steps);
            rule.Steps = request.Steps
                .Where(s => !string.IsNullOrWhiteSpace(s.StepType))
                .OrderBy(s => s.StepOrder)
                .Select((s, idx) => new DocumentWorkflowStep
                {
                    DocumentWorkflowRuleId = ruleId,
                    StepOrder = s.StepOrder > 0 ? s.StepOrder : idx + 1,
                    StepType = s.StepType.Trim(),
                    StepConfigJson = s.StepConfigJson,
                    IsActive = s.IsActive,
                    CreatedByUserId = GetCurrentUserId(),
                    CreatedDate = DateTime.UtcNow
                })
                .ToList();
        }

        rule.UpdatedByUserId = GetCurrentUserId();
        rule.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var updated = await _context.DocumentWorkflowRules
            .AsNoTracking()
            .Include(x => x.Steps)
            .FirstAsync(x => x.DocumentWorkflowRuleId == ruleId);

        return Ok(MapWorkflowRule(updated));
    }

    [HttpDelete("workflow-rules/{ruleId:int}")]
    public async Task<ActionResult> DeleteWorkflowRule(int ruleId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        var rule = await _context.DocumentWorkflowRules
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.DocumentWorkflowRuleId == ruleId);
        if (rule == null)
        {
            return NotFound(new { message = "Workflow rule not found." });
        }

        if (rule.Steps.Count > 0)
        {
            _context.DocumentWorkflowSteps.RemoveRange(rule.Steps);
        }
        _context.DocumentWorkflowRules.Remove(rule);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("workflow-rules/{ruleId:int}/test")]
    public async Task<ActionResult<DocumentWorkflowRuleTestResponse>> TestWorkflowRule(int ruleId, [FromForm] IFormFile file)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "A document file is required." });
        }

        var rule = await _context.DocumentWorkflowRules
            .AsNoTracking()
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.DocumentWorkflowRuleId == ruleId);
        if (rule == null)
        {
            return NotFound(new { message = "Workflow rule not found." });
        }

        var templates = await _context.DocumentClassificationLabels
            .AsNoTracking()
            .Include(x => x.DocumentLabelSet)
            .Where(x => x.IsActive && x.DocumentLabelSet != null && x.DocumentLabelSet.IsActive)
            .Select(x => new ClassificationTemplate
            {
                DocumentClassificationLabelId = x.DocumentClassificationLabelId,
                ClassificationLabel = x.ClassificationLabel,
                ClassificationDescription = x.ClassificationDescription,
                ClassificationPrompt = x.ClassificationPrompt
            })
            .ToListAsync();

        var extractedText = await ExtractTextAsync(file);
        var consolidatedContent = $"File name: {file.FileName}\n\nContent:\n{extractedText}";
        var (label, score, explainability, _) = ClassifyAgainstTemplates(consolidatedContent, templates);
        var eligible =
            rule.IsActive &&
            score >= rule.MinimumScore &&
            string.Equals(rule.ClassificationLabel, label, StringComparison.OrdinalIgnoreCase);

        var contextFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var stepResults = new List<DocumentWorkflowStepTestResultDto>();
        foreach (var step in rule.Steps.Where(s => s.IsActive).OrderBy(s => s.StepOrder))
        {
            if (!eligible)
            {
                stepResults.Add(new DocumentWorkflowStepTestResultDto
                {
                    StepOrder = step.StepOrder,
                    StepType = step.StepType,
                    Status = "skipped",
                    Details = "Rule is not eligible for this document classification."
                });
                continue;
            }

            var stepType = step.StepType.Trim().ToLowerInvariant();
            if (stepType == "runextraction")
            {
                var config = ParseJsonObject(step.StepConfigJson);
                var extractionTemplateId = GetConfigInt(config, "extractionTemplateId");
                if (!extractionTemplateId.HasValue || extractionTemplateId.Value <= 0)
                {
                    stepResults.Add(new DocumentWorkflowStepTestResultDto
                    {
                        StepOrder = step.StepOrder,
                        StepType = step.StepType,
                        Status = "error",
                        Details = "Missing extractionTemplateId in step config."
                    });
                    continue;
                }

                var templateFields = await _context.DocumentExtractionFields
                    .AsNoTracking()
                    .Where(f => f.IsActive && f.DocumentExtractionTemplateId == extractionTemplateId.Value)
                    .OrderBy(f => f.DocumentExtractionFieldId)
                    .Select(f => new { f.FieldName, f.ExampleValue })
                    .ToListAsync();
                var extracted = ExtractFieldsFromContent(
                    consolidatedContent,
                    templateFields.Select(f => (f.FieldName, f.ExampleValue)).ToList());
                foreach (var kv in extracted)
                {
                    contextFields[kv.Key] = kv.Value;
                }

                stepResults.Add(new DocumentWorkflowStepTestResultDto
                {
                    StepOrder = step.StepOrder,
                    StepType = step.StepType,
                    Status = "would_run",
                    Details = $"Would extract {extracted.Count} field(s): {string.Join(", ", extracted.Keys)}"
                });
                continue;
            }

            if (stepType == "runsummarisation")
            {
                var config = ParseJsonObject(step.StepConfigJson);
                var summarisationTemplateId = GetConfigInt(config, "summarisationTemplateId");
                var prompt = GetConfigString(config, "prompt");
                if (string.IsNullOrWhiteSpace(prompt) && summarisationTemplateId.HasValue && summarisationTemplateId.Value > 0)
                {
                    prompt = await _context.DocumentSummarisationTemplates
                        .AsNoTracking()
                        .Where(t => t.IsActive && t.DocumentSummarisationTemplateId == summarisationTemplateId.Value)
                        .Select(t => t.SummarisationPrompt)
                        .FirstOrDefaultAsync();
                }

                if (string.IsNullOrWhiteSpace(prompt))
                {
                    stepResults.Add(new DocumentWorkflowStepTestResultDto
                    {
                        StepOrder = step.StepOrder,
                        StepType = step.StepType,
                        Status = "error",
                        Details = "Missing summarisation prompt. Configure summarisationTemplateId or prompt."
                    });
                    continue;
                }

                var summary = await _documentAiService.SummariseAsync(consolidatedContent, prompt.Trim(), HttpContext.RequestAborted);
                contextFields["summary"] = summary;
                contextFields["summarisation"] = summary;

                var preview = summary.Length > 260 ? summary[..260] + "..." : summary;
                stepResults.Add(new DocumentWorkflowStepTestResultDto
                {
                    StepOrder = step.StepOrder,
                    StepType = step.StepType,
                    Status = "would_run",
                    Details = $"Would generate summary (length {summary.Length}): {preview}"
                });
                continue;
            }

            if (stepType == "createjournallog" || stepType == "createcontactlog")
            {
                var config = ParseJsonObject(step.StepConfigJson);
                var template =
                    GetConfigString(config, stepType == "createjournallog" ? "descriptionTemplate" : "notesTemplate") ??
                    string.Empty;
                var rendered = RenderWorkflowTemplate(template, label, score, file.FileName, contextFields);
                stepResults.Add(new DocumentWorkflowStepTestResultDto
                {
                    StepOrder = step.StepOrder,
                    StepType = step.StepType,
                    Status = "would_run",
                    Details = string.IsNullOrWhiteSpace(rendered)
                        ? "Would execute create log step with current config."
                        : $"Rendered template preview: {rendered}"
                });
                continue;
            }

            stepResults.Add(new DocumentWorkflowStepTestResultDto
            {
                StepOrder = step.StepOrder,
                StepType = step.StepType,
                Status = "would_run",
                Details = $"Would execute step '{step.StepType}'."
            });
        }

        return Ok(new DocumentWorkflowRuleTestResponse
        {
            DocumentWorkflowRuleId = rule.DocumentWorkflowRuleId,
            WorkflowName = rule.WorkflowName,
            ClassificationLabel = label,
            ClassificationScore = score,
            RuleEligible = eligible,
            EligibilityReason = eligible
                ? $"Matched label '{label}' at score {score} (threshold {rule.MinimumScore})."
                : $"Classified as '{label}' with score {score}; rule requires label '{rule.ClassificationLabel}' and score >= {rule.MinimumScore}. Explainability: {explainability}",
            Steps = stepResults
        });
    }

    [HttpPost("extraction-templates/{templateId:int}/fields")]
    public async Task<ActionResult<DocumentExtractionFieldDto>> CreateExtractionField(int templateId, [FromBody] CreateDocumentExtractionFieldRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        if (string.IsNullOrWhiteSpace(request.FieldName))
        {
            return BadRequest(new { message = "Field name is required." });
        }

        var template = await _context.DocumentExtractionTemplates.FirstOrDefaultAsync(x => x.DocumentExtractionTemplateId == templateId);
        if (template == null) return NotFound(new { message = "Extraction template not found." });

        var userId = GetCurrentUserId();
        var entity = new DocumentExtractionField
        {
            DocumentExtractionTemplateId = templateId,
            FieldName = request.FieldName.Trim(),
            ExampleValue = request.ExampleValue?.Trim(),
            AnchorTextBefore = request.AnchorTextBefore?.Trim(),
            AnchorTextAfter = request.AnchorTextAfter?.Trim(),
            FieldExtractionPrompt = request.FieldExtractionPrompt?.Trim(),
            PageNumber = request.PageNumber,
            BoundingBoxJson = request.BoundingBoxJson,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };

        _context.DocumentExtractionFields.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(MapExtractionField(entity));
    }

    [HttpPost("correction-feedback")]
    public async Task<ActionResult> CreateCorrectionFeedback([FromBody] CreateDocumentCorrectionFeedbackRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!HasPropertyHubAccess(currentUser)) return Forbid("Access denied: Property Hub access required.");

        if (request.DocumentProcessingRunId <= 0 || string.IsNullOrWhiteSpace(request.ProcessingType))
        {
            return BadRequest(new { message = "DocumentProcessingRunId and ProcessingType are required." });
        }

        var entity = new DocumentCorrectionFeedback
        {
            DocumentProcessingRunId = request.DocumentProcessingRunId,
            ProcessingType = request.ProcessingType.Trim(),
            FieldName = request.FieldName?.Trim(),
            OriginalValue = request.OriginalValue,
            CorrectedValue = request.CorrectedValue,
            ReviewerNotes = request.ReviewerNotes?.Trim(),
            CreatedByUserId = GetCurrentUserId(),
            CreatedDate = DateTime.UtcNow
        };

        _context.DocumentCorrectionFeedback.Add(entity);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("classification/suggest")]
    public async Task<ActionResult<List<DocumentClassificationSuggestionDto>>> SuggestClassificationLabels([FromForm] List<IFormFile> files)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        if (files == null || files.Count == 0)
        {
            return BadRequest(new { message = "At least one PDF file is required." });
        }

        var results = new List<DocumentClassificationSuggestionDto>();

        foreach (var file in files.Where(f => f.Length > 0))
        {
            var extractedText = await ExtractTextAsync(file);
            var suggestion = await _documentAiService.BuildClassificationSuggestionAsync(
                file.FileName,
                extractedText,
                HttpContext.RequestAborted);
            results.Add(suggestion);
        }

        return Ok(results);
    }

    [HttpPost("classification/test")]
    public async Task<ActionResult<DocumentClassificationTestResponse>> TestClassification([FromForm] IFormFile file)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!HasPropertyHubAccess(currentUser)) return Forbid("Access denied: Property Hub access required.");

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "A document file is required." });
        }

        var templates = await _context.DocumentClassificationLabels
            .AsNoTracking()
            .Include(x => x.DocumentLabelSet)
            .Where(x => x.IsActive && x.DocumentLabelSet != null && x.DocumentLabelSet.IsActive)
            .Select(x => new ClassificationTemplate
            {
                DocumentClassificationLabelId = x.DocumentClassificationLabelId,
                ClassificationLabel = x.ClassificationLabel,
                ClassificationDescription = x.ClassificationDescription,
                ClassificationPrompt = x.ClassificationPrompt
            })
            .ToListAsync();

        var extractedText = await ExtractTextAsync(file);
        var consolidatedContent = $"File name: {file.FileName}\n\nContent:\n{extractedText}";
        var (label, score, explainability, bestTemplate) = ClassifyAgainstTemplates(consolidatedContent, templates);

        return Ok(new DocumentClassificationTestResponse
        {
            FileName = file.FileName,
            ClassificationLabel = label,
            ClassificationDescription = bestTemplate?.ClassificationDescription,
            ClassificationScore = score,
            ClassificationExplainability = explainability,
            DocumentClassificationLabelId = bestTemplate?.DocumentClassificationLabelId,
            TextPreview = extractedText.Length > 800 ? extractedText[..800] + "..." : extractedText
        });
    }

    [HttpPost("summarisation/preview")]
    public async Task<ActionResult<DocumentSummarisationPreviewResponse>> PreviewSummarisation([FromForm] IFormFile file, [FromForm] string prompt)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!HasPropertyHubAccess(currentUser)) return Forbid("Access denied: Property Hub access required.");

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "A PDF file is required." });
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            return BadRequest(new { message = "A summarisation prompt is required." });
        }

        var extractedText = await ExtractTextAsync(file);
        var summary = await _documentAiService.SummariseAsync(extractedText, prompt.Trim(), HttpContext.RequestAborted);

        return Ok(new DocumentSummarisationPreviewResponse
        {
            FileName = file.FileName,
            PromptUsed = prompt.Trim(),
            Summary = summary,
            TextPreview = extractedText.Length > 500 ? extractedText[..500] + "..." : extractedText
        });
    }

    [HttpPost("extraction/preview")]
    public async Task<ActionResult<DocumentExtractionPreviewResponse>> PreviewExtraction(
        [FromForm] IFormFile file,
        [FromForm] int? extractionTemplateId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!HasPropertyHubAccess(currentUser)) return Forbid("Access denied: Property Hub access required.");

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "A PDF file is required." });
        }

        var extractedText = await ExtractTextAsync(file);
        var textPreview = extractedText.Length > 800 ? extractedText[..800] + "..." : extractedText;

        if (extractionTemplateId.HasValue && extractionTemplateId.Value > 0)
        {
            var templateExists = await _context.DocumentExtractionTemplates
                .AsNoTracking()
                .AnyAsync(x => x.DocumentExtractionTemplateId == extractionTemplateId.Value);
            if (!templateExists)
            {
                return BadRequest(new { message = "Extraction template not found." });
            }

            var consolidatedContent = $"File name: {file.FileName}\n\nContent:\n{extractedText}";
            var templateFields = await _context.DocumentExtractionFields
                .AsNoTracking()
                .Where(f => f.IsActive && f.DocumentExtractionTemplateId == extractionTemplateId.Value)
                .OrderBy(f => f.DocumentExtractionFieldId)
                .Select(f => new { f.FieldName, f.ExampleValue })
                .ToListAsync();

            var extracted = ExtractFieldsFromContent(
                consolidatedContent,
                templateFields.Select(f => (f.FieldName, f.ExampleValue)).ToList());

            var mappedFields = templateFields.Select(f =>
            {
                var token = NormalizeFieldToken(f.FieldName);
                extracted.TryGetValue(token, out var parsed);
                return new DocumentExtractionSuggestedFieldDto
                {
                    FieldName = f.FieldName,
                    ExampleValue = parsed ?? string.Empty,
                    NormalizedToken = string.IsNullOrWhiteSpace(token) ? null : token
                };
            }).ToList();

            return Ok(new DocumentExtractionPreviewResponse
            {
                FileName = file.FileName,
                ExtractedText = extractedText,
                TextPreview = textPreview,
                SuggestedFields = mappedFields,
                ExtractionTemplateId = extractionTemplateId.Value,
                PreviewMode = "template_fields"
            });
        }

        var suggestedFields = await _documentAiService.SuggestExtractionFieldsAsync(extractedText, HttpContext.RequestAborted);

        return Ok(new DocumentExtractionPreviewResponse
        {
            FileName = file.FileName,
            ExtractedText = extractedText,
            TextPreview = textPreview,
            SuggestedFields = suggestedFields,
            PreviewMode = "ai_suggestions"
        });
    }

    [HttpPost("extraction/selection-suggest")]
    public async Task<ActionResult<List<DocumentExtractionSuggestedFieldDto>>> SuggestSelectionExtraction([FromBody] SuggestExtractionFromSelectionRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!HasPropertyHubAccess(currentUser)) return Forbid("Access denied: Property Hub access required.");

        if (string.IsNullOrWhiteSpace(request.SelectedText))
        {
            return BadRequest(new { message = "SelectedText is required." });
        }

        var suggestions = await _documentAiService.SuggestFieldsFromSelectionAsync(
            request.SelectedText.Trim(),
            request.ExtractedText?.Trim() ?? string.Empty,
            HttpContext.RequestAborted);

        return Ok(suggestions);
    }

    [HttpPost("email-processing/property-hub/trigger")]
    public async Task<ActionResult<TriggerPropertyHubEmailProcessingResponse>> TriggerPropertyHubEmailProcessing(
        [FromBody] TriggerPropertyHubEmailProcessingRequest request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Unauthorized();
        if (!_authService.HasPropertyHubAdminAccess(currentUser)) return Forbid("Access denied: Property Hub Admin permission required.");

        var functionUrl =
            _configuration["EmailProcessor:FunctionUrl"] ??
            Environment.GetEnvironmentVariable("EmailProcessor__FunctionUrl") ??
            _configuration["EmailProcessor_FunctionUrl"] ??
            Environment.GetEnvironmentVariable("EmailProcessor_FunctionUrl");
        var functionKey =
            _configuration["EmailProcessor:FunctionKey"] ??
            Environment.GetEnvironmentVariable("EmailProcessor__FunctionKey") ??
            _configuration["EmailProcessor_FunctionKey"] ??
            Environment.GetEnvironmentVariable("EmailProcessor_FunctionKey");

        functionUrl = functionUrl?.Trim();
        functionKey = functionKey?.Trim();

        if (string.IsNullOrWhiteSpace(functionUrl) || string.IsNullOrWhiteSpace(functionKey))
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(functionUrl))
            {
                missing.Add("EmailProcessor__FunctionUrl");
            }
            if (string.IsNullOrWhiteSpace(functionKey))
            {
                missing.Add("EmailProcessor__FunctionKey");
            }

            return StatusCode(500, new TriggerPropertyHubEmailProcessingResponse
            {
                Status = "error",
                Message = $"Email processor function configuration is missing: {string.Join(", ", missing)}."
            });
        }

        var endpoint = functionUrl.Contains("?", StringComparison.Ordinal)
            ? $"{functionUrl}&code={Uri.EscapeDataString(functionKey)}"
            : $"{functionUrl}?code={Uri.EscapeDataString(functionKey)}";

        var payload = new TriggerPropertyHubEmailProcessingRequest
        {
            MailboxUser = string.IsNullOrWhiteSpace(request.MailboxUser) ? null : request.MailboxUser.Trim(),
            MaxEmails = request.MaxEmails
        };

        var json = JsonSerializer.Serialize(payload);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(httpRequest, HttpContext.RequestAborted);
        var rawContent = await response.Content.ReadAsStringAsync(HttpContext.RequestAborted);

        if (!response.IsSuccessStatusCode)
        {
            string message = $"Email processor function returned {(int)response.StatusCode}.";
            if (!string.IsNullOrWhiteSpace(rawContent))
            {
                try
                {
                    using var errorDoc = JsonDocument.Parse(rawContent);
                    if (errorDoc.RootElement.TryGetProperty("message", out var messageEl))
                    {
                        var innerMessage = messageEl.GetString();
                        if (!string.IsNullOrWhiteSpace(innerMessage))
                        {
                            message = $"Email processor function returned {(int)response.StatusCode}: {innerMessage}";
                        }
                    }
                }
                catch
                {
                    // Non-JSON response body; keep generic message and preserve raw content in payload.
                }
            }

            return StatusCode((int)response.StatusCode, new TriggerPropertyHubEmailProcessingResponse
            {
                Status = "error",
                Message = message,
                ProcessingResult = rawContent
            });
        }

        object resultPayload;
        try
        {
            resultPayload = JsonSerializer.Deserialize<object>(rawContent) ?? rawContent;
        }
        catch
        {
            resultPayload = rawContent;
        }

        return Ok(new TriggerPropertyHubEmailProcessingResponse
        {
            Status = "ok",
            Message = "Email processing trigger completed.",
            ProcessingResult = resultPayload
        });
    }

    private async Task<UserDto?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return null;
        return await _authService.GetCurrentUserAsync(userId.Value);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }

    private static bool HasPropertyHubAccess(UserDto user)
    {
        if (user.IsGlobalAdmin) return true;

        return user.WorkstreamAccess.Any(wa =>
            wa.WorkstreamName.Equals("Property Hub", StringComparison.OrdinalIgnoreCase) ||
            wa.WorkstreamName.Contains("Property", StringComparison.OrdinalIgnoreCase));
    }

    private static DocumentLabelSetDto MapLabelSet(DocumentLabelSet x)
    {
        return new DocumentLabelSetDto
        {
            DocumentLabelSetId = x.DocumentLabelSetId,
            LabelSetName = x.LabelSetName,
            LabelSetDescription = x.LabelSetDescription,
            IsActive = x.IsActive,
            CreatedDate = x.CreatedDate,
            Labels = x.ClassificationLabels
                .OrderBy(y => y.DocumentClassificationLabelId)
                .Select(MapClassificationLabel)
                .ToList()
        };
    }

    private static DocumentClassificationLabelDto MapClassificationLabel(DocumentClassificationLabel x)
    {
        return new DocumentClassificationLabelDto
        {
            DocumentClassificationLabelId = x.DocumentClassificationLabelId,
            DocumentLabelSetId = x.DocumentLabelSetId,
            ClassificationLabel = x.ClassificationLabel,
            ClassificationDescription = x.ClassificationDescription,
            ClassificationPrompt = x.ClassificationPrompt,
            SeedDocumentName = x.SeedDocumentName,
            IsAutoGenerated = x.IsAutoGenerated,
            IsActive = x.IsActive
        };
    }

    private static DocumentSummarisationTemplateDto MapSummarisationTemplate(DocumentSummarisationTemplate x)
    {
        return new DocumentSummarisationTemplateDto
        {
            DocumentSummarisationTemplateId = x.DocumentSummarisationTemplateId,
            SummarisationName = x.SummarisationName,
            SummarisationDescription = x.SummarisationDescription,
            SummarisationPrompt = x.SummarisationPrompt,
            IsActive = x.IsActive,
            CreatedDate = x.CreatedDate
        };
    }

    private static DocumentExtractionTemplateDto MapExtractionTemplate(DocumentExtractionTemplate x)
    {
        return new DocumentExtractionTemplateDto
        {
            DocumentExtractionTemplateId = x.DocumentExtractionTemplateId,
            ExtractionTemplateName = x.ExtractionTemplateName,
            ExtractionTemplateDescription = x.ExtractionTemplateDescription,
            IsActive = x.IsActive,
            CreatedDate = x.CreatedDate,
            Fields = x.ExtractionFields
                .OrderBy(y => y.DocumentExtractionFieldId)
                .Select(MapExtractionField)
                .ToList()
        };
    }

    private static DocumentExtractionFieldDto MapExtractionField(DocumentExtractionField x)
    {
        return new DocumentExtractionFieldDto
        {
            DocumentExtractionFieldId = x.DocumentExtractionFieldId,
            DocumentExtractionTemplateId = x.DocumentExtractionTemplateId,
            FieldName = x.FieldName,
            ExampleValue = x.ExampleValue,
            AnchorTextBefore = x.AnchorTextBefore,
            AnchorTextAfter = x.AnchorTextAfter,
            FieldExtractionPrompt = x.FieldExtractionPrompt,
            PageNumber = x.PageNumber,
            BoundingBoxJson = x.BoundingBoxJson,
            IsActive = x.IsActive
        };
    }

    private static DocumentWorkflowRuleDto MapWorkflowRule(DocumentWorkflowRule x)
    {
        return new DocumentWorkflowRuleDto
        {
            DocumentWorkflowRuleId = x.DocumentWorkflowRuleId,
            WorkflowName = x.WorkflowName,
            ClassificationLabel = x.ClassificationLabel,
            MinimumScore = x.MinimumScore,
            Priority = x.Priority,
            StopOnFailure = x.StopOnFailure,
            IsActive = x.IsActive,
            CreatedDate = x.CreatedDate,
            Steps = x.Steps
                .OrderBy(s => s.StepOrder)
                .Select(MapWorkflowStep)
                .ToList()
        };
    }

    private static DocumentWorkflowStepDto MapWorkflowStep(DocumentWorkflowStep x)
    {
        return new DocumentWorkflowStepDto
        {
            DocumentWorkflowStepId = x.DocumentWorkflowStepId,
            DocumentWorkflowRuleId = x.DocumentWorkflowRuleId,
            StepOrder = x.StepOrder,
            StepType = x.StepType,
            StepConfigJson = x.StepConfigJson,
            IsActive = x.IsActive
        };
    }

    private static Dictionary<string, object?> ParseJsonObject(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(raw);
            return dict ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static int? GetConfigInt(Dictionary<string, object?> config, string key)
    {
        if (!config.TryGetValue(key, out var value) || value == null)
        {
            return null;
        }

        if (value is JsonElement jsonEl)
        {
            if (jsonEl.ValueKind == JsonValueKind.Number && jsonEl.TryGetInt32(out var asInt))
            {
                return asInt;
            }
            if (jsonEl.ValueKind == JsonValueKind.String && int.TryParse(jsonEl.GetString(), out var parsed))
            {
                return parsed;
            }
        }
        else if (int.TryParse(value.ToString(), out var fallback))
        {
            return fallback;
        }

        return null;
    }

    private static string? GetConfigString(Dictionary<string, object?> config, string key)
    {
        if (!config.TryGetValue(key, out var value) || value == null)
        {
            return null;
        }

        if (value is JsonElement jsonEl)
        {
            return jsonEl.ValueKind == JsonValueKind.String ? jsonEl.GetString() : jsonEl.ToString();
        }

        return value.ToString();
    }

    private static string AlternateLabelMatchersForExtraction(string fieldNameTrimmed)
    {
        var raw = fieldNameTrimmed.Trim();
        var esc = Regex.Escape(raw);
        if (!raw.Contains('_'))
        {
            return esc;
        }

        var segs = raw.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segs.Length < 2)
        {
            return esc;
        }

        var flex = string.Join(@"[\s._\u2013\-]{0,8}", segs.Select(Regex.Escape));
        return flex == esc ? esc : $@"(?:{esc}|{flex})";
    }

    private static string BuildSiblingLabelStopAheadForExtraction(string currentFieldTrimmed, IReadOnlyList<string> allSiblingNamesTrimmed)
    {
        var clauses = new List<string>();
        foreach (var s in allSiblingNamesTrimmed.Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(s.Trim(), currentFieldTrimmed.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var pat = AlternateLabelMatchersForExtraction(s.Trim());
            clauses.Add(@$"\s+(?:{pat})\s*[:\u003a\u2013\-]");
        }

        if (clauses.Count == 0)
        {
            return @"(?=$)";
        }

        return $@"(?=(?:{string.Join("|", clauses)}|$))";
    }

    private static Dictionary<string, string> ExtractFieldsFromContent(
        string content,
        List<(string FieldName, string? ExampleValue)> fields)
    {
        var allNamesTrimmed = fields
            .Select(f => f.FieldName.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var extracted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            var token = NormalizeFieldToken(field.FieldName);
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            var rawLabel = field.FieldName.Trim();

            var strictLineRx = $@"(?im)^\s*{Regex.Escape(rawLabel)}\s*[:\u003a\u2013\-]+\s*(?<lv>[^\r\n]+)\s*$";
            var strictLm = Regex.Match(content, strictLineRx);
            if (strictLm.Success && !string.IsNullOrWhiteSpace(strictLm.Groups["lv"].Value))
            {
                extracted[token] = strictLm.Groups["lv"].Value.Trim();
                continue;
            }

            var labelAlt = AlternateLabelMatchersForExtraction(rawLabel);
            var flexLineRx = $@"(?im)^\s*(?:{labelAlt})\s*[:\u003a\u2013\-]+\s*(?<lv>[^\r\n]+)\s*$";
            var flexLm = Regex.Match(content, flexLineRx);
            if (flexLm.Success && !string.IsNullOrWhiteSpace(flexLm.Groups["lv"].Value))
            {
                extracted[token] = flexLm.Groups["lv"].Value.Trim();
                continue;
            }

            var siblingStop = BuildSiblingLabelStopAheadForExtraction(rawLabel, allNamesTrimmed);
            var strictInlineRx = $@"(?is)(?:^|[\s,;])(?:{Regex.Escape(rawLabel)})\s*[:\u003a\u2013\-]+\s*(?<mv>.+?){siblingStop}";
            var si = Regex.Match(content, strictInlineRx);
            if (si.Success && !string.IsNullOrWhiteSpace(si.Groups["mv"].Value))
            {
                extracted[token] = si.Groups["mv"].Value.Trim();
                continue;
            }

            var flexInlineRx = $@"(?is)(?:^|[\s,;])(?:{labelAlt})\s*[:\u003a\u2013\-]+\s*(?<mv>.+?){siblingStop}";
            var fi = Regex.Match(content, flexInlineRx);
            if (fi.Success && !string.IsNullOrWhiteSpace(fi.Groups["mv"].Value))
            {
                extracted[token] = fi.Groups["mv"].Value.Trim();
                continue;
            }

            if (!string.IsNullOrWhiteSpace(field.ExampleValue))
            {
                var idx = content.IndexOf(field.ExampleValue, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    extracted[token] = field.ExampleValue.Trim();
                }
            }
        }

        return extracted;
    }

    private static string RenderWorkflowTemplate(
        string template,
        string classificationLabel,
        double classificationScore,
        string fileName,
        IReadOnlyDictionary<string, string> fields)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        var rendered = template
            .Replace("{classificationLabel}", classificationLabel, StringComparison.OrdinalIgnoreCase)
            .Replace("{classificationScore}", classificationScore.ToString("0.####"), StringComparison.OrdinalIgnoreCase)
            .Replace("{subject}", fileName, StringComparison.OrdinalIgnoreCase)
            .Replace("{from}", "test@local", StringComparison.OrdinalIgnoreCase)
            .Replace("{receivedDate}", DateTime.UtcNow.ToString("u"), StringComparison.OrdinalIgnoreCase);

        rendered = Regex.Replace(rendered, @"\{field:([a-zA-Z0-9_\- ]+)\}", match =>
        {
            var key = NormalizeFieldToken(match.Groups[1].Value);
            return fields.TryGetValue(key, out var value) ? value : string.Empty;
        });

        rendered = rendered.Replace(
            "{extractionJson}",
            JsonSerializer.Serialize(fields),
            StringComparison.OrdinalIgnoreCase);

        if (fields.TryGetValue("summary", out var summary))
        {
            rendered = rendered
                .Replace("{summary}", summary, StringComparison.OrdinalIgnoreCase)
                .Replace("{summarisation}", summary, StringComparison.OrdinalIgnoreCase);
        }

        return rendered;
    }

    private static string NormalizeFieldToken(string value)
    {
        return Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
    }

    private static async Task<string> ExtractTextAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension == ".pdf")
        {
            await using var stream = file.OpenReadStream();
            var text = DocumentHubAiHelper.ExtractTextFromPdf(stream);
            return DocumentHubAiHelper.NormalizeWhitespace(text);
        }

        if (extension is ".txt" or ".csv" or ".json" or ".xml" or ".log" or ".md")
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var raw = await reader.ReadToEndAsync();
            return DocumentHubAiHelper.NormalizeWhitespace(raw);
        }

        return string.Empty;
    }

    private static (string Label, double Score, string Explainability, ClassificationTemplate? BestTemplate) ClassifyAgainstTemplates(
        string content,
        List<ClassificationTemplate> templates)
    {
        if (templates.Count == 0 || string.IsNullOrWhiteSpace(content))
        {
            return ("Unclassified", 0, "No active classification templates or no usable content was available.", null);
        }

        var contentTokens = Tokenize(content);
        var normalizedContent = NormalizeForPhraseMatch(content);
        if (contentTokens.Count == 0)
        {
            return ("Unclassified", 0, "Content did not contain enough meaningful tokens for template matching.", null);
        }

        ClassificationTemplate? bestTemplate = null;
        double bestScore = 0d;
        double secondBestScore = 0d;
        double bestCoreScore = 0d;
        double bestLabelCoverage = 0d;
        double bestPhraseBoost = 0d;
        var bestOverlapTerms = new List<string>();

        foreach (var template in templates)
        {
            var weightedTemplateTokens = BuildWeightedTemplateTokens(template);
            if (weightedTemplateTokens.Count == 0)
            {
                continue;
            }

            var overlapTerms = weightedTemplateTokens.Keys
                .Where(contentTokens.Contains)
                .Take(10)
                .ToList();
            var matchedWeight = weightedTemplateTokens
                .Where(kv => contentTokens.Contains(kv.Key))
                .Sum(kv => kv.Value);
            var totalWeight = weightedTemplateTokens.Values.Sum();
            var coreScore = totalWeight <= 0 ? 0d : matchedWeight / totalWeight;

            var labelTokens = ExtractTokens(template.ClassificationLabel);
            var labelMatches = labelTokens.Count == 0
                ? 0
                : labelTokens.Count(contentTokens.Contains);
            var labelCoverage = labelTokens.Count == 0
                ? 0d
                : (double)labelMatches / labelTokens.Count;

            var phraseBoost = ContainsPhrase(normalizedContent, template.ClassificationLabel) ? 0.25d : 0d;
            var score = Math.Min(1d, (coreScore * 0.65d) + (labelCoverage * 0.25d) + phraseBoost);

            if (score > bestScore)
            {
                secondBestScore = bestScore;
                bestScore = score;
                bestCoreScore = coreScore;
                bestLabelCoverage = labelCoverage;
                bestPhraseBoost = phraseBoost;
                bestTemplate = template;
                bestOverlapTerms = overlapTerms;
            }
            else if (score > secondBestScore)
            {
                secondBestScore = score;
            }
        }

        var calibratedScore = CalibrateConfidence(
            bestScore,
            secondBestScore,
            bestLabelCoverage,
            bestPhraseBoost,
            bestOverlapTerms.Count);

        if (bestTemplate == null || calibratedScore < 0.28)
        {
            var explanation = bestTemplate == null
                ? "No template produced a meaningful lexical overlap with the document content."
                : $"Best template calibrated confidence {Math.Round(calibratedScore, 4)} is below threshold 0.28. " +
                  $"Raw={Math.Round(bestScore, 4)}, secondBest={Math.Round(secondBestScore, 4)}. " +
                  $"Closest template '{bestTemplate.ClassificationLabel}' had core={Math.Round(bestCoreScore, 4)}, " +
                  $"labelCoverage={Math.Round(bestLabelCoverage, 4)}, phraseBoost={Math.Round(bestPhraseBoost, 2)} " +
                  $"with overlap terms: {string.Join(", ", bestOverlapTerms.DefaultIfEmpty("none"))}.";
            return ("Unclassified", Math.Round(calibratedScore, 4), explanation, null);
        }

        var explainability =
            $"Matched template '{bestTemplate.ClassificationLabel}' with calibrated confidence {Math.Round(calibratedScore, 4)} " +
            $"(raw={Math.Round(bestScore, 4)}, secondBest={Math.Round(secondBestScore, 4)}). " +
            $"(core={Math.Round(bestCoreScore, 4)}, labelCoverage={Math.Round(bestLabelCoverage, 4)}, phraseBoost={Math.Round(bestPhraseBoost, 2)}). " +
            $"Overlap terms: {string.Join(", ", bestOverlapTerms.DefaultIfEmpty("none"))}.";

        return (bestTemplate.ClassificationLabel, Math.Round(calibratedScore, 4), explainability, bestTemplate);
    }

    private static double CalibrateConfidence(
        double rawScore,
        double secondBestScore,
        double labelCoverage,
        double phraseBoost,
        int overlapTermCount)
    {
        var margin = Math.Max(0d, rawScore - secondBestScore);
        var calibrated = rawScore;
        calibrated += Math.Min(0.22d, margin * 1.1d);
        calibrated += Math.Min(0.18d, labelCoverage * 0.22d);
        calibrated += Math.Min(0.12d, overlapTermCount * 0.015d);
        if (phraseBoost > 0)
        {
            calibrated += 0.08d;
        }

        calibrated = Math.Min(0.995d, Math.Max(0d, calibrated));

        // Ease-out curve so strong template matches read as high confidence.
        calibrated = 1d - Math.Pow(1d - calibrated, 1.65d);
        return Math.Min(0.995d, Math.Max(0d, calibrated));
    }

    private static Dictionary<string, double> BuildWeightedTemplateTokens(ClassificationTemplate template)
    {
        var weighted = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        AddWeightedTokens(weighted, template.ClassificationLabel, 3.0d);
        AddWeightedTokens(weighted, template.ClassificationDescription, 1.5d);
        AddWeightedTokens(weighted, template.ClassificationPrompt, 1.0d);
        return weighted;
    }

    private static void AddWeightedTokens(Dictionary<string, double> target, string? value, double weight)
    {
        foreach (var token in ExtractTokens(value))
        {
            if (!target.TryGetValue(token, out var current) || weight > current)
            {
                target[token] = weight;
            }
        }
    }

    private static List<string> ExtractTokens(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        return Regex.Matches(value.ToLowerInvariant(), "[a-z0-9]{3,}")
            .Select(m => m.Value)
            .Where(token => !StopWords.Contains(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static HashSet<string> Tokenize(string value)
    {
        return ExtractTokens(value).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool ContainsPhrase(string haystack, string phrase)
    {
        var normalizedPhrase = NormalizeForPhraseMatch(phrase);
        return normalizedPhrase.Length >= 4 && haystack.Contains(normalizedPhrase, StringComparison.Ordinal);
    }

    private static string NormalizeForPhraseMatch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Regex.Replace(value.ToLowerInvariant(), @"\s+", " ").Trim();
    }

    private sealed class ClassificationTemplate
    {
        public int DocumentClassificationLabelId { get; set; }
        public string ClassificationLabel { get; set; } = string.Empty;
        public string? ClassificationDescription { get; set; }
        public string ClassificationPrompt { get; set; } = string.Empty;
    }
}
