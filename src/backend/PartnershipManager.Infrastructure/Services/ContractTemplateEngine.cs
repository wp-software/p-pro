// F3-BLD-BE-001: Contract Template Engine
// File: src/backend/PartnershipManager.Infrastructure/Services/ContractTemplateEngine.cs
// Author: GitHub Copilot
// Date: 13/02/2026

using System.Text;
using System.Text.RegularExpressions;
using PartnershipManager.Application.Common.Security;

namespace PartnershipManager.Infrastructure.Services;

/// <summary>
/// Template engine for processing contract templates with variable substitution
/// </summary>
public static class ContractTemplateEngine
{
    private static readonly Regex VariableRegex = new(@"\{\{([^\}]+)\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Extract all variable names from content
    /// </summary>
    /// <param name="content">Template or clause content</param>
    /// <returns>List of variable names (without braces)</returns>
    public static List<string> ExtractVariables(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<string>();

        var matches = VariableRegex.Matches(content);
        return matches
            .Select(m => m.Groups[1].Value.Trim())
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Replace variables in content with provided values
    /// </summary>
    /// <param name="content">Content with {{variables}}</param>
    /// <param name="variables">Dictionary of variable values</param>
    /// <param name="throwOnMissing">Whether to throw exception if variable is missing</param>
    /// <returns>Content with variables replaced</returns>
    public static string SubstituteVariables(
        string content,
        Dictionary<string, string> variables,
        bool throwOnMissing = false)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        if (variables == null || variables.Count == 0)
        {
            if (throwOnMissing && ExtractVariables(content).Any())
                throw new InvalidOperationException("Variables found in content but no values provided");
            
            return content;
        }

        // Create case-insensitive dictionary for lookup
        var variablesLower = new Dictionary<string, string>(
            variables,
            StringComparer.OrdinalIgnoreCase);

        return VariableRegex.Replace(content, match =>
        {
            var variableName = match.Groups[1].Value.Trim();
            
            if (variablesLower.TryGetValue(variableName, out var value))
            {
                return value;
            }

            if (throwOnMissing)
            {
                throw new InvalidOperationException($"Missing value for variable: {variableName}");
            }

            // Return original if not found and not throwing
            return match.Value;
        });
    }

    /// <summary>
    /// Validate that all required variables have values
    /// </summary>
    /// <param name="content">Content to validate</param>
    /// <param name="variables">Provided variables</param>
    /// <returns>List of missing variable names</returns>
    public static List<string> ValidateVariables(
        string content,
        Dictionary<string, string> variables)
    {
        var requiredVariables = ExtractVariables(content);
        
        if (!requiredVariables.Any())
            return new List<string>();

        var variablesLower = new Dictionary<string, string>(
            variables ?? new Dictionary<string, string>(),
            StringComparer.OrdinalIgnoreCase);

        return requiredVariables
            .Where(v => !variablesLower.ContainsKey(v))
            .ToList();
    }

    /// <summary>
    /// Merge template content with clauses
    /// </summary>
    /// <param name="templateContent">Main template content</param>
    /// <param name="clauseContents">List of clause contents (already with variables substituted)</param>
    /// <param name="clausePlaceholder">Placeholder in template where clauses should be inserted (default: {{CLAUSES}})</param>
    /// <returns>Merged content</returns>
    public static string MergeClauses(
        string templateContent,
        List<string> clauseContents,
        string clausePlaceholder = "{{CLAUSES}}")
    {
        if (string.IsNullOrWhiteSpace(templateContent))
            return templateContent;

        if (clauseContents == null || !clauseContents.Any())
        {
            // Remove placeholder if no clauses
            return templateContent.Replace(clausePlaceholder, string.Empty);
        }

        // Build clauses HTML
        var clausesHtml = new StringBuilder();
        for (int i = 0; i < clauseContents.Count; i++)
        {
            clausesHtml.AppendLine($"<div class=\"clause\" data-clause-index=\"{i + 1}\">");
            clausesHtml.AppendLine(clauseContents[i]);
            clausesHtml.AppendLine("</div>");
            
            if (i < clauseContents.Count - 1)
            {
                clausesHtml.AppendLine(); // Spacing between clauses
            }
        }

        // Replace placeholder with clauses
        return templateContent.Replace(clausePlaceholder, clausesHtml.ToString());
    }

    /// <summary>
    /// Generate HTML wrapper for contract content
    /// </summary>
    /// <param name="title">Contract title</param>
    /// <param name="content">Contract content (with clauses merged)</param>
    /// <param name="metadata">Optional metadata to include in header</param>
    /// <returns>Complete HTML document</returns>
    public static string GenerateHtmlDocument(
        string title,
        string content,
        Dictionary<string, string>? metadata = null)
    {
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"pt-BR\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine($"    <title>{System.Security.SecurityElement.Escape(title)}</title>");
        html.AppendLine("    <style>");
        html.AppendLine(GetDefaultStyles());
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");
        
        // Header with title
        html.AppendLine("        <header>");
        html.AppendLine($"            <h1>{System.Security.SecurityElement.Escape(title)}</h1>");
        
        // Metadata
        if (metadata != null && metadata.Any())
        {
            html.AppendLine("            <div class=\"metadata\">");
            foreach (var meta in metadata)
            {
                html.AppendLine($"                <p><strong>{System.Security.SecurityElement.Escape(meta.Key)}:</strong> {System.Security.SecurityElement.Escape(meta.Value)}</p>");
            }
            html.AppendLine("            </div>");
        }
        
        html.AppendLine("        </header>");
        
        // Main content
        html.AppendLine("        <main>");
        html.AppendLine(content);
        html.AppendLine("        </main>");
        
        // Footer
        html.AppendLine("        <footer>");
        html.AppendLine($"            <p>Documento gerado em {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC</p>");
        html.AppendLine("            <p>P-Pro | WP Manager - Sistema de Gestão de Parcerias</p>");
        html.AppendLine("        </footer>");
        
        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        return html.ToString();
    }

    /// <summary>
    /// Get default CSS styles for contract documents
    /// </summary>
    private static string GetDefaultStyles()
    {
        return @"
body {
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    line-height: 1.6;
    color: #333;
    max-width: 210mm;
    margin: 0 auto;
    padding: 20mm;
    background: #fff;
}

.container {
    background: white;
}

header {
    border-bottom: 3px solid #0066cc;
    padding-bottom: 20px;
    margin-bottom: 30px;
}

header h1 {
    color: #0066cc;
    font-size: 28px;
    margin: 0 0 15px 0;
}

.metadata {
    background: #f8f9fa;
    padding: 15px;
    border-left: 4px solid #0066cc;
    margin-top: 15px;
}

.metadata p {
    margin: 5px 0;
    font-size: 14px;
}

main {
    margin: 30px 0;
}

.clause {
    margin: 20px 0;
    padding: 15px;
    background: #fafafa;
    border-left: 3px solid #ccc;
}

.clause:hover {
    background: #f5f5f5;
}

h2 {
    color: #0066cc;
    font-size: 20px;
    margin-top: 30px;
}

h3 {
    color: #333;
    font-size: 16px;
    margin-top: 20px;
}

p {
    margin: 10px 0;
    text-align: justify;
}

ul, ol {
    margin: 10px 0;
    padding-left: 30px;
}

footer {
    border-top: 2px solid #ddd;
    padding-top: 20px;
    margin-top: 40px;
    text-align: center;
    font-size: 12px;
    color: #666;
}

footer p {
    margin: 5px 0;
}

@media print {
    body {
        padding: 0;
    }
    
    .clause {
        page-break-inside: avoid;
    }
}
        ".Trim();
    }

    /// <summary>
    /// Sanitize HTML content to prevent XSS
    /// </summary>
    /// <param name="html">HTML content</param>
    /// <returns>Sanitized HTML</returns>
    public static string SanitizeHtml(string html)
    {
        return HtmlContentSanitizer.Sanitize(html);
    }
}
