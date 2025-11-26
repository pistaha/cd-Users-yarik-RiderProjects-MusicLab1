using System.Net;
using System.Net.Sockets;

namespace MusicLab1;

public class HttpServer
{
    // Константы для конфигурации сервера
    private const int DefaultBacklog = 10;
    private const bool DefaultReuseAddress = true;
    
    // Константы для сообщений
    private const string MalformedRequestMessage = "Malformed request";
    private const string InternalErrorMessage = "Internal server error";
    
    private readonly TcpListener _listener;
    private readonly Router _router;
    private volatile bool _running = true;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public HttpServer(IPAddress address, int port, Router router)
    {
        _listener = new TcpListener(address, port);
        _router = router;
    }

    public async Task StartAsync()
    {
        try
        {
            _listener.Start(DefaultBacklog);
            
            while (_running && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
                    _ = HandleClientAsync(client, _cancellationTokenSource.Token).ContinueWith(t =>
                    {
                        if (t.IsFaulted && t.Exception != null)
                        {
                            Console.WriteLine($"Client handling error: {t.Exception.InnerException?.Message}");
                        }
                    }, TaskContinuationOptions.OnlyOnFaulted);
                }
                catch (OperationCanceledException)
                {
                    // Сервер был остановлен
                    break;
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Socket error: {ex.Message}");
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Server error: {ex.Message}");
        }
        finally
        {
            _listener.Stop();
        }
    }

    public void Stop()
    {
        _running = false;
        _cancellationTokenSource.Cancel();
        _listener.Stop();
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using (client)
            await using (var stream = client.GetStream())
            {
                var request = await HttpRequest.ParseAsync(stream, cancellationToken);
                if (request is null)
                {
                    await WriteResponseAsync(stream, HttpResponse.BadRequest(MalformedRequestMessage), cancellationToken);
                    return;
                }

                Console.WriteLine($"-> {DateTime.Now:HH:mm:ss} {request.Method} {request.Path}");

                try
                {
                    var response = await _router.HandleAsync(request, cancellationToken);
                    await WriteResponseAsync(stream, response, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" Error handling request: {ex.Message}");
                    var errorResponse = HttpResponse.InternalServerError(InternalErrorMessage);
                    await WriteResponseAsync(stream, errorResponse, cancellationToken);
                }
            }
        }
        catch (Exception ex) when (IsNetworkException(ex))
        {
            // Игнорируем сетевые исключения (разрыв соединения и т.д.)
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Error handling client: {ex.Message}");
        }
    }

    private static async Task WriteResponseAsync(NetworkStream stream, HttpResponse response, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = response.ToBytes();
            await stream.WriteAsync(bytes, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        catch (IOException)
        {
            // Клиент разорвал соединение
        }
        catch (OperationCanceledException)
        {
            // Операция была отменена
        }
    }

    private static bool IsNetworkException(Exception ex)
    {
        return ex is IOException || 
               ex is SocketException || 
               ex is ObjectDisposedException ||
               ex is OperationCanceledException;
    }
}