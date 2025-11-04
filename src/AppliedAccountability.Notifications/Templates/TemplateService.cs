using AppliedAccountability.Notifications.Abstractions;
using Microsoft.Extensions.Logging;
using Scriban;

namespace AppliedAccountability.Notifications.Templates;

/// <summary>
/// Template rendering service using Scriban.
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly Dictionary<string, Template> _compiledTemplates = new();

    public TemplateService(ILogger<TemplateService> logger)
    {
        _logger = logger;
    }

    public async Task<string> RenderAsync(string template, Dictionary<string, object>? data, CancellationToken cancellationToken = default)
    {
        try
        {
            var compiledTemplate = Template.Parse(template);
            if (compiledTemplate.HasErrors)
            {
                var errors = string.Join(", ", compiledTemplate.Messages.Select(m => m.Message));
                throw new InvalidOperationException($"Template parsing failed: {errors}");
            }

            return await compiledTemplate.RenderAsync(data ?? new Dictionary<string, object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template");
            throw;
        }
    }

    public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, object>? data, CancellationToken cancellationToken = default)
    {
        // For now, this is a simple implementation
        // In production, you'd load templates from a database or file system
        if (!_compiledTemplates.TryGetValue(templateName, out var template))
        {
            throw new InvalidOperationException($"Template '{templateName}' not found");
        }

        return await template.RenderAsync(data ?? new Dictionary<string, object>());
    }

    /// <summary>
    /// Registers a named template.
    /// </summary>
    public void RegisterTemplate(string name, string templateContent)
    {
        var compiled = Template.Parse(templateContent);
        if (compiled.HasErrors)
        {
            var errors = string.Join(", ", compiled.Messages.Select(m => m.Message));
            throw new InvalidOperationException($"Template '{name}' parsing failed: {errors}");
        }

        _compiledTemplates[name] = compiled;
    }
}
