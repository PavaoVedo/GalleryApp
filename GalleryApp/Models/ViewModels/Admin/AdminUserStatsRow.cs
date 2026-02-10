namespace GalleryApp.Models.ViewModels.Admin;

public class AdminUserStatsRow
{
    public string UserId { get; set; } = default!;
    public string? Email { get; set; }

    public int Uploads { get; set; }
    public int DownloadsOriginal { get; set; }
    public int DownloadsProcessed { get; set; }
    public int Edits { get; set; }
    public int Searches { get; set; }
    public int Deletes { get; set; }

    public DateTime? LastActionUtc { get; set; }
}
