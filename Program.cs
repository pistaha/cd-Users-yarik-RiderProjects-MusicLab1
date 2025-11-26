using System.Net;
using MusicLab1.Controllers;
using MusicLab1.Models;

namespace MusicLab1;

internal class Program
{
    private static async Task Main()
    {
        try
        {
            var state = new ProjectState();
            
            // Синхронное добавление начальных песен
            state.AddSong("Taki Taki", "Selena Gomez");
            state.AddSong("Espresso", "Sabrina Carpenter");
            state.AddSong("Flowers", "Miley Cyrus");
            state.AddSong("Levitating", "Dua Lipa");

            var viewsPath = FindViewsPath();
            var renderer = new ViewRenderer(viewsPath);

            var router = new Router();
            var home = new HomeController(renderer, state);
            var project = new ProjectController(renderer, state);

            // Асинхронная регистрация маршрутов
            await Task.WhenAll(
                router.MapGet("/", home.Index, "Главная страница"),
                router.MapGet("/status", project.Status, "Статус проекта"),
                router.MapPost("/action", project.Action, "Добавить песню"),
                router.MapPost("/delete", project.Delete, "Удалить песню")
            );
       
            var server = new HttpServer(IPAddress.Loopback, 8080, router);
            Console.WriteLine("🎵 Music Server by Y running on http://localhost:8080");
            Console.WriteLine("📊 Available endpoints:");
            
            foreach (var route in router.Routes)
            {
                var description = string.IsNullOrEmpty(route.Description) ? string.Empty : $" - {route.Description}";
                Console.WriteLine($"   {route.Method.PadRight(6)} {route.Path}{description}");
            }
            
            Console.WriteLine("Press Ctrl+C to stop the server");
            
            // Обработка Ctrl+C
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                server.Stop();
            };
            
            await server.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static string FindViewsPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Views"),
            Path.Combine(baseDir, "..", "..", "..", "Views"),
            Path.Combine(baseDir, "..", "..", "..", "MusicLab1", "Views"),
            Path.Combine(Directory.GetCurrentDirectory(), "Views"),
        };

        foreach (var path in candidates.Select(Path.GetFullPath))
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "index.html")))
            {
                Console.WriteLine($"✅ Views found: {path}");
                return path;
            }
        }

        throw new DirectoryNotFoundException("Не удалось найти папку Views с шаблонами.");
    }
}