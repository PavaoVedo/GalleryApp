using System.ComponentModel.DataAnnotations;

namespace GalleryApp.Models.ViewModels;

public class DownloadProcessedViewModel
{
    public Guid PhotoId { get; set; }

    [Range(50, 4000)]
    public int? ResizeWidth { get; set; }

    [Range(50, 4000)]
    public int? ResizeHeight { get; set; }

    public bool Sepia { get; set; }

    [Range(0, 10)]
    public int Blur { get; set; } = 0;

    [Required]
    public string Format { get; set; } = "jpg";
}
