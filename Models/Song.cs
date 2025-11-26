namespace MusicLab1.Models;

public class Song
{
    public Guid Id { get; }
    public string Title { get; }
    public string Artist { get; }
    public DateTime AddedAt { get; }
    
    public Song(string title, string artist)
    {
        Id = Guid.NewGuid();
        Title = title;
        Artist = artist;
        AddedAt = DateTime.Now;
    }

    public override string ToString()
    {
        return $"{Title} - {Artist}";
    }

    public string ToJson()
    {
        return $$"""
                 {
                     "id": "{{Id}}",
                     "title": "{{Title.Replace("\"", "\\\"")}}",
                     "artist": "{{Artist.Replace("\"", "\\\"")}}",
                     "addedAt": "{{AddedAt:o}}"
                 }
                 """;
    }
}