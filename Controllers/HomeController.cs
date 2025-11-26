using System.Net;
using MusicLab1.Models;

namespace MusicLab1.Controllers;

public class HomeController
{
    private const string IndexTemplate = "index.html";
    private const string SongCountKey = "song_count";
    private const string SongPreviewKey = "song_preview";
    
    private readonly IViewRenderer _renderer;
    private readonly ProjectState _state;

    public HomeController(IViewRenderer renderer, ProjectState state)
    {
        _renderer = renderer;
        _state = state;
    }

    public async Task<HttpResponse> Index(HttpRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var model = new Dictionary<string, string>
        {
            [SongCountKey] = _state.SongsCount.ToString(),
            [SongPreviewKey] = BuildSongPreview()
        };
        
        var html = _renderer.Render(IndexTemplate, model);
        return HttpResponse.Ok(html);
    }

    private string BuildSongPreview()
    {
        var songs = _state.Songs;
        if (songs == null || !songs.Any())
        {
            return "<li class='empty'>Пока нет добавленных песен</li>";
        }

        var previewSongs = songs.Take(4).ToList();
        if (previewSongs.Count == 0)
        {
            return "<li class='empty'>Пока нет добавленных песен</li>";
        }

        var previewHtml = new List<string>();
        
        for (int i = 0; i < previewSongs.Count; i++)
        {
            var song = previewSongs[i];
            var encodedTitle = WebUtility.HtmlEncode(song.Title);
            var encodedArtist = WebUtility.HtmlEncode(song.Artist);
            
            previewHtml.Add($"<li class='song-chip'><span class='song-index'>{i + 1}</span><div><strong>{encodedTitle}</strong><p>{encodedArtist}</p></div></li>");
        }
        
        return string.Join(Environment.NewLine, previewHtml);
    }
}