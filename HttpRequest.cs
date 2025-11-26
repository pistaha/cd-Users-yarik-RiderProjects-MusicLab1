using System.Collections.ObjectModel;
using System.Text;

namespace MusicLab1;

public class HttpRequest
{
    // Константы для HTTP методов
    public const string MethodGet = "GET";
    public const string MethodPost = "POST";
    public const string MethodPut = "PUT";
    public const string MethodDelete = "DELETE";
    
    // Константы для заголовков
    public const string HeaderContentLength = "Content-Length";
    public const string HeaderContentType = "Content-Type";
    
    // Константы для типов контента
    public const string ContentTypeFormUrlEncoded = "application/x-www-form-urlencoded";
    public const string ContentTypeJson = "application/json";

    // Ограничения для входящих запросов
    private const int MaxRequestLineLength = 2048;
    private const int MaxHeaderCount = 64;
    private const int MaxBodySize = 1024 * 1024; // 1 MB для лабораторной работы

    private readonly ReadOnlyDictionary<string, string> _headers;
    private readonly ReadOnlyDictionary<string, string> _queryParameters;
    private readonly ReadOnlyDictionary<string, string> _formParameters;

    public string Method { get; }
    public string Path { get; }
    public string QueryString { get; }
    public string HttpVersion { get; }
    public string Body { get; }
    public IReadOnlyDictionary<string, string> Headers => _headers;
    public IReadOnlyDictionary<string, string> QueryParameters => _queryParameters;
    public IReadOnlyDictionary<string, string> FormParameters => _formParameters;

    private HttpRequest(
        string method,
        string path,
        string queryString,
        string httpVersion,
        Dictionary<string, string> headers,
        string body,
        Dictionary<string, string> queryParameters,
        Dictionary<string, string> formParameters)
    {
        Method = method;
        Path = string.IsNullOrEmpty(path) ? "/" : path;
        QueryString = queryString;
        HttpVersion = httpVersion;
        Body = body ?? string.Empty;
        
        _headers = new ReadOnlyDictionary<string, string>(headers);
        _queryParameters = new ReadOnlyDictionary<string, string>(queryParameters);
        _formParameters = new ReadOnlyDictionary<string, string>(formParameters);
    }

    public static async Task<HttpRequest?> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);

        var requestLine = await ReadRequestLineAsync(reader, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(requestLine))
        {
            return null;
        }

        if (requestLine.Length > MaxRequestLineLength)
        {
            return null;
        }

        var requestLineParts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (requestLineParts.Length != 3)
        {
            return null;
        }

        var method = requestLineParts[0].ToUpperInvariant();
        var target = requestLineParts[1];
        var version = requestLineParts[2];

        var (path, queryString) = SplitPathAndQuery(target);
        var headers = await ReadHeadersAsync(reader, cancellationToken).ConfigureAwait(false);
        if (headers is null)
        {
            return null;
        }

        var body = await ReadBodyAsync(reader, headers, cancellationToken).ConfigureAwait(false);
        if (body is null)
        {
            return null;
        }

        var queryParameters = ParseFormEncoded(queryString);
        var formParameters = ShouldParseForm(headers)
            ? ParseFormEncoded(body)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return new HttpRequest(
            method,
            path,
            queryString,
            version,
            headers,
            body,
            queryParameters,
            formParameters);
    }

    public bool TryGetHeader(string name, out string? value) =>
        Headers.TryGetValue(name, out value);

    public bool IsMethod(string method) =>
        string.Equals(Method, method, StringComparison.OrdinalIgnoreCase);

    public string? GetParam(string name)
    {
        if (FormParameters.TryGetValue(name, out var formValue))
            return formValue;

        if (QueryParameters.TryGetValue(name, out var queryValue))
            return queryValue;

        return null;
    }

    private static (string path, string query) SplitPathAndQuery(string target)
    {
        if (string.IsNullOrEmpty(target))
            return ("/", string.Empty);
        
        var index = target.IndexOf('?');
        if (index < 0)
            return (target, string.Empty);

        var path = index == 0 ? "/" : target[..index];
        var query = index < target.Length - 1 ? target[(index + 1)..] : string.Empty;
        return (path, query);
    }

    private static async Task<string?> ReadRequestLineAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        string? line;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            line = await reader.ReadLineAsync().ConfigureAwait(false);
        } while (line is not null && line.Length == 0);

        return line;
    }

    private static async Task<Dictionary<string, string>?> ReadHeadersAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? line;
        
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null && line.Length > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            headers[name] = value;

            if (headers.Count > MaxHeaderCount)
            {
                return null;
            }
        }

        return headers;
    }

    private static async Task<string?> ReadBodyAsync(StreamReader reader, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        if (!headers.TryGetValue(HeaderContentLength, out var lengthValue))
        {
            return string.Empty;
        }

        if (!int.TryParse(lengthValue, out var contentLength) || contentLength < 0 || contentLength > MaxBodySize)
        {
            return null;
        }

        if (contentLength == 0)
        {
            return string.Empty;
        }

        var buffer = new char[contentLength];
        var read = 0;

        while (read < contentLength && !cancellationToken.IsCancellationRequested)
        {
            var current = await reader.ReadAsync(buffer, read, contentLength - read).ConfigureAwait(false);
            if (current == 0)
            {
                break;
            }
            read += current;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        if (read < contentLength)
        {
            return null;
        }

        return new string(buffer, 0, read);
    }

    private static bool ShouldParseForm(IReadOnlyDictionary<string, string> headers)
    {
        if (!headers.TryGetValue(HeaderContentType, out var contentType))
        {
            return false;
        }

        return contentType.StartsWith(ContentTypeFormUrlEncoded, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> ParseFormEncoded(string data)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(data))
        {
            return result;
        }

        var pairs = data.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var separatorIndex = pair.IndexOf('=');
            string key;
            string value;

            if (separatorIndex >= 0)
            {
                key = pair[..separatorIndex];
                value = pair[(separatorIndex + 1)..];
            }
            else
            {
                key = pair;
                value = string.Empty;
            }

            key = Uri.UnescapeDataString(key.Replace('+', ' '));
            value = Uri.UnescapeDataString(value.Replace('+', ' '));

            if (!string.IsNullOrWhiteSpace(key))
            {
                result[key] = value;
            }
        }

        return result;
    }
}