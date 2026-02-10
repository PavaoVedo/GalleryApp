namespace GalleryApp.Models;

public class PhotoHashtag
{
    public Guid PhotoId { get; set; }
    public Photo Photo { get; set; } = default!;

    public Guid HashtagId { get; set; }
    public Hashtag Hashtag { get; set; } = default!;
}
