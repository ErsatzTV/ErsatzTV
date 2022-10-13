using Ganss.Xss;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ErsatzTV.Shared;

public partial class MarkdownView
{
    private string _content;

    [Inject]
    public IHtmlSanitizer HtmlSanitizer { get; set; }

    [Inject]
    public IJSRuntime JsRuntime { get; set; }

    [Parameter]
    public string Content
    {
        get => _content;
        set
        {
            _content = value;
            HtmlContent = ConvertStringToMarkupString(_content);
        }
    }

    public MarkupString HtmlContent { get; private set; }

    private MarkupString ConvertStringToMarkupString(string value)
    {
        if (!string.IsNullOrWhiteSpace(_content))
        {
            // Convert markdown string to HTML
            string html = Markdown.ToHtml(value, new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());

            // Sanitize HTML before rendering
            string sanitizedHtml = HtmlSanitizer.Sanitize(html);

            // Return sanitized HTML as a MarkupString that Blazor can render
            return new MarkupString(sanitizedHtml);
        }

        return new MarkupString();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("styleMarkdown");
        }
        catch (Exception)
        {
            // ignored
        }

        await base.OnAfterRenderAsync(true);
    }
}
