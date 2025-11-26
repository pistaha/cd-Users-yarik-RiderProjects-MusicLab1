using System.Net;

namespace MusicLab1;

public interface IViewRenderer
{
    string Render(string viewName, IDictionary<string, string> model);
}

public class ViewRenderer : IViewRenderer
{
    private const string TemplatePrefix = "{{";
    private const string TemplateSuffix = "}}";
    private const string TemplateRawPrefix = "{{="; // Префикс для "raw" (без экранирования HTML)
    private const string TemplateNotFoundMessage = "<h1>Template '{0}' not found</h1>";
    
    private readonly string _viewsPath;

    public ViewRenderer(string viewsPath)
    {
        _viewsPath = viewsPath;
    }

    public string Render(string viewName, IDictionary<string, string> model)
    {
        var fullPath = Path.Combine(_viewsPath, viewName);
        if (!File.Exists(fullPath))
            return string.Format(TemplateNotFoundMessage, viewName);
            
        var html = File.ReadAllText(fullPath);

        // Сначала заменяем "raw" вставки (без экранирования)
        foreach (var kv in model.OrderByDescending(x => x.Key.Length))
        {
            html = html.Replace(TemplateRawPrefix + kv.Key + TemplateSuffix, kv.Value);
        }

        // Затем заменяем обычные вставки (с экранированием HTML)
        foreach (var kv in model.OrderByDescending(x => x.Key.Length))
        {
            html = html.Replace(TemplatePrefix + kv.Key + TemplateSuffix, WebUtility.HtmlEncode(kv.Value));
        }
        
        return html;
    }
}