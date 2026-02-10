using System.ComponentModel.DataAnnotations;

namespace GalleryApp.Models;

public class ActionLog
{
    [Key]
    public long Id { get; set; }

    public string? UserId { get; set; }         

    [MaxLength(256)]
    public string? UserEmail { get; set; }      

    [Required]
    [MaxLength(80)]
    public string Action { get; set; } = default!;   

    [MaxLength(80)]
    public string? EntityType { get; set; }     

    [MaxLength(80)]
    public string? EntityId { get; set; }        

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(2048)]
    public string? Details { get; set; }         
}
