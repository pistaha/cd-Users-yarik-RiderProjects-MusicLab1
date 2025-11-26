namespace MusicLab1;

public class Router
{
    private readonly Dictionary<RouteKey, RouteHandler> _handlers = new();
    private readonly List<RouteInfo> _routes = new();

    public IReadOnlyList<RouteInfo> Routes => _routes;
    
    public async Task<RouteInfo> MapGet(
        string path,
        RouteHandler handler,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await AddRoute(HttpRequest.MethodGet, path, handler, description, cancellationToken);
    }

    public async Task<RouteInfo> MapPost(
        string path,
        RouteHandler handler,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await AddRoute(HttpRequest.MethodPost, path, handler, description, cancellationToken);
    }

    private async Task<RouteInfo> AddRoute(
        string method,
        string path,
        RouteHandler handler,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException("HTTP method must be provided.", nameof(method));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Route path must be provided.", nameof(path));
        }

        var normalizedMethod = method.ToUpperInvariant();
        var normalizedPath = NormalizePath(path);
        var key = new RouteKey(normalizedMethod, normalizedPath);

        if (!_handlers.TryAdd(key, handler))
        {
            throw new InvalidOperationException($"Route '{normalizedMethod} {normalizedPath}' is already registered.");
        }

        var info = new RouteInfo(normalizedMethod, normalizedPath, description ?? string.Empty);
        _routes.Add(info);
        
        return info;
    }

    public async Task<HttpResponse> HandleAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var key = new RouteKey(request.Method.ToUpperInvariant(), NormalizePath(request.Path));
        if (_handlers.TryGetValue(key, out var handler))
        {
            return await handler(request, cancellationToken);
        }

        return HttpResponse.NotFound("<h1>404 Not Found</h1><p>Страница не найдена</p><a href='/'>На главную</a>");
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalized = path.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        var queryIndex = normalized.IndexOf('?');
        if (queryIndex >= 0)
        {
            normalized = normalized[..queryIndex];
        }

        if (normalized.Length > 1)
        {
            normalized = normalized.TrimEnd('/');
        }

        return normalized;
    }

    private readonly record struct RouteKey(string Method, string Path);

    public delegate Task<HttpResponse> RouteHandler(HttpRequest request, CancellationToken cancellationToken);
}

public sealed record RouteInfo(string Method, string Path, string Description);
