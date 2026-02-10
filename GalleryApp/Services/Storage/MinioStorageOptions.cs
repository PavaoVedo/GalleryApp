namespace GalleryApp.Services.Storage;

public class MinioStorageOptions
{
    public string Endpoint { get; set; } = default!;
    public string AccessKey { get; set; } = default!;
    public string SecretKey { get; set; } = default!;
    public string Bucket { get; set; } = "galleryapp";
    public bool UseSSL { get; set; } = false;
}
