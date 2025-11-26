using System.Net;
using MusicLab1.Models;

namespace MusicLab1.Controllers;

public class ProjectController
{
    private const string StatusTemplate = "status.html";
    private const string SongListKey = "song_list";
    private const string SongCountKey = "song_count";
    private const string MessageBlockKey = "message_block";
    
    private readonly IViewRenderer _renderer;
    private readonly ProjectState _state;

    public ProjectController(IViewRenderer renderer, ProjectState state)
    {
        _renderer = renderer;
        _state = state;
    }

    public async Task<HttpResponse> Status(HttpRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await RenderStatusViewAsync(string.Empty, cancellationToken);
    }

    public async Task<HttpResponse> Action(HttpRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (!request.IsMethod(HttpRequest.MethodPost))
        {
            var methodError = "Метод не поддерживается. Используйте POST.";
            return await RenderStatusViewAsync(BuildMessage(methodError, "error"), cancellationToken);
        }
        
        var title = request.GetParam("title");
        var artist = request.GetParam("artist");

        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(artist))
        {
            try
            {
                // Синхронный вызов, не оборачиваем в Task.Run
                _state.AddSong(title, artist);
                var success = $"Трек «{title.Trim()}» добавлен.";
                return await RenderStatusViewAsync(BuildMessage(success, "success"), cancellationToken);
            }
            catch (ArgumentException ex)
            {
                return await RenderStatusViewAsync(BuildMessage(ex.Message, "error"), cancellationToken);
            }
        }
        
        var validationError = "Название и исполнитель должны быть заполнены.";
        return await RenderStatusViewAsync(BuildMessage(validationError, "error"), cancellationToken);
    }

    public async Task<HttpResponse> Delete(HttpRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (!request.IsMethod(HttpRequest.MethodPost))
        {
            var methodError = "Метод не поддерживается. Используйте POST.";
            return await RenderStatusViewAsync(BuildMessage(methodError, "error"), cancellationToken);
        }
        
        var idValue = request.GetParam("id");
        if (Guid.TryParse(idValue, out var songId))
        {
            // Синхронный вызов, не оборачиваем в Task.Run
            var removed = _state.RemoveSong(songId);
            var message = removed
                ? "Трек удалён из списка."
                : "Не удалось найти такой трек.";
            return await RenderStatusViewAsync(BuildMessage(message, removed ? "success" : "error"), cancellationToken);
        }

        var idError = "Некорректный идентификатор трека.";
        return await RenderStatusViewAsync(BuildMessage(idError, "error"), cancellationToken);
    }

    private async Task<HttpResponse> RenderStatusViewAsync(string messageBlock, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // Выполняем синхронные операции напрямую
        var model = new Dictionary<string, string>
        {
            [SongListKey] = BuildSongListMarkup(),
            [SongCountKey] = _state.SongsCount.ToString(),
            [MessageBlockKey] = messageBlock
        };
        
        var html = _renderer.Render(StatusTemplate, model);
        return HttpResponse.Ok(html);
    }

    private string BuildSongListMarkup()
    {
        var songs = _state.Songs;
        if (songs == null || !songs.Any())
        {
            return "<div class='song-item empty'>Пока нет добавленных песен</div>";
        }

        var songHtml = new List<string>();
        
        foreach (var song in songs)
        {
            var title = WebUtility.HtmlEncode(song.Title);
            var artist = WebUtility.HtmlEncode(song.Artist);
            var id = WebUtility.HtmlEncode(song.Id.ToString());
            
            songHtml.Add($"<div class='song-item'><div class='song-meta'>🎵 <strong>{title}</strong> — {artist} <small>{song.AddedAt:dd.MM.yyyy HH:mm}</small></div><form method='post' action='/delete' class='song-actions'><input type='hidden' name='id' value='{id}' /><button type='submit' class='song-delete'>Удалить песню</button></form></div>");
        }
        
        return string.Join(Environment.NewLine, songHtml);
    }

    private static string BuildMessage(string text, string style)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var safeText = WebUtility.HtmlEncode(text);
        var safeStyle = WebUtility.HtmlEncode(style);
        return $"<div class='alert {safeStyle}'>{safeText}</div>";
    }
}