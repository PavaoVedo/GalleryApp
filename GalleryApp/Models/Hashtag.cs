using System.ComponentModel.DataAnnotations;

namespace GalleryApp.Models;

public class Hashtag
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(60)]
    public string Tag { get; set; } = default!; 

    public ICollection<PhotoHashtag> PhotoHashtags { get; set; } = new List<PhotoHashtag>();
}
