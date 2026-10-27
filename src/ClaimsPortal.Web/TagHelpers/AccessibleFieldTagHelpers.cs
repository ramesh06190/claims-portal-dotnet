using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ClaimsPortal.Web.TagHelpers;

/// <summary>
/// Adds aria-invalid and links the error message with aria-describedby on any input or select
/// that has a model state error. Built in ASP.NET only adds a CSS class, which screen readers
/// never hear.
///
/// Use error-key when several inputs share one error, like the three parts of a date.
/// </summary>
[HtmlTargetElement("input", Attributes = "asp-for")]
[HtmlTargetElement("select", Attributes = "asp-for")]
[HtmlTargetElement("input", Attributes = ErrorKeyAttribute)]
public sealed class AccessibleFieldTagHelper : TagHelper
{
    private const string ErrorKeyAttribute = "error-key";

    // Run after the built in tag helpers have written id and name
    public override int Order => 1000;

    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = default!;

    [HtmlAttributeName(ErrorKeyAttribute)]
    public string? ErrorKey { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var key = ErrorKey ?? output.Attributes["name"]?.Value?.ToString();
        if (key is null || !ViewContext.ModelState.TryGetValue(key, out var entry) || entry.Errors.Count == 0)
        {
            return;
        }

        output.Attributes.SetAttribute("aria-invalid", "true");
        var describedBy = output.Attributes["aria-describedby"]?.Value?.ToString();
        var errorId = $"{key}-error";
        output.Attributes.SetAttribute("aria-describedby", string.IsNullOrEmpty(describedBy) ? errorId : $"{describedBy} {errorId}");
    }
}

/// <summary>Renders &lt;p class="field__error" id="{key}-error"&gt;Error: message&lt;/p&gt; when the key has an error.</summary>
[HtmlTargetElement("field-error", TagStructure = TagStructure.WithoutEndTag)]
public sealed class FieldErrorTagHelper : TagHelper
{
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = default!;

    public string Key { get; set; } = "";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (!ViewContext.ModelState.TryGetValue(Key, out var entry) || entry.Errors.Count == 0)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "p";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "field__error");
        output.Attributes.SetAttribute("id", $"{Key}-error");
        // "Error:" is read out so the message is not conveyed by color alone
        output.Content.SetContent($"Error: {entry.Errors[0].ErrorMessage}");
    }
}
