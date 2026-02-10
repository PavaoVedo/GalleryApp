using System.ComponentModel.DataAnnotations;

namespace GalleryApp.Models.ViewModels;

public class EditPhotoViewModel
{
    public Guid Id { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Hashtags { get; set; }
    public string? PreviewUrl { get; set; }
    public string? AuthorEmail { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}
