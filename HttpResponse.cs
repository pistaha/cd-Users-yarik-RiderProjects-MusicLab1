using System.Text;

namespace MusicLab1;

public class HttpResponse
{
    // Константы для статус-кодов
    public const int StatusOk = 200;
    public const int StatusCreated = 201;
    public const int StatusFound = 302;
    public const int StatusBadRequest = 400;
    public const int StatusNotFound = 404;
    public const int StatusPayloadTooLarge = 413;
    public const int StatusInternalServerError = 500;

    // Константы для фраз статусов
    public const string ReasonOk = "OK";
    public const string ReasonCreated = "Created";
    public const string ReasonFound = "Found";
    public const string ReasonBadRequest = "Bad Request";
    public const string ReasonNotFound = "Not Found";
    public const string ReasonPayloadTooLarge = "Payload Too Large";
    public const string ReasonInternalServerError = "Internal Server Error";

    // Константы для заголовков
    public const string HeaderContentType = "Content-Type";
    public const string HeaderContentLength = "Content-Length";
    public const string HeaderLocation = "Location";
    public const string HeaderServer = "Server";

    // Константы для типов контента
    public const string ContentTypeHtml = "text/html; charset=utf-8";
    public const string ContentTypeJson = "application/json; charset=utf-8";
    public const string ContentTypeText = "text/plain; charset=utf-8";

    // Константы для формата ответа
    private const string ResponseLineFormat = "HTTP/1.1 {0} {1}\r\n";
    private const string HeaderFormat = "{0}: {1}\r\n";
    private const string ResponseEnd = "\r\n";
    private const string DefaultRedirectBody = "<h1>302 Found</h1><p>Redirecting to <a href='{0}'>{0}</a></p>";

    public int StatusCode { get; }
    public string ReasonPhrase { get; }
    public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string Body { get; }

    private HttpResponse(int statusCode, string reasonPhrase, string body, string contentType = ContentTypeHtml)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        Body = body;
        Headers[HeaderContentType] = contentType;
        Headers[HeaderServer] = "MusicLab1/1.0.0";
    }

    // Статические фабричные методы
    public static HttpResponse Ok(string body) => new(StatusOk, ReasonOk, body);
    public static HttpResponse Ok(string body, string contentType) => new(StatusOk, ReasonOk, body, contentType);
    public static HttpResponse Json(string json) => new(StatusOk, ReasonOk, json, ContentTypeJson);
    public static HttpResponse Text(string text) => new(StatusOk, ReasonOk, text, ContentTypeText);
    public static HttpResponse Created(string body) => new(StatusCreated, ReasonCreated, body);
    
    public static HttpResponse NotFound(string body) => new(StatusNotFound, ReasonNotFound, body);
    public static HttpResponse BadRequest(string body) => new(StatusBadRequest, ReasonBadRequest, body);
    public static HttpResponse PayloadTooLarge(string body) => new(StatusPayloadTooLarge, ReasonPayloadTooLarge, body);
    public static HttpResponse InternalServerError(string body) => new(StatusInternalServerError, ReasonInternalServerError, body);

    public static HttpResponse Redirect(string location)
    {
        var resp = new HttpResponse(StatusFound, ReasonFound, string.Format(DefaultRedirectBody, location));
        resp.Headers[HeaderLocation] = location;
        return resp;
    }

    public static HttpResponse Redirect(string location, string message)
    {
        var resp = new HttpResponse(StatusFound, ReasonFound, message);
        resp.Headers[HeaderLocation] = location;
        return resp;
    }

    public byte[] ToBytes()
    {
        var bodyBytes = Encoding.UTF8.GetBytes(Body);
        Headers[HeaderContentLength] = bodyBytes.Length.ToString();
        
        var headerBuilder = new StringBuilder();
        headerBuilder.AppendFormat(ResponseLineFormat, StatusCode, ReasonPhrase);
        
        foreach (var header in Headers)
        {
            headerBuilder.AppendFormat(HeaderFormat, header.Key, header.Value);
        }
        
        headerBuilder.Append(ResponseEnd);
        
        var headerBytes = Encoding.UTF8.GetBytes(headerBuilder.ToString());
        var result = new byte[headerBytes.Length + bodyBytes.Length];
        
        Buffer.BlockCopy(headerBytes, 0, result, 0, headerBytes.Length);
        Buffer.BlockCopy(bodyBytes, 0, result, headerBytes.Length, bodyBytes.Length);
        
        return result;
    }
}