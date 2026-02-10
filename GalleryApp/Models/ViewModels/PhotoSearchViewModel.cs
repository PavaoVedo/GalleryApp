using System.ComponentModel.DataAnnotations;
using GalleryApp.Models;

namespace GalleryApp.Models.ViewModels;

public class PhotoSearchViewModel
{
    public string? Hashtags { get; set; }          
    public string? AuthorEmail { get; set; }       
    public double? MinSizeMb { get; set; }
    public double? MaxSizeMb { get; set; }

    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    public List<Photo> Results { get; set; } = new();
}
