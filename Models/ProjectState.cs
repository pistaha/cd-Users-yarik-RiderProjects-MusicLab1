using System.Collections.Concurrent;
namespace MusicLab1.Models;

public class ProjectState
{
    private readonly ConcurrentDictionary<Guid, Song> _songs = new();
    private int _songsCount = 0;

    public IReadOnlyList<Song> Songs
    {
        get
        {
            return _songs.Values.ToList().AsReadOnly();
        }
    }

    public int SongsCount => _songsCount;

    public void AddSong(string title, string artist)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(artist))
            throw new ArgumentException("Title and artist cannot be empty");

        var trimmedTitle = title.Trim();
        var trimmedArtist = artist.Trim();
        var song = new Song(trimmedTitle, trimmedArtist);

        if (_songs.TryAdd(song.Id, song))
        {
            Interlocked.Increment(ref _songsCount);
        }
        else
        {
            throw new InvalidOperationException("Не удалось добавить песню");
        }
    }

    public bool RemoveSong(Guid id)
    {
        if (_songs.TryRemove(id, out _))
        {
            Interlocked.Decrement(ref _songsCount);
            return true;
        }
        
        return false;
    }
}