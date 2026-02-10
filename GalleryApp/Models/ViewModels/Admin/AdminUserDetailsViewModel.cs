using GalleryApp.Models;

namespace GalleryApp.Models.ViewModels.Admin;

public class AdminUserDetailsViewModel
{
    public string UserId { get; set; } = default!;
    public string? Email { get; set; }

    public int TotalPhotos { get; set; }
    public DateTime? LastActionUtc { get; set; }

    public List<ActionLog> RecentLogs { get; set; } = new();
    public List<Photo> RecentPhotos { get; set; } = new();
}
