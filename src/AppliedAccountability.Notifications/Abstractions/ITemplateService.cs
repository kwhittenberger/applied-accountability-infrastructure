namespace AppliedAccountability.Notifications.Abstractions;

/// <summary>
/// Service for rendering notification templates.
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Renders a template with the provided data.
    /// </summary>
    Task<string> RenderAsync(string template, Dictionary<string, object>? data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a named template with the provided data.
    /// </summary>
    Task<string> RenderTemplateAsync(string templateName, Dictionary<string, object>? data, CancellationToken cancellationToken = default);
}
